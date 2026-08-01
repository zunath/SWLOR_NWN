using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Editors;

public sealed class NuiConversationOpeningRow
{
    public NuiConversationOpeningRow(ConversationLink link, ConversationNode node, int index, int count)
    {
        Link = link;
        Node = node;
        Index = index;
        Count = count;
    }

    public ConversationLink Link { get; }
    public ConversationNode Node { get; }
    public int Index { get; }
    public int Count { get; }
    public int Number => Index + 1;
    public bool CanMoveUp => Index > 0;
    public bool CanMoveDown => Index + 1 < Count;
    public string Title => string.IsNullOrWhiteSpace(Node.SpeakerName) ? $"Opening {Number}" : Node.SpeakerName;
    public string Summary => NuiConversationText.Summarize(Node.Text);
    public string CheckSummary => Link.Conditions.Count == 0
        ? "Anyone else"
        : Link.Conditions.Count == 1 ? "1 check" : $"{Link.Conditions.Count} checks";
}

public sealed partial class NuiConversationTextBlockRow : ObservableObject
{
    private readonly ConversationTextBlock _block;
    private readonly Action<Action> _edit;

    public NuiConversationTextBlockRow(ConversationTextBlock block, Action<Action> edit)
    {
        _block = block;
        _edit = edit;
    }

    public IReadOnlyList<ConversationTextStyle> Styles { get; } = Enum.GetValues<ConversationTextStyle>();

    public string Text
    {
        get => _block.Text;
        set
        {
            if (_block.Text == value)
                return;
            _edit(() => _block.Text = value ?? string.Empty);
            OnPropertyChanged();
        }
    }

    public ConversationTextStyle Style
    {
        get => _block.Style;
        set
        {
            if (_block.Style == value)
                return;
            _edit(() => _block.Style = value);
            OnPropertyChanged();
        }
    }
}

/// <summary>
/// Read-only projection of a dialogue text block for the editor's in-game preview. Keeping this
/// separate from the editable row lets the preview substitute representative dynamic-token values
/// and use the exact semantic colors the runtime NUI window applies.
/// </summary>
public sealed class NuiConversationPreviewTextRow
{
    public NuiConversationPreviewTextRow(ConversationTextBlock block, string text)
    {
        Text = text;
        Foreground = new SolidColorBrush(PreviewColor(block));
    }

    public string Text { get; }
    public IBrush Foreground { get; }

    private static Color PreviewColor(ConversationTextBlock block)
    {
        if (block.Style == ConversationTextStyle.Custom && block.Color != null)
        {
            return Color.FromArgb(
                block.Color.Alpha,
                block.Color.Red,
                block.Color.Green,
                block.Color.Blue);
        }

        return block.Style switch
        {
            ConversationTextStyle.Action => Color.FromRgb(1, 254, 1),
            ConversationTextStyle.Highlight => Color.FromRgb(80, 140, 255),
            ConversationTextStyle.Check => Color.FromRgb(254, 80, 80),
            ConversationTextStyle.PlayerReply => Color.FromRgb(102, 178, 255),
            ConversationTextStyle.Muted => Colors.Gray,
            _ => Colors.White
        };
    }
}

public sealed partial class NuiConversationChoiceRow : ObservableObject
{
    private readonly ConversationChoice _choice;
    private readonly Action<Action> _edit;
    private readonly Func<string, string>? _displayText;

    public NuiConversationChoiceRow(
        ConversationChoiceLink link,
        ConversationChoice choice,
        int index,
        int count,
        Action<Action> edit,
        Func<string, string>? displayText = null)
    {
        Link = link;
        _choice = choice;
        Index = index;
        Count = count;
        _edit = edit;
        _displayText = displayText;
    }

    public ConversationChoiceLink Link { get; }
    public ConversationChoice Choice => _choice;
    public int Index { get; }
    public int Count { get; }
    public int Number => Index + 1;
    public bool CanMoveUp => Index > 0;
    public bool CanMoveDown => Index + 1 < Count;
    public bool HasNextLine => !_choice.EndsConversation && _choice.Next.Count > 0;
    public bool IsConditional => Link.Conditions.Count > 0;
    public string DisplayText => _displayText?.Invoke(_choice.Text.Text) ?? _choice.Text.Text;

    public string Text
    {
        get => _choice.Text.Text;
        set
        {
            if (_choice.Text.Text == value)
                return;
            _edit(() => _choice.Text.Text = value ?? string.Empty);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public bool EndsConversation
    {
        get => _choice.EndsConversation;
        set
        {
            if (_choice.EndsConversation == value)
                return;
            _edit(() =>
            {
                _choice.EndsConversation = value;
                if (value)
                    _choice.Next.Clear();
            });
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNextLine));
        }
    }
}

public sealed partial class GraphSnippetEditorViewModel : ObservableObject
{
    private readonly IList<string> _arguments;
    private readonly Action<Action> _edit;
    private readonly Action<GraphSnippetEditorViewModel> _remove;
    private readonly SnippetArgumentOptions _options;
    private readonly Action<bool>? _setNegated;
    private readonly Action<string>? _setOnceMarker;
    private bool _loading;

    public GraphSnippetEditorViewModel(
        SnippetDescriptor snippet,
        IList<string> arguments,
        SnippetArgumentOptions options,
        Action<Action> edit,
        Action<GraphSnippetEditorViewModel> remove,
        bool isNegated = false,
        Action<bool>? setNegated = null,
        string onceMarker = "",
        Action<string>? setOnceMarker = null)
    {
        Snippet = snippet;
        _arguments = arguments;
        _options = options;
        _edit = edit;
        _remove = remove;
        _setNegated = setNegated;
        _setOnceMarker = setOnceMarker;
        _isNegated = isNegated;
        _runOnce = !string.IsNullOrWhiteSpace(onceMarker);
        _onceMarker = onceMarker;

        _loading = true;
        var count = Math.Max(arguments.Count, snippet.MinimumArgumentCount);
        for (var index = 0; index < count; index++)
        {
            var argument = snippet.ArgumentAt(index);
            if (argument == null)
                continue;

            var value = index < arguments.Count ? arguments[index] : string.Empty;
            var argumentIndex = index;
            Arguments.Add(new ArgumentEditorViewModel(
                argument,
                value,
                options.For(argument, arguments.ToArray()),
                () => CommitArgument(argumentIndex)));
        }
        _loading = false;
    }

    public SnippetDescriptor Snippet { get; }
    public string Description => Snippet.Description;
    public bool CanNegate => _setNegated != null;
    public bool CanRunOnce => _setOnceMarker != null;
    public ObservableCollection<ArgumentEditorViewModel> Arguments { get; } = new();
    public string Sentence => Snippet.ToSentence(Arguments.Select(argument => argument.Value).ToArray(), IsNegated);
    public bool CanAddArguments => Snippet.RepeatGroupSize > 0 || Arguments.Count < Snippet.Arguments.Count;
    public bool CanRemoveArguments => Arguments.Count > Snippet.MinimumArgumentCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Sentence))]
    private bool _isNegated;

    [ObservableProperty]
    private bool _runOnce;

    [ObservableProperty]
    private string _onceMarker = string.Empty;

    partial void OnIsNegatedChanged(bool value)
    {
        if (_loading || _setNegated == null)
            return;
        _edit(() => _setNegated(value));
    }

    partial void OnRunOnceChanged(bool value)
    {
        if (_loading || _setOnceMarker == null)
            return;
        _edit(() => _setOnceMarker(value ? DefaultOnceMarker() : string.Empty));
        if (value && string.IsNullOrWhiteSpace(OnceMarker))
            OnceMarker = DefaultOnceMarker();
    }

    partial void OnOnceMarkerChanged(string value)
    {
        if (_loading || _setOnceMarker == null || !RunOnce)
            return;
        _edit(() => _setOnceMarker(value ?? string.Empty));
    }

    [RelayCommand]
    private void Remove() => _remove(this);

    [RelayCommand(CanExecute = nameof(CanAddArguments))]
    private void AddArguments()
    {
        var count = Snippet.RepeatGroupSize > 0 && Arguments.Count >= Snippet.Arguments.Count
            ? Snippet.RepeatGroupSize
            : 1;
        _loading = true;
        for (var offset = 0; offset < count; offset++)
        {
            var argumentIndex = Arguments.Count;
            var argument = Snippet.ArgumentAt(argumentIndex);
            if (argument == null)
                break;
            Arguments.Add(new ArgumentEditorViewModel(
                argument,
                string.Empty,
                _options.For(argument, Arguments.Select(item => item.Value).ToArray()),
                () => CommitArgument(argumentIndex)));
        }
        _loading = false;
        CommitValues();
        NotifyArgumentShapeChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveArguments))]
    private void RemoveArguments()
    {
        var removable = Arguments.Count - Snippet.MinimumArgumentCount;
        var count = Snippet.RepeatGroupSize > 0 && Arguments.Count > Snippet.Arguments.Count
            ? Math.Min(Snippet.RepeatGroupSize, removable)
            : 1;
        for (var offset = 0; offset < count && Arguments.Count > Snippet.MinimumArgumentCount; offset++)
            Arguments.RemoveAt(Arguments.Count - 1);
        CommitValues();
        NotifyArgumentShapeChanged();
    }

    private void CommitArgument(int changedIndex)
    {
        if (_loading)
            return;

        var values = Arguments.Select(argument => argument.Value).ToArray();
        for (var index = changedIndex + 1; index < Arguments.Count; index++)
        {
            var argument = Snippet.ArgumentAt(index);
            if (argument == null)
                continue;
            Arguments[index].RefreshOptions(_options.For(argument, values));
            values[index] = Arguments[index].Value;
        }

        CommitValues(values);
    }

    private void CommitValues(string[]? values = null)
    {
        values ??= Arguments.Select(argument => argument.Value).ToArray();
        _edit(() =>
        {
            _arguments.Clear();
            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
                _arguments.Add(value);
        });
        OnPropertyChanged(nameof(Sentence));
    }

    private void NotifyArgumentShapeChanged()
    {
        OnPropertyChanged(nameof(CanAddArguments));
        OnPropertyChanged(nameof(CanRemoveArguments));
        AddArgumentsCommand.NotifyCanExecuteChanged();
        RemoveArgumentsCommand.NotifyCanExecuteChanged();
    }

    private string DefaultOnceMarker() => $"conversation-{Snippet.Key}";
}

public sealed record NuiConversationProblem(string Message, bool IsError, string Location)
{
    public string Severity => IsError ? "ERROR" : "CHECK";
}

internal static class NuiConversationText
{
    public static string Summarize(IEnumerable<ConversationTextBlock> blocks)
    {
        var text = string.Concat(blocks.Select(block => block.Text)).ReplaceLineEndings(" ").Trim();
        return text.Length <= 70 ? text : text[..67].TrimEnd() + "...";
    }
}
