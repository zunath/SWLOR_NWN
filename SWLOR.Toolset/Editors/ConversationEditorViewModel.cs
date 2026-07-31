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

        private sealed record WalkStep(DlgNode Line, PretendPlayer Player);

        [ObservableProperty]
        private string _lineText = string.Empty;

        [ObservableProperty]
        private bool _showHiddenChoices;

        [ObservableProperty]
        private string _reuseWarning = string.Empty;

        [ObservableProperty]
        private string _walkStatus = string.Empty;

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

            if (gameCode != null)
            {
                foreach (var quest in gameCode.Quests.Values.OrderBy(q => q.Name, StringComparer.OrdinalIgnoreCase))
                    ScaffoldableQuests.Add(new ArgumentOption(quest.Id, quest.Name));
            }

            BuildPlayerControls();
            Redraw();
            GoToFirstUnwritten();
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
            if (_currentLine == null || LineText == _currentLine.Text)
                return;

            var node = _currentLine;
            RunEdit("Edit a line", () => node.Text = LineText);
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
            RunEdit($"Add a condition", () => link.AddCondition(snippet.Key));
            GuardToAdd = null;
        }

        [RelayCommand]
        private void AddConsequence()
        {
            var node = _editingNode;
            if (node == null || ConsequenceToAdd == null)
                return;

            var snippet = ConsequenceToAdd;
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
                var snippet = _snippets.Find(action.Key);
                if (snippet != null)
                {
                    Consequences.Add(new SnippetEditorViewModel(
                        action, snippet, _argumentOptions, canNegate: false, CommitSnippet, RemoveConsequence, _evaluator.DisplayValue));
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
            foreach (var situation in model.Situations())
            {
                var row = new SituationRowViewModel(situation) { IsSelected = situation.Order == selectedOrder };
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
            OnPropertyChanged(nameof(HasCoverage));
            OnPropertyChanged(nameof(HasQuestPills));
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
            LineText = line.Text;
            WalkStatus = string.Empty;

            var incoming = _dialog.IncomingLinks(line).Count;
            ReuseWarning = incoming > 1
                ? $"This line is reached from {incoming} places — editing it changes all of them."
                : string.Empty;

            RefreshChoices();
            RefreshLineState();
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
            foreach (var link in _currentLine.Links)
            {
                // An imported or externally edited DLG can carry a link whose index is outside
                // ReplyList; dereferencing Target would throw before the tab could render at all.
                // The row is shown (never hidden) so the builder can see the broken route and
                // remove it.
                if (!_dialog.HasNode(link.TargetKind, link.TargetIndex))
                {
                    Choices.Add(new ChoiceRowViewModel(
                        link, "!",
                        $"broken route — reply #{link.TargetIndex} does not exist",
                        null, isDangling: true));
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
                    Choices.Add(new ChoiceRowViewModel(link, "—", string.Empty, hiddenBecause));
                    continue;
                }

                Choices.Add(new ChoiceRowViewModel(
                    link,
                    hiddenBecause == null ? $"{number++}." : "—",
                    DescribeConsequence(link.Target),
                    hiddenBecause));
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
            var effects = reply.Actions.Select(action => _evaluator.DescribeAction(action)).ToList();
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
