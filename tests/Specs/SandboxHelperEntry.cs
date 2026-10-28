using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Runner.Application;

namespace Specs;

// The sandbox re-executes this test host as its helper, so the helper arguments must be handled before xunit starts.
internal static class SandboxHelperEntry
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Test host entry point.")]
    internal static void Run()
    {
        if (SandboxHelper.RunIfRequested(Environment.GetCommandLineArgs()[1..]) is { } code)
        {
            Environment.Exit(code);
        }
    }
}
