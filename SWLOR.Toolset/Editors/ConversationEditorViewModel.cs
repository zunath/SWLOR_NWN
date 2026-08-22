using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The conversation editor: the dialogue in the shape the player hears it, for one hypothetical
    /// player at a time. Picking a choice walks forward; clicking a line edits it in place.
    /// </summary>
    /// <remarks>
    /// A walk shows one path, so it is blind to the line nobody wrote. The situation rail and the
    /// coverage strip are what answer that, and they are navigation rather than reporting: choosing a
    /// situation sets the pretend player to someone who reaches it, so "what have I not written?" and
    /// "take me there" are the same gesture.
    /// </remarks>
    public partial class ConversationEditorViewModel : Document, IEditorDocument
    {
        private readonly DocumentSession _session;
        private readonly DlgDocument _dialog;
        private readonly ReachabilityEvaluator _evaluator;
        private readonly ConversationAnalyzer _analyzer;
        private readonly SnippetCatalog _snippets;
        private readonly SnippetArgumentOptions _argumentOptions;
        private readonly SnippetArgument? _merchantStoreArgument;
        private readonly IGameCodeIndex? _gameCode;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly string _resRef;

        /// <summary>The conversation's resref (stable, unlike <see cref="Document.Title"/>, which carries a dirty marker).</summary>
        public string ResRef => _resRef;

        /// <summary>
        /// The live in-editor document, including unsaved edits. Dialogue search overlays this over
        /// the on-disk copy so results reflect what the builder currently sees.
        /// </summary>
        public DlgDocument LiveDialog => _dialog;

        /// <summary>Where the walk has been and the player state after entering each NPC line.</summary>
        private readonly List<WalkStep> _trail = new();

        private PretendPlayer _player = new();
        private DlgNode? _currentLine;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;
        private bool _suspendRedraw;
        private DlgLink? _editingLink;
        private DlgNode? _editingNode;
        private string _rulesTitle = string.Empty;
        private bool _initializingMerchantFields;

        private sealed record WalkStep(DlgNode Line, PretendPlayer Player);

        [ObservableProperty]
        private string _lineText = string.Empty;

        [ObservableProperty]
        private bool _showHiddenChoices;

        [ObservableProperty]
        private string _reuseWarning = string.Empty;

        [ObservableProperty]
        private string _walkStatus = string.Empty;

        [ObservableProperty]
        private ConversationBehaviorOption? _selectedBehavior;

        [ObservableProperty]
        private bool _showBehaviorChooser;

        [ObservableProperty]
        private string _merchantGreeting = string.Empty;

        [ObservableProperty]
        private string _merchantChoiceText = string.Empty;

        [ObservableProperty]
        private ArgumentOption? _selectedMerchantStore;

        private bool _merchantStoresLoading;
        private bool _merchantStoresLoaded;

        [ObservableProperty]
        private bool _showAdvanced;

        [ObservableProperty]
        private string _advancedSpeaker = string.Empty;

        [ObservableProperty]
        private string _advancedSound = string.Empty;

        [ObservableProperty]
        private decimal _advancedAnimation;

        [ObservableProperty]
        private string _advancedComment = string.Empty;

        [ObservableProperty]
        private string _advancedScript = string.Empty;

        [ObservableProperty]
        private DynamicTextTokenOption? _selectedDynamicTextToken;

        [ObservableProperty]
        private DynamicTextTokenOption? _selectedMerchantDynamicTextToken;

        /// <summary>
        /// The choice whose guards and consequences are open for editing, if any. Written by hand
        /// rather than generated, because a redraw has to re-point it at the rebuilt row for the
        /// same route WITHOUT rebuilding the editors underneath — which a generated setter's change
        /// hook would do twice.
        /// </summary>
        private ChoiceRowViewModel? _editingChoice;

        public ChoiceRowViewModel? EditingChoice
        {
            get => _editingChoice;
            set
            {
                if (ReferenceEquals(_editingChoice, value))
                    return;

                _editingChoice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditingChoice));
                OnPropertyChanged(nameof(IsEditingRules));
            }
        }

        [ObservableProperty]
        private SnippetDescriptor? _guardToAdd;

        [ObservableProperty]
        private SnippetDescriptor? _consequenceToAdd;

        [ObservableProperty]
        private ArgumentOption? _questToScaffold;

        /// <summary>What laying out the chosen quest would create, shown before it is committed.</summary>
        public ObservableCollection<ScaffoldBeat> ScaffoldPreview { get; } = new();

        public ObservableCollection<SituationRowViewModel> Situations { get; } = new();

        public ObservableCollection<CoverageRowViewModel> Coverage { get; } = new();

        public ObservableCollection<ChoiceRowViewModel> Choices { get; } = new();

        public ObservableCollection<ProblemRowViewModel> Problems { get; } = new();

        public ObservableCollection<QuestPillViewModel> QuestPills { get; } = new();

        /// <summary>The non-quest facts this conversation reads: key items, skills, faction, tutorial.</summary>
        public ObservableCollection<PlayerFactPillViewModel> FactPills { get; } = new();

        /// <summary>Guards on the route into whichever choice or line is being edited.</summary>
        public ObservableCollection<SnippetEditorViewModel> Guards { get; } = new();

        /// <summary>What the line being edited does when reached.</summary>
        public ObservableCollection<SnippetEditorViewModel> Consequences { get; } = new();

        /// <summary>Guards that can be added to the selected route.</summary>
        public ObservableCollection<SnippetDescriptor> AvailableGuards { get; } = new();

        /// <summary>Consequences that can be added to the selected line.</summary>
        public ObservableCollection<SnippetDescriptor> AvailableConsequences { get; } = new();

        public ObservableCollection<ConversationBehaviorOption> BehaviorOptions { get; } = new();

        public ObservableCollection<DynamicTextTokenOption> DynamicTextTokens { get; } = new();

        public ObservableCollection<ArgumentOption> MerchantStores { get; } = new();

        public ObservableCollection<string> CurrentOutcomeSummaries { get; } = new();

        /// <summary>Quests the scaffold can lay out, for the "set up a quest" picker.</summary>
        public ObservableCollection<ArgumentOption> ScaffoldableQuests { get; } = new();

        /// <summary>The trail across the top, most recent last.</summary>
        public ObservableCollection<string> Breadcrumb { get; } = new();

        public bool IsDirty => _session.UndoStack.IsDirty;

        public bool CanUndo => _session.UndoStack.CanUndo;

        public bool CanRedo => _session.UndoStack.CanRedo;

        /// <summary>True when this player hears nothing at all - no opening fits them.</summary>
        public bool HasNoLine => _currentLine == null;

        public bool HasLine => _currentLine != null;

        public bool HasReuseWarning => !string.IsNullOrEmpty(ReuseWarning);

        public int HiddenChoiceCount => Choices.Count(choice => choice.IsHidden);

        // Explicit booleans rather than binding a count straight to IsVisible: an int-to-bool
        // conversion compiles, but what it does at runtime is not something to leave to chance.
        public bool HasHiddenChoices => HiddenChoiceCount > 0;

        public bool HasProblems => Problems.Count > 0;

        public int BlockingProblemCount => Problems.Count(problem => problem.IsBroken);

        public bool HasBlockingProblems => BlockingProblemCount > 0;

        public string ValidationSummary => BlockingProblemCount switch
        {
            0 => "Everything looks good",
            1 => "1 error blocks saving",
            _ => $"{BlockingProblemCount} errors block saving"
        };

        public bool IsMerchant => SelectedBehavior?.Kind == ConversationBehaviorKind.Merchant;

        public bool IsQuestGiver => SelectedBehavior?.Kind == ConversationBehaviorKind.QuestGiver;

        public bool IsConversation => SelectedBehavior?.Kind == ConversationBehaviorKind.Conversation;

        public bool IsOutlineBehavior => IsQuestGiver || IsConversation;

        public bool ShowEditor => !ShowBehaviorChooser;

        public string OutlineTitle => IsQuestGiver ? "Quest moments" : "Opening lines";

        public string OutlineHelp => IsQuestGiver
            ? "Choose the quest moment you want to write."
            : "The first NPC line whose checks pass is used.";

        public bool HasCompetingSituations => Situations.Count > 1;

        public string PreviewLineText => DynamicTextPreview.Resolve(LineText);

        public int ReuseUseCount => _currentLine == null ? 0 : _dialog.IncomingLinks(_currentLine).Count;

        public bool CanRemoveSharedLine => ReuseUseCount > 1;

        public bool HasAdvancedValues => !string.IsNullOrWhiteSpace(AdvancedSpeaker)
                                         || !string.IsNullOrWhiteSpace(AdvancedSound)
                                         || AdvancedAnimation != 0
                                         || !string.IsNullOrWhiteSpace(AdvancedComment)
                                         || !string.IsNullOrWhiteSpace(AdvancedScript);

        public bool CanEditCustomScript => _currentLine != null
                                           && (_currentLine.Actions.Count == 0
                                               || !DlgDocument.IsActionDispatcher(_currentLine.Script));

        public bool CanAddOutcome => _editingNode != null &&
                                     (string.IsNullOrWhiteSpace(_editingNode.Script) ||
                                      DlgDocument.IsActionDispatcher(_editingNode.Script));

        public bool HasCustomActionScriptForOutcomes =>
            _editingNode != null &&
            !string.IsNullOrWhiteSpace(_editingNode.Script) &&
            !DlgDocument.IsActionDispatcher(_editingNode.Script);

        public bool HasCurrentOutcomes => CurrentOutcomeSummaries.Count > 0;

        public string MerchantRequiredOutcome
        {
            get
            {
                var store = FindStoreChoice();
                return store == null ? "Opens the selected shop" : _evaluator.DescribeAction(store.Value.Action);
            }
        }

        public bool HasCoverage => Coverage.Count > 0;

        public bool HasQuestPills => QuestPills.Count > 0;

        public string HiddenChoiceSummary => HiddenChoiceCount switch
        {
            0 => "No choices are hidden here.",
            1 => "1 choice is hidden for this player.",
            _ => $"{HiddenChoiceCount} choices are hidden for this player."
        };

        public event Action<ConversationEditorViewModel>? Closed;

        public event Action<ConversationEditorViewModel>? CloseRequested;

        public event Action? CatalogEntryChanged;

        public ConversationEditorViewModel(
            string filePath,
            string resRef,
            SnippetCatalog snippets,
            IGameCodeIndex? gameCode,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<string, IReadOnlyList<string>>? tagsFor = null)
        {
            _log = log;
            _prompts = prompts;
            _gameCode = gameCode;
            _snippets = snippets;
            _resRef = resRef;
            Id = $"conversation:{filePath}";
            Title = resRef;

            _session = DocumentSession.Open(filePath);
            _dialog = new DlgDocument(_session.Document);
            _evaluator = new ReachabilityEvaluator(snippets, gameCode);
            _analyzer = new ConversationAnalyzer(snippets, _evaluator, gameCode);
            _argumentOptions = new SnippetArgumentOptions(gameCode, tagsFor);

            foreach (var snippet in snippets.Conditions)
                AvailableGuards.Add(snippet);
            foreach (var snippet in snippets.Actions)
                AvailableConsequences.Add(snippet);

            BehaviorOptions.Add(new ConversationBehaviorOption(
                ConversationBehaviorKind.Merchant,
                "Merchant",
                "A greeting, one shop choice, and an automatic Goodbye choice."));
            BehaviorOptions.Add(new ConversationBehaviorOption(
                ConversationBehaviorKind.QuestGiver,
                "Quest giver",
                "Dialogue moments generated from a quest's real steps."));
            BehaviorOptions.Add(new ConversationBehaviorOption(
                ConversationBehaviorKind.Conversation,
                "Conversation",
                "A flexible outline of NPC lines, player choices, and follow-ups."));

            DynamicTextTokens.Add(new DynamicTextTokenOption("Player first name", "<FirstName>"));
            DynamicTextTokens.Add(new DynamicTextTokenOption("Player full name", "<FullName>"));
            DynamicTextTokens.Add(new DynamicTextTokenOption("Player class", "<Class>"));
            DynamicTextTokens.Add(new DynamicTextTokenOption("Day or night", "<Day/Night>"));
            DynamicTextTokens.Add(new DynamicTextTokenOption("Boy or girl", "<Boy/Girl>"));

            MerchantStores.Add(new ArgumentOption(string.Empty, "Nearest store"));
            var storeSnippet = snippets.Find("action-open-store");
            _merchantStoreArgument = storeSnippet?.Arguments.FirstOrDefault();

            if (gameCode != null)
            {
                foreach (var quest in gameCode.Quests.Values.OrderBy(q => q.Name, StringComparer.OrdinalIgnoreCase))
                    ScaffoldableQuests.Add(new ArgumentOption(quest.Id, quest.Name));
            }

            _selectedBehavior = BehaviorOptions.First(option => option.Kind == DetectBehavior());
            ShowBehaviorChooser = IsNewConversationTemplate();

            BuildPlayerControls();
            Redraw();
            RefreshMerchantFields();
            GoToFirstUnwritten();
        }

        private ConversationBehaviorKind DetectBehavior()
        {
            var conditionKeys = _dialog.AllLinks()
                .SelectMany(link => link.Conditions)
                .Select(condition => condition.SnippetKey);
            var actionKeys = _dialog.Entries.Concat(_dialog.Replies)
                .SelectMany(node => node.Actions)
                .Select(action => action.SnippetKey);
            var keys = conditionKeys.Concat(actionKeys).ToList();
            var hasStore = keys.Contains("action-open-store", StringComparer.OrdinalIgnoreCase);
            var hasQuest = keys.Any(key => key.Contains("quest", StringComparison.OrdinalIgnoreCase));

            if (hasStore && !hasQuest)
                return ConversationBehaviorKind.Merchant;
            if (hasQuest && !hasStore)
                return ConversationBehaviorKind.QuestGiver;
            return ConversationBehaviorKind.Conversation;
        }

        private bool IsNewConversationTemplate() =>
            _dialog.Entries.Count == 1
            && _dialog.Replies.Count == 0
            && _dialog.Openings.Count == 1
            && _dialog.Entries[0].Text == ModuleResourceTemplateFactory.PlaceholderEntryText;

        partial void OnSelectedBehaviorChanged(ConversationBehaviorOption? value)
        {
            OnPropertyChanged(nameof(IsMerchant));
            OnPropertyChanged(nameof(IsQuestGiver));
            OnPropertyChanged(nameof(IsConversation));
            OnPropertyChanged(nameof(IsOutlineBehavior));
            OnPropertyChanged(nameof(OutlineTitle));
            OnPropertyChanged(nameof(OutlineHelp));

            CloseRulesEditor();
            ShowAdvanced = false;
            // Behavior-specific fields stay alive for the editor session. In particular, do not
            // reload the merchant form here: switching away and back must restore its draft.
            if (value?.Kind == ConversationBehaviorKind.QuestGiver)
                GoToFirstUnwritten();
            else if (value?.Kind == ConversationBehaviorKind.Conversation)
                StartOver();
        }

        partial void OnLineTextChanged(string value) => OnPropertyChanged(nameof(PreviewLineText));

        [RelayCommand]
        private void ChooseBehavior(ConversationBehaviorOption? behavior)
        {
            if (behavior == null)
                return;

            SelectedBehavior = behavior;
            ShowBehaviorChooser = false;
            OnPropertyChanged(nameof(ShowEditor));
        }

        partial void OnShowBehaviorChooserChanged(bool value) => OnPropertyChanged(nameof(ShowEditor));

        [RelayCommand]
        private void ToggleAdvanced() => ShowAdvanced = !ShowAdvanced;

        [RelayCommand]
        private void InsertDynamicText()
        {
            if (SelectedDynamicTextToken == null)
                return;

            LineText += SelectedDynamicTextToken.Token;
            SelectedDynamicTextToken = null;
        }

        [RelayCommand]
        private void InsertMerchantDynamicText()
        {
            if (SelectedMerchantDynamicTextToken == null)
                return;

            MerchantGreeting += SelectedMerchantDynamicTextToken.Token;
            SelectedMerchantDynamicTextToken = null;
        }

        private static bool IsAuthoringPlaceholder(string text) =>
            text is QuestConversationScaffold.Placeholder or ModuleResourceTemplateFactory.PlaceholderEntryText;

        private static string EditableText(string text) => IsAuthoringPlaceholder(text) ? string.Empty : text;

        private (DlgNode Reply, DlgParam Action)? FindStoreChoice()
        {
            foreach (var reply in _dialog.Replies)
            {
                var action = reply.Actions.FirstOrDefault(candidate =>
                    candidate.SnippetKey.Equals("action-open-store", StringComparison.OrdinalIgnoreCase));
                if (action != null)
                    return (reply, action);
            }

            return null;
        }

        private void RefreshMerchantFields()
        {
            _initializingMerchantFields = true;
            try
            {
                MerchantGreeting = EditableText(_dialog.Openings.FirstOrDefault()?.Target.Text ?? string.Empty);
                var store = FindStoreChoice();
                MerchantChoiceText = store == null ? string.Empty : EditableText(store.Value.Reply.Text);
                var tag = store?.Action.Arguments.FirstOrDefault() ?? string.Empty;
                var selected = MerchantStores.FirstOrDefault(option => option.Value == tag);
                if (selected == null)
                {
                    selected = new ArgumentOption(tag, tag);
                    if (!string.IsNullOrWhiteSpace(tag))
                        MerchantStores.Add(selected);
                }

                SelectedMerchantStore = selected;
            }
            finally
            {
                _initializingMerchantFields = false;
            }

            OnPropertyChanged(nameof(MerchantRequiredOutcome));
        }

        /// <summary>
        /// Loads placed store tags only when the builder opens the picker. Resolving them scans every
        /// area's GIT (hundreds of megabytes in SWLOR), so doing it in the constructor made every
        /// dialogue tab wait for merchant data it usually never uses.
        /// </summary>
        public async Task LoadMerchantStoresAsync()
        {
            if (_merchantStoresLoaded || _merchantStoresLoading || _merchantStoreArgument == null)
                return;

            _merchantStoresLoading = true;
            try
            {
                var options = await Task.Run(() =>
                    _argumentOptions.For(_merchantStoreArgument, Array.Empty<string>())).ConfigureAwait(true);
                if (_disposed)
                    return;

                var selectedValue = SelectedMerchantStore?.Value ?? string.Empty;
                foreach (var option in options)
                {
                    if (MerchantStores.All(existing =>
                            !existing.Value.Equals(option.Value, StringComparison.OrdinalIgnoreCase)))
                    {
                        MerchantStores.Add(option);
                    }
                }

                SelectedMerchantStore = MerchantStores.FirstOrDefault(option =>
                                            option.Value.Equals(selectedValue, StringComparison.OrdinalIgnoreCase))
                                        ?? SelectedMerchantStore;
                _merchantStoresLoaded = true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not load store choices for {_resRef}: {ex.Message}");
            }
            finally
            {
                _merchantStoresLoading = false;
            }
        }

        private void RefreshAdvancedFields()
        {
            if (_currentLine == null)
            {
                AdvancedSpeaker = string.Empty;
                AdvancedSound = string.Empty;
                AdvancedAnimation = 0;
                AdvancedComment = string.Empty;
                AdvancedScript = string.Empty;
            }
            else
            {
                AdvancedSpeaker = _currentLine.Speaker;
                AdvancedSound = _currentLine.Sound;
                AdvancedAnimation = _currentLine.Animation;
                AdvancedComment = _currentLine.Comment;
                AdvancedScript = DlgDocument.IsActionDispatcher(_currentLine.Script)
                    ? string.Empty
                    : _currentLine.Script;
            }

            OnPropertyChanged(nameof(HasAdvancedValues));
            OnPropertyChanged(nameof(CanEditCustomScript));
        }

        // ---------- navigation ----------

        /// <summary>Puts the pretend player somewhere that reaches this situation, and walks there.</summary>
        [RelayCommand]
        private void SelectSituation(SituationRowViewModel? row)
        {
            if (row == null)
                return;

            var model = new SituationModel(_dialog, _evaluator, _gameCode);
            var player = model.PlayerFor(row.Situation);
            if (player == null)
            {
                WalkStatus = $"No player can reach “{row.Title}”, so there is nothing to show.";
                _currentLine = null;
                Choices.Clear();
                Breadcrumb.Clear();
                Breadcrumb.Add(row.Title);
                RefreshLineState();
                return;
            }

            _player = player;
            SyncPillsFromPlayer();
            StartWalk(row.Title);
        }

        /// <summary>Follows a choice, applying whatever it does on the way.</summary>
        [RelayCommand]
        private void PickChoice(ChoiceRowViewModel? choice)
        {
            if (choice == null)
                return;

            if (choice.IsDangling)
            {
                WalkStatus = "That choice points at a line that no longer exists, so it goes nowhere.";
                return;
            }

            _player = _evaluator.ApplyActions(choice.Target, _player);
            SyncPillsFromPlayer();

            var next = _evaluator.ResolveNextLine(choice.Target, _player);
            if (next == null)
            {
                WalkStatus = "That ends the conversation.";
                _currentLine = null;
                Choices.Clear();
                RefreshLineState();
                return;
            }

            Breadcrumb.Add(Shorten(choice.Text));
            EnterLine(next.Target);
        }

        /// <summary>Steps back one line in the walk.</summary>
        [RelayCommand]
        private void Back()
        {
            if (_currentLine == null)
            {
                if (_trail.Count == 0)
                    return;

                _player = _trail[^1].Player.Clone();
                SyncPillsFromPlayer();
                ShowLine(_trail[^1].Line);
                return;
            }

            if (_trail.Count <= 1)
                return;

            _trail.RemoveAt(_trail.Count - 1);
            if (Breadcrumb.Count > 1)
                Breadcrumb.RemoveAt(Breadcrumb.Count - 1);

            _player = _trail[^1].Player.Clone();
            SyncPillsFromPlayer();
            ShowLine(_trail[^1].Line);
        }

        /// <summary>Returns to whichever opening this player actually gets.</summary>
        [RelayCommand]
        private void StartOver()
        {
            var situation = Situations.FirstOrDefault(row => row.IsSelected);
            StartWalk(situation?.Title ?? "Start");
        }

        /// <summary>Sets the pretend player to a fresh character with nothing done.</summary>
        [RelayCommand]
        private void ResetPlayer()
        {
            _player = new PretendPlayer();
            SyncPillsFromPlayer();
            StartWalk("Start");
        }

        /// <summary>
        /// Jumps to the first situation with nothing written in it, or — when there is nothing left
        /// to write — to what a brand-new player hears.
        /// </summary>
        /// <remarks>
        /// Falling back to situation 1 would open a finished conversation on "Finished Field
        /// Tinctures", which is the last thing anybody sees and a strange place to start reading.
        /// The fresh player is where the conversation actually begins.
        /// </remarks>
        [RelayCommand]
        private void GoToFirstUnwritten()
        {
            var next = Situations.FirstOrDefault(row => row.IsEmpty);
            if (next != null)
            {
                SelectSituationCommand.Execute(next);
                return;
            }

            ResetPlayerCommand.Execute(null);
        }

        [RelayCommand]
        private void ToggleHiddenChoices()
        {
            ShowHiddenChoices = !ShowHiddenChoices;
            RefreshChoices();
        }

        // ---------- editing ----------

        /// <summary>Commits the text box back to the line being edited.</summary>
        [RelayCommand]
        private void CommitLine()
        {
            if (_currentLine == null)
                return;

            var node = _currentLine;
            var text = string.IsNullOrWhiteSpace(LineText) && IsAuthoringPlaceholder(node.Text)
                ? node.Text
                : LineText;
            if (text == node.Text)
                return;

            RunEdit("Edit a line", () => node.Text = text);
        }

        /// <summary>
        /// Writes the compact merchant form into an ordinary NWN conversation. The shop outcome is
        /// required and read-only in the guided surface; Goodbye is supplied automatically.
        /// </summary>
        [RelayCommand]
        private void CommitMerchant()
        {
            if (_initializingMerchantFields || !MerchantDraftDiffers())
                return;

            RunEdit("Update merchant dialogue", () => EnsureMerchantStructure());
        }

        private void EnsureMerchantStructure()
        {
            var opening = _dialog.Openings.FirstOrDefault();
            DlgNode greeting;
            if (opening == null)
            {
                greeting = _dialog.AddEntry(QuestConversationScaffold.Placeholder);
                _dialog.AddOpening(greeting);
            }
            else
            {
                greeting = opening.Target;
            }

            greeting.Text = string.IsNullOrWhiteSpace(MerchantGreeting)
                ? QuestConversationScaffold.Placeholder
                : MerchantGreeting;

            var store = FindStoreChoice();
            DlgNode shopChoice;
            DlgParam storeAction;
            if (store == null)
            {
                shopChoice = _dialog.AddReply(QuestConversationScaffold.Placeholder);
                storeAction = shopChoice.AddAction("action-open-store");
            }
            else
            {
                shopChoice = store.Value.Reply;
                storeAction = store.Value.Action;
            }

            shopChoice.Text = string.IsNullOrWhiteSpace(MerchantChoiceText)
                ? QuestConversationScaffold.Placeholder
                : MerchantChoiceText;
            storeAction.Value = SelectedMerchantStore?.Value ?? string.Empty;

            var shopIsLinked = greeting.Links.Any(link =>
                _dialog.HasNode(link.TargetKind, link.TargetIndex)
                && ReferenceEquals(link.Target.Struct, shopChoice.Struct));
            if (!shopIsLinked)
                _dialog.AddLink(greeting, shopChoice, isChild: _dialog.IncomingLinks(shopChoice).Count > 0);

            var hasGoodbye = greeting.Links.Any(link =>
            {
                if (!_dialog.HasNode(link.TargetKind, link.TargetIndex))
                    return false;

                var reply = link.Target;
                return reply.Actions.Count == 0
                       && reply.Links.Count == 0
                       && link.Conditions.Count == 0
                       && string.IsNullOrWhiteSpace(link.Active);
            });
            if (!hasGoodbye)
            {
                var goodbye = _dialog.AddReply("Goodbye.");
                _dialog.AddLink(greeting, goodbye);
            }
        }

        private bool MerchantDraftDiffers()
        {
            var opening = _dialog.Openings.FirstOrDefault();
            if (opening == null)
                return true;

            var expectedGreeting = string.IsNullOrWhiteSpace(MerchantGreeting)
                ? QuestConversationScaffold.Placeholder
                : MerchantGreeting;
            if (opening.Target.Text != expectedGreeting)
                return true;

            var store = FindStoreChoice();
            if (store == null)
                return true;

            var expectedChoice = string.IsNullOrWhiteSpace(MerchantChoiceText)
                ? QuestConversationScaffold.Placeholder
                : MerchantChoiceText;
            if (store.Value.Reply.Text != expectedChoice
                || store.Value.Action.Value != (SelectedMerchantStore?.Value ?? string.Empty))
                return true;

            var greeting = opening.Target;
            var linkedToStore = greeting.Links.Any(link =>
                _dialog.HasNode(link.TargetKind, link.TargetIndex)
                && ReferenceEquals(link.Target.Struct, store.Value.Reply.Struct));
            if (!linkedToStore)
                return true;

            return !greeting.Links.Any(link =>
            {
                if (!_dialog.HasNode(link.TargetKind, link.TargetIndex))
                    return false;

                var reply = link.Target;
                return reply.Actions.Count == 0
                       && reply.Links.Count == 0
                       && link.Conditions.Count == 0
                       && string.IsNullOrWhiteSpace(link.Active);
            });
        }

        /// <summary>
        /// Whether the Advanced panel holds edits not yet written to the current line. The fields
        /// commit on LostFocus, which a keyboard-shortcut save never fires — so the save path asks
        /// this and flushes, instead of silently discarding what was typed.
        /// </summary>
        private bool AdvancedDraftDiffers()
        {
            if (_currentLine == null)
                return false;

            var displayedScript = DlgDocument.IsActionDispatcher(_currentLine.Script)
                ? string.Empty
                : _currentLine.Script;
            return _currentLine.Speaker != AdvancedSpeaker
                   || _currentLine.Sound != AdvancedSound
                   || _currentLine.Animation != ClampedAnimation()
                   || _currentLine.Comment != AdvancedComment
                   || displayedScript != AdvancedScript;
        }

        /// <summary>
        /// The Advanced panel's animation value bounded to its uint storage. The NumericUpDown
        /// constrains normal input, but the property itself is settable to anything, and this runs
        /// on the save path before its try block - it must never throw.
        /// </summary>
        private uint ClampedAnimation()
        {
            return AdvancedAnimation <= 0
                ? 0u
                : AdvancedAnimation >= uint.MaxValue
                    ? uint.MaxValue
                    : decimal.ToUInt32(AdvancedAnimation);
        }

        [RelayCommand]
        private void CommitAdvanced()
        {
            if (_currentLine == null)
                return;

            var line = _currentLine;
            RunEdit("Update advanced dialogue settings", () =>
            {
                line.Speaker = AdvancedSpeaker;
                line.Sound = AdvancedSound;
                line.Animation = ClampedAnimation();
                line.Comment = AdvancedComment;
                if (line.Actions.Count == 0)
                {
                    line.Script = AdvancedScript;
                }
                else if (!DlgDocument.IsActionDispatcher(line.Script))
                {
                    // Imported files can contain both a custom script and snippet parameters,
                    // which means the snippets never run. Clearing the custom field repairs that.
                    line.Script = string.IsNullOrWhiteSpace(AdvancedScript)
                        ? DlgDocument.ActionDispatcher
                        : AdvancedScript;
                }
            });
        }

        /// <summary>Adds NWN's empty reply node without exposing it as a blank author field.</summary>
        [RelayCommand]
        private void ContinueAutomatically()
        {
            if (_currentLine == null || !_currentLine.IsEntry)
                return;

            var line = _currentLine;
            RunEdit("Continue automatically", () =>
            {
                var continuation = _dialog.AddReply(string.Empty);
                var next = _dialog.AddEntry(QuestConversationScaffold.Placeholder);
                _dialog.AddLink(line, continuation);
                _dialog.AddLink(continuation, next);
            });

            var automatic = line.Links.LastOrDefault()?.Target;
            var nextLine = automatic?.Links.LastOrDefault()?.Target;
            if (nextLine != null)
                EnterLine(nextLine);
        }

        [RelayCommand]
        private void EndConversation()
        {
            if (_currentLine == null || !_currentLine.IsEntry)
                return;

            var line = _currentLine;
            var alreadyHasEnding = line.Links.Any(link =>
                _dialog.HasNode(link.TargetKind, link.TargetIndex)
                && link.Target.Links.Count == 0
                && link.Conditions.Count == 0
                && string.IsNullOrWhiteSpace(link.Active));
            if (alreadyHasEnding)
            {
                WalkStatus = "This line already has an ending choice.";
                return;
            }

            RunEdit("Add an ending choice", () =>
            {
                var goodbye = _dialog.AddReply("Goodbye.");
                _dialog.AddLink(line, goodbye);
            });
        }

        /// <summary>Adds an empty choice under the current line, ready to be typed into.</summary>
        [RelayCommand]
        private void AddChoice()
        {
            if (_currentLine == null)
                return;

            var parent = _currentLine;
            RunEdit("Add a choice", () =>
            {
                var reply = _dialog.AddReply(QuestConversationScaffold.Placeholder);
                _dialog.AddLink(parent, reply);
            });
        }

        [RelayCommand]
        private void MoveChoiceUp(ChoiceRowViewModel? choice)
        {
            if (choice == null || !choice.CanMoveUp)
                return;

            RunEdit("Move a player choice up", () =>
                _dialog.MoveLink(choice.Link, choice.Order - 2));
        }

        [RelayCommand]
        private void MoveChoiceDown(ChoiceRowViewModel? choice)
        {
            if (choice == null || !choice.CanMoveDown)
                return;

            RunEdit("Move a player choice down", () =>
                _dialog.MoveLink(choice.Link, choice.Order));
        }

        /// <summary>Commits one inline player-choice text box on focus loss.</summary>
        public void CommitChoiceText(ChoiceRowViewModel? choice, string? text)
        {
            if (choice == null || choice.IsDangling)
                return;

            var value = string.IsNullOrWhiteSpace(text) && IsAuthoringPlaceholder(choice.Target.Text)
                ? choice.Target.Text
                : text ?? string.Empty;
            if (choice.Target.Text == value)
                return;

            RunEdit("Edit a player choice", () => choice.Target.Text = value);
        }

        /// <summary>Adds another top-level NPC alternative. NWN checks these from top to bottom.</summary>
        [RelayCommand]
        private void AddSituation()
        {
            DlgNode? added = null;
            RunEdit("Add an NPC alternative", () =>
            {
                added = _dialog.AddEntry(QuestConversationScaffold.Placeholder);
                _dialog.AddOpening(added);
            });

            if (added != null)
                ShowLine(added);
        }

        /// <summary>Adds the NPC line that follows a player choice which currently ends the talk.</summary>
        [RelayCommand]
        private void AddFollowUp(ChoiceRowViewModel? choice)
        {
            if (choice == null || choice.IsDangling || choice.Target.Links.Count != 0)
                return;

            RunEdit("Add a follow-up line", () =>
            {
                var entry = _dialog.AddEntry(QuestConversationScaffold.Placeholder);
                _dialog.AddLink(choice.Target, entry);
            });
        }

        /// <summary>
        /// Removes a choice. Detaching the route is the cheap, local edit; the line itself only goes
        /// when nothing else reaches it, and the cost of that is reported rather than assumed.
        /// </summary>
        [RelayCommand]
        private void RemoveChoice(ChoiceRowViewModel? choice)
        {
            if (choice == null)
                return;

            // Removing a dangling route IS its repair: there is no target node to weigh the cost
            // of, only the broken link itself to detach.
            if (choice.IsDangling)
            {
                if (ReferenceEquals(EditingChoice, choice))
                    CloseRulesEditor();

                RunEdit("Remove a choice", () => _dialog.RemoveLink(choice.Link));
                WalkStatus = "Removed a broken route that pointed at a missing line.";
                return;
            }

            var link = choice.Link;
            var target = choice.Target;
            var alsoReachedElsewhere = _dialog.IncomingLinks(target).Count > 1;
            var cost = _dialog.EstimateRemoveNode(target);

            if (ReferenceEquals(EditingChoice, choice))
                CloseRulesEditor();

            RunEdit("Remove a choice", () =>
            {
                if (alsoReachedElsewhere)
                {
                    _dialog.RemoveLink(link);
                    return;
                }

                _dialog.RemoveNode(target);
            });

            WalkStatus = alsoReachedElsewhere
                ? "That choice is used elsewhere too, so only this route was removed."
                : DescribeRemovalCost(cost);
        }

        /// <summary>
        /// What a removal disturbed, said plainly after the fact.
        /// </summary>
        /// <remarks>
        /// Routes address lines by position, so deleting one from the middle renumbers everything
        /// after it and rewrites every route that pointed past it. That is correct and unavoidable,
        /// but it is also a large diff appearing from a small action, and a builder reviewing the
        /// change deserves to know why. Undo is one step either way.
        /// </remarks>
        private static string DescribeRemovalCost(DlgRemovalCost cost)
        {
            if (cost.IsLocal)
                return "Removed.";

            var parts = new List<string>();
            if (cost.NodesRenumbered > 0)
                parts.Add($"{cost.NodesRenumbered} later line(s) shifted up");
            if (cost.LinksRewritten > 0)
                parts.Add($"{cost.LinksRewritten} route(s) repointed");

            return parts.Count == 0
                ? "Removed."
                : $"Removed — {string.Join(" and ", parts)}. That is a bigger diff than it looks; undo is one step.";
        }

        /// <summary>
        /// Splits a line that several routes share, so this one can be edited on its own. The copy
        /// keeps everything the original had, including where it leads.
        /// </summary>
        [RelayCommand]
        private void MakeSeparateCopy()
        {
            if (_currentLine == null)
                return;

            var line = _currentLine;
            var route = _trail.Count > 1 ? FindRouteInto(line) : null;
            if (route == null)
            {
                WalkStatus = "Only one route reaches this line, so there is nothing to split.";
                return;
            }

            DlgNode? copy = null;
            RunEdit("Make a separate copy", () =>
            {
                copy = _dialog.DuplicateNode(line);
                _dialog.Retarget(route, copy);
            });

            if (copy != null)
            {
                if (_trail.Count > 0)
                    _trail[^1] = new WalkStep(copy, _player.Clone());
                ShowLine(copy);
            }
        }

        private DlgLink? RouteIntoCurrentLine()
        {
            if (_currentLine == null)
                return null;

            var nested = FindRouteInto(_currentLine);
            if (nested != null)
                return nested;

            return Situations
                .Where(row => row.IsSelected)
                .Select(row => row.Situation.Opening)
                .FirstOrDefault(opening =>
                    _dialog.HasNode(opening.TargetKind, opening.TargetIndex)
                    && ReferenceEquals(opening.Target.Struct, _currentLine.Struct));
        }

        /// <summary>Detaches only the current incoming route from a line used in several places.</summary>
        [RelayCommand]
        private void RemoveCurrentLineFromHere()
        {
            if (_currentLine == null || _dialog.IncomingLinks(_currentLine).Count <= 1)
                return;

            var route = RouteIntoCurrentLine();
            if (route == null)
                return;

            RunEdit("Remove shared line from here", () => _dialog.RemoveLink(route));
            WalkStatus = "Removed from this location. The shared line remains everywhere else it is used.";
            StartWalk(Situations.FirstOrDefault(row => row.IsSelected)?.Title ?? "Start");
        }

        /// <summary>Deletes a shared node and all incoming routes only after an explicit warning.</summary>
        [RelayCommand]
        private async Task DeleteCurrentLineEverywhere()
        {
            if (_currentLine == null)
                return;

            var line = _currentLine;
            var useCount = _dialog.IncomingLinks(line).Count;
            if (useCount == 0)
                return;

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                "Delete this line everywhere?",
                $"This line is used in {useCount} place(s). Its text, outcomes, and every route to it will be removed.",
                "Delete everywhere").ConfigureAwait(true);
            if (!confirmed)
                return;

            RunEdit("Delete shared line everywhere", () => _dialog.RemoveNode(line));
            WalkStatus = "Deleted the shared line everywhere it was used.";
            StartWalk("Start");
        }

        [RelayCommand]
        private void GoToProblem(ProblemRowViewModel? row)
        {
            if (row?.Problem.Situation != null)
            {
                var situation = Situations.FirstOrDefault(candidate =>
                    ReferenceEquals(candidate.Situation.Opening.Struct, row.Problem.Situation.Opening.Struct));
                if (situation != null)
                    SelectSituation(situation);
                return;
            }

            if (row?.Problem.Node != null && _dialog.IndexOf(row.Problem.Node) >= 0)
            {
                ShowLine(row.Problem.Node);
                return;
            }

            if (row?.Problem.Link?.Parent?.IsEntry == true)
                ShowLine(row.Problem.Link.Parent);
        }

        /// <summary>Applies only fixes whose intended result is mechanically unambiguous.</summary>
        [RelayCommand]
        private void FixProblem(ProblemRowViewModel? row)
        {
            if (row == null)
                return;

            if (row.Problem.RuleId == "conditional-choice-no-fallback" && row.Problem.Node?.IsEntry == true)
            {
                var line = row.Problem.Node;
                RunEdit("Add a fallback choice", () =>
                {
                    var goodbye = _dialog.AddReply("Goodbye.");
                    _dialog.AddLink(line, goodbye);
                });
                WalkStatus = "Added an unconditional Goodbye choice.";
                return;
            }

            if (row.Problem.RuleId != "unreachable-opening" || row.Problem.Situation == null)
                return;

            var openings = _dialog.Openings;
            var targetIndex = openings.ToList().FindIndex(opening =>
                ReferenceEquals(opening.Struct, row.Problem.Situation.Opening.Struct));
            if (targetIndex < 0)
                return;

            var fallbackIndex = openings.Take(targetIndex).ToList().FindIndex(opening =>
                opening.Conditions.Count == 0 && string.IsNullOrWhiteSpace(opening.Active));
            if (fallbackIndex < 0)
                return;

            RunEdit("Move fallback last", () => _dialog.MoveOpening(fallbackIndex, openings.Count - 1));
            WalkStatus = "Moved the unconditional fallback to the end of NWN's check order.";
        }

        /// <summary>Moves a situation one place up the order, which is how a dead one is revived.</summary>
        [RelayCommand]
        private void MoveSituationUp(SituationRowViewModel? row)
        {
            if (row == null || row.Order <= 1)
                return;

            var order = row.Order;
            RunEdit("Move a situation up", () => _dialog.MoveOpening(order - 1, order - 2));
        }

        /// <summary>Moves a situation one place down the order.</summary>
        [RelayCommand]
        private void MoveSituationDown(SituationRowViewModel? row)
        {
            if (row == null || row.Order >= Situations.Count)
                return;

            var order = row.Order;
            RunEdit("Move a situation down", () => _dialog.MoveOpening(order - 1, order));
        }

        // ---------- guards and consequences ----------

        /// <summary>Opens the guards and consequences of a choice for editing.</summary>
        [RelayCommand]
        private void EditChoice(ChoiceRowViewModel? choice)
        {
            if (choice == null || ReferenceEquals(EditingChoice, choice))
            {
                CloseRulesEditor();
                return;
            }

            // A dangling route has no target node to hang actions on; removal is its one repair.
            if (choice.IsDangling)
                return;

            EditingChoice = choice;
            _editingLink = choice.Link;
            _editingNode = choice.Target;
            RulesTitle = "THIS CHOICE";
            RefreshSnippetEditors();
        }

        /// <summary>Edits the guard and NPC actions attached to a conversation opening.</summary>
        [RelayCommand]
        private void EditSituation(SituationRowViewModel? row)
        {
            if (row == null)
                return;

            EditingChoice = null;
            _editingLink = row.Situation.Opening;
            _editingNode = row.Situation.Opening.Target;
            RulesTitle = row.Title;
            RefreshSnippetEditors();
        }

        /// <summary>Edits the route into, and actions on, the NPC line currently being shown.</summary>
        [RelayCommand]
        private void EditCurrentLine()
        {
            if (_currentLine == null)
                return;

            EditingChoice = null;
            _editingLink = FindRouteInto(_currentLine)
                           ?? Situations.FirstOrDefault(row => row.IsSelected)?.Situation.Opening;
            _editingNode = _currentLine;
            RulesTitle = "THIS NPC LINE";
            RefreshSnippetEditors();
        }

        /// <summary>Opens only the shop choice's optional outcomes; opening the shop stays required.</summary>
        [RelayCommand]
        private void EditMerchantActions()
        {
            if (MerchantDraftDiffers())
                RunEdit("Finish merchant dialogue", EnsureMerchantStructure);

            var store = FindStoreChoice();
            if (store == null)
                return;

            EditingChoice = null;
            _editingLink = _dialog.AllLinks().FirstOrDefault(link =>
                _dialog.HasNode(link.TargetKind, link.TargetIndex)
                && ReferenceEquals(link.Target.Struct, store.Value.Reply.Struct));
            _editingNode = store.Value.Reply;
            RulesTitle = "WHEN THE PLAYER CHOOSES THE SHOP";
            RefreshSnippetEditors();
        }

        [RelayCommand]
        private void CloseRulesEditor()
        {
            EditingChoice = null;
            _editingLink = null;
            _editingNode = null;
            RulesTitle = string.Empty;
            RefreshSnippetEditors();
        }

        [RelayCommand]
        private void AddGuard()
        {
            var link = _editingLink;
            if (link == null || GuardToAdd == null)
                return;

            var snippet = GuardToAdd;
            if (link.Conditions.Any(condition =>
                    condition.SnippetKey.Equals(snippet.Key, StringComparison.OrdinalIgnoreCase)))
            {
                WalkStatus = "That check is already attached here. Change its existing details instead.";
                return;
            }

            RunEdit($"Add a condition", () => link.AddCondition(snippet.Key));
            GuardToAdd = null;
        }

        [RelayCommand]
        private void AddConsequence()
        {
            var node = _editingNode;
            if (node == null || ConsequenceToAdd == null)
                return;

            if (!CanAddOutcome)
            {
                WalkStatus = "Clear the custom action script before adding an outcome.";
                return;
            }

            var snippet = ConsequenceToAdd;
            if (node.Actions.Any(action =>
                    action.SnippetKey.Equals(snippet.Key, StringComparison.OrdinalIgnoreCase)))
            {
                WalkStatus = "That outcome is already attached here. NWN supports one value per outcome type.";
                return;
            }

            if (RunEdit($"Add an effect", () => node.AddAction(snippet.Key)))
                ConsequenceToAdd = null;
        }

        /// <summary>
        /// Writes an edited guard or consequence back. Both key and value are rewritten, because
        /// toggling the negation changes the key and the whole point of the '!' toggle is that a
        /// writer never sees that.
        /// </summary>
        private void CommitSnippet(SnippetEditorViewModel editor)
        {
            var (key, value) = editor.ToParam();
            if (editor.Param.Key == key && editor.Param.Value == value)
                return;

            RunEdit("Change a condition", () =>
            {
                editor.Param.Key = key;
                editor.Param.Value = value;
            });
        }

        private void RemoveGuard(SnippetEditorViewModel editor)
        {
            var link = _editingLink;
            if (link == null)
                return;

            RunEdit("Remove a condition", () => link.RemoveCondition(editor.Param));
        }

        private void RemoveConsequence(SnippetEditorViewModel editor)
        {
            var node = _editingNode;
            if (node == null)
                return;

            RunEdit("Remove an effect", () => node.RemoveAction(editor.Param));
        }

        private void RefreshSnippetEditors()
        {
            Guards.Clear();
            Consequences.Clear();
            OnPropertyChanged(nameof(CanAddOutcome));
            OnPropertyChanged(nameof(HasCustomActionScriptForOutcomes));

            if (_editingLink == null && _editingNode == null)
            {
                OnPropertyChanged(nameof(IsEditingChoice));
                OnPropertyChanged(nameof(IsEditingRules));
                return;
            }

            foreach (var condition in _editingLink?.Conditions ?? Array.Empty<DlgParam>())
            {
                var snippet = _snippets.Find(condition.Key);
                if (snippet != null)
                {
                    Guards.Add(new SnippetEditorViewModel(
                        condition, snippet, _argumentOptions, canNegate: true, CommitSnippet, RemoveGuard, _evaluator.DisplayValue));
                }
            }

            foreach (var action in _editingNode?.Actions ?? Array.Empty<DlgParam>())
            {
                if (action.IsOncePerPlayerMarker)
                    continue;

                var snippet = _snippets.Find(action.Key);
                if (snippet != null)
                {
                    Consequences.Add(new SnippetEditorViewModel(
                        action, snippet, _argumentOptions, canNegate: false, CommitSnippet, RemoveConsequence,
                        _evaluator.DisplayValue,
                        canRemove: !(IsMerchant && action.SnippetKey.Equals("action-open-store", StringComparison.OrdinalIgnoreCase))));
                }
            }

            OnPropertyChanged(nameof(IsEditingChoice));
            OnPropertyChanged(nameof(IsEditingRules));
        }

        public bool IsEditingChoice => EditingChoice != null;

        public bool IsEditingRules => _editingLink != null || _editingNode != null;

        public string RulesTitle
        {
            get => _rulesTitle;
            private set
            {
                if (_rulesTitle == value)
                    return;
                _rulesTitle = value;
                OnPropertyChanged();
            }
        }

        // ---------- the scaffold ----------

        partial void OnQuestToScaffoldChanged(ArgumentOption? value)
        {
            ScaffoldPreview.Clear();
            if (value != null && _gameCode != null)
            {
                foreach (var beat in new QuestConversationScaffold(_gameCode).Preview(value.Value, _dialog))
                    ScaffoldPreview.Add(beat);
            }

            OnPropertyChanged(nameof(CanScaffold));
        }

        public bool CanScaffold => ScaffoldPreview.Count > 0;

        /// <summary>Lays out the situations a quest giver needs, above whatever is already here.</summary>
        [RelayCommand]
        private void ScaffoldQuest()
        {
            var questId = QuestToScaffold?.Value;
            if (_gameCode == null || string.IsNullOrWhiteSpace(questId) || _gameCode.FindQuest(questId) == null)
                return;

            var scaffold = new QuestConversationScaffold(_gameCode);
            RunEdit($"Set up {QuestToScaffold!.Label}", () => scaffold.Apply(_dialog, questId));

            QuestToScaffold = null;
            RebuildPlayerControls();
            GoToFirstUnwritten();
        }

        /// <summary>
        /// Rebuilds the pretend-player row after an edit that can introduce new quests or guards.
        /// Only the scaffold does that today, so this is not on the ordinary redraw path — rebuilding
        /// the controls on every keystroke would reset whatever the writer had set them to.
        /// </summary>
        private void RebuildPlayerControls()
        {
            _suspendRedraw = true;
            try
            {
                QuestPills.Clear();
                FactPills.Clear();
                BuildPlayerControls();
            }
            finally
            {
                _suspendRedraw = false;
            }

            SyncPillsFromPlayer();
            OnPropertyChanged(nameof(HasQuestPills));
        }

        private DlgLink? FindRouteInto(DlgNode line)
        {
            var parent = _trail.Count > 1 ? _trail[^2].Line : null;
            if (parent == null)
                return null;

            foreach (var choiceLink in parent.Links)
            {
                if (!_dialog.HasNode(choiceLink.TargetKind, choiceLink.TargetIndex))
                    continue;

                var reply = choiceLink.Target;
                var route = reply.Links.FirstOrDefault(link =>
                    _dialog.HasNode(link.TargetKind, link.TargetIndex)
                    && ReferenceEquals(link.Target.Struct, line.Struct));
                if (route != null)
                    return route;
            }

            return null;
        }

        // ---------- redraw ----------

        private void Redraw()
        {
            if (_suspendRedraw)
                return;

            var model = new SituationModel(_dialog, _evaluator, _gameCode);
            var selectedOrder = Situations.FirstOrDefault(row => row.IsSelected)?.Order;

            Situations.Clear();
            var situations = model.Situations();
            foreach (var situation in situations)
            {
                var row = new SituationRowViewModel(situation)
                {
                    IsSelected = situation.Order == selectedOrder,
                    HasCompetingLines = situations.Count > 1
                };
                Situations.Add(row);
            }

            Coverage.Clear();
            foreach (var quest in model.Coverage())
                Coverage.Add(new CoverageRowViewModel(quest));

            Problems.Clear();
            foreach (var problem in _analyzer.Analyze(_dialog)
                         .OrderBy(problem => problem.Severity))
                Problems.Add(new ProblemRowViewModel(problem));

            RefreshChoices();
            RestoreEditingChoice();
            OnPropertyChanged(nameof(HasProblems));
            OnPropertyChanged(nameof(BlockingProblemCount));
            OnPropertyChanged(nameof(HasBlockingProblems));
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(HasCoverage));
            OnPropertyChanged(nameof(HasQuestPills));
            OnPropertyChanged(nameof(HasCompetingSituations));
        }

        /// <summary>
        /// Re-points the open guard/consequence panel at the rebuilt row for the same route, so an
        /// edit made inside it does not close it. Matched on the link's struct rather than on the
        /// row, because every redraw makes new rows.
        /// </summary>
        private void RestoreEditingChoice()
        {
            if (EditingChoice == null)
            {
                RefreshSnippetEditors();
                return;
            }

            var previous = EditingChoice.Link.Struct;
            var restored = Choices.FirstOrDefault(choice => ReferenceEquals(choice.Link.Struct, previous));
            if (restored == null)
            {
                CloseRulesEditor();
                return;
            }

            EditingChoice = restored;
            RefreshSnippetEditors();
        }

        private void StartWalk(string situationTitle)
        {
            var opening = _evaluator.ResolveOpening(_dialog, _player);
            Breadcrumb.Clear();
            Breadcrumb.Add(situationTitle);
            _trail.Clear();

            if (opening == null)
            {
                WalkStatus = "No opening fits this player, so the conversation would not start.";
                _currentLine = null;
                Choices.Clear();
                RefreshLineState();
                return;
            }

            EnterLine(opening.Target);

            var reached = Situations.FirstOrDefault(row =>
                ReferenceEquals(row.Situation.Opening.Struct, opening.Struct));
            foreach (var row in Situations)
                row.IsSelected = ReferenceEquals(row, reached);
        }

        private void EnterLine(DlgNode line)
        {
            _player = _evaluator.ApplyActions(line, _player);
            SyncPillsFromPlayer();
            _trail.Add(new WalkStep(line, _player.Clone()));
            ShowLine(line);
        }

        private void ShowLine(DlgNode line)
        {
            _currentLine = line;
            LineText = EditableText(line.Text);
            WalkStatus = string.Empty;

            var incoming = _dialog.IncomingLinks(line).Count;
            ReuseWarning = incoming > 1
                ? $"Used in {incoming} places. Editing its text or outcomes changes all of them."
                : string.Empty;

            RefreshChoices();
            RefreshCurrentOutcomes();
            RefreshAdvancedFields();
            RefreshLineState();
        }

        private void RefreshCurrentOutcomes()
        {
            CurrentOutcomeSummaries.Clear();
            if (_currentLine != null)
            {
                foreach (var action in _currentLine.Actions.Where(action => !action.IsOncePerPlayerMarker))
                    CurrentOutcomeSummaries.Add(_evaluator.DescribeAction(action));
            }

            OnPropertyChanged(nameof(HasCurrentOutcomes));
        }

        private void RefreshChoices()
        {
            Choices.Clear();
            if (_currentLine == null || !_currentLine.IsEntry)
            {
                NotifyChoiceCounts();
                return;
            }

            var number = 1;
            var links = _currentLine.Links;
            for (var linkIndex = 0; linkIndex < links.Count; linkIndex++)
            {
                var link = links[linkIndex];
                var order = linkIndex + 1;
                // An imported or externally edited DLG can carry a link whose index is outside
                // ReplyList; dereferencing Target would throw before the tab could render at all.
                // The row is shown (never hidden) so the builder can see the broken route and
                // remove it.
                if (!_dialog.HasNode(link.TargetKind, link.TargetIndex))
                {
                    Choices.Add(new ChoiceRowViewModel(
                        link, "!",
                        $"broken route — reply #{link.TargetIndex} does not exist",
                        null, order, links.Count, isDangling: true));
                    continue;
                }

                var reachability = _evaluator.Evaluate(link, _player);
                var hiddenBecause = reachability.IsOpen
                    ? null
                    : string.Join(", and ", reachability.Guards
                        .Where(guard => guard.Outcome == GuardOutcome.Fails)
                        .Select(guard => guard.Sentence));

                if (hiddenBecause != null && !ShowHiddenChoices)
                {
                    Choices.Add(new ChoiceRowViewModel(
                        link, "—", string.Empty, hiddenBecause, order, links.Count));
                    continue;
                }

                Choices.Add(new ChoiceRowViewModel(
                    link,
                    hiddenBecause == null ? $"{number++}." : "—",
                    DescribeConsequence(link.Target),
                    hiddenBecause,
                    order,
                    links.Count));
            }

            // The hidden ones are kept in the collection so the count is honest; the view filters
            // them out until the writer asks to see them.
            NotifyChoiceCounts();
        }

        private void NotifyChoiceCounts()
        {
            OnPropertyChanged(nameof(HiddenChoiceCount));
            OnPropertyChanged(nameof(HasHiddenChoices));
            OnPropertyChanged(nameof(HiddenChoiceSummary));
        }

        private string DescribeConsequence(DlgNode reply)
        {
            var effects = reply.Actions
                .Where(action => !action.IsOncePerPlayerMarker)
                .Select(action => _evaluator.DescribeAction(action))
                .ToList();
            if (effects.Count > 0)
                return string.Join("; ", effects);

            // A line can run its own NWScript instead of a snippet. Saying "just talk" there would
            // be a lie about a choice that does something, so it is named instead - even though
            // there is nothing the editor can tell the writer about what it does.
            if (!string.IsNullOrEmpty(reply.Script) && !DlgDocument.IsActionDispatcher(reply.Script))
                return $"runs the script {reply.Script}";

            return reply.Links.Count == 0 ? "ends the talk" : "just talk";
        }

        private void RefreshLineState()
        {
            OnPropertyChanged(nameof(HasLine));
            OnPropertyChanged(nameof(HasNoLine));
            OnPropertyChanged(nameof(HasReuseWarning));
            OnPropertyChanged(nameof(ReuseUseCount));
            OnPropertyChanged(nameof(CanRemoveSharedLine));
            OnPropertyChanged(nameof(PreviewLineText));
            OnPropertyChanged(nameof(HasAdvancedValues));
            OnPropertyChanged(nameof(CanEditCustomScript));
        }

        /// <summary>
        /// Builds one control per thing this conversation actually asks about the player. Built from
        /// the conversation rather than from the whole game, so an NPC that checks one key item gets
        /// one checkbox — and a conversation guarded on something with no control is one a writer
        /// could not navigate at all.
        /// </summary>
        private void BuildPlayerControls()
        {
            var model = new SituationModel(_dialog, _evaluator, _gameCode);

            foreach (var questId in model.MentionedQuestIds().Distinct(StringComparer.Ordinal))
            {
                var quest = _gameCode?.FindQuest(questId);
                QuestPills.Add(new QuestPillViewModel(
                    questId,
                    quest?.Name ?? questId,
                    quest?.StateCount ?? 1,
                    QuestPillViewModel.NotStarted,
                    OnPillChanged));
            }

            foreach (var keyItem in Mentioned(model, SnippetArgumentType.KeyItemId))
            {
                FactPills.Add(new PlayerFactPillViewModel(
                    PlayerFactKind.KeyItem, keyItem, KeyItemName(keyItem), OnPillChanged));
            }

            foreach (var skill in Mentioned(model, SnippetArgumentType.SkillId))
                FactPills.Add(new PlayerFactPillViewModel(PlayerFactKind.Skill, skill, skill, OnPillChanged));

            foreach (var faction in Mentioned(model, SnippetArgumentType.FactionId))
            {
                var name = int.TryParse(faction, out var id) && _gameCode != null
                           && _gameCode.Factions.TryGetValue(id, out var factionName)
                    ? factionName
                    : $"Faction {faction}";

                if (model.Uses("condition-has-faction-standing"))
                {
                    FactPills.Add(new PlayerFactPillViewModel(
                        PlayerFactKind.FactionStanding, faction, $"{name} standing", OnPillChanged));
                }

                if (model.Uses("condition-has-faction-points"))
                {
                    FactPills.Add(new PlayerFactPillViewModel(
                        PlayerFactKind.FactionPoints, faction, $"{name} points", OnPillChanged));
                }
            }

            if (model.Uses("condition-has-completed-tutorial"))
            {
                FactPills.Add(new PlayerFactPillViewModel(
                    PlayerFactKind.Tutorial, "tutorial", "Finished the tutorial", OnPillChanged));
            }
        }

        private IEnumerable<string> Mentioned(SituationModel model, SnippetArgumentType type) =>
            model.MentionedArguments(_snippets, type).Distinct(StringComparer.OrdinalIgnoreCase);

        private string KeyItemName(string value)
        {
            if (_gameCode == null)
                return value;

            return int.TryParse(value, out var id) && _gameCode.KeyItems.TryGetValue(id, out var name)
                ? name
                : value;
        }

        private void OnPillChanged()
        {
            if (_suspendRedraw)
                return;

            _player = new PretendPlayer();
            foreach (var pill in QuestPills)
                _player.WithQuest(pill.QuestId, pill.ToProgress());
            foreach (var pill in FactPills)
                pill.ApplyTo(_player);

            StartWalk(Situations.FirstOrDefault(row => row.IsSelected)?.Title ?? "Start");
        }

        /// <summary>Pushes the current pretend player back onto the pills, without re-triggering them.</summary>
        private void SyncPillsFromPlayer()
        {
            _suspendRedraw = true;
            try
            {
                foreach (var pill in QuestPills)
                {
                    var progress = _player.GetQuest(pill.QuestId);
                    pill.SelectedOption = progress.IsCompleted
                        ? QuestPillViewModel.Finished
                        : progress.CurrentState != null
                            ? $"on step {progress.CurrentState}"
                            : QuestPillViewModel.NotStarted;
                }

                foreach (var pill in FactPills)
                    pill.ReadFrom(_player);
            }
            finally
            {
                _suspendRedraw = false;
            }
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(blank)";

            var trimmed = text.Trim();
            return trimmed.Length <= 28 ? trimmed : trimmed[..25].TrimEnd() + "…";
        }

        // ---------- document plumbing ----------

        private bool RunEdit(string description, Action mutation)
        {
            try
            {
                _session.Execute(description, mutation);
                AfterHistoryChange();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Edit failed ({description}): {ex.Message}");
                return false;
            }
        }

        private void AfterHistoryChange()
        {
            RebuildPlayerControls();
            Redraw();

            // The walk's current line may have been renumbered or removed by the edit; re-resolving
            // from the openings is cheaper than tracking it and cannot end up pointing at a hole.
            if (_currentLine != null && _dialog.IndexOf(_currentLine) < 0)
                StartWalk(Situations.FirstOrDefault(row => row.IsSelected)?.Title ?? "Start");
            else if (_currentLine != null)
                ShowLine(_currentLine);

            RefreshMerchantFields();

            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_resRef} *" : _resRef;
        }

        [RelayCommand]
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        public async Task<bool> TrySaveAsync()
        {
            // Captured ahead of CommitLine: when the line text is also dirty, CommitLine's
            // history refresh rewrites the Advanced fields from the still-old node values, and an
            // un-snapshotted Advanced draft would compare clean and be silently dropped.
            var advancedDraft = AdvancedDraftDiffers()
                ? (Speaker: AdvancedSpeaker,
                   Sound: AdvancedSound,
                   Animation: AdvancedAnimation,
                   Comment: AdvancedComment,
                   Script: AdvancedScript)
                : ((string, string, decimal, string, string)?)null;

            CommitLine();
            if (advancedDraft is { } draft)
            {
                (AdvancedSpeaker, AdvancedSound, AdvancedAnimation, AdvancedComment, AdvancedScript) = draft;
                CommitAdvanced();
            }
            if (IsMerchant && MerchantDraftDiffers())
                RunEdit("Finish merchant dialogue", EnsureMerchantStructure);

            Redraw();
            var firstBroken = Problems.FirstOrDefault(problem => problem.IsBroken);
            if (firstBroken != null)
            {
                GoToProblem(firstBroken);
                WalkStatus = $"Cannot save: {firstBroken.Message}";
                _log.AppendLine($"Cannot save {_session.FilePath}: {BlockingProblemCount} dialogue error(s) must be fixed first.");
                return false;
            }

            if (!IsDirty)
                return true;

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts.ConfirmExternalChangeAsync(_session.FilePath).ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;

                    if (choice == ExternalChangeChoice.Reload)
                    {
                        _session.ReloadFromDisk();
                        CloseRulesEditor();
                        AfterHistoryChange();
                        CatalogEntryChanged?.Invoke();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    // Overwrite accepts the generation currently on disk. A final compare-and-swap
                    // below still refuses a second external write that lands while we prepare bytes.
                    _session.RecordCurrentFileState();
                }

                // NumWords is saved metadata derived entirely from the authored lines. Updating it
                // must not create a second Undo entry after the user's actual dialogue edit.
                _session.ExecuteDerived(() => _dialog.RecomputeWordCount());

                var saveBytes = _session.ToBytes();
                if (!SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Cannot save {_session.FilePath}: the file changed on disk while the save " +
                        "was being prepared. Nothing was written - reload or save again.");
                    return false;
                }
                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState(saveBytes);
                AfterHistoryChange();
                CatalogEntryChanged?.Invoke();
                _log.AppendLine($"Saved {_session.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            _session.Undo();
            AfterHistoryChange();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            _session.Redo();
            AfterHistoryChange();
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

            if (_disposed)
                return base.OnClose();

            _disposed = true;
            _session.Dispose();
            Closed?.Invoke(this);
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
    }
}
