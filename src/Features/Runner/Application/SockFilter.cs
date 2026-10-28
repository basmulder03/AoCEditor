using System.Runtime.InteropServices;

namespace Runner.Application;

internal readonly record struct SockFilter(ushort Code, byte Jt, byte Jf, uint K);
