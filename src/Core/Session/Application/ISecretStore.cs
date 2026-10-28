namespace AoCEditor.Core.Session.Application;

public interface ISecretStore
{
    /// <summary>Ok carries the value; NotFound when the name is unknown.</summary>
    Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken);

    Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken);

    /// <summary>Deleting a name that does not exist is NotFound.</summary>
    Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken);
}
