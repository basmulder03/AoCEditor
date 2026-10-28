using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AoCEditor.Core.Session.Application;

namespace AoCEditor.Infrastructure.Session;

[SupportedOSPlatform("linux")]
internal sealed partial class LibSecretStore : ISecretStore
{
    private const string Unavailable = "Secret Service is not available.";

    public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) =>
        RunAsync(() => Native(name, (schema, attrs) =>
        {
            var password = secret_password_lookupv_sync(schema, attrs, IntPtr.Zero, out var error);
            if (error != IntPtr.Zero)
            {
                g_error_free(error);
                return new SecretResult.Unavailable(Unavailable);
            }

            if (password == IntPtr.Zero)
            {
                return new SecretResult.NotFound();
            }

            try
            {
                return new SecretResult.Ok(Marshal.PtrToStringUTF8(password));
            }
            finally
            {
                secret_password_free(password);
            }
        }), cancellationToken);

    public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken) =>
        RunAsync(() => Native(name, (schema, attrs) =>
        {
            var stored = secret_password_storev_sync(schema, attrs, "default", "AoCEditor " + name, value, IntPtr.Zero, out var error);
            if (error != IntPtr.Zero)
            {
                g_error_free(error);
            }

            return stored ? new SecretResult.Ok() : new SecretResult.Unavailable(Unavailable);
        }), cancellationToken);

    public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) =>
        RunAsync(() => Native(name, (schema, attrs) =>
        {
            var removed = secret_password_clearv_sync(schema, attrs, IntPtr.Zero, out var error);
            if (error != IntPtr.Zero)
            {
                g_error_free(error);
                return new SecretResult.Unavailable(Unavailable);
            }

            return removed ? new SecretResult.Ok() : new SecretResult.NotFound();
        }), cancellationToken);

    private static Task<SecretResult> RunAsync(Func<SecretResult> work, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            try
            {
                return work();
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return new SecretResult.Unavailable("libsecret is not installed.");
            }
        }, cancellationToken);

    // SecretSchema layout: name, flags, 32 x {attribute name, type}, then reserved fields.
    private const int SchemaSize = 16 + (32 * 16) + 64;

    private static SecretResult Native(string name, Func<IntPtr, IntPtr, SecretResult> action)
    {
        var allocated = new List<IntPtr>();
        var schema = Marshal.AllocHGlobal(SchemaSize);
        IntPtr attrs = IntPtr.Zero;
        try
        {
            for (var i = 0; i < SchemaSize; i += 8)
            {
                Marshal.WriteInt64(schema, i, 0);
            }

            var schemaName = Utf8(allocated, "dev.aoceditor.Secret");
            var attrName = Utf8(allocated, "name");
            Marshal.WriteIntPtr(schema, 0, schemaName);
            Marshal.WriteInt32(schema, 8, 2); // SECRET_SCHEMA_DONT_MATCH_NAME
            Marshal.WriteIntPtr(schema, 16, attrName);
            Marshal.WriteInt32(schema, 24, 0); // SECRET_SCHEMA_ATTRIBUTE_STRING

            var glib = NativeLibrary.Load(GLib);
            attrs = g_hash_table_new(NativeLibrary.GetExport(glib, "g_str_hash"), NativeLibrary.GetExport(glib, "g_str_equal"));
            g_hash_table_insert(attrs, attrName, Utf8(allocated, name));
            return action(schema, attrs);
        }
        finally
        {
            if (attrs != IntPtr.Zero)
            {
                g_hash_table_unref(attrs);
            }

            Marshal.FreeHGlobal(schema);
            foreach (var p in allocated)
            {
                Marshal.FreeCoTaskMem(p);
            }
        }
    }

    private static IntPtr Utf8(List<IntPtr> allocated, string value)
    {
        var p = Marshal.StringToCoTaskMemUTF8(value);
        allocated.Add(p);
        return p;
    }

    private const string LibSecret = "libsecret-1.so.0";
    private const string GLib = "libglib-2.0.so.0";

    [LibraryImport(LibSecret, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool secret_password_storev_sync(IntPtr schema, IntPtr attributes, string collection, string label, string password, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial IntPtr secret_password_lookupv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool secret_password_clearv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial void secret_password_free(IntPtr password);

    [LibraryImport(GLib)]
    private static partial IntPtr g_hash_table_new(IntPtr hash, IntPtr equal);

    [LibraryImport(GLib)]
    private static partial void g_hash_table_insert(IntPtr table, IntPtr key, IntPtr value);

    [LibraryImport(GLib)]
    private static partial void g_hash_table_unref(IntPtr table);

    [LibraryImport(GLib)]
    private static partial void g_error_free(IntPtr error);
}
