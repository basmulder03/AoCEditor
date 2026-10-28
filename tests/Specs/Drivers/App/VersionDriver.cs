using Shared;

namespace Specs.Drivers.App;

public sealed class VersionDriver
{
    public string Version { get; private set; } = "";

    public void AskForVersion() => Version = AppInfo.Version;
}
