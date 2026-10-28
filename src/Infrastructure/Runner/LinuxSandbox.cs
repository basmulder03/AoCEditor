using System.Runtime.InteropServices;
using AoCEditor.Core.Runner.Application;

namespace AoCEditor.Infrastructure.Runner;

/// <summary>
/// Linux confinement through direct syscalls: Landlock for the filesystem (and signal and abstract-socket scoping where the
/// kernel has it), a seccomp filter for sockets and dangerous system calls, and rlimits. Everything is applied by the helper process to itself right before it executes the program.
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
    private const long SysCloseRange = 436;
    private const int CloseRangeCloexec = 4;
    private const int MinFilesystemAbi = 3;
    private const int ScopeAbi = 6;
    private const ulong ScopeAbstractUnixSocket = 1UL << 0;
    private const ulong ScopeSignal = 1UL << 1;

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

        return LevelFor(LandlockAbi(), SeccompArch() is not null && prctl(PrGetSeccomp, 0, 0, 0, 0) >= 0);
    }

    internal static int LandlockAbi() => LandlockVersion();

    // Truncation control (ABI 3) is needed so a read-only grant cannot be used to empty files.
    internal static SandboxLevel LevelFor(int landlockAbi, bool seccomp) =>
        SandboxLevel.Limits | (landlockAbi >= MinFilesystemAbi ? SandboxLevel.Filesystem : 0) | (seccomp ? SandboxLevel.Network : 0);

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

        if (plan.Network && !InstallFilter(plan.AllowInternet))
        {
            return "could not install the system call filter";
        }

        // Marked close-on-exec rather than closed now: the runtime still needs its own descriptors (JIT mappings) until the program replaces it.
        return Syscall(SysCloseRange, 3, -1, CloseRangeCloexec) == 0 ? null : "could not close inherited file descriptors";
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
        if (abi < MinFilesystemAbi)
        {
            return "Landlock ABI 3 or newer is not available";
        }

        var handled = AllV1 | (abi >= 2 ? Refer : 0) | (abi >= 3 ? Truncate : 0);
        var attr = Marshal.AllocHGlobal(24);
        try
        {
            // handled filesystem rights, handled network rights (none), scopes; the kernel only accepts the longer form from ABI 6.
            Marshal.WriteInt64(attr, 0, (long)handled);
            Marshal.WriteInt64(attr, 8, 0);
            Marshal.WriteInt64(attr, 16, abi >= ScopeAbi ? (long)(ScopeAbstractUnixSocket | ScopeSignal) : 0);
            var ruleset = (int)Syscall(SysLandlockCreateRuleset, attr, abi >= ScopeAbi ? 24 : 8, 0);
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

    private static Architecture? SeccompArch() => SeccompFilter.ArchOf(RuntimeInformation.ProcessArchitecture) is null ? null : RuntimeInformation.ProcessArchitecture;

    private static bool InstallFilter(bool allowInternet)
    {
        if (SeccompArch() is not { } architecture)
        {
            return false;
        }

        // The helper made itself a session leader, so its process group id is its pid; the program keeps that pid.
        var program = SeccompFilter.Build(architecture, allowInternet, getpid());
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

    [LibraryImport("libc")]
    private static partial int getpid();

    [LibraryImport("libc", SetLastError = true)]
    private static partial int kill(int pid, int signal);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int execvp(string file, nint argv);
}
