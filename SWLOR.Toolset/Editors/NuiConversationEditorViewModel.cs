using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors;

/// <summary>
/// Graph-native authoring for the NUI conversation window. It edits the same JSON the server embeds;
/// no DLG nodes, generated shells, NWScript dispatchers, or 255-slot allocation participate.
/// </summary>
public sealed partial class NuiConversationEditorViewModel : Document, IEditorDocument
{
    public const string DefaultPreviewPortraitResref = "po_hu_m_01_l";

    private readonly string _filePath;
    private readonly string _resRef;
    private readonly SnippetCatalog _snippets;
    private readonly SnippetArgumentOptions _argumentOptions;
    private readonly OutputLogService _log;
    private readonly IEditorPromptService _prompts;
    private readonly ScriptSession _session;
    private readonly Behaviors.ChoicePreviewService? _choicePreviews;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly HashSet<string> _collapsedTreeBranches = new(StringComparer.Ordinal);
    private ConversationGraph _graph;
    private string _savedJson;
    private string? _selectedNodeId;
    private ConversationLink? _selectedOpeningLink;
    private ConversationLink? _selectedIncomingNodeLink;
    private ConversationChoiceLink? _selectedChoiceLink;
    private bool _loading;
    private bool _closeApproved;
    private bool _closePromptOpen;
    private bool _disposed;
    private int _portraitRequestVersion;

    public event Action<NuiConversationEditorViewModel>? Closed;
    public event Action<NuiConversationEditorViewModel>? CloseRequested;

    public ObservableCollection<ConversationBehaviorOption> BehaviorOptions { get; } = new();
    public ObservableCollection<DynamicTextTokenOption> DynamicTextTokens { get; } = new();
    public ObservableCollection<NuiConversationOpeningRow> OpeningLines { get; } = new();
    public ObservableCollection<NuiConversationTreeRow> TreeRows { get; } = new();
    public ObservableCollection<NuiConversationTextBlockRow> TextBlocks { get; } = new();
    public ObservableCollection<NuiConversationTextBlockRow> AdditionalTextBlocks { get; } = new();
    public ObservableCollection<NuiConversationChoiceRow> Choices { get; } = new();
    public ObservableCollection<GraphSnippetEditorViewModel> Conditions { get; } = new();
    public ObservableCollection<GraphSnippetEditorViewModel> Actions { get; } = new();
    public ObservableCollection<SnippetDescriptor> AvailableConditions { get; } = new();
    public ObservableCollection<SnippetDescriptor> AvailableActions { get; } = new();
    public ObservableCollection<NuiConversationProblem> Problems { get; } = new();
    public ObservableCollection<NuiConversationChoiceRow> PreviewChoices { get; } = new();
    public ObservableCollection<NuiConversationPreviewTextRow> PreviewTextBlocks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMerchant))]
    [NotifyPropertyChangedFor(nameof(IsConversation))]
    private ConversationBehaviorOption? _selectedBehavior;

    [ObservableProperty]
    private NuiConversationOpeningRow? _selectedOpening;

    [ObservableProperty]
    private NuiConversationChoiceRow? _selectedChoice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTreeRow))]
    [NotifyPropertyChangedFor(nameof(IsNpcTreeRowSelected))]
    [NotifyPropertyChangedFor(nameof(IsPlayerTreeRowSelected))]
    [NotifyPropertyChangedFor(nameof(SelectedTreeRowTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedTreeRowHelp))]
    [NotifyPropertyChangedFor(nameof(ConditionSectionTitle))]
    [NotifyPropertyChangedFor(nameof(ConditionSectionHelp))]
    [NotifyPropertyChangedFor(nameof(ActionSectionTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSectionHelp))]
    private NuiConversationTreeRow? _selectedTreeRow;

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

    [ObservableProperty]
    private Bitmap? _previewPortrait;

    [ObservableProperty]
    private string _previewPortraitSourceResref = DefaultPreviewPortraitResref;

    public NuiConversationEditorViewModel(
        string filePath,
        string resRef,
        SnippetCatalog snippets,
        IGameCodeIndex? gameCode,
        OutputLogService log,
        IEditorPromptService prompts,
        Func<string, IReadOnlyList<string>>? tagsFor = null,
        Behaviors.ChoicePreviewService? choicePreviews = null)
    {
        _filePath = filePath;
        _resRef = resRef;
        _snippets = snippets;
        _argumentOptions = new SnippetArgumentOptions(gameCode, tagsFor);
        _log = log;
        _prompts = prompts;
        _choicePreviews = choicePreviews;
        _session = ScriptSession.Open(filePath);
        _graph = LoadGraph(_session.Document.Text, filePath);
        _savedJson = Serialize(_graph);
        Id = $"nui-conversation:{filePath}";

        BehaviorOptions.Add(new ConversationBehaviorOption(
            ConversationBehaviorKind.Merchant,
            "Merchant",
            "Greeting and shop choice; Goodbye is always supplied."));
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
    public bool IsConversation => SelectedBehavior?.Kind == ConversationBehaviorKind.Conversation;
    public bool IsGeneral => !IsMerchant;
    public bool HasSelectedLine => CurrentNode != null;
    public bool HasSelectedChoice => SelectedChoice != null;
    public bool HasSelectedTreeRow => SelectedTreeRow is { IsMissing: false };
    public bool IsNpcTreeRowSelected => SelectedTreeRow?.IsNpc == true;
    public bool IsPlayerTreeRowSelected => SelectedTreeRow?.IsPlayer == true;
    public string SelectedTreeRowTitle => SelectedTreeRow?.IsMissing == true
        ? "Missing route"
        : IsPlayerTreeRowSelected ? "Player response" : "NPC line";
    public string SelectedTreeRowHelp => SelectedTreeRow?.IsMissing == true
        ? "Remove this route or repair its target in the conversation source."
        : IsPlayerTreeRowSelected
            ? "Edit what the player can say and what happens after they choose it."
            : "Edit what the NPC says and what must be true for this route to appear.";
    public string ConditionSectionTitle => IsPlayerTreeRowSelected
        ? "Show this response when…"
        : "Show this line when…";
    public string ConditionSectionHelp => Conditions.Count == 0
        ? "Always shown. Add a check only when this route needs a restriction."
        : "Every check below must pass. They run from top to bottom.";
    public string ActionSectionTitle => IsPlayerTreeRowSelected
        ? "When the player selects it…"
        : "When this line appears…";
    public string ActionSectionHelp => Actions.Count == 0
        ? "Nothing else happens."
        : "These actions run from top to bottom.";
    public string ActionScopeHelp => SelectedChoice == null
        ? "Outcomes run top-to-bottom when this NPC line appears."
        : "Outcomes run top-to-bottom after the player picks this choice.";
    public NuiConversationTextBlockRow? PrimaryTextBlock => TextBlocks.FirstOrDefault();
    public string MoreOptionsHeader => AdditionalTextBlocks.Count == 0
        ? "More options"
        : $"More options · {AdditionalTextBlocks.Count} formatted " +
          (AdditionalTextBlocks.Count == 1 ? "passage" : "passages");
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
        _selectedIncomingNodeLink = value.Link;
        SelectNode(value.Node.Id, value.Link);
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

    partial void OnSelectedTreeRowChanged(NuiConversationTreeRow? value)
    {
        if (_loading || value == null || value.IsMissing)
            return;
        ApplyTreeSelection(value);
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
            var link = new ConversationLink { TargetNodeId = node.Id };
            _graph.EntryPoints.Add(link);
            _selectedNodeId = node.Id;
            _selectedOpeningLink = link;
            _selectedIncomingNodeLink = link;
            _selectedChoiceLink = null;
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
        Edit(() => CurrentNode.Text.Add(new ConversationTextBlock { Style = ConversationTextStyle.Highlight }));
    }

    [RelayCommand]
    private void RemoveTextBlock(NuiConversationTextBlockRow? row)
    {
        if (CurrentNode == null || row == null || CurrentNode.Text.Count <= 1 ||
            ReferenceEquals(row, PrimaryTextBlock))
        {
            return;
        }

        Edit(() => CurrentNode.Text.Remove(row.Block));
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
    private void ToggleTreeBranch(NuiConversationTreeRow? row)
    {
        if (row is not { HasChildren: true })
            return;

        SelectedTreeRow = row;
        if (!_collapsedTreeBranches.Add(row.Key))
            _collapsedTreeBranches.Remove(row.Key);
        RefreshAll(selectFirst: false);
    }

    [RelayCommand]
    private void ExpandAllTreeBranches()
    {
        if (_collapsedTreeBranches.Count == 0)
            return;
        _collapsedTreeBranches.Clear();
        RefreshAll(selectFirst: false);
    }

    [RelayCommand]
    private void CollapseAllTreeBranches()
    {
        var expandableRows = TreeRows.Where(row => row.HasChildren).ToArray();
        if (expandableRows.Length == 0)
            return;
        foreach (var row in expandableRows)
            _collapsedTreeBranches.Add(row.Key);
        RefreshAll(selectFirst: false);
    }

    [RelayCommand]
    private void RemoveTreeRow(NuiConversationTreeRow? row)
    {
        if (row == null)
            return;

        if (row.IsPlayer && row.ParentNode != null && row.ChoiceLink != null && row.Choice != null)
        {
            Edit(() =>
            {
                row.ParentNode.Choices.Remove(row.ChoiceLink);
                _selectedChoiceLink = null;
                var stillUsed = _graph.Nodes.Values.SelectMany(node => node.Choices)
                    .Any(link => link.ChoiceId == row.Choice.Id);
                if (!stillUsed)
                    _graph.Choices.Remove(row.Choice.Id);
                SelectNearestTreeContext(row.ParentNode.Id, row.ParentNodeLink);
            });
            return;
        }

        if (row.IsPlayer || row.NodeLink == null)
            return;
        if (row.IsEntryPoint)
        {
            if (_graph.EntryPoints.Count <= 1)
                return;
            Edit(() =>
            {
                _graph.EntryPoints.Remove(row.NodeLink);
                SelectFirstOpeningContext();
            });
            return;
        }

        if (row.ParentChoice == null)
            return;
        Edit(() =>
        {
            row.ParentChoice.Next.Remove(row.NodeLink);
            row.ParentChoice.EndsConversation = row.ParentChoice.Next.Count == 0;
            _selectedChoiceLink = null;
            SelectFirstOpeningContext();
        });
    }

    [RelayCommand]
    private void AddFollowUp(NuiConversationChoiceRow? row)
    {
        if (row == null)
            return;
        Edit(() =>
        {
            var node = CreateNode("New NPC line");
            row.Choice.EndsConversation = false;
            var link = new ConversationLink { TargetNodeId = node.Id };
            row.Choice.Next.Add(link);
            _selectedNodeId = node.Id;
            _selectedOpeningLink = null;
            _selectedIncomingNodeLink = link;
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
            _selectedIncomingNodeLink = next;
            SelectNode(next.TargetNodeId, next);
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
            if (_session.HasExternalChange())
            {
                var choice = await _prompts.ConfirmExternalChangeAsync(_filePath).ConfigureAwait(true);
                if (choice == ExternalChangeChoice.Cancel)
                    return false;
                if (choice == ExternalChangeChoice.Reload)
                {
                    var reloaded = _session.ReloadFromDisk();
                    _graph = LoadGraph(reloaded.Text, _filePath);
                    _savedJson = Serialize(_graph);
                    _undo.Clear();
                    _redo.Clear();
                    RefreshAll(selectFirst: true);
                    return true;
                }

                // Overwrite accepts the current generation. The compare-and-swap save below still
                // refuses if another writer changes or deletes it after this point.
                _session.RecordCurrentFileState();
            }

            var json = Serialize(_graph);
            var saveBytes = _session.ToBytes(json);
            if (!SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
            {
                _log.AppendLine(
                    $"Cannot save {_filePath}: the file changed on disk while the save was being prepared. " +
                    "Nothing was written - reload or save again.");
                return false;
            }

            _session.MarkSaved(json, saveBytes);
            _savedJson = json;
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
        RefreshTreeDisplay();
        ShowSelectedNodeInPreview();
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
            var selectedTreeKey = SelectedTreeRow?.Key;
            var selectedChoiceId = SelectedTreeRow?.Choice?.Id;
            var selectedParentNodeId = SelectedTreeRow?.ParentNode?.Id;
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
                _selectedIncomingNodeLink = first?.Link;
                _selectedChoiceLink = null;
                selectedOpeningIndex = 0;
            }

            RebuildTreeRows();
            var selectedTreeRow = TreeRows.FirstOrDefault(row =>
                                      row.IsPlayer && ReferenceEquals(row.ChoiceLink, _selectedChoiceLink))
                                  ?? TreeRows.FirstOrDefault(row =>
                                      row.IsPlayer &&
                                      row.Choice?.Id == selectedChoiceId &&
                                      row.ParentNode?.Id == selectedParentNodeId)
                                  ?? TreeRows.FirstOrDefault(row =>
                                      row.IsNpc &&
                                      row.Node?.Id == _selectedNodeId &&
                                      ReferenceEquals(row.NodeLink, _selectedIncomingNodeLink))
                                  ?? TreeRows.FirstOrDefault(row => row.Key == selectedTreeKey)
                                  ?? TreeRows.FirstOrDefault(row => row.IsNpc && row.Node?.Id == _selectedNodeId)
                                  ?? TreeRows.FirstOrDefault(row => !row.IsMissing);
            SelectedTreeRow = selectedTreeRow;
            if (selectedTreeRow != null && !selectedTreeRow.IsMissing)
                ApplyTreeSelectionContext(selectedTreeRow);

            SelectedOpening = selectedOpeningIndex >= 0 && selectedOpeningIndex < OpeningLines.Count
                ? OpeningLines[selectedOpeningIndex]
                : OpeningLines.FirstOrDefault(row => row.Node.Id == _selectedNodeId);
            RebuildSelectedLine();
            RefreshMerchantFields();
            Validate();
            ShowSelectedNodeInPreview();
        }
        finally
        {
            _loading = false;
        }
        NotifyHistoryChanged();
    }

    private void RebuildTreeRows()
    {
        TreeRows.Clear();
        var expandedNodes = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < _graph.EntryPoints.Count; index++)
        {
            AddTreeNodeBranch(
                _graph.EntryPoints[index],
                parentChoice: null,
                depth: 0,
                key: $"entry:{index}",
                index,
                _graph.EntryPoints.Count,
                isEntryPoint: true,
                expandedNodes);
        }
    }

    private void AddTreeNodeBranch(
        ConversationLink link,
        ConversationChoice? parentChoice,
        int depth,
        string key,
        int index,
        int siblingCount,
        bool isEntryPoint,
        ISet<string> expandedNodes)
    {
        if (!_graph.Nodes.TryGetValue(link.TargetNodeId, out var node))
        {
            TreeRows.Add(NuiConversationTreeRow.ForMissingTarget(
                key,
                depth,
                index,
                siblingCount,
                link,
                parentChoice,
                isEntryPoint));
            return;
        }

        var isReference = !expandedNodes.Add(node.Id);
        var hasChildren = !isReference && node.Choices.Any(choice => _graph.Choices.ContainsKey(choice.ChoiceId));
        var isBranchExpanded = !_collapsedTreeBranches.Contains(key);
        TreeRows.Add(NuiConversationTreeRow.ForNpc(
            key,
            depth,
            index,
            siblingCount,
            node,
            link,
            parentChoice,
            isEntryPoint,
            isReference,
            hasChildren,
            isBranchExpanded));
        if (isReference || (hasChildren && !isBranchExpanded))
            return;

        for (var choiceIndex = 0; choiceIndex < node.Choices.Count; choiceIndex++)
        {
            var choiceLink = node.Choices[choiceIndex];
            if (!_graph.Choices.TryGetValue(choiceLink.ChoiceId, out var choice))
                continue;

            var choiceKey = $"{key}/choice:{choiceIndex}";
            var choiceHasChildren = !choice.EndsConversation && choice.Next.Count > 0;
            var choiceIsExpanded = !_collapsedTreeBranches.Contains(choiceKey);
            TreeRows.Add(NuiConversationTreeRow.ForPlayer(
                choiceKey,
                depth + 1,
                choiceIndex,
                node.Choices.Count,
                node,
                link,
                isEntryPoint,
                choice,
                choiceLink,
                choiceHasChildren,
                choiceIsExpanded));

            if (choiceHasChildren && !choiceIsExpanded)
                continue;

            for (var nextIndex = 0; nextIndex < choice.Next.Count; nextIndex++)
            {
                AddTreeNodeBranch(
                    choice.Next[nextIndex],
                    choice,
                    depth + 2,
                    $"{choiceKey}/next:{nextIndex}",
                    nextIndex,
                    choice.Next.Count,
                    isEntryPoint: false,
                    expandedNodes);
            }
        }
    }

    private void RefreshTreeDisplay()
    {
        foreach (var row in TreeRows)
            row.RefreshDisplay();
        OnPropertyChanged(nameof(ConditionSectionHelp));
        OnPropertyChanged(nameof(ActionSectionHelp));
    }

    private void ApplyTreeSelection(NuiConversationTreeRow row)
    {
        _loading = true;
        try
        {
            ApplyTreeSelectionContext(row);
            SelectedOpening = _selectedOpeningLink == null
                ? null
                : OpeningLines.FirstOrDefault(opening => ReferenceEquals(opening.Link, _selectedOpeningLink));
            RebuildSelectedLine();
        }
        finally
        {
            _loading = false;
        }
        ShowSelectedNodeInPreview();
    }

    private void ApplyTreeSelectionContext(NuiConversationTreeRow row)
    {
        if (row.IsNpc && row.Node != null && row.NodeLink != null)
        {
            _selectedNodeId = row.Node.Id;
            _selectedIncomingNodeLink = row.NodeLink;
            _selectedOpeningLink = row.IsEntryPoint ? row.NodeLink : null;
            _selectedChoiceLink = null;
            return;
        }

        if (row.IsPlayer && row.ParentNode != null && row.ChoiceLink != null)
        {
            _selectedNodeId = row.ParentNode.Id;
            _selectedIncomingNodeLink = row.ParentNodeLink;
            _selectedOpeningLink = row.ParentNodeIsEntryPoint ? row.ParentNodeLink : null;
            _selectedChoiceLink = row.ChoiceLink;
        }
    }

    private void RebuildSelectedLine()
    {
        TextBlocks.Clear();
        AdditionalTextBlocks.Clear();
        Choices.Clear();
        var node = CurrentNode;
        if (node == null)
        {
            OnPropertyChanged(nameof(PrimaryTextBlock));
            OnPropertyChanged(nameof(MoreOptionsHeader));
            return;
        }

        SpeakerName = node.SpeakerName;
        SpeakerTag = node.SpeakerTag;
        PortraitResref = node.PortraitResref;
        SoundResref = node.SoundResref;
        Animation = node.Animation;
        foreach (var block in node.Text)
            TextBlocks.Add(new NuiConversationTextBlockRow(block, EditValue));
        foreach (var block in TextBlocks.Skip(1))
            AdditionalTextBlocks.Add(block);
        OnPropertyChanged(nameof(PrimaryTextBlock));
        OnPropertyChanged(nameof(MoreOptionsHeader));

        for (var index = 0; index < node.Choices.Count; index++)
        {
            var link = node.Choices[index];
            if (_graph.Choices.TryGetValue(link.ChoiceId, out var choice))
                Choices.Add(new NuiConversationChoiceRow(
                    link,
                    choice,
                    index,
                    node.Choices.Count,
                    EditValue,
                    structuralEdit: Edit));
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
        if (actionDestination != null)
        {
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
                    row => Edit(() => actionDestination.Remove(action))));
            }
        }
        OnPropertyChanged(nameof(ConditionSectionHelp));
        OnPropertyChanged(nameof(ActionSectionHelp));
    }

    private IList<ConversationCondition>? CurrentConditionDestination()
    {
        if (_selectedChoiceLink != null)
            return _selectedChoiceLink.Conditions;
        return _selectedIncomingNodeLink?.Conditions;
    }

    private IList<ConversationAction>? CurrentActionDestination()
    {
        if (_selectedChoiceLink != null &&
            _graph.Choices.TryGetValue(_selectedChoiceLink.ChoiceId, out var choice))
            return choice.Actions;
        return CurrentNode?.OnEnterActions;
    }

    private void SelectNode(string nodeId, ConversationLink? incomingLink = null)
    {
        if (!_graph.Nodes.ContainsKey(nodeId))
            return;
        var treeRow = TreeRows.FirstOrDefault(row =>
                          row.IsNpc &&
                          row.Node?.Id == nodeId &&
                          (incomingLink == null || ReferenceEquals(row.NodeLink, incomingLink)))
                      ?? TreeRows.FirstOrDefault(row => row.IsNpc && row.Node?.Id == nodeId);
        if (treeRow != null)
        {
            if (ReferenceEquals(SelectedTreeRow, treeRow))
                ApplyTreeSelection(treeRow);
            else
                SelectedTreeRow = treeRow;
            return;
        }
        _selectedNodeId = nodeId;
        _selectedIncomingNodeLink = incomingLink ?? FindIncomingNodeLink(nodeId);
        _selectedChoiceLink = null;
        _loading = true;
        RebuildSelectedLine();
        _loading = false;
        ShowSelectedNodeInPreview();
    }

    private ConversationLink? FindIncomingNodeLink(string nodeId) =>
        _graph.EntryPoints.FirstOrDefault(link => link.TargetNodeId == nodeId)
        ?? _graph.Choices.Values.SelectMany(choice => choice.Next)
            .FirstOrDefault(link => link.TargetNodeId == nodeId);

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

    /// <summary>
    /// A tree drag may only change evaluation order within the exact list that already owns the
    /// route. Dropping never reparents a branch: an opening stays an opening, a response stays on
    /// its NPC line, and a follow-up stays on its player response.
    /// </summary>
    public bool CanDropTreeRow(NuiConversationTreeRow? source, NuiConversationTreeRow? target)
    {
        if (source == null || target == null || ReferenceEquals(source, target))
            return false;

        if (source.IsPlayer || target.IsPlayer)
        {
            return source.IsPlayer &&
                   target.IsPlayer &&
                   ReferenceEquals(source.ParentNode, target.ParentNode) &&
                   ReferenceEquals(source.ParentNodeLink, target.ParentNodeLink);
        }

        if (source.NodeLink == null || target.NodeLink == null)
            return false;
        if (source.IsEntryPoint || target.IsEntryPoint)
            return source.IsEntryPoint && target.IsEntryPoint;
        return source.ParentChoice != null && ReferenceEquals(source.ParentChoice, target.ParentChoice);
    }

    /// <summary>
    /// The compact rows use direction rather than tiny upper/lower hit zones: moving down inserts
    /// after the hovered sibling, while moving up inserts before it. The preview and commit both
    /// consume this answer so the displayed destination is the destination that will be saved.
    /// </summary>
    public bool TreeDropInsertsAfter(NuiConversationTreeRow? source, NuiConversationTreeRow? target) =>
        CanDropTreeRow(source, target) && source!.Index < target!.Index;

    /// <summary>
    /// Commits the insertion slot shown by the tree drag preview. Returns false when the proposed
    /// slot is invalid or would leave the row where it already is.
    /// </summary>
    public bool DropTreeRow(NuiConversationTreeRow? source, NuiConversationTreeRow? target)
    {
        if (!CanDropTreeRow(source, target))
            return false;

        var insertAfter = TreeDropInsertsAfter(source, target);
        var insertionIndex = target!.Index + (insertAfter ? 1 : 0);
        if (source!.Index < insertionIndex)
            insertionIndex--;
        if (insertionIndex == source.Index)
            return false;

        if (source.IsPlayer && source.ParentNode != null && source.ChoiceLink != null)
        {
            Edit(() =>
            {
                source.ParentNode.Choices.RemoveAt(source.Index);
                source.ParentNode.Choices.Insert(insertionIndex, source.ChoiceLink);
                _selectedNodeId = source.ParentNode.Id;
                _selectedIncomingNodeLink = source.ParentNodeLink;
                _selectedOpeningLink = source.ParentNodeIsEntryPoint ? source.ParentNodeLink : null;
                _selectedChoiceLink = source.ChoiceLink;
            });
            return true;
        }

        if (source.IsPlayer || source.NodeLink == null)
            return false;
        var siblings = source.IsEntryPoint ? _graph.EntryPoints : source.ParentChoice?.Next;
        if (siblings == null)
            return false;
        Edit(() =>
        {
            siblings.RemoveAt(source.Index);
            siblings.Insert(insertionIndex, source.NodeLink);
            _selectedNodeId = source.Node?.Id;
            _selectedIncomingNodeLink = source.NodeLink;
            _selectedOpeningLink = source.IsEntryPoint ? source.NodeLink : null;
            _selectedChoiceLink = null;
        });
        return true;
    }

    private void SelectNearestTreeContext(string nodeId, ConversationLink? incomingLink)
    {
        _selectedNodeId = nodeId;
        _selectedIncomingNodeLink = incomingLink;
        _selectedOpeningLink = _graph.EntryPoints.FirstOrDefault(link => ReferenceEquals(link, incomingLink));
        _selectedChoiceLink = null;
    }

    private void SelectFirstOpeningContext()
    {
        var first = _graph.EntryPoints.FirstOrDefault(link => _graph.Nodes.ContainsKey(link.TargetNodeId));
        _selectedNodeId = first?.TargetNodeId;
        _selectedIncomingNodeLink = first;
        _selectedOpeningLink = first;
        _selectedChoiceLink = null;
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
        string ResolveNodeText(string text) => ResolvePreviewText(text, node);

        PreviewSpeaker = string.IsNullOrWhiteSpace(node.SpeakerName) ? "NPC name" : ResolveNodeText(node.SpeakerName);
        RequestPreviewPortrait(ResolveNodeText(node.PortraitResref));
        foreach (var block in node.Text)
        {
            if (!string.IsNullOrWhiteSpace(block.Text))
                PreviewTextBlocks.Add(new NuiConversationPreviewTextRow(block, ResolveNodeText(block.Text)));
        }

        var visibleChoices = new List<(ConversationChoiceLink Link, ConversationChoice Choice)>();
        for (var index = 0; index < node.Choices.Count; index++)
        {
            var link = node.Choices[index];
            if (_graph.Choices.TryGetValue(link.ChoiceId, out var choice) && !choice.IsAutomatic)
                visibleChoices.Add((link, choice));
        }

        for (var index = 0; index < visibleChoices.Count; index++)
        {
            var (link, choice) = visibleChoices[index];
            PreviewChoices.Add(new NuiConversationChoiceRow(
                link,
                choice,
                index,
                visibleChoices.Count,
                _ => { },
                ResolveNodeText));
        }

        if (PreviewChoices.Count == 0)
        {
            var goodbye = new ConversationChoice
            {
                Id = "preview-goodbye",
                EndsConversation = true,
                Text = new ConversationTextBlock
                {
                    Text = "Goodbye.",
                    Style = ConversationTextStyle.PlayerReply
                }
            };
            PreviewChoices.Add(new NuiConversationChoiceRow(
                new ConversationChoiceLink { ChoiceId = goodbye.Id },
                goodbye,
                0,
                1,
                _ => { },
                ResolveNodeText));
        }
        PreviewStatus = node.Choices.Any(link => link.Conditions.Count > 0)
            ? "Conditional responses are shown because no sample player state is selected."
            : string.Empty;
    }

    private void ShowSelectedNodeInPreview()
    {
        if (CurrentNode != null)
        {
            ShowPreviewNode(CurrentNode.Id);
            return;
        }

        StartPreview();
    }

    private static string ResolvePreviewText(string text, ConversationNode node)
    {
        var ownerName = string.IsNullOrWhiteSpace(node.SpeakerName) ||
                        node.SpeakerName.Contains("{{owner.name}}", StringComparison.OrdinalIgnoreCase)
            ? "NPC name"
            : node.SpeakerName;

        return (text ?? string.Empty)
            .Replace("{{player.name}}", "Player", StringComparison.OrdinalIgnoreCase)
            .Replace("{{owner.name}}", ownerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{player.race}}", "Human", StringComparison.OrdinalIgnoreCase)
            .Replace("{{player.gender.boy-girl}}", "boy", StringComparison.OrdinalIgnoreCase)
            .Replace("{{player.gender.sir-madam}}", "sir", StringComparison.OrdinalIgnoreCase);
    }

    private void RequestPreviewPortrait(string portraitResref)
    {
        var requestVersion = ++_portraitRequestVersion;
        PreviewPortrait = null;
        portraitResref = string.IsNullOrWhiteSpace(portraitResref)
            ? DefaultPreviewPortraitResref
            : portraitResref;
        PreviewPortraitSourceResref = portraitResref;

        if (_choicePreviews == null)
            return;

        if (_choicePreviews.Cached(portraitResref, 128) is { } cached)
        {
            PreviewPortrait = cached;
            return;
        }

        _ = LoadPreviewPortraitAsync(portraitResref, requestVersion);
    }

    private async Task LoadPreviewPortraitAsync(string portraitResref, int requestVersion)
    {
        var bitmap = await _choicePreviews!.ResolveAsync(portraitResref, 128).ConfigureAwait(true);
        if (_disposed || requestVersion != _portraitRequestVersion)
            return;

        if (bitmap == null && !portraitResref.Equals(DefaultPreviewPortraitResref, StringComparison.OrdinalIgnoreCase))
        {
            portraitResref = DefaultPreviewPortraitResref;
            bitmap = _choicePreviews.Cached(portraitResref, 128) ??
                     await _choicePreviews.ResolveAsync(portraitResref, 128).ConfigureAwait(true);
            if (_disposed || requestVersion != _portraitRequestVersion)
                return;
            PreviewPortraitSourceResref = portraitResref;
        }

        PreviewPortrait = bitmap;
    }

    private ConversationBehaviorKind DetectBehavior()
    {
        var actionKeys = _graph.Nodes.Values.SelectMany(node => node.OnEnterActions)
            .Concat(_graph.Choices.Values.SelectMany(choice => choice.Actions))
            .Select(action => action.Key)
            .ToArray();
        if (actionKeys.Contains("action-open-store", StringComparer.OrdinalIgnoreCase))
            return ConversationBehaviorKind.Merchant;
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

    private static ConversationGraph LoadGraph(string json, string path)
    {
        return JsonConvert.DeserializeObject<ConversationGraph>(json)
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
