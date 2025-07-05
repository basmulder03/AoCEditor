using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace AoC.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>  
        {  
            events.AddWindows(wndLifeCycleBuilder =>  
            {  
                wndLifeCycleBuilder.OnWindowCreated(window =>  
                {  
                    // window.ExtendsContentIntoTitleBar = false;  
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);  
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
                    (appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)?.Maximize();
                });  
            });  
        });
#endif

        return builder.Build();
    }
}