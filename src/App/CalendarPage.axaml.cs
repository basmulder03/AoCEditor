using AoCEditor.UI.Sync;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace AoCEditor;

public partial class CalendarPage : UserControl
{
    private bool _loaded;

    public CalendarPage() : this(null)
    {
    }

    public CalendarPage(AppServices? services)
    {
        InitializeComponent();
        if (services is null)
        {
            return;
        }

        var model = new CalendarViewModel(services.Sync, services.SyncState, TimeProvider.System, services.LoggerFactory.CreateLogger<CalendarViewModel>());
        model.DaySelected += (_, e) => DaySelected?.Invoke(this, e);
        model.AccountRequested += (_, e) => AccountRequested?.Invoke(this, e);
        View.DataContext = model;
        AttachedToVisualTree += async (_, _) =>
        {
            if (!_loaded)
            {
                _loaded = true;
                await model.LoadAsync(CancellationToken.None);
            }
        };
    }

    public event EventHandler<DaySelectedEventArgs>? DaySelected;

    public event EventHandler? AccountRequested;
}
