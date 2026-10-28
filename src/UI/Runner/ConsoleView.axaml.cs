using System.Collections.Specialized;
using Avalonia.Controls;

namespace Runner.UI;

public partial class ConsoleView : UserControl
{
    private ConsoleViewModel? _model;

    public ConsoleView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_model is not null)
        {
            _model.Lines.CollectionChanged -= OnLinesChanged;
        }

        _model = DataContext as ConsoleViewModel;
        if (_model is not null)
        {
            _model.Lines.CollectionChanged += OnLinesChanged;
        }
    }

    void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Scroller.ScrollToEnd();
        }
    }
}
