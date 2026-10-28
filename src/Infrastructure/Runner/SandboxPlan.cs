using System.Globalization;

namespace Runner.Application;

/// <summary>
/// What the helper process applies to itself before it executes the program. Filesystem and Network switch the
/// matching protections on; Command is run as is, without a shell.
/// </summary>
public sealed record SandboxPlan(
    IReadOnlyList<string> Readable,
    IReadOnlyList<string> Writable,
    SandboxLimits Limits,
    bool Filesystem,
    bool Network,
    bool AllowInternet,
    IReadOnlyList<string> Command)
{
    public const string Flag = "--aoc-sandbox-exec";

    public IReadOnlyList<string> ToArguments()
    {
        var args = new List<string> { Flag };
        foreach (var path in Readable)
        {
            args.AddRange(["--ro", path]);
        }

        foreach (var path in Writable)
        {
            args.AddRange(["--rw", path]);
        }

        args.AddRange(["--mem", Limits.MemoryBytes.ToString(CultureInfo.InvariantCulture)]);
        args.AddRange(["--cpu", Limits.CpuSeconds.ToString(CultureInfo.InvariantCulture)]);
        args.AddRange(["--tasks", Limits.ExtraTasks.ToString(CultureInfo.InvariantCulture)]);
        args.AddRange(["--fsize", Limits.FileBytes.ToString(CultureInfo.InvariantCulture)]);
        if (Filesystem)
        {
            args.Add("--fs");
        }

        if (Network)
        {
            args.Add("--net");
        }

        if (AllowInternet)
        {
            args.Add("--inet");
        }

        args.Add("--");
        args.AddRange(Command);
        return args;
    }

    /// <summary>Null when the arguments are not meant for the helper; a malformed plan throws so the program never runs with fewer restrictions than asked for.</summary>
    public static SandboxPlan? TryParse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || args[0] != Flag)
        {
            return null;
        }

        List<string> readable = [], writable = [];
        long mem = 0, fsize = 0;
        int cpu = 0, tasks = 0;
        bool filesystem = false, network = false, inet = false;
        var i = 1;
        for (; i < args.Count && args[i] != "--"; i++)
        {
            switch (args[i])
            {
                case "--ro": readable.Add(Value(args, ref i)); break;
                case "--rw": writable.Add(Value(args, ref i)); break;
                case "--mem": mem = long.Parse(Value(args, ref i), CultureInfo.InvariantCulture); break;
                case "--cpu": cpu = int.Parse(Value(args, ref i), CultureInfo.InvariantCulture); break;
                case "--tasks": tasks = int.Parse(Value(args, ref i), CultureInfo.InvariantCulture); break;
                case "--fsize": fsize = long.Parse(Value(args, ref i), CultureInfo.InvariantCulture); break;
                case "--fs": filesystem = true; break;
                case "--net": network = true; break;
                case "--inet": inet = true; break;
                default: throw new FormatException($"Unknown sandbox option '{args[i]}'.");
            }
        }

        if (i >= args.Count - 1)
        {
            throw new FormatException("The sandbox plan has no command.");
        }

        return new SandboxPlan(readable, writable, new SandboxLimits(mem, cpu, tasks, fsize), filesystem, network, inet, [.. args.Skip(i + 1)]);
    }

    private static string Value(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new FormatException("A sandbox option is missing its value.");
}
