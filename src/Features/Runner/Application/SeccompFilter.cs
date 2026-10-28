using System.Runtime.InteropServices;

namespace Runner.Application;

internal readonly record struct SockFilter(ushort Code, byte Jt, byte Jf, uint K);

/// <summary>
/// Builds the classic-BPF program of the sandbox. Every listed system call fails with EPERM, sockets are refused
/// (or limited to AF_INET and AF_INET6 for build steps), utimensat() only works on descriptors (a null path), kill() is limited to non-negative pids and the own process
/// group, and clone() with namespace flags fails. clone3() reports ENOSYS because its flags sit in memory the filter
/// cannot read; libc then falls back to clone().
/// </summary>
internal static class SeccompFilter
{
    private const ushort Ld = 0x20, Jeq = 0x15, Jge = 0x35, Jset = 0x45, Ret = 0x06;
    private const uint ArchOffset = 4, NrOffset = 0, Arg0Offset = 16, Arg1LowOffset = 24, Arg1HighOffset = 28, Arg1Offset = 24, Arg2Offset = 32;
    private const uint AllowAction = 0x7fff0000, ErrnoAction = 0x00050000, Eperm = 1, Enosys = 38, X32Bit = 0x40000000;
    private const uint AfInet = 2, AfInet6 = 10, NegativePid = 0x80000000;
    private const uint NamespaceFlags = 0x7E020000;
    private const uint LooseModeBits = 0xE3F;

    public static (uint Audit, int Index)? ArchOf(Architecture architecture) => architecture switch
    {
        Architecture.X64 => (0xC000003E, 0),
        Architecture.Arm64 => (0xC00000B7, 1),
        _ => null,
    };

    // Only the path-based calls are refused (chmod and utimensat below are partly allowed): toolchains (the .NET runtime for its named mutexes and file copies, for one) use
    // fchmod, fchown, futimens and the f*xattr calls on descriptors they opened themselves, which Landlock already limits to granted folders.
    // Number per architecture (x64, arm64); -1 where the architecture has no such call.
    private static readonly (string Name, int X64, int Arm64)[] Denied =
    [
        ("ptrace", 101, 117), ("process_vm_readv", 310, 270), ("process_vm_writev", 311, 271), ("pidfd_open", 434, 434), ("pidfd_getfd", 438, 438),
        ("keyctl", 250, 219), ("add_key", 248, 217), ("request_key", 249, 218), ("bpf", 321, 280), ("perf_event_open", 298, 241), ("userfaultfd", 323, 282),
        ("io_uring_setup", 425, 425), ("io_uring_enter", 426, 426), ("io_uring_register", 427, 427),
        ("rt_sigqueueinfo", 129, 138), ("rt_tgsigqueueinfo", 297, 240),
                ("chown", 92, -1), ("lchown", 94, -1), ("fchownat", 260, 54),
        ("utime", 132, -1), ("utimes", 235, -1), ("futimesat", 261, -1),
        ("setxattr", 188, 5), ("lsetxattr", 189, 6), ("removexattr", 197, 14), ("lremovexattr", 198, 15),
        ("setxattrat", 463, 463), ("removexattrat", 466, 466),
        ("setsid", 112, 157), ("setpgid", 109, 154), ("unshare", 272, 97), ("setns", 308, 268),
    ];

    public static SockFilter[] Build(Architecture architecture, bool allowInternet, int ownGroup)
    {
        var (audit, index) = ArchOf(architecture) ?? throw new PlatformNotSupportedException("No system call filter for this architecture.");
        int Pick(int x64, int arm64) => index == 0 ? x64 : arm64;
        var socket = Pick(41, 198);
        var kill = Pick(62, 129);
        var clone = Pick(56, 220);
        var code = new List<(ushort Code, string? Jt, string? Jf, uint K)>();
        var labels = new Dictionary<string, int>();
        void Emit(ushort c, uint k, string? jt = null, string? jf = null) => code.Add((c, jt, jf, k));
        void Mark(string label) => labels[label] = code.Count;

        Emit(Ld, ArchOffset);
        Emit(Jeq, audit, jf: "eperm");
        Emit(Ld, NrOffset);
        Emit(Jge, X32Bit, jt: "eperm");
        foreach (var (_, x64, arm64) in Denied)
        {
            if (Pick(x64, arm64) is var nr and >= 0)
            {
                Emit(Jeq, (uint)nr, jt: "eperm");
            }
        }

        Emit(Jeq, 435, jt: "enosys");

        if (!allowInternet)
        {
            Emit(Jeq, (uint)socket, jt: "eperm");
        }
        else
        {
            Emit(Jeq, (uint)socket, jf: "afterSocket");
            Emit(Ld, Arg0Offset);
            Emit(Jeq, AfInet, jt: "allow");
            Emit(Jeq, AfInet6, jt: "allow");
            Emit(Ret, ErrnoAction | Eperm);
            Mark("afterSocket");
            Emit(Ld, NrOffset);
        }

        Emit(Jeq, (uint)kill, jf: "afterKill");
        Emit(Ld, Arg0Offset);
        Emit(Jset, NegativePid, jf: "allow");
        Emit(Jeq, (uint)-ownGroup, jt: "allow", jf: "eperm");
        Mark("afterKill");
        Emit(Ld, NrOffset);

        // chmod only tightens: NuGet and the .NET runtime make their own folders owner-only, but nothing may grant group, other or special bits.
        var n = 0;
        foreach (var (nr, argOffset) in new[] { (Pick(90, -1), Arg1Offset), (Pick(268, 53), Arg2Offset), (452, Arg2Offset) })
        {
            if (nr >= 0)
            {
                Emit(Jeq, (uint)nr, jf: "afterChmod" + n);
                Emit(Ld, argOffset);
                Emit(Jset, LooseModeBits, jt: "eperm", jf: "allow");
                Mark("afterChmod" + n++);
                Emit(Ld, NrOffset);
            }
        }

        Emit(Jeq, (uint)Pick(280, 88), jf: "afterUtimensat");
        Emit(Ld, Arg1LowOffset);
        Emit(Jeq, 0, jf: "eperm");
        Emit(Ld, Arg1HighOffset);
        Emit(Jeq, 0, jt: "allow", jf: "eperm");
        Mark("afterUtimensat");
        Emit(Ld, NrOffset);

        Emit(Jeq, (uint)clone, jf: "allow");
        Emit(Ld, Arg0Offset);
        Emit(Jset, NamespaceFlags, jt: "eperm");
        Mark("allow");
        Emit(Ret, AllowAction);
        Mark("eperm");
        Emit(Ret, ErrnoAction | Eperm);
        Mark("enosys");
        Emit(Ret, ErrnoAction | Enosys);

        var program = new SockFilter[code.Count];
        for (var i = 0; i < code.Count; i++)
        {
            var (c, jt, jf, k) = code[i];
            program[i] = new SockFilter(c, Offset(jt, i), Offset(jf, i), k);
        }

        return program;

        byte Offset(string? label, int at) => label is null ? (byte)0 : checked((byte)(labels[label] - at - 1));
    }
}
