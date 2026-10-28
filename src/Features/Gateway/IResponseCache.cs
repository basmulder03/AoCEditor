namespace Gateway;

public interface IResponseCache
{
    ValueTask<string?> TryGetAsync(string path, CancellationToken cancellationToken);

    ValueTask SetAsync(string path, string content, CancellationToken cancellationToken);
}
