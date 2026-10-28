using System.Runtime.InteropServices;

namespace AoCEditor.UI.Session;

/// <summary>
/// Reads cookies, HttpOnly ones included, from a WebKitGTK 4.1 view. WebKit objects belong to the GTK thread, so every call is queued there
/// with g_main_context_invoke and the async answer is bridged back to a Task. Any problem becomes Unsupported with a reason.
/// </summary>
internal sealed unsafe partial class LinuxCookieSource(IntPtr view) : ICookieSource
{
    private const string Webkit = "libwebkit2gtk-4.1.so.0";
    private const string Soup = "libsoup-3.0.so.0";
    private const string Glib = "libglib-2.0.so.0";
    private const int MaxCookies = 256;
    private const int MaxTextBytes = 8192;

    private static readonly (string Library, string[] Symbols)[] Required =
    [
        (Webkit, ["webkit_web_view_get_website_data_manager", "webkit_website_data_manager_get_cookie_manager", "webkit_cookie_manager_get_cookies", "webkit_cookie_manager_get_cookies_finish", "webkit_cookie_manager_delete_cookie"]),
        (Soup, ["soup_cookie_get_name", "soup_cookie_get_value", "soup_cookie_get_domain", "soup_cookie_free"]),
        (Glib, ["g_list_length", "g_list_nth_data", "g_list_free", "g_error_free", "g_main_context_invoke"]),
    ];

    public Task<CookieRead> ReadAsync(Uri site, CancellationToken cancellationToken) => QueryAsync(site, null, cancellationToken);

    public Task DeleteAsync(Uri site, string name, CancellationToken cancellationToken) => QueryAsync(site, name, cancellationToken);

    private Task<CookieRead> QueryAsync(Uri site, string? deleteName, CancellationToken cancellationToken)
    {
        if (view == IntPtr.Zero)
        {
            return Task.FromResult<CookieRead>(new CookieRead.Unsupported("The embedded browser gave no WebKitGTK view."));
        }

        if (Missing() is { } missing)
        {
            return Task.FromResult<CookieRead>(new CookieRead.Unsupported(missing));
        }

        return CookieSources.GuardAsync(_ =>
        {
            var state = new Request(view, site.GetLeftPart(UriPartial.Path), deleteName);
            state.Handle = GCHandle.ToIntPtr(GCHandle.Alloc(state));
            g_main_context_invoke(IntPtr.Zero, &OnGtkThread, state.Handle);
            return state.Answer.Task;
        }, CookieSources.NativeTimeout, cancellationToken);
    }

    // A missing library or symbol would otherwise only show up as an exception on the GTK thread, which cannot be caught there.
    private static string? Missing()
    {
        foreach (var (library, symbols) in Required)
        {
            if (!NativeLibrary.TryLoad(library, out var handle))
            {
                return $"The WebKitGTK 4.1 browser libraries are not installed ({library}).";
            }

            foreach (var symbol in symbols)
            {
                if (!NativeLibrary.TryGetExport(handle, symbol, out _))
                {
                    return $"The installed {library} lacks {symbol}.";
                }
            }
        }

        return null;
    }

    private sealed class Request(IntPtr view, string uri, string? deleteName)
    {
        public IntPtr View { get; } = view;

        public string Uri { get; } = uri;

        public string? DeleteName { get; } = deleteName;

        public IntPtr Manager { get; set; }

        public IntPtr Handle { get; set; }

        public TaskCompletionSource<CookieRead> Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [UnmanagedCallersOnly]
    private static int OnGtkThread(IntPtr data)
    {
        var state = (Request)GCHandle.FromIntPtr(data).Target!;
        try
        {
            var manager = webkit_website_data_manager_get_cookie_manager(webkit_web_view_get_website_data_manager(state.View));
            if (manager == IntPtr.Zero)
            {
                Finish(state, new CookieRead.Unsupported("WebKitGTK gave no cookie manager."));
            }
            else
            {
                state.Manager = manager;
                webkit_cookie_manager_get_cookies(manager, state.Uri, IntPtr.Zero, &OnCookies, data);
            }
        }
        catch (Exception ex)
        {
            Finish(state, new CookieRead.Unsupported(ex.Message));
        }

        return 0;
    }

    [UnmanagedCallersOnly]
    private static void OnCookies(IntPtr source, IntPtr result, IntPtr data)
    {
        var state = (Request)GCHandle.FromIntPtr(data).Target!;
        var list = IntPtr.Zero;
        try
        {
            var error = IntPtr.Zero;
            list = webkit_cookie_manager_get_cookies_finish(state.Manager, result, &error);
            if (error != IntPtr.Zero)
            {
                g_error_free(error);
                Finish(state, new CookieRead.Unsupported("WebKitGTK could not read the cookies."));
                return;
            }

            var items = new List<BrowserCookie>();
            var count = g_list_length(list);
            for (uint i = 0; i < count; i++)
            {
                var cookie = g_list_nth_data(list, i);
                var name = Text(soup_cookie_get_name(cookie));
                if (name is not null && name == state.DeleteName)
                {
                    webkit_cookie_manager_delete_cookie(state.Manager, cookie, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }

                if (items.Count < MaxCookies)
                {
                    items.Add(new BrowserCookie(name ?? "", Text(soup_cookie_get_domain(cookie)) ?? "", Text(soup_cookie_get_value(cookie)) ?? ""));
                }

                soup_cookie_free(cookie);
            }

            Finish(state, new CookieRead.Cookies(items));
        }
        catch (Exception ex)
        {
            Finish(state, new CookieRead.Unsupported(ex.Message));
        }
        finally
        {
            if (list != IntPtr.Zero)
            {
                g_list_free(list);
            }
        }
    }

    private static void Finish(Request state, CookieRead result)
    {
        GCHandle.FromIntPtr(state.Handle).Free();
        state.Answer.TrySetResult(result);
    }

    // Reads a NUL-terminated UTF-8 string, giving up (null) when no terminator appears within the cap.
    private static string? Text(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        var bytes = (byte*)pointer;
        for (var length = 0; length < MaxTextBytes; length++)
        {
            if (bytes[length] == 0)
            {
                return System.Text.Encoding.UTF8.GetString(bytes, length);
            }
        }

        return null;
    }

    [LibraryImport(Webkit)]
    private static partial IntPtr webkit_web_view_get_website_data_manager(IntPtr view);

    [LibraryImport(Webkit)]
    private static partial IntPtr webkit_website_data_manager_get_cookie_manager(IntPtr manager);

    [LibraryImport(Webkit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void webkit_cookie_manager_get_cookies(IntPtr manager, string uri, IntPtr cancellable, delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> callback, IntPtr data);

    [LibraryImport(Webkit)]
    private static partial IntPtr webkit_cookie_manager_get_cookies_finish(IntPtr manager, IntPtr result, IntPtr* error);

    [LibraryImport(Webkit)]
    private static partial void webkit_cookie_manager_delete_cookie(IntPtr manager, IntPtr cookie, IntPtr cancellable, IntPtr callback, IntPtr data);

    [LibraryImport(Soup)]
    private static partial IntPtr soup_cookie_get_name(IntPtr cookie);

    [LibraryImport(Soup)]
    private static partial IntPtr soup_cookie_get_value(IntPtr cookie);

    [LibraryImport(Soup)]
    private static partial IntPtr soup_cookie_get_domain(IntPtr cookie);

    [LibraryImport(Soup)]
    private static partial void soup_cookie_free(IntPtr cookie);

    [LibraryImport(Glib)]
    private static partial uint g_list_length(IntPtr list);

    [LibraryImport(Glib)]
    private static partial IntPtr g_list_nth_data(IntPtr list, uint index);

    [LibraryImport(Glib)]
    private static partial void g_list_free(IntPtr list);

    [LibraryImport(Glib)]
    private static partial void g_error_free(IntPtr error);

    [LibraryImport(Glib)]
    private static partial void g_main_context_invoke(IntPtr context, delegate* unmanaged<IntPtr, int> function, IntPtr data);
}
