using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Shared.Tests.Architecture;

internal static class AssemblyReferences
{
    public static List<string> Of(string assemblyFile)
    {
        using var stream = File.OpenRead(assemblyFile);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        return [.. reader.AssemblyReferences.Select(h => reader.GetString(reader.GetAssemblyReference(h).Name))];
    }

    public static List<string> OfLayer(string layer) => Of(Path.Combine(AppContext.BaseDirectory, $"AoCEditor.{layer}.dll"));
}
