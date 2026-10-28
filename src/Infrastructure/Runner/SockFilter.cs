using System.Runtime.InteropServices;

namespace AoCEditor.Infrastructure.Runner;

internal readonly record struct SockFilter(ushort Code, byte Jt, byte Jf, uint K);
