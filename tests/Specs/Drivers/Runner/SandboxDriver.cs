using System.Net;
using System.Net.Sockets;
using Runner.Application;

namespace Specs.Drivers.Runner;

public sealed class SandboxDriver : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-spec-sbx-" + Guid.NewGuid().ToString("N"));
    private readonly List<OutputLine> _lines = [];
    private TcpListener? _listener;

    public SandboxDriver()
    {
        Directory.CreateDirectory(Solution);
        Directory.CreateDirectory(Private);
    }

    public static bool Available => OperatingSystem.IsLinux() && LinuxSandbox.Detect() == SandboxLevel.Full;

    public string Solution => Path.Combine(_root, "solution");

    public string Private => Path.Combine(_root, "private");

    public string Output => string.Join("\n", _lines.Select(l => l.Text));

    public TimeSpan Elapsed { get; private set; }

    public bool ServerSawConnection => _listener?.Pending() ?? false;

    public void CreatePrivateFile() => File.WriteAllText(Path.Combine(Private, "secret.txt"), "private-contents");

    public void StartServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
    }

    public Task RunShellAsync(string script) => RunAsync(["sh", "-c", script], 20);

    public Task RunConnectAsync()
    {
        var port = ((IPEndPoint)_listener!.LocalEndpoint).Port;
        return RunAsync(["python3", "-c", $"import socket\ntry: socket.create_connection(('127.0.0.1',{port}),timeout=3)\nexcept OSError: pass"], 20);
    }

    public Task RunBusyLoopAsync() => RunAsync(["sh", "-c", "while :; do :; done"], 20, cpuSeconds: 1);

    public int FilesInSolutionFolder => Directory.GetFiles(Solution).Length;

    private async Task RunAsync(string[] command, int timeoutSeconds, int cpuSeconds = 20)
    {
        var request = new RunRequest(command, Solution, null, TimeSpan.FromSeconds(timeoutSeconds), Limits: new SandboxLimits(1L << 30, cpuSeconds, 200, 1 << 20));
        var progress = new Progress(_lines);
        var result = await new ProcessRunner().RunAsync(request, progress, CancellationToken.None);
        Elapsed = result.Elapsed;
    }

    public void Dispose()
    {
        _listener?.Stop();
        Directory.Delete(_root, recursive: true);
    }

    private sealed class Progress(List<OutputLine> sink) : IProgress<OutputLine>
    {
        public void Report(OutputLine value)
        {
            lock (sink)
            {
                sink.Add(value);
            }
        }
    }
}
