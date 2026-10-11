using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Nwn.Toolset.Avalonia.Graph;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;

namespace SWLOR.Toolset.Editors.Conversations;

/// <summary>A live-draft overview; all authoring remains in the owning conversation editor.</summary>
public sealed class ConversationGraphWindow : Window
{
    private readonly DlgDocument _dialog;
    private readonly Grid _layout = new() { RowDefinitions = new RowDefinitions("Auto,*,150") };
    private readonly TextBox _details = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(8) };
    private GraphCanvas? _canvas;

    public ConversationGraphWindow(DlgDocument dialog, string title)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        Title = $"Conversation graph · {title}";
        Width = 1100;
        Height = 750;
        MinWidth = 800;
        MinHeight = 500;
        var fit = new Button { Content = "Fit" };
        fit.Click += (_, _) => _canvas?.Fit();
        var refresh = new Button { Content = "Refresh", Name = "RefreshConversationGraph" };
        refresh.Click += (_, _) => Refresh();
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8) };
        header.Children.Add(refresh);
        header.Children.Add(fit);
        header.Children.Add(new TextBlock { Text = "Refresh to include current edits. Select a node to read its full text.", VerticalAlignment = VerticalAlignment.Center });
        _layout.Children.Add(header);
        Grid.SetRow(_details, 2);
        _layout.Children.Add(_details);
        Content = _layout;
        Opened += (_, _) => _canvas?.Fit();
        Refresh();
    }

    private void Refresh()
    {
        var snapshot = ConversationGraphAdapter.Build(_dialog);
        if (_canvas is not null) _layout.Children.Remove(_canvas);
        _canvas = new GraphCanvas();
        _canvas.SetDocument(snapshot.Diagram);
        _canvas.NodeSelected += (_, args) => _details.Text = args.NodeId is { } id ? snapshot.FullText[id] : string.Empty;
        Grid.SetRow(_canvas, 1);
        _layout.Children.Add(_canvas);
        _details.Text = string.Empty;
        _layout.UpdateLayout();
        _canvas.Fit();
    }
}
