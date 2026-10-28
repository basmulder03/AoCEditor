using AoCEditor.Core.Session.Application;


namespace AoCEditor.Infrastructure.Session;

public static class SecretStores
{
    public static ISecretStore CreateForCurrentOs() =>
        OperatingSystem.IsLinux() ? new LibSecretStore()
        : OperatingSystem.IsWindows() ? new WindowsCredentialStore()
        : new UnavailableSecretStore("No supported secret store on this operating system.");

    private sealed class UnavailableSecretStore(string reason) : ISecretStore
    {
        private readonly Task<SecretResult> _result = Task.FromResult<SecretResult>(new SecretResult.Unavailable(reason));

        public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) => _result;

        public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken) => _result;

        public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) => _result;
    }
}
