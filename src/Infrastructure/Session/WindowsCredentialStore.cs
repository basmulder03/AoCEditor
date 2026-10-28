using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using AoCEditor.Core.Session.Application;

namespace AoCEditor.Infrastructure.Session;

[SupportedOSPlatform("windows")]
internal sealed partial class WindowsCredentialStore : ISecretStore
{
    private const uint CredTypeGeneric = 1;
    private const uint PersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) =>
        Task.Run<SecretResult>(() =>
        {
            if (!CredReadW(Target(name), CredTypeGeneric, 0, out var cred))
            {
                return Failure();
            }

            try
            {
                var c = Marshal.PtrToStructure<Credential>(cred);
                var bytes = new byte[c.CredentialBlobSize];
                Marshal.Copy(c.CredentialBlob, bytes, 0, bytes.Length);
                return new SecretResult.Ok(Encoding.UTF8.GetString(bytes));
            }
            finally
            {
                CredFree(cred);
            }
        }, cancellationToken);

    public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken) =>
        Task.Run<SecretResult>(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var blob = Marshal.AllocHGlobal(bytes.Length);
            var target = Marshal.StringToCoTaskMemUni(Target(name));
            try
            {
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                var cred = new Credential
                {
                    Type = CredTypeGeneric,
                    TargetName = target,
                    CredentialBlobSize = (uint)bytes.Length,
                    CredentialBlob = blob,
                    Persist = PersistLocalMachine,
                };
                return CredWriteW(ref cred, 0) ? new SecretResult.Ok() : Failure();
            }
            finally
            {
                Array.Clear(bytes);
                Marshal.FreeHGlobal(blob);
                Marshal.FreeCoTaskMem(target);
            }
        }, cancellationToken);

    public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) =>
        Task.Run<SecretResult>(
            () => CredDeleteW(Target(name), CredTypeGeneric, 0) ? new SecretResult.Ok() : Failure(),
            cancellationToken);

    private static string Target(string name) => "AoCEditor/" + name;

    private static SecretResult Failure() =>
        Marshal.GetLastPInvokeError() == ErrorNotFound
            ? new SecretResult.NotFound()
            : new SecretResult.Unavailable("Credential Manager call failed.");

    [StructLayout(LayoutKind.Sequential)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredWriteW(ref Credential credential, uint flags);

    [LibraryImport("advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredDeleteW(string target, uint type, uint flags);

    [LibraryImport("advapi32.dll")]
    private static partial void CredFree(IntPtr buffer);
}
