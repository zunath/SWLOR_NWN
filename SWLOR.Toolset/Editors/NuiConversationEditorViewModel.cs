using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors;

/// <summary>
/// Graph-native authoring for the NUI conversation window. It edits the same JSON the server embeds;
/// no DLG nodes, generated shells, NWScript dispatchers, or 255-slot allocation participate.
/// </summary>
public sealed partial class NuiConversationEditorViewModel : Document, IEditorDocument
{
    private readonly string _filePath;
    private readonly string _resRef;
    private readonly SnippetCatalog _snippets;
    private readonly SnippetArgumentOptions _argumentOptions;
    private readonly OutputLogService _log;
    private readonly IEditorPromptService _prompts;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private ConversationGraph _graph;
    private string _savedJson;
    private DateTime _diskWriteTimeUtc;
    private string? _selectedNodeId;
    private ConversationLink? _selectedOpeningLink;
    private ConversationChoiceLink? _selectedChoiceLink;
    private bool _loading;
    private bool _closeApproved;
    private bool _closePromptOpen;
    private bool _disposed;

    public event Action<NuiConversationEditorViewModel>? Closed;
    public event Action<NuiConversationEditorViewModel>? CloseRequested;

    public ObservableCollection<ConversationBehaviorOption> BehaviorOptions { get; } = new();
    public ObservableCollection<DynamicTextTokenOption> DynamicTextTokens { get; } = new();
    public ObservableCollection<NuiConversationOpeningRow> OpeningLines { get; } = new();
    public ObservableCollection<NuiConversationTextBlockRow> TextBlocks { get; } = new();
    public ObservableCollection<NuiConversationChoiceRow> Choices { get; } = new();
    public ObservableCollection<GraphSnippetEditorViewModel> Conditions { get; } = new();
    public ObservableCollection<GraphSnippetEditorViewModel> Actions { get; } = new();
    public ObservableCollection<SnippetDescriptor> AvailableConditions { get; } = new();
    public ObservableCollection<SnippetDescriptor> AvailableActions { get; } = new();
    public ObservableCollection<NuiConversationProblem> Problems { get; } = new();
    public ObservableCollection<NuiConversationChoiceRow> PreviewChoices { get; } = new();
    public ObservableCollection<NuiConversationTextBlockRow> PreviewTextBlocks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMerchant))]
    [NotifyPropertyChangedFor(nameof(IsQuestGiver))]
    [NotifyPropertyChangedFor(nameof(IsConversation))]
    [NotifyPropertyChangedFor(nameof(OutlineTitle))]
    private ConversationBehaviorOption? _selectedBehavior;

    [ObservableProperty]
    private NuiConversationOpeningRow? _selectedOpening;

    [ObservableProperty]
    private NuiConversationChoiceRow? _selectedChoice;

    [ObservableProperty]
    private SnippetDescriptor? _conditionToAdd;

    [ObservableProperty]
    private SnippetDescriptor? _actionToAdd;

    [ObservableProperty]
    private DynamicTextTokenOption? _dynamicTextToInsert;

    [ObservableProperty]
    private string _speakerName = string.Empty;

    [ObservableProperty]
    private string _speakerTag = string.Empty;

    [ObservableProperty]
    private string _portraitResref = string.Empty;

    [ObservableProperty]
    private string _soundResref = string.Empty;

    [ObservableProperty]
    private decimal _animation;

    [ObservableProperty]
    private string _merchantGreeting = string.Empty;

    [ObservableProperty]
    private string _merchantChoiceText = string.Empty;

    [ObservableProperty]
    private string _merchantStoreTag = string.Empty;

    [ObservableProperty]
    private string _previewSpeaker = string.Empty;

    [ObservableProperty]
    private string _previewStatus = string.Empty;

    public NuiConversationEditorViewModel(
        string filePath,
        string resRef,
        SnippetCatalog snippets,
        IGameCodeIndex? gameCode,
        OutputLogService log,
        IEditorPromptService prompts,
        Func<string, IReadOnlyList<string>>? tagsFor = null)
    {
        _filePath = filePath;
        _resRef = resRef;
        _snippets = snippets;
        _argumentOptions = new SnippetArgumentOptions(gameCode, tagsFor);
        _log = log;
        _prompts = prompts;
        _graph = LoadGraph(filePath);
        _savedJson = Serialize(_graph);
        _diskWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        Id = $"nui-conversation:{filePath}";

        BehaviorOptions.Add(new ConversationBehaviorOption(
            ConversationBehaviorKind.Merchant,
            "Merchant",
            "Greeting and shop choice; Goodbye is always supplied."));
        BehaviorOptions.Add(new ConversationBehaviorOption(
            ConversationBehaviorKind.QuestGiver,
            "Quest giver",
            "Ordered quest moments with checks and outcomes."));
        BehaviorOptions.Add(new ConversationBehaviorOption(
            ConversationBehaviorKind.Conversation,
            "Conversation",
            "Flexible NPC lines, player choices, branches, and loops."));

        DynamicTextTokens.Add(new DynamicTextTokenOption("Player name", "{{player.name}}"));
        DynamicTextTokens.Add(new DynamicTextTokenOption("Speaker name", "{{owner.name}}"));
        DynamicTextTokens.Add(new DynamicTextTokenOption("Player race", "{{player.race}}"));
        DynamicTextTokens.Add(new DynamicTextTokenOption("Boy or girl", "{{player.gender.boy-girl}}"));
        DynamicTextTokens.Add(new DynamicTextTokenOption("Sir or madam", "{{player.gender.sir-madam}}"));

        foreach (var snippet in snippets.Conditions)
            AvailableConditions.Add(snippet);
        foreach (var snippet in snippets.Actions)
            AvailableActions.Add(snippet);

        _selectedBehavior = BehaviorOptions.First(option => option.Kind == DetectBehavior());
        RefreshAll(selectFirst: true);
        UpdateTitle();
    }

    public string ResRef => _resRef;
    public bool IsMerchant => SelectedBehavior?.Kind == ConversationBehaviorKind.Merchant;
    public bool IsQuestGiver => SelectedBehavior?.Kind == ConversationBehaviorKind.QuestGiver;
    public bool IsConversation => SelectedBehavior?.Kind == ConversationBehaviorKind.Conversation;
    public bool IsGeneral => !IsMerchant;
    public string OutlineTitle => IsQuestGiver ? "Quest moments" : "Opening lines";
    public bool HasSelectedLine => CurrentNode != null;
    public bool HasSelectedChoice => SelectedChoice != null;
    public string ActionScopeHelp => SelectedChoice == null
        ? "Outcomes run top-to-bottom when this NPC line appears."
        : "Outcomes run top-to-bottom after the player picks this choice.";
    public bool HasProblems => Problems.Count > 0;
    public bool HasErrors => Problems.Any(problem => problem.IsError);
    public string ValidationSummary => HasErrors
        ? $"{Problems.Count(problem => problem.IsError)} errors"
        : Problems.Count == 0 ? "Everything looks good" : $"{Problems.Count} checks";
    public bool IsDirty => Serialize(_graph) != _savedJson;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Returns a deep copy suitable for background readers such as conversation search. The live
    /// graph stays owned by the UI thread.
    /// </summary>
    public ConversationGraph SnapshotGraph() =>
        JsonConvert.DeserializeObject<ConversationGraph>(Serialize(_graph))
        ?? throw new InvalidOperationException("The conversation snapshot was empty.");

    private ConversationNode? CurrentNode => _selectedNodeId != null && _graph.Nodes.TryGetValue(_selectedNodeId, out var node)
        ? node
        : null;

    partial void OnSelectedBehaviorChanged(ConversationBehaviorOption? value)
    {
        OnPropertyChanged(nameof(IsGeneral));
        if (value?.Kind == ConversationBehaviorKind.Merchant)
            EnsureMerchantStructure();
        RefreshAll(selectFirst: CurrentNode == null);
    }

    partial void OnSelectedOpeningChanged(NuiConversationOpeningRow? value)
    {
        if (_loading || value == null)
            return;
        _selectedOpeningLink = value.Link;
        SelectNode(value.Node.Id);
    }

    partial void OnSelectedChoiceChanged(NuiConversationChoiceRow? value)
    {
        if (_loading)
            return;
        _selectedChoiceLink = value?.Link;
        RebuildOperations();
        OnPropertyChanged(nameof(HasSelectedChoice));
        OnPropertyChanged(nameof(ActionScopeHelp));
    }

    partial void OnSpeakerNameChanged(string value) => EditNode(node => node.SpeakerName = value ?? string.Empty);
    partial void OnSpeakerTagChanged(string value) => EditNode(node => node.SpeakerTag = value ?? string.Empty);
    partial void OnPortraitResrefChanged(string value) => EditNode(node => node.PortraitResref = value ?? string.Empty);
    partial void OnSoundResrefChanged(string value) => EditNode(node => node.SoundResref = value ?? string.Empty);
    partial void OnAnimationChanged(decimal value) => EditNode(node => node.Animation = (uint)Math.Max(0, value));
    partial void OnMerchantGreetingChanged(string value) => EditMerchant((node, _, _) =>
        SetPrimaryText(node, value));
    partial void OnMerchantChoiceTextChanged(string value) => EditMerchant((_, choice, _) =>
        choice.Text.Text = value ?? string.Empty);
    partial void OnMerchantStoreTagChanged(string value) => EditMerchant((_, _, action) =>
    {
        action.Arguments.Clear();
        if (!string.IsNullOrWhiteSpace(value))
            action.Arguments.Add(value);
    });

    [RelayCommand]
    private void SelectOpening(NuiConversationOpeningRow? opening) => SelectedOpening = opening;

    [RelayCommand]
    private void MoveOpeningUp(NuiConversationOpeningRow? opening) => MoveOpening(opening, -1);

    [RelayCommand]
    private void MoveOpeningDown(NuiConversationOpeningRow? opening) => MoveOpening(opening, 1);

    [RelayCommand]
    private void AddOpening()
    {
        Edit(() =>
        {
            var node = CreateNode("New NPC line");
            _graph.EntryPoints.Add(new ConversationLink { TargetNodeId = node.Id });
            _selectedNodeId = node.Id;
            _selectedOpeningLink = _graph.EntryPoints[^1];
        });
    }

    [RelayCommand]
    private void RemoveOpening(NuiConversationOpeningRow? opening)
    {
        if (opening == null || _graph.EntryPoints.Count <= 1)
            return;
        Edit(() => _graph.EntryPoints.Remove(opening.Link));
    }

    [RelayCommand]
    private void AddTextBlock()
    {
        if (CurrentNode == null)
            return;
        Edit(() => CurrentNode.Text.Add(new ConversationTextBlock { Style = ConversationTextStyle.Normal }));
    }

    [RelayCommand]
    private void AddChoice()
    {
        if (CurrentNode == null)
            return;
        Edit(() =>
        {
            var id = NextId("choice", _graph.Choices.Keys);
            var choice = new ConversationChoice
            {
                Id = id,
                EndsConversation = true,
                Text = new ConversationTextBlock
                {
                    Text = "New player choice",
                    Style = ConversationTextStyle.PlayerReply
                }
            };
            _graph.Choices.Add(id, choice);
            var link = new ConversationChoiceLink { ChoiceId = id };
            CurrentNode.Choices.Add(link);
            _selectedChoiceLink = link;
        });
    }

    [RelayCommand]
    private void RemoveChoice(NuiConversationChoiceRow? row)
    {
        if (row == null || CurrentNode == null)
            return;
        Edit(() =>
        {
            CurrentNode.Choices.Remove(row.Link);
            _selectedChoiceLink = null;
            var stillUsed = _graph.Nodes.Values.SelectMany(node => node.Choices)
                .Any(link => link.ChoiceId == row.Choice.Id);
            if (!stillUsed)
                _graph.Choices.Remove(row.Choice.Id);
        });
    }

    [RelayCommand]
    private void MoveChoiceUp(NuiConversationChoiceRow? row) => MoveChoice(row, -1);

    [RelayCommand]
    private void MoveChoiceDown(NuiConversationChoiceRow? row) => MoveChoice(row, 1);

    [RelayCommand]
    private void AddFollowUp(NuiConversationChoiceRow? row)
    {
        if (row == null)
            return;
        Edit(() =>
        {
            var node = CreateNode("New NPC line");
            row.Choice.EndsConversation = false;
            row.Choice.Next.Add(new ConversationLink { TargetNodeId = node.Id });
            _selectedNodeId = node.Id;
            _selectedOpeningLink = null;
            _selectedChoiceLink = null;
        });
    }

    [RelayCommand]
    private void OpenNextLine(NuiConversationChoiceRow? row)
    {
        var next = row?.Choice.Next.FirstOrDefault();
        if (next != null && _graph.Nodes.ContainsKey(next.TargetNodeId))
        {
            _selectedOpeningLink = null;
            SelectNode(next.TargetNodeId);
        }
    }

    [RelayCommand]
    private void AddCondition()
    {
        if (ConditionToAdd == null)
            return;
        var destination = CurrentConditionDestination();
        if (destination == null)
            return;
        Edit(() => destination.Add(new ConversationCondition { Key = ConditionToAdd.Key }));
        ConditionToAdd = null;
    }

    [RelayCommand]
    private void AddAction()
    {
        if (ActionToAdd == null)
            return;
        var destination = CurrentActionDestination();
        if (destination == null)
            return;
        Edit(() => destination.Add(new ConversationAction { Key = ActionToAdd.Key }));
        ActionToAdd = null;
    }

    [RelayCommand]
    private void InsertDynamicText()
    {
        if (DynamicTextToInsert == null || TextBlocks.Count == 0)
            return;
        TextBlocks[0].Text += DynamicTextToInsert.Token;
        DynamicTextToInsert = null;
    }

    [RelayCommand]
    private void StartPreview()
    {
        var first = _graph.EntryPoints.FirstOrDefault(link => _graph.Nodes.ContainsKey(link.TargetNodeId));
        if (first == null)
        {
            PreviewStatus = "No opening line is available.";
            return;
        }
        ShowPreviewNode(first.TargetNodeId);
    }

    [RelayCommand]
    private void PickPreviewChoice(NuiConversationChoiceRow? row)
    {
        if (row == null)
            return;
        if (row.Choice.EndsConversation || row.Choice.Next.Count == 0)
        {
            PreviewStatus = "Conversation ended.";
            PreviewChoices.Clear();
            return;
        }
        ShowPreviewNode(row.Choice.Next[0].TargetNodeId);
    }

    [RelayCommand]
    private async Task Save() => await TrySaveAsync().ConfigureAwait(true);

    public async Task<bool> TrySaveAsync()
    {
        Validate();
        if (HasErrors)
        {
            _log.AppendLine($"Cannot save {_filePath}: fix the conversation errors first.");
            return false;
        }
        if (!IsDirty)
            return true;

        try
        {
            if (File.Exists(_filePath) && File.GetLastWriteTimeUtc(_filePath) != _diskWriteTimeUtc)
            {
                var choice = await _prompts.ConfirmExternalChangeAsync(_filePath).ConfigureAwait(true);
                if (choice == ExternalChangeChoice.Cancel)
                    return false;
                if (choice == ExternalChangeChoice.Reload)
                {
                    _graph = LoadGraph(_filePath);
                    _savedJson = Serialize(_graph);
                    _undo.Clear();
                    _redo.Clear();
                    RefreshAll(selectFirst: true);
                    return true;
                }
            }

            var json = Serialize(_graph);
            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _filePath, true);
            _savedJson = json;
            _diskWriteTimeUtc = File.GetLastWriteTimeUtc(_filePath);
            NotifyHistoryChanged();
            _log.AppendLine($"Saved NUI conversation {_filePath}.");
            return true;
        }
        catch (Exception exception)
        {
            _log.AppendLine($"Save failed for {_filePath}: {exception.Message}");
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo()
    {
        if (_undo.Count == 0)
            return;
        _redo.Push(Serialize(_graph));
        RestoreSnapshot(_undo.Pop());
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo()
    {
        if (_redo.Count == 0)
            return;
        _undo.Push(Serialize(_graph));
        RestoreSnapshot(_redo.Pop());
    }

    internal void ApproveApplicationClose() => _closeApproved = true;

    public override bool OnClose()
    {
        if (!_closeApproved && IsDirty)
        {
            if (!_closePromptOpen)
            {
                _closePromptOpen = true;
                _ = ConfirmCloseAsync();
            }
            return false;
        }
        if (!_disposed)
        {
            _disposed = true;
            Closed?.Invoke(this);
        }
        return base.OnClose();
    }

    private async Task ConfirmCloseAsync()
    {
        try
        {
            var choice = await _prompts.ConfirmCloseAsync(Title ?? _resRef).ConfigureAwait(true);
            if (choice == UnsavedChangesChoice.Cancel)
                return;
            if (choice == UnsavedChangesChoice.Save && !await TrySaveAsync().ConfigureAwait(true))
                return;
            _closeApproved = true;
            CloseRequested?.Invoke(this);
        }
        finally
        {
            _closePromptOpen = false;
        }
    }

    private void EditNode(Action<ConversationNode> mutation)
    {
        if (_loading || CurrentNode == null)
            return;
        EditValue(() => mutation(CurrentNode));
    }

    private void EditMerchant(Action<ConversationNode, ConversationChoice, ConversationAction> mutation)
    {
        if (_loading || !TryGetMerchantParts(out var node, out var choice, out var action))
            return;
        EditValue(() => mutation(node, choice, action));
    }

    private void Edit(Action mutation)
    {
        if (_loading)
            return;
        var before = Serialize(_graph);
        mutation();
        var after = Serialize(_graph);
        if (before == after)
            return;
        _undo.Push(before);
        _redo.Clear();
        RefreshAll(selectFirst: false);
    }

    /// <summary>
    /// Records a field edit without reconstructing the bound row collections. Rebuilding on every
    /// keystroke steals focus from Avalonia's TextBox and was the source of the old editor's laggy,
    /// one-character-at-a-time feel.
    /// </summary>
    private void EditValue(Action mutation)
    {
        if (_loading)
            return;
        var before = Serialize(_graph);
        mutation();
        var after = Serialize(_graph);
        if (before == after)
            return;
        _undo.Push(before);
        _redo.Clear();
        Validate();
        StartPreview();
        NotifyHistoryChanged();
    }

    private void RestoreSnapshot(string json)
    {
        _graph = JsonConvert.DeserializeObject<ConversationGraph>(json)
                 ?? throw new InvalidOperationException("The conversation history snapshot was empty.");
        RefreshAll(selectFirst: _selectedNodeId == null || !_graph.Nodes.ContainsKey(_selectedNodeId));
    }

    private void RefreshAll(bool selectFirst)
    {
        _loading = true;
        try
        {
            var selectedOpeningIndex = _selectedOpeningLink == null ? -1 : _graph.EntryPoints.IndexOf(_selectedOpeningLink);
            OpeningLines.Clear();
            for (var index = 0; index < _graph.EntryPoints.Count; index++)
            {
                var link = _graph.EntryPoints[index];
                if (_graph.Nodes.TryGetValue(link.TargetNodeId, out var node))
                    OpeningLines.Add(new NuiConversationOpeningRow(link, node, index, _graph.EntryPoints.Count));
            }

            if (selectFirst || CurrentNode == null)
            {
                var first = OpeningLines.FirstOrDefault();
                _selectedNodeId = first?.Node.Id;
                _selectedOpeningLink = first?.Link;
                selectedOpeningIndex = 0;
            }

            SelectedOpening = selectedOpeningIndex >= 0 && selectedOpeningIndex < OpeningLines.Count
                ? OpeningLines[selectedOpeningIndex]
                : OpeningLines.FirstOrDefault(row => row.Node.Id == _selectedNodeId);
            RebuildSelectedLine();
            RefreshMerchantFields();
            Validate();
            StartPreview();
        }
        finally
        {
            _loading = false;
        }
        NotifyHistoryChanged();
    }

    private void RebuildSelectedLine()
    {
        TextBlocks.Clear();
        Choices.Clear();
        var node = CurrentNode;
        if (node == null)
            return;

        SpeakerName = node.SpeakerName;
        SpeakerTag = node.SpeakerTag;
        PortraitResref = node.PortraitResref;
        SoundResref = node.SoundResref;
        Animation = node.Animation;
        foreach (var block in node.Text)
            TextBlocks.Add(new NuiConversationTextBlockRow(block, EditValue));

        for (var index = 0; index < node.Choices.Count; index++)
        {
            var link = node.Choices[index];
            if (_graph.Choices.TryGetValue(link.ChoiceId, out var choice))
                Choices.Add(new NuiConversationChoiceRow(link, choice, index, node.Choices.Count, EditValue));
        }
        SelectedChoice = _selectedChoiceLink == null
            ? null
            : Choices.FirstOrDefault(row => ReferenceEquals(row.Link, _selectedChoiceLink));
        RebuildOperations();
        OnPropertyChanged(nameof(HasSelectedLine));
        OnPropertyChanged(nameof(HasSelectedChoice));
    }

    private void RebuildOperations()
    {
        Conditions.Clear();
        Actions.Clear();

        var destination = CurrentConditionDestination();
        if (destination != null)
        {
            foreach (var condition in destination)
            {
                var snippet = _snippets.Find(condition.Key);
                if (snippet == null)
                    continue;
                Conditions.Add(new GraphSnippetEditorViewModel(
                    snippet,
                    condition.Arguments,
                    _argumentOptions,
                    EditValue,
                    row => Edit(() => destination.Remove(condition)),
                    condition.IsNegated,
                    value => condition.IsNegated = value));
            }
        }

        var actionDestination = CurrentActionDestination();
        if (actionDestination == null)
            return;
        foreach (var action in actionDestination)
        {
            var snippet = _snippets.Find(action.Key);
            if (snippet == null)
                continue;
            Actions.Add(new GraphSnippetEditorViewModel(
                snippet,
                action.Arguments,
                _argumentOptions,
                EditValue,
                row => Edit(() => actionDestination.Remove(action)),
                onceMarker: action.OncePerPlayerId,
                setOnceMarker: value => action.OncePerPlayerId = value));
        }
    }

    private IList<ConversationCondition>? CurrentConditionDestination()
    {
        if (_selectedChoiceLink != null)
            return _selectedChoiceLink.Conditions;
        return _selectedOpeningLink?.Conditions;
    }

    private IList<ConversationAction>? CurrentActionDestination()
    {
        if (SelectedChoice != null)
            return SelectedChoice.Choice.Actions;
        return CurrentNode?.OnEnterActions;
    }

    private void SelectNode(string nodeId)
    {
        if (!_graph.Nodes.ContainsKey(nodeId))
            return;
        _selectedNodeId = nodeId;
        _selectedChoiceLink = null;
        _loading = true;
        RebuildSelectedLine();
        _loading = false;
    }

    private void MoveOpening(NuiConversationOpeningRow? opening, int direction)
    {
        if (opening == null)
            return;
        var target = opening.Index + direction;
        if (target < 0 || target >= _graph.EntryPoints.Count)
            return;
        Edit(() =>
        {
            _graph.EntryPoints.RemoveAt(opening.Index);
            _graph.EntryPoints.Insert(target, opening.Link);
            _selectedOpeningLink = opening.Link;
        });
    }

    private void MoveChoice(NuiConversationChoiceRow? row, int direction)
    {
        if (row == null || CurrentNode == null)
            return;
        var target = row.Index + direction;
        if (target < 0 || target >= CurrentNode.Choices.Count)
            return;
        Edit(() =>
        {
            CurrentNode.Choices.RemoveAt(row.Index);
            CurrentNode.Choices.Insert(target, row.Link);
            _selectedChoiceLink = row.Link;
        });
    }

    private void EnsureMerchantStructure()
    {
        if (TryGetMerchantParts(out var existingNode, out var existingChoice, out _))
        {
            var hasGoodbye = existingNode.Choices
                .Where(link => link.ChoiceId != existingChoice.Id)
                .Any(link => _graph.Choices.TryGetValue(link.ChoiceId, out var candidate) &&
                             candidate.EndsConversation &&
                             candidate.Actions.Count == 0);
            if (hasGoodbye)
                return;

            Edit(() => AddMerchantGoodbye(existingNode));
            return;
        }
        Edit(() =>
        {
            ConversationNode node;
            if (_graph.EntryPoints.Count == 0 ||
                !_graph.Nodes.TryGetValue(_graph.EntryPoints[0].TargetNodeId, out node!))
            {
                node = CreateNode("Welcome. What can I get for you?");
                _graph.EntryPoints.Insert(0, new ConversationLink { TargetNodeId = node.Id });
            }

            var storeChoiceId = NextId("choice", _graph.Choices.Keys);
            var storeChoice = new ConversationChoice
            {
                Id = storeChoiceId,
                EndsConversation = true,
                Text = new ConversationTextBlock
                {
                    Text = "Show me what you have for sale.",
                    Style = ConversationTextStyle.PlayerReply
                },
                Actions =
                {
                    new ConversationAction { Key = "action-open-store" }
                }
            };
            _graph.Choices.Add(storeChoiceId, storeChoice);
            node.Choices.Insert(0, new ConversationChoiceLink { ChoiceId = storeChoiceId });

            AddMerchantGoodbye(node);
        });
    }

    private void AddMerchantGoodbye(ConversationNode node)
    {
        var goodbyeId = NextId("choice", _graph.Choices.Keys);
        _graph.Choices.Add(goodbyeId, new ConversationChoice
        {
            Id = goodbyeId,
            EndsConversation = true,
            Text = new ConversationTextBlock
            {
                Text = "Goodbye.",
                Style = ConversationTextStyle.PlayerReply
            }
        });
        node.Choices.Add(new ConversationChoiceLink { ChoiceId = goodbyeId });
    }

    private bool TryGetMerchantParts(
        out ConversationNode node,
        out ConversationChoice choice,
        out ConversationAction action)
    {
        node = null!;
        choice = null!;
        action = null!;
        if (_graph.EntryPoints.Count == 0 ||
            !_graph.Nodes.TryGetValue(_graph.EntryPoints[0].TargetNodeId, out var firstNode))
            return false;
        node = firstNode;

        foreach (var link in node.Choices)
        {
            if (!_graph.Choices.TryGetValue(link.ChoiceId, out var candidate))
                continue;
            var storeAction = candidate.Actions.FirstOrDefault(item =>
                item.Key.Equals("action-open-store", StringComparison.OrdinalIgnoreCase));
            if (storeAction == null)
                continue;
            choice = candidate;
            action = storeAction;
            return true;
        }
        return false;
    }

    private void RefreshMerchantFields()
    {
        if (!TryGetMerchantParts(out var node, out var choice, out var action))
            return;
        MerchantGreeting = NuiConversationText.Summarize(node.Text);
        MerchantChoiceText = choice.Text.Text;
        MerchantStoreTag = action.Arguments.FirstOrDefault() ?? string.Empty;
    }

    private void Validate()
    {
        Problems.Clear();
        foreach (var error in ConversationGraphValidator.Validate(_graph))
            Problems.Add(new NuiConversationProblem(error, true, "Graph"));

        for (var index = 0; index < _graph.EntryPoints.Count - 1; index++)
        {
            if (_graph.EntryPoints[index].Conditions.Count == 0)
            {
                Problems.Add(new NuiConversationProblem(
                    "An unconditional opening appears before another opening; later checks can never run.",
                    true,
                    $"Opening {index + 1}"));
                break;
            }
        }

        foreach (var node in _graph.Nodes.Values)
        {
            if (node.Text.All(block => string.IsNullOrWhiteSpace(block.Text)))
                Problems.Add(new NuiConversationProblem("NPC line has no text.", true, node.Id));
            ValidateConditions(node.Choices.SelectMany(link => link.Conditions), node.Id);
            ValidateActions(node.OnEnterActions, node.Id);
        }
        ValidateConditions(_graph.EntryPoints.SelectMany(link => link.Conditions), "Openings");
        foreach (var choice in _graph.Choices.Values)
        {
            if (!choice.IsAutomatic && string.IsNullOrWhiteSpace(choice.Text.Text))
                Problems.Add(new NuiConversationProblem("Player choice has no text.", true, choice.Id));
            ValidateConditions(choice.Next.SelectMany(link => link.Conditions), choice.Id);
            ValidateActions(choice.Actions, choice.Id);
        }
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private void ValidateConditions(IEnumerable<ConversationCondition> conditions, string location)
    {
        foreach (var condition in conditions)
        {
            var snippet = _snippets.Find(condition.Key);
            if (snippet == null)
                Problems.Add(new NuiConversationProblem($"Unknown check '{condition.Key}'.", true, location));
            else if (!snippet.IsValidArgumentCount(condition.Arguments.Count))
                Problems.Add(new NuiConversationProblem($"Check '{condition.Key}' has incomplete inputs.", true, location));
        }
    }

    private void ValidateActions(IEnumerable<ConversationAction> actions, string location)
    {
        foreach (var action in actions)
        {
            var snippet = _snippets.Find(action.Key);
            if (snippet == null && action.Key != "system.execute-owner-script")
                Problems.Add(new NuiConversationProblem($"Unknown outcome '{action.Key}'.", true, location));
            else if (snippet != null && !snippet.IsValidArgumentCount(action.Arguments.Count))
                Problems.Add(new NuiConversationProblem($"Outcome '{action.Key}' has incomplete inputs.", true, location));
        }
    }

    private void ShowPreviewNode(string nodeId)
    {
        PreviewTextBlocks.Clear();
        PreviewChoices.Clear();
        if (!_graph.Nodes.TryGetValue(nodeId, out var node))
        {
            PreviewStatus = "The next line is missing.";
            return;
        }
        PreviewSpeaker = string.IsNullOrWhiteSpace(node.SpeakerName) ? "NPC" : node.SpeakerName;
        foreach (var block in node.Text)
            PreviewTextBlocks.Add(new NuiConversationTextBlockRow(block, _ => { }));
        for (var index = 0; index < node.Choices.Count; index++)
        {
            var link = node.Choices[index];
            if (_graph.Choices.TryGetValue(link.ChoiceId, out var choice))
                PreviewChoices.Add(new NuiConversationChoiceRow(link, choice, index, node.Choices.Count, _ => { }));
        }
        PreviewStatus = node.Choices.Any(link => link.Conditions.Count > 0)
            ? "Preview shows conditional choices; use Preview State later to hide them."
            : string.Empty;
    }

    private ConversationBehaviorKind DetectBehavior()
    {
        var actionKeys = _graph.Nodes.Values.SelectMany(node => node.OnEnterActions)
            .Concat(_graph.Choices.Values.SelectMany(choice => choice.Actions))
            .Select(action => action.Key)
            .ToArray();
        if (actionKeys.Contains("action-open-store", StringComparer.OrdinalIgnoreCase))
            return ConversationBehaviorKind.Merchant;
        if (actionKeys.Any(key => key.Contains("quest", StringComparison.OrdinalIgnoreCase)))
            return ConversationBehaviorKind.QuestGiver;
        return ConversationBehaviorKind.Conversation;
    }

    private ConversationNode CreateNode(string text)
    {
        var id = NextId("node", _graph.Nodes.Keys);
        var node = new ConversationNode { Id = id };
        node.Text.Add(new ConversationTextBlock { Text = text });
        _graph.Nodes.Add(id, node);
        return node;
    }

    private static void SetPrimaryText(ConversationNode node, string text)
    {
        if (node.Text.Count == 0)
            node.Text.Add(new ConversationTextBlock());
        node.Text[0].Text = text ?? string.Empty;
    }

    private static string NextId(string prefix, IEnumerable<string> existing)
    {
        var used = existing.ToHashSet(StringComparer.Ordinal);
        for (var index = 1; ; index++)
        {
            var candidate = $"{prefix}-{index:D5}";
            if (!used.Contains(candidate))
                return candidate;
        }
    }

    private static ConversationGraph LoadGraph(string path)
    {
        return JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(path))
               ?? throw new InvalidOperationException($"Conversation graph '{path}' is empty.");
    }

    private static string Serialize(ConversationGraph graph) =>
        JsonConvert.SerializeObject(graph, Formatting.Indented);

    private void NotifyHistoryChanged()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        UpdateTitle();
    }

    private void UpdateTitle() => Title = IsDirty ? $"{_resRef} *" : _resRef;
}
