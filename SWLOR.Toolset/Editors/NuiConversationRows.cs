using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Editors;

public enum NuiConversationTreeRowKind
{
    NpcLine,
    PlayerChoice,
    MissingTarget
}

/// <summary>
/// A flattened row in the conversation tree. The row keeps the exact graph link it represents so
/// checks and ordering are edited on the route where they run, including nested NPC lines.
/// </summary>
public sealed partial class NuiConversationTreeRow : ObservableObject
{
    private static readonly IBrush NpcAccent = new ImmutableSolidColorBrush(Color.FromRgb(222, 105, 113));
    private static readonly IBrush PlayerAccent = new ImmutableSolidColorBrush(Color.FromRgb(91, 159, 255));
    private static readonly IBrush MissingAccent = new ImmutableSolidColorBrush(Color.FromRgb(232, 178, 91));
    private static readonly IBrush AlwaysVisibleAccent = new ImmutableSolidColorBrush(Color.FromRgb(113, 196, 140));
    private static readonly IBrush ConditionalAccent = new ImmutableSolidColorBrush(Color.FromRgb(232, 178, 91));

    private NuiConversationTreeRow(
        string key,
        NuiConversationTreeRowKind kind,
        int depth,
        int index,
        int siblingCount,
        ConversationNode? node,
        ConversationChoice? choice,
        ConversationLink? nodeLink,
        ConversationChoiceLink? choiceLink,
        ConversationNode? parentNode,
        ConversationLink? parentNodeLink,
        bool parentNodeIsEntryPoint,
        ConversationChoice? parentChoice,
        bool isEntryPoint,
        bool isReference,
        bool hasChildren,
        bool isBranchExpanded,
        string missingTargetId)
    {
        Key = key;
        Kind = kind;
        Depth = depth;
        Index = index;
        SiblingCount = siblingCount;
        Node = node;
        Choice = choice;
        NodeLink = nodeLink;
        ChoiceLink = choiceLink;
        ParentNode = parentNode;
        ParentNodeLink = parentNodeLink;
        ParentNodeIsEntryPoint = parentNodeIsEntryPoint;
        ParentChoice = parentChoice;
        IsEntryPoint = isEntryPoint;
        IsReference = isReference;
        HasChildren = hasChildren;
        IsBranchExpanded = isBranchExpanded;
        MissingTargetId = missingTargetId;
    }

    public static NuiConversationTreeRow ForNpc(
        string key,
        int depth,
        int index,
        int siblingCount,
        ConversationNode node,
        ConversationLink nodeLink,
        ConversationChoice? parentChoice,
        bool isEntryPoint,
        bool isReference,
        bool hasChildren,
        bool isBranchExpanded) =>
        new(
            key,
            NuiConversationTreeRowKind.NpcLine,
            depth,
            index,
            siblingCount,
            node,
            null,
            nodeLink,
            null,
            null,
            null,
            false,
            parentChoice,
            isEntryPoint,
            isReference,
            hasChildren,
            isBranchExpanded,
            string.Empty);

    public static NuiConversationTreeRow ForPlayer(
        string key,
        int depth,
        int index,
        int siblingCount,
        ConversationNode parentNode,
        ConversationLink parentNodeLink,
        bool parentNodeIsEntryPoint,
        ConversationChoice choice,
        ConversationChoiceLink choiceLink,
        bool hasChildren,
        bool isBranchExpanded) =>
        new(
            key,
            NuiConversationTreeRowKind.PlayerChoice,
            depth,
            index,
            siblingCount,
            null,
            choice,
            null,
            choiceLink,
            parentNode,
            parentNodeLink,
            parentNodeIsEntryPoint,
            null,
            false,
            false,
            hasChildren,
            isBranchExpanded,
            string.Empty);

    public static NuiConversationTreeRow ForMissingTarget(
        string key,
        int depth,
        int index,
        int siblingCount,
        ConversationLink nodeLink,
        ConversationChoice? parentChoice,
        bool isEntryPoint) =>
        new(
            key,
            NuiConversationTreeRowKind.MissingTarget,
            depth,
            index,
            siblingCount,
            null,
            null,
            nodeLink,
            null,
            null,
            null,
            false,
            parentChoice,
            isEntryPoint,
            false,
            false,
            true,
            nodeLink.TargetNodeId);

    public string Key { get; }
    public NuiConversationTreeRowKind Kind { get; }
    public int Depth { get; }
    public int Index { get; }
    public int SiblingCount { get; }
    public ConversationNode? Node { get; }
    public ConversationChoice? Choice { get; }
    public ConversationLink? NodeLink { get; }
    public ConversationChoiceLink? ChoiceLink { get; }
    public ConversationNode? ParentNode { get; }
    public ConversationLink? ParentNodeLink { get; }
    public bool ParentNodeIsEntryPoint { get; }
    public ConversationChoice? ParentChoice { get; }
    public bool IsEntryPoint { get; }
    public bool IsReference { get; }
    public bool HasChildren { get; }
    public bool IsBranchExpanded { get; }
    public string MissingTargetId { get; }

    public bool IsNpc => Kind == NuiConversationTreeRowKind.NpcLine;
    public bool IsPlayer => Kind == NuiConversationTreeRowKind.PlayerChoice;
    public bool IsMissing => Kind == NuiConversationTreeRowKind.MissingTarget;
    public bool CanMoveUp => Index > 0;
    public bool CanMoveDown => Index + 1 < SiblingCount;
    public double IndentWidth => Depth * 22d;
    public IBrush AccentBrush => Kind switch
    {
        NuiConversationTreeRowKind.NpcLine => NpcAccent,
        NuiConversationTreeRowKind.PlayerChoice => PlayerAccent,
        _ => MissingAccent
    };

    public string KindLabel => Kind switch
    {
        NuiConversationTreeRowKind.NpcLine => IsEntryPoint ? $"OPENING {Index + 1}" : "NPC",
        NuiConversationTreeRowKind.PlayerChoice when Choice?.IsAutomatic == true => "CONTINUE",
        NuiConversationTreeRowKind.PlayerChoice => "PLAYER",
        _ => "MISSING"
    };

    public string Text => Kind switch
    {
        NuiConversationTreeRowKind.NpcLine => NuiConversationText.Summarize(Node?.Text ?? []),
        NuiConversationTreeRowKind.PlayerChoice => Choice?.Text.Text ?? string.Empty,
        _ => $"Missing NPC line '{MissingTargetId}'"
    };

    public int CheckCount => IsNpc || IsMissing
        ? NodeLink?.Conditions.Count ?? 0
        : ChoiceLink?.Conditions.Count ?? 0;
    public int ActionCount => IsNpc ? Node?.OnEnterActions.Count ?? 0 : Choice?.Actions.Count ?? 0;
    public bool HasActions => ActionCount > 0;
    public bool EndsConversation => Choice?.EndsConversation == true;
    public string BranchToggleIcon => IsBranchExpanded ? "▾" : "▸";
    public string BranchToggleToolTip => IsBranchExpanded
        ? "Hide the responses and follow-up lines in this branch."
        : "Show the responses and follow-up lines in this branch.";
    public string VisibilityIcon => CheckCount == 0 ? "●" : "◆";
    public IBrush VisibilityBrush => CheckCount == 0 ? AlwaysVisibleAccent : ConditionalAccent;
    public string VisibilityToolTip => CheckCount == 0
        ? "Always shown: this route has no requirements."
        : CheckCount == 1
            ? "Conditional: shown only when its check passes."
            : $"Conditional: shown only when all {CheckCount} checks pass.";
    public string ActionToolTip => ActionCount == 1
        ? "Runs 1 action when this line is used."
        : $"Runs {ActionCount} actions when this line is used.";
    public string EndToolTip => "Ends the conversation when the player selects this response.";
    public string ReferenceToolTip => "This is a shared or looping line; its children are shown at the first occurrence.";

    public string Details
    {
        get
        {
            if (IsMissing)
                return "This route points to a line that no longer exists.";

            var checks = IsNpc ? NodeLink?.Conditions.Count ?? 0 : ChoiceLink?.Conditions.Count ?? 0;
            var actions = IsNpc ? Node?.OnEnterActions.Count ?? 0 : Choice?.Actions.Count ?? 0;
            var parts = new List<string>
            {
                checks == 0 ? "always shown" : checks == 1 ? "1 check" : $"{checks} checks"
            };
            if (actions > 0)
                parts.Add(actions == 1 ? "1 action" : $"{actions} actions");
            if (Choice?.EndsConversation == true)
                parts.Add("ends conversation");
            else if (Choice?.Next.Count > 1)
                parts.Add($"{Choice.Next.Count} next lines");
            if (IsReference)
                parts.Add("shared or looping line");
            return string.Join("  •  ", parts);
        }
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Details));
        OnPropertyChanged(nameof(CheckCount));
        OnPropertyChanged(nameof(ActionCount));
        OnPropertyChanged(nameof(HasActions));
        OnPropertyChanged(nameof(EndsConversation));
        OnPropertyChanged(nameof(VisibilityIcon));
        OnPropertyChanged(nameof(VisibilityBrush));
        OnPropertyChanged(nameof(VisibilityToolTip));
        OnPropertyChanged(nameof(ActionToolTip));
    }
}

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
    internal ConversationTextBlock Block => _block;

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
        Foreground = new ImmutableSolidColorBrush(PreviewColor(block));
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
    private readonly Action<Action> _structuralEdit;
    private readonly Func<string, string>? _displayText;

    public NuiConversationChoiceRow(
        ConversationChoiceLink link,
        ConversationChoice choice,
        int index,
        int count,
        Action<Action> edit,
        Func<string, string>? displayText = null,
        Action<Action>? structuralEdit = null)
    {
        Link = link;
        _choice = choice;
        Index = index;
        Count = count;
        _edit = edit;
        _structuralEdit = structuralEdit ?? edit;
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
            _structuralEdit(() =>
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
    private bool _loading;

    public GraphSnippetEditorViewModel(
        SnippetDescriptor snippet,
        IList<string> arguments,
        SnippetArgumentOptions options,
        Action<Action> edit,
        Action<GraphSnippetEditorViewModel> remove,
        bool isNegated = false,
        Action<bool>? setNegated = null)
    {
        Snippet = snippet;
        _arguments = arguments;
        _options = options;
        _edit = edit;
        _remove = remove;
        _setNegated = setNegated;
        _isNegated = isNegated;

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
    public ObservableCollection<ArgumentEditorViewModel> Arguments { get; } = new();
    public string Sentence => Snippet.ToSentence(Arguments.Select(argument => argument.Value).ToArray(), IsNegated);
    public bool CanAddArguments => Snippet.RepeatGroupSize > 0 || Arguments.Count < Snippet.Arguments.Count;
    public bool CanRemoveArguments => Arguments.Count > Snippet.MinimumArgumentCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Sentence))]
    private bool _isNegated;

    partial void OnIsNegatedChanged(bool value)
    {
        if (_loading || _setNegated == null)
            return;
        _edit(() => _setNegated(value));
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
