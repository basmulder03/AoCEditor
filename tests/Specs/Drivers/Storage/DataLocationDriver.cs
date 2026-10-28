using Storage.Domain;

namespace Specs.Drivers.Storage;

public sealed class DataLocationDriver
{
    private string _solutions = "";

    public bool Allowed { get; private set; }

    public void SetSolutionsFolder(string folder) => _solutions = Path.GetFullPath(folder, Path.GetTempPath());

    public void ChooseDataLocation(string folder) =>
        Allowed = DataRootValidator.IsAllowed(Path.GetFullPath(folder, Path.GetTempPath()), _solutions);
}
