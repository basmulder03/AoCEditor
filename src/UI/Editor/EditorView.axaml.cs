using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.TextMate;
using Editor.Application;
using TextMateSharp.Grammars;

namespace Editor.UI;

public partial class EditorView : UserControl
{
    private EditorViewModel? _model;
    private TextMate.Installation? _textMate;
    private string? _scope;

    public EditorView()
    {
        InitializeComponent();
        Editor.TextChanged += (_, _) =>
        {
            if (_model is not null)
            {
                _model.Text = Editor.Text;
            }
        };
        Editor.GotFocus += (_, _) => _model?.CheckDisk();
        ReloadButton.Click += (_, _) => _model?.Reload();
        OverwriteButton.Click += (_, _) => _model?.Save(overwrite: true);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            _model?.Save();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_model is not null)
        {
            _model.PropertyChanged -= OnModelChanged;
        }

        _model = DataContext as EditorViewModel;
        if (_model is not null)
        {
            _model.PropertyChanged += OnModelChanged;
            Sync();
        }
    }

    void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.Text) or nameof(EditorViewModel.FilePath))
        {
            Sync();
        }
    }

    void Sync()
    {
        if (_model is null)
        {
            return;
        }

        if (Editor.Text != _model.Text)
        {
            Editor.Text = _model.Text;
        }

        var scope = _model.FilePath is null ? null : Highlighting.ScopeFor(_model.FilePath);
        if (scope != _scope)
        {
            _scope = scope;
            _textMate ??= Editor.InstallTextMate(new RegistryOptions(ThemeName.LightPlus));
            _textMate.SetGrammar(scope);
        }
    }
}
