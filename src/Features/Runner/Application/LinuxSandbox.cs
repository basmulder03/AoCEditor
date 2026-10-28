using System.Runtime.InteropServices;

namespace Runner.Application;

/// <summary>
/// Linux confinement through direct syscalls: Landlock for the filesystem, a seccomp filter that refuses socket creation
/// for the network, and rlimits. Everything is applied by the helper process to itself right before it executes the program.
/// </summary>
internal static partial class LinuxSandbox
{
    private const int PrSetNoNewPrivs = 38;
    private const int PrGetSeccomp = 21;
    private const int PrSetSeccomp = 22;
    private const int SeccompModeFilter = 2;
    private const long SysLandlockCreateRuleset = 444;
    private const long SysLandlockAddRule = 445;
    private const long SysLandlockRestrictSelf = 446;
    private const int OPath = 0x200000;
    private const int OCloExec = 0x80000;
    private const int Sigkill = 9;

    private const ulong Execute = 1UL << 0;
    private const ulong WriteFile = 1UL << 1;
    private const ulong ReadFile = 1UL << 2;
    private const ulong ReadDir = 1UL << 3;
    private const ulong AllV1 = (1UL << 13) - 1;
    private const ulong Refer = 1UL << 13;
    private const ulong Truncate = 1UL << 14;

    private static readonly string[] ProcessInfoPaths =
    [
        "/proc/self", "/proc/cpuinfo", "/proc/meminfo", "/proc/stat", "/proc/sys", "/proc/version", "/proc/filesystems", "/proc/loadavg", "/proc/uptime",
    ];

    /// <summary>What this computer can enforce; None when it is not Linux.</summary>
    public static SandboxLevel Detect()
    {
        if (!OperatingSystem.IsLinux())
        {
            return SandboxLevel.None;
        }

        var level = SandboxLevel.Limits;
        if (LandlockVersion() >= 1)
        {
            level |= SandboxLevel.Filesystem;
        }

        if (SeccompArch() is not null && prctl(PrGetSeccomp, 0, 0, 0, 0) >= 0)
        {
            level |= SandboxLevel.Network;
        }

        return level;
    }

    /// <summary>Kills every process the sandboxed program started (the helper makes itself a session leader).</summary>
    public static void KillGroup(int pid)
    {
        if (pid > 1)
        {
            _ = kill(-pid, Sigkill);
        }
    }

    /// <summary>Applies the plan to the calling thread and process; on failure returns the reason and the program must not run.</summary>
    public static string? Apply(SandboxPlan plan)
    {
        _ = setsid();
        if (!SetLimits(plan.Limits))
        {
            return "could not set the resource limits";
        }

        if (prctl(PrSetNoNewPrivs, 1, 0, 0, 0) != 0)
        {
            return "could not drop privileges";
        }

        if (plan.Filesystem && ApplyLandlock(plan) is { } landlockError)
        {
            return landlockError;
        }

        return plan.Network && !DenySockets() ? "could not block network access" : null;
    }

    private static int LandlockVersion() => (int)Syscall(SysLandlockCreateRuleset, 0, 0, 1);

    private static bool SetLimits(SandboxLimits limits)
    {
        var tasks = CountUserTasks() + limits.ExtraTasks;
        return SetLimit(0, (ulong)limits.CpuSeconds, (ulong)limits.CpuSeconds + 5)
            && SetLimit(1, (ulong)limits.FileBytes, (ulong)limits.FileBytes)
            && SetLimit(2, (ulong)limits.MemoryBytes, (ulong)limits.MemoryBytes)
            && SetLimit(4, 0, 0)
            && SetLimit(6, (ulong)tasks, (ulong)tasks);
    }

    private static bool SetLimit(int resource, ulong soft, ulong hard)
    {
        var limit = new RLimit(soft, hard);
        return setrlimit(resource, in limit) == 0;
    }

    // RLIMIT_NPROC counts all tasks of the user, so the cap has to sit above what the user already runs.
    private static long CountUserTasks()
    {
        var uid = StatusField("/proc/self/status", "Uid:");
        long count = 0;
        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            if (char.IsAsciiDigit(Path.GetFileName(dir)[0])
                && StatusField(dir + "/status", "Uid:") == uid
                && long.TryParse(StatusField(dir + "/status", "Threads:"), out var threads))
            {
                count += threads;
            }
        }

        return count;
    }

    private static string? StatusField(string file, string name)
    {
        try
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.StartsWith(name, StringComparison.Ordinal))
                {
                    return line[name.Length..].Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                }
            }
        }
        catch (IOException)
        {
            // The process ended while it was being counted.
        }
        catch (UnauthorizedAccessException)
        {
            // Not readable; it cannot be ours.
        }

        return null;
    }

    private static string? ApplyLandlock(SandboxPlan plan)
    {
        var abi = LandlockVersion();
        if (abi < 1)
        {
            return "Landlock is not available";
        }

        var handled = AllV1 | (abi >= 2 ? Refer : 0) | (abi >= 3 ? Truncate : 0);
        var attr = Marshal.AllocHGlobal(8);
        try
        {
            Marshal.WriteInt64(attr, (long)handled);
            var ruleset = (int)Syscall(SysLandlockCreateRuleset, attr, 8, 0);
            if (ruleset < 0)
            {
                return "could not create the Landlock ruleset";
            }

            try
            {
                foreach (var path in plan.Readable.Concat(ProcessInfoPaths))
                {
                    if (!Allow(ruleset, path, handled, directoryAccess: Execute | ReadFile | ReadDir, fileAccess: Execute | ReadFile))
                    {
                        return $"could not allow reading '{path}'";
                    }
                }

                foreach (var path in plan.Writable)
                {
                    if (!Allow(ruleset, path, handled, directoryAccess: handled, fileAccess: Execute | ReadFile | WriteFile | Truncate))
                    {
                        return $"could not allow writing '{path}'";
                    }
                }

                foreach (var device in new[] { "/dev/null", "/dev/zero", "/dev/random", "/dev/urandom", "/dev/tty" })
                {
                    if (!Allow(ruleset, device, handled, directoryAccess: 0, fileAccess: ReadFile | WriteFile))
                    {
                        return $"could not allow '{device}'";
                    }
                }

                return Syscall(SysLandlockRestrictSelf, ruleset, 0, 0) == 0 ? null : "could not enforce the Landlock ruleset";
            }
            finally
            {
                _ = close(ruleset);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(attr);
        }
    }

    // Paths that do not exist are skipped: there is nothing to allow.
    private static bool Allow(int ruleset, string path, ulong handled, ulong directoryAccess, ulong fileAccess)
    {
        var fd = open(path, OPath | OCloExec);
        if (fd < 0)
        {
            return true;
        }

        try
        {
            var rule = Marshal.AllocHGlobal(16);
            try
            {
                Marshal.WriteInt64(rule, 0, (long)((Directory.Exists(path) ? directoryAccess : fileAccess) & handled));
                Marshal.WriteInt32(rule, 8, fd);
                return Syscall(SysLandlockAddRule, ruleset, 1, rule) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(rule);
            }
        }
        finally
        {
            _ = close(fd);
        }
    }

    private static (uint Arch, uint Socket)? SeccompArch() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => (0xC000003E, 41),
        Architecture.Arm64 => (0xC00000B7, 198),
        _ => null,
    };

    // Refuses socket() and io_uring_setup() (io_uring can open sockets), and every syscall of a foreign ABI.
    // Landlock's TCP rules would still leave UDP and abstract unix sockets open, which is why this filter is used.
    private static bool DenySockets()
    {
        if (SeccompArch() is not { } target)
        {
            return false;
        }

        const ushort Ld = 0x20, Jeq = 0x15, Jge = 0x35, Ret = 0x06;
        const uint Allow = 0x7fff0000, Errno = 0x00050000 | 13, IoUringSetup = 425, X32Bit = 0x40000000;
        SockFilter[] program =
        [
            new(Ld, 0, 0, 4),
            new(Jeq, 0, 5, target.Arch),
            new(Ld, 0, 0, 0),
            new(Jge, 3, 0, X32Bit),
            new(Jeq, 2, 0, target.Socket),
            new(Jeq, 1, 0, IoUringSetup),
            new(Ret, 0, 0, Allow),
            new(Ret, 0, 0, Errno),
        ];

        var filters = Marshal.AllocHGlobal(program.Length * 8);
        var prog = Marshal.AllocHGlobal(16);
        try
        {
            for (var i = 0; i < program.Length; i++)
            {
                var f = program[i];
                Marshal.WriteInt16(filters, i * 8, (short)f.Code);
                Marshal.WriteByte(filters, (i * 8) + 2, f.Jt);
                Marshal.WriteByte(filters, (i * 8) + 3, f.Jf);
                Marshal.WriteInt32(filters, (i * 8) + 4, (int)f.K);
            }

            Marshal.WriteInt64(prog, 0, 0);
            Marshal.WriteInt16(prog, 0, (short)program.Length);
            Marshal.WriteIntPtr(prog, 8, filters);
            return prctl(PrSetSeccomp, SeccompModeFilter, (ulong)prog, 0, 0) == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(prog);
            Marshal.FreeHGlobal(filters);
        }
    }

    /// <summary>Replaces this process with the program; returns only when that failed.</summary>
    public static int Exec(IReadOnlyList<string> command)
    {
        var argv = Marshal.AllocHGlobal((command.Count + 1) * nint.Size);
        for (var i = 0; i < command.Count; i++)
        {
            Marshal.WriteIntPtr(argv, i * nint.Size, Marshal.StringToCoTaskMemUTF8(command[i]));
        }

        Marshal.WriteIntPtr(argv, command.Count * nint.Size, 0);
        _ = execvp(command[0], argv);
        return Marshal.GetLastPInvokeError();
    }

    private readonly record struct SockFilter(ushort Code, byte Jt, byte Jf, uint K);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct RLimit(ulong Soft, ulong Hard);

    [LibraryImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static partial long SyscallNative(long number, nint a, nint b, nint c, nint d);

    private static long Syscall(long number, nint a, nint b, nint c, nint d = 0) => SyscallNative(number, a, b, c, d);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int setrlimit(int resource, in RLimit limit);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int open(string path, int flags);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int setsid();

    [LibraryImport("libc", SetLastError = true)]
    private static partial int kill(int pid, int signal);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int execvp(string file, nint argv);
}
