using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace SWLOR.Toolset.Editors.Tlk;

public partial class TlkEditorDocumentView : UserControl
{
    private TlkEditorDocumentViewModel? _subscribed;

    public TlkEditorDocumentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToNavigation();
        AttachedToVisualTree += (_, _) => SubscribeToNavigation();
        DetachedFromVisualTree += (_, _) => UnsubscribeFromNavigation();
    }

    private void SubscribeToNavigation()
    {
        UnsubscribeFromNavigation();
        if (DataContext is not TlkEditorDocumentViewModel viewModel)
            return;
        _subscribed = viewModel;
        viewModel.SelectionNavigationRequested += OnSelectionNavigationRequested;
    }

    private void UnsubscribeFromNavigation()
    {
        if (_subscribed != null)
            _subscribed.SelectionNavigationRequested -= OnSelectionNavigationRequested;
        _subscribed = null;
    }

    private void OnSelectionNavigationRequested(int id)
    {
        if (DataContext is TlkEditorDocumentViewModel viewModel && viewModel.SelectedRow != null)
            RowGrid.ScrollIntoView(viewModel.SelectedRow);
    }

    private async void OnCopyRowId(object? sender, RoutedEventArgs args)
    {
        if (DataContext is TlkEditorDocumentViewModel viewModel)
            await SetClipboardTextAsync(viewModel.SelectedIdDisplay);
    }

    private async void OnCopyStrRef(object? sender, RoutedEventArgs args)
    {
        if (DataContext is TlkEditorDocumentViewModel viewModel)
            await SetClipboardTextAsync(viewModel.SelectedStrRefDisplay);
    }

    private async void OnRowGridKeyDown(object? sender, KeyEventArgs args)
    {
        if (DataContext is not TlkEditorDocumentViewModel viewModel ||
            (args.KeyModifiers & KeyModifiers.Control) == 0)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;

        if (args.Key == Key.C)
        {
            var ids = (RowGrid.SelectedItems?.OfType<TlkEditorRowViewModel>() ??
                       Enumerable.Empty<TlkEditorRowViewModel>())
                .Select(row => row.Id)
                .ToArray();
            if (ids.Length > 0)
            {
                await clipboard.SetTextAsync(viewModel.CopyRows(ids));
                args.Handled = true;
            }
        }
        else if (args.Key == Key.V)
        {
            var text = await clipboard.TryGetTextAsync();
            if (text != null)
            {
                await viewModel.PasteRowsAsync(text);
                args.Handled = true;
            }
        }
    }

    private Task SetClipboardTextAsync(string text) =>
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
}
