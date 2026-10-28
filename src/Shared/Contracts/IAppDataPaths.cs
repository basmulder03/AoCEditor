namespace Shared.Contracts;

/// <summary>Locations under the per-user app-data folder; never inside the solutions folder.</summary>
public interface IAppDataPaths
{
    string Root { get; }

    string Cache { get; }

    string Database { get; }
}
