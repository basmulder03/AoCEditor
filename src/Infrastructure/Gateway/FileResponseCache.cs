using System.Security.Cryptography;
using System.Text;
using AoCEditor.Core.Gateway;

namespace AoCEditor.Infrastructure.Gateway;

public sealed class FileResponseCache(string directory) : IResponseCache
{
    public async ValueTask<string?> TryGetAsync(string path, CancellationToken cancellationToken)
    {
        var file = FileFor(path);
        return File.Exists(file) ? await File.ReadAllTextAsync(file, cancellationToken) : null;
    }

    public async ValueTask SetAsync(string path, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var file = FileFor(path);
        var temp = $"{file}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, content, cancellationToken);
            File.Move(temp, file, overwrite: true);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    private string FileFor(string path) =>
        Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path))) + ".cache");
}
