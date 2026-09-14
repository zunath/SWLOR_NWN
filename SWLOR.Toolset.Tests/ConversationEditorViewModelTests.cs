using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The editor's own logic, driven the way the view drives it. Nothing here touches Avalonia —
    /// commands are invoked directly — but the walk state, the pretend-player controls and the
    /// redraw-after-edit path are all real, and none of it was covered by the Domain tests.
    /// </summary>
    public class ConversationEditorViewModelTests
    {
        private static readonly SnippetCatalog Snippets = SnippetCatalog.Build();
        private static readonly IGameCodeIndex GameCode = new GameCodeIndex(GameServerSourceRoot);

        private string _workingCopy = string.Empty;

        private static string GameServerSourceRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                    if (Directory.Exists(Path.Combine(candidate, "Feature", "QuestDefinition")))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the SWLOR.Game.Server source tree.");
            }
        }

        /// <summary>
        /// A private copy of dantherbs. The editor opens a real DocumentSession and can save, so it
        /// must never be pointed at the module itself.
        /// </summary>
        [SetUp]
        public void CopyConversation()
        {
            var source = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");
            _workingCopy = Path.Combine(Path.GetTempPath(), $"swlor-dlg-{Guid.NewGuid():N}.dlg.json");
            File.Copy(source, _workingCopy);
        }

        [TearDown]
        public void RemoveWorkingCopy()
        {
            if (File.Exists(_workingCopy))
                File.Delete(_workingCopy);
        }

        private ConversationEditorViewModel Open(IEditorPromptService? prompts = null) =>
            new(
                _workingCopy,
                "dantherbs",
                Snippets,
                GameCode,
                new OutputLogService(),
                prompts ?? new StubPrompts());

        private void ReplaceWithCleanConversation(string text = "Original line.")
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            document.Openings[0].Target.Text = text;
            File.WriteAllBytes(_workingCopy, document.ToBytes());
        }

        // ---------- opening ----------

        [Test]
        public void OpeningShowsTheSituationsAndLandsOnALine()
        {
            using var editor = new Disposable(Open());

            editor.Value.Situations.Should().HaveCount(8);
            editor.Value.HasLine.Should().BeTrue();
            editor.Value.LineText.Should().NotBeEmpty();
            editor.Value.IsDirty.Should().BeFalse("opening a conversation must not change it");
        }

        [Test]
        public async Task OpeningAMerchantDoesNotScanPlacedStores()
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            var choice = document.AddReply("Show me your stock.");
            choice.AddAction("action-open-store").Value = "authored_store";
            document.AddLink(document.Openings[0].Target, choice);
            File.WriteAllBytes(_workingCopy, document.ToBytes());

            var resolverCalls = 0;
            using var editor = new Disposable(new ConversationEditorViewModel(
                _workingCopy,
                "test_convo",
                Snippets,
                GameCode,
                new OutputLogService(),
                new StubPrompts(),
                _ =>
                {
                    Interlocked.Increment(ref resolverCalls);
                    Thread.Sleep(250);
                    return new[] { "authored_store", "other_store" };
                }));

            resolverCalls.Should().Be(0,
                "opening a dialogue must not synchronously scan every placed store in the module");
            editor.Value.SelectedMerchantStore!.Value.Should().Be("authored_store",
                "the saved value remains visible before the optional list is loaded");

            await editor.Value.LoadMerchantStoresAsync();

            resolverCalls.Should().Be(1);
            editor.Value.MerchantStores.Select(store => store.Value)
                .Should().Contain(new[] { "authored_store", "other_store" });
        }

        [Test]
        public void TheCoverageStripListsBothQuests()
        {
            using var editor = new Disposable(Open());

            editor.Value.Coverage.Select(row => row.Name).Should()
                .BeEquivalentTo("Field Tinctures", "Harvesting Herbs");
        }

        [Test]
        public void ThePretendPlayerGetsOneControlPerQuestTheConversationReads()
        {
            using var editor = new Disposable(Open());

            editor.Value.QuestPills.Select(pill => pill.Name).Should()
                .BeEquivalentTo("Field Tinctures", "Harvesting Herbs");
            editor.Value.FactPills.Should().BeEmpty("this conversation reads nothing but quests");
        }

        [Test]
        public void TheDeadOpeningIsReportedInTheFindings()
        {
            using var editor = new Disposable(Open());

            editor.Value.Problems.Should().Contain(problem => problem.IsBroken);
            editor.Value.Problems.First(problem => problem.IsBroken).Message
                .Should().Contain("can never happen");
        }

        // ---------- navigation ----------

        [Test]
        public void SelectingASituationWalksToIt()
        {
            using var editor = new Disposable(Open());
            var reminder = editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures");

            editor.Value.SelectSituationCommand.Execute(reminder);

            editor.Value.LineText.Should().StartWith("The bottles are lined up");
            editor.Value.Breadcrumb.Should().ContainSingle().Which.Should().Be("Doing Field Tinctures");
        }

        [Test]
        public void SelectingASituationSetsThePillsToMatch()
        {
            using var editor = new Disposable(Open());
            var turnIn = editor.Value.Situations.Single(row => row.Title == "On step 2 of Field Tinctures");

            editor.Value.SelectSituationCommand.Execute(turnIn);

            editor.Value.QuestPills.Single(pill => pill.Name == "Field Tinctures")
                .SelectedOption.Should().Be("on step 2");
        }

        [Test]
        public void SelectingTheDeadSituationSaysSoInsteadOfShowingSomethingElse()
        {
            // dantherbs now has four dead situations (see
            // ReachabilityTests.ADuplicateGuardKeyOnlyEvaluatesItsNegatedForm), so pick one by name
            // rather than assuming there is only one.
            using var editor = new Disposable(Open());
            var dead = editor.Value.Situations.Single(row => row.Title == "Finished Harvesting Herbs");

            editor.Value.SelectSituationCommand.Execute(dead);

            editor.Value.HasLine.Should().BeFalse();
            editor.Value.WalkStatus.Should().Contain("No player can reach");
        }

        [Test]
        public void PickingAChoiceMovesToTheNextLineAndRecordsTheTrail()
        {
            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);

            var ask = editor.Value.Choices.Single(choice => choice.Text == "What do you need this time?");
            editor.Value.PickChoiceCommand.Execute(ask);

            editor.Value.LineText.Should().StartWith("Three Wild Innards");
            editor.Value.Breadcrumb.Should().HaveCount(2);
        }

        [Test]
        public void PickingAnAcceptChoiceMovesThePretendPlayerOntoTheQuest()
        {
            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);

            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text == "What do you need this time?"));
            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text.StartsWith("I'll bring")));

            editor.Value.QuestPills.Single(pill => pill.Name == "Field Tinctures")
                .SelectedOption.Should().Be("on step 1");
        }

        [Test]
        public void ReachingAnNpcLineAppliesItsActionsBeforeShowingChoices()
        {
            var document = DlgDocument.Load(_workingCopy);
            var answer = document.Entries.Single(entry => entry.Text.StartsWith("Three Wild Innards"));
            answer.AddAction("action-accept-quest", "field_tinctures");
            File.WriteAllBytes(_workingCopy, document.ToBytes());

            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);
            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text == "What do you need this time?"));

            editor.Value.QuestPills.Single(pill => pill.Name == "Field Tinctures")
                .SelectedOption.Should().Be("on step 1");
        }

        [Test]
        public void BackReturnsToThePreviousLine()
        {
            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);
            var opening = editor.Value.LineText;

            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text == "What do you need this time?"));
            editor.Value.BackCommand.Execute(null);

            editor.Value.LineText.Should().Be(opening);
        }

        [Test]
        public void BackRestoresThePretendPlayerStateFromBeforeTheChoice()
        {
            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);
            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text == "What do you need this time?"));
            editor.Value.PickChoiceCommand.Execute(
                editor.Value.Choices.Single(choice => choice.Text.StartsWith("I'll bring")));
            editor.Value.QuestPills.Single(pill => pill.Name == "Field Tinctures")
                .SelectedOption.Should().Be("on step 1");

            editor.Value.BackCommand.Execute(null);

            editor.Value.QuestPills.Single(pill => pill.Name == "Field Tinctures")
                .SelectedOption.Should().Be("never started");
        }

        [Test]
        public void BackFromAnEndingRestoresTheLastNpcLine()
        {
            var document = DlgDocument.Load(_workingCopy);
            var endingReply = document.AddReply("End this test conversation.");
            foreach (var entry in document.Entries)
                document.AddLink(entry, endingReply);
            File.WriteAllBytes(_workingCopy, document.ToBytes());

            using var editor = new Disposable(Open());
            var offer = editor.Value.Situations.Single(row => row.Title == "Offering Field Tinctures");
            editor.Value.SelectSituationCommand.Execute(offer);
            var lastNpcLine = editor.Value.LineText;
            var playerState = editor.Value.QuestPills
                .ToDictionary(pill => pill.Name, pill => pill.SelectedOption);
            var ending = editor.Value.Choices.First(choice => choice.Consequence == "ends the talk");

            editor.Value.PickChoiceCommand.Execute(ending);
            editor.Value.HasNoLine.Should().BeTrue();

            editor.Value.BackCommand.Execute(null);

            editor.Value.HasLine.Should().BeTrue();
            editor.Value.LineText.Should().Be(lastNpcLine);
            editor.Value.QuestPills.Should().OnlyContain(pill =>
                pill.SelectedOption == playerState[pill.Name]);
        }

        [Test]
        public void ChangingAPillRedrawsTheConversation()
        {
            using var editor = new Disposable(Open());

            editor.Value.QuestPills.Single(pill => pill.Name == "Harvesting Herbs").SelectedOption = "finished";

            editor.Value.LineText.Should().StartWith("Hold a moment.",
                "finishing the first quest is what unlocks the offer of the second");
        }

        // ---------- editing ----------

        [Test]
        public void EditingALineMarksTheDocumentDirtyAndSurvivesARedraw()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));

            editor.Value.LineText = "The bottles are still empty.";
            editor.Value.CommitLineCommand.Execute(null);

            editor.Value.IsDirty.Should().BeTrue();
            editor.Value.LineText.Should().Be("The bottles are still empty.");
            editor.Value.CanUndo.Should().BeTrue();
        }

        [Test]
        public void UndoRestoresTheLineAndTheCleanState()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            var original = editor.Value.LineText;

            editor.Value.LineText = "Changed.";
            editor.Value.CommitLineCommand.Execute(null);
            editor.Value.UndoCommand.Execute(null);

            editor.Value.LineText.Should().Be(original);
            editor.Value.IsDirty.Should().BeFalse();
        }

        [Test]
        public async Task SavingDerivedWordCountsDoesNotAddAnUndoStep()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(editor.Value.Situations.Single());
            var original = editor.Value.LineText;

            editor.Value.LineText = "A deliberately different line.";
            editor.Value.CommitLineCommand.Execute(null);
            await editor.Value.TrySaveAsync();
            DlgDocument.Load(_workingCopy).Openings
                .Select(link => link.Target.Text)
                .Should().Contain("A deliberately different line.");
            editor.Value.UndoCommand.Execute(null);

            editor.Value.LineText.Should().Be(original);
            editor.Value.CanUndo.Should().BeFalse(
                "the save-time NumWords refresh is derived data, not a user edit");
        }

        [Test]
        public void AddingAChoiceAppearsUnderTheCurrentLine()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            var before = editor.Value.Choices.Count;

            editor.Value.AddChoiceCommand.Execute(null);

            editor.Value.Choices.Should().HaveCount(before + 1);
            editor.Value.Choices.Last().Text.Should().Be("End conversation",
                "the NWN placeholder is never exposed as player-facing authoring text");
        }

        [Test]
        public void MovingAChoiceChangesItsDisplayOrderAndCanBeUndone()
        {
            using var editor = new Disposable(Open());
            foreach (var situation in editor.Value.Situations.ToList())
            {
                editor.Value.SelectSituationCommand.Execute(situation);
                if (editor.Value.Choices.Count >= 2)
                    break;
            }

            editor.Value.Choices.Should().HaveCountGreaterThanOrEqualTo(2);
            var before = editor.Value.Choices.Select(choice => choice.Text).ToList();
            var first = editor.Value.Choices[0];

            first.CanMoveUp.Should().BeFalse();
            first.CanMoveDown.Should().BeTrue();
            editor.Value.MoveChoiceDownCommand.Execute(first);

            editor.Value.Choices.Select(choice => choice.Text).Take(2).Should()
                .Equal(before[1], before[0]);
            editor.Value.IsDirty.Should().BeTrue();

            editor.Value.UndoCommand.Execute(null);

            editor.Value.Choices.Select(choice => choice.Text).Should().Equal(before);
        }

        [Test]
        public void AddingAFollowUpTurnsANewChoiceIntoAMultiTurnBranch()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.AddChoiceCommand.Execute(null);
            var choice = editor.Value.Choices.Last();

            choice.CanAddFollowUp.Should().BeTrue();
            editor.Value.AddFollowUpCommand.Execute(choice);

            choice.Target.Links.Should().ContainSingle();
            choice.Target.Links[0].Target.IsEntry.Should().BeTrue();
        }

        [Test]
        public void RemovingAChoiceReportsWhatItDisturbed()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));

            editor.Value.RemoveChoiceCommand.Execute(editor.Value.Choices[0]);

            // Removing from the middle of the list renumbers everything after it, and the editor
            // says so rather than letting a large diff appear unexplained.
            editor.Value.WalkStatus.Should().Contain("bigger diff than it looks");
        }

        [Test]
        public void RemovingTheChoiceBeingEditedClosesItsRulesPanel()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "On step 2 of Field Tinctures"));
            var choice = editor.Value.Choices.Single(row => row.Text.Contains("hazard pay"));
            editor.Value.EditChoiceCommand.Execute(choice);

            editor.Value.RemoveChoiceCommand.Execute(choice);

            editor.Value.IsEditingChoice.Should().BeFalse();
            editor.Value.IsEditingRules.Should().BeFalse();
            editor.Value.Guards.Should().BeEmpty();
            editor.Value.Consequences.Should().BeEmpty();
        }

        [Test]
        public void UndoingTheAdditionOfTheChoiceBeingEditedClosesItsRulesPanel()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.AddChoiceCommand.Execute(null);
            var choice = editor.Value.Choices.Last();
            editor.Value.EditChoiceCommand.Execute(choice);

            editor.Value.UndoCommand.Execute(null);

            editor.Value.IsEditingChoice.Should().BeFalse();
            editor.Value.IsEditingRules.Should().BeFalse();
            editor.Value.Guards.Should().BeEmpty();
            editor.Value.Consequences.Should().BeEmpty();
        }

        [Test]
        public void EditingAChoiceExposesItsGuardsAndConsequencesAsSentences()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "On step 2 of Field Tinctures"));

            var collect = editor.Value.Choices.Single(choice => choice.Text.Contains("hazard pay"));
            editor.Value.EditChoiceCommand.Execute(collect);

            editor.Value.IsEditingChoice.Should().BeTrue();
            editor.Value.Consequences.Should().ContainSingle()
                .Which.Sentence.Should().Be(
                    "moves Field Tinctures to its next step, and pays out on the last one");
        }

        [Test]
        public void ChangingAConsequenceArgumentWritesItBack()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "On step 2 of Field Tinctures"));
            var collect = editor.Value.Choices.Single(choice => choice.Text.Contains("hazard pay"));
            editor.Value.EditChoiceCommand.Execute(collect);

            var argument = editor.Value.Consequences[0].Arguments[0];
            argument.HasOptions.Should().BeTrue("a quest id is picked from the real quest list");
            argument.Selected = argument.Options.Single(option => option.Value == "harvest_herbs");

            editor.Value.IsDirty.Should().BeTrue();
            var reloaded = DlgDocument.Parse(File.ReadAllBytes(_workingCopy));
            editor.Value.SaveCommand.Execute(null);
            reloaded = DlgDocument.Parse(File.ReadAllBytes(_workingCopy));
            reloaded.Replies.SelectMany(reply => reply.Actions)
                .Should().Contain(action => action.Value == "harvest_herbs");
        }

        [Test]
        public void SelectingAQuestRebuildsItsDependentStatePicker()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            var choice = editor.Value.Choices[0];
            editor.Value.EditChoiceCommand.Execute(choice);
            editor.Value.GuardToAdd = Snippets.Find("condition-on-quest-state");
            editor.Value.AddGuardCommand.Execute(null);
            var guard = editor.Value.Guards.Single(rule =>
                rule.Snippet.Key == "condition-on-quest-state");
            var quest = guard.Arguments[0];
            var state = guard.Arguments[1];

            state.HasOptions.Should().BeFalse(
                "no quest is selected when a new quest-state guard is first added");

            quest.Selected = quest.Options.Single(option => option.Value == "field_tinctures");

            state.HasOptions.Should().BeTrue();
            state.IsFreeText.Should().BeFalse();
            state.Options.Should().NotBeEmpty()
                .And.OnlyContain(option => option.Label.StartsWith("Step "));

            state.Selected = state.Options.Last();

            guard.Param.Arguments.Should().Equal("field_tinctures", state.Selected.Value);
        }

        [Test]
        public void AddingAGuardWiresTheDispatcherWithoutTheWriterSeeingIt()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));

            var choice = editor.Value.Choices[0];
            editor.Value.EditChoiceCommand.Execute(choice);
            editor.Value.GuardToAdd = Snippets.Find("condition-has-completed-tutorial");
            editor.Value.AddGuardCommand.Execute(null);

            choice.Link.Active.Should().Be(DlgDocument.ConditionDispatcher);
            editor.Value.Guards.Should().ContainSingle()
                .Which.Sentence.Should().Be("the player has finished the tutorial on some character");
        }

        [Test]
        public void OptionalAndRepeatedSnippetArgumentsCanBeAddedAndRemoved()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.AddChoiceCommand.Execute(null);
            var choice = editor.Value.Choices.Last();
            editor.Value.EditChoiceCommand.Execute(choice);

            editor.Value.ConsequenceToAdd = Snippets.Find("action-open-store");
            editor.Value.AddConsequenceCommand.Execute(null);
            var optional = editor.Value.Consequences.Single();
            optional.Arguments.Should().BeEmpty();
            optional.AddArgumentsCommand.Execute(null);
            optional.Arguments.Should().ContainSingle();
            optional.Arguments[0].FreeText = "test_store";
            optional.Param.Value.Should().Be("test_store");
            optional.RemoveArgumentsCommand.Execute(null);
            optional.Arguments.Should().BeEmpty();

            editor.Value.ConsequenceToAdd = Snippets.Find("action-give-key-items");
            editor.Value.AddConsequenceCommand.Execute(null);
            editor.Value.Consequences.Should().HaveCount(2,
                "a line can carry multiple distinct snippet outcomes");
            editor.Value.Consequences.Select(consequence => consequence.Snippet.Key).Should()
                .BeEquivalentTo("action-open-store", "action-give-key-items");
            choice.Target.Actions.Should().NotContain(action => action.IsOncePerPlayerMarker);

            editor.Value.GuardToAdd = Snippets.Find("condition-completed-quest");
            editor.Value.AddGuardCommand.Execute(null);
            var repeated = editor.Value.Guards.Single();
            repeated.Arguments.Should().ContainSingle();
            repeated.AddArgumentsCommand.Execute(null);
            repeated.Arguments.Should().HaveCount(2);
            repeated.RemoveArgumentsCommand.Execute(null);
            repeated.Arguments.Should().ContainSingle();
        }

        [Test]
        public void CustomActionScriptDisablesOutcomesUntilCleared()
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            document.Openings[0].Target.Script = "custom_action";
            File.WriteAllBytes(_workingCopy, document.ToBytes());
            using var editor = new Disposable(Open());
            editor.Value.EditCurrentLineCommand.Execute(null);

            editor.Value.CanAddOutcome.Should().BeFalse();
            editor.Value.HasCustomActionScriptForOutcomes.Should().BeTrue();
            editor.Value.ConsequenceToAdd = Snippets.Find("action-open-store");

            editor.Value.AddConsequenceCommand.Execute(null);

            editor.Value.LiveDialog.Openings[0].Target.Actions.Should().BeEmpty();
            editor.Value.WalkStatus.Should().Contain("Clear the custom action script");
            editor.Value.IsDirty.Should().BeFalse();

            editor.Value.AdvancedScript = string.Empty;
            editor.Value.CommitAdvancedCommand.Execute(null);

            editor.Value.CanAddOutcome.Should().BeTrue();
            editor.Value.HasCustomActionScriptForOutcomes.Should().BeFalse();
            editor.Value.AddConsequenceCommand.Execute(null);
            editor.Value.LiveDialog.Openings[0].Target.Actions.Should().ContainSingle();
        }

        [Test]
        public async Task SaveFlushesUncommittedAdvancedEdits()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open());

            // Typed into the Advanced panel, then saved by keyboard shortcut: no LostFocus ever
            // fires, so nothing but the save path itself can commit these fields.
            editor.Value.AdvancedComment = "Typed just before Ctrl+S.";

            (await editor.Value.TrySaveAsync()).Should().BeTrue();

            DlgDocument.Load(_workingCopy).Openings[0].Target.Comment
                .Should().Be("Typed just before Ctrl+S.",
                    "a save must flush Advanced-panel edits that never lost focus");
        }

        [Test]
        public async Task SaveKeepsAdvancedEditsWhenTheLineTextIsAlsoDirty()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open());

            // Both halves pending: CommitLine's history refresh used to rewrite the Advanced
            // fields from the old node before the save could commit them.
            editor.Value.LineText = "Rewritten line.";
            editor.Value.AdvancedComment = "Typed alongside the line edit.";

            (await editor.Value.TrySaveAsync()).Should().BeTrue();

            var saved = DlgDocument.Load(_workingCopy).Openings[0].Target;
            saved.Text.Should().Be("Rewritten line.");
            saved.Comment.Should().Be("Typed alongside the line edit.",
                "a save must flush the Advanced draft even when the line text also changed");
        }

        [Test]
        public async Task SaveClampsAnOutOfRangeAdvancedAnimationInsteadOfThrowing()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open());

            // The NumericUpDown bounds interactive input, but the property is settable to anything;
            // the save path converts it before entering its try block and must never throw.
            editor.Value.AdvancedAnimation = (decimal)uint.MaxValue + 1;

            (await editor.Value.TrySaveAsync()).Should().BeTrue();

            DlgDocument.Load(_workingCopy).Openings[0].Target.Animation.Should().Be(uint.MaxValue);
        }

        [Test]
        public void QuestOutcomesDoNotCreateHiddenExecutionMetadata()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.AddChoiceCommand.Execute(null);
            var choice = editor.Value.Choices.Last();
            editor.Value.EditChoiceCommand.Execute(choice);

            editor.Value.ConsequenceToAdd = Snippets.Find("action-accept-quest");
            editor.Value.AddConsequenceCommand.Execute(null);
            editor.Value.ConsequenceToAdd = Snippets.Find("action-advance-quest");
            editor.Value.AddConsequenceCommand.Execute(null);
            editor.Value.ConsequenceToAdd = Snippets.Find("action-request-quest-items");
            editor.Value.AddConsequenceCommand.Execute(null);

            editor.Value.Consequences.Select(consequence => consequence.Snippet.Key).Should().Contain(
                "action-accept-quest",
                "action-advance-quest",
                "action-request-quest-items");
            choice.Target.Actions.Should().NotContain(action => action.IsOncePerPlayerMarker,
                "quest state and explicit conditions, not hidden permanent markers, control repetition");
        }

        [Test]
        public void UnmatchedDropdownValuesSurviveUnrelatedSnippetEdits()
        {
            var document = DlgDocument.Load(_workingCopy);
            var opening = document.Openings.Single(link => link.Target.Text.StartsWith("The bottles are lined up"));
            opening.Conditions[0].Value = "removed_quest";
            File.WriteAllBytes(_workingCopy, document.ToBytes());

            using var editor = new Disposable(Open());
            var situation = editor.Value.Situations.Single(row =>
                row.Situation.Opening.Target.Text.StartsWith("The bottles are lined up"));
            editor.Value.EditSituationCommand.Execute(situation);
            var guard = editor.Value.Guards.Single();
            var argument = guard.Arguments.Single();
            argument.HasOptions.Should().BeTrue();
            argument.Selected.Should().BeNull();
            argument.Value.Should().Be("removed_quest");

            guard.IsNegated = !guard.IsNegated;

            guard.Param.Value.Should().Be("removed_quest");
        }

        [Test]
        public void SituationRulesEditTheOpeningGuard()
        {
            using var editor = new Disposable(Open());
            var situation = editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures");
            editor.Value.EditSituationCommand.Execute(situation);
            editor.Value.GuardToAdd = Snippets.Find("condition-has-completed-tutorial");

            editor.Value.AddGuardCommand.Execute(null);

            situation.Situation.Opening.Conditions.Should().Contain(condition =>
                condition.SnippetKey == "condition-has-completed-tutorial");
        }

        [Test]
        public async Task ReloadingAnExternalChangeClosesTheRulesEditor()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open(new StubPrompts(ExternalChangeChoice.Reload)));
            var situation = editor.Value.Situations.Single();
            editor.Value.EditSituationCommand.Execute(situation);
            editor.Value.GuardToAdd = Snippets.Find("condition-has-completed-tutorial");
            editor.Value.AddGuardCommand.Execute(null);

            var external = File.ReadAllText(_workingCopy)
                .Replace(
                    "Original line.",
                    "The line was changed outside the toolset.",
                    StringComparison.Ordinal);
            File.WriteAllText(_workingCopy, external);

            (await editor.Value.TrySaveAsync()).Should().BeTrue();
            editor.Value.IsEditingRules.Should().BeFalse();
            editor.Value.Guards.Should().BeEmpty();
            editor.Value.Consequences.Should().BeEmpty();
        }

        [Test]
        public void CurrentNpcLineActionsAreShownInTheRulesEditor()
        {
            var document = DlgDocument.Load(_workingCopy);
            var opening = document.Openings.Single(link => link.Target.Text.StartsWith("The bottles are lined up"));
            opening.Target.AddAction("action-give-key-items", "1");
            File.WriteAllBytes(_workingCopy, document.ToBytes());

            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.EditCurrentLineCommand.Execute(null);

            editor.Value.IsEditingRules.Should().BeTrue();
            editor.Value.Consequences.Should().ContainSingle()
                .Which.Snippet.Key.Should().Be("action-give-key-items");
        }

        [Test]
        public void MovingASituationUpChangesWhichOneWins()
        {
            using var editor = new Disposable(Open());
            var second = editor.Value.Situations[1];
            var secondTitle = second.Title;

            editor.Value.MoveSituationUpCommand.Execute(second);

            editor.Value.Situations[0].Title.Should().Be(secondTitle);
            editor.Value.IsDirty.Should().BeTrue();
        }

        [Test]
        public void MovingTheDeadSituationUpBringsItBackToLife()
        {
            // The fix the design offers for an unreachable opening, exercised end to end. dantherbs
            // now has four dead situations (see
            // ReachabilityTests.ADuplicateGuardKeyOnlyEvaluatesItsNegatedForm), so pick this one by
            // name rather than assuming it is the only one.
            using var editor = new Disposable(Open());
            var dead = editor.Value.Situations.Single(row => row.Title == "Finished Harvesting Herbs");

            for (var i = dead.Order; i > 1; i--)
            {
                var row = editor.Value.Situations.Single(candidate =>
                    candidate.Title == "Finished Harvesting Herbs");
                editor.Value.MoveSituationUpCommand.Execute(row);
            }

            editor.Value.Situations.Single(row => row.Title == "Finished Harvesting Herbs")
                .IsUnreachable.Should().BeFalse();
        }

        [Test]
        public void SavingWritesTheFileAndClearsTheDirtyFlag()
        {
            ReplaceWithCleanConversation();
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(editor.Value.Situations.Single());
            editor.Value.LineText = "Saved text.";
            editor.Value.CommitLineCommand.Execute(null);

            editor.Value.SaveCommand.Execute(null);

            editor.Value.IsDirty.Should().BeFalse();
            DlgDocument.Parse(File.ReadAllBytes(_workingCopy)).Entries
                .Should().Contain(entry => entry.Text == "Saved text.");
        }

        [Test]
        public async Task BrokenDialogueIsNotSaved()
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            File.WriteAllBytes(_workingCopy, document.ToBytes());
            var before = File.ReadAllBytes(_workingCopy);
            using var editor = new Disposable(Open());

            (await editor.Value.TrySaveAsync()).Should().BeFalse();

            editor.Value.HasBlockingProblems.Should().BeTrue();
            editor.Value.WalkStatus.Should().StartWith("Cannot save:");
            File.ReadAllBytes(_workingCopy).Should().Equal(before);
        }

        [Test]
        public void ANewDialogueStartsWithOnlyTheBehaviorChoice()
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            File.WriteAllBytes(_workingCopy, document.ToBytes());
            using var editor = new Disposable(Open());

            editor.Value.ShowBehaviorChooser.Should().BeTrue();
            editor.Value.BehaviorOptions.Select(option => option.Name).Should()
                .Equal("Merchant", "Quest giver", "Conversation");
        }

        [Test]
        public void MerchantBehaviorSuppliesTheStoreOutcomeAndGoodbye()
        {
            var document = DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));
            File.WriteAllBytes(_workingCopy, document.ToBytes());
            using var editor = new Disposable(Open());
            var merchant = editor.Value.BehaviorOptions.Single(option =>
                option.Kind == ConversationBehaviorKind.Merchant);
            editor.Value.ChooseBehaviorCommand.Execute(merchant);
            editor.Value.MerchantGreeting = "Welcome, <FirstName>.";
            editor.Value.MerchantChoiceText = "Show me your stock.";

            editor.Value.CommitMerchantCommand.Execute(null);

            var greeting = editor.Value.LiveDialog.Openings[0].Target;
            greeting.Text.Should().Be("Welcome, <FirstName>.");
            greeting.Links.Select(link => link.Target.Text).Should().Contain("Goodbye.");
            greeting.Links.Select(link => link.Target).Should().Contain(reply =>
                reply.Actions.Any(action => action.SnippetKey == "action-open-store"));
        }

        [Test]
        public void SwitchingBehaviorsKeepsTheMerchantDraftForTheSession()
        {
            using var editor = new Disposable(Open());
            var merchant = editor.Value.BehaviorOptions.Single(option =>
                option.Kind == ConversationBehaviorKind.Merchant);
            var conversation = editor.Value.BehaviorOptions.Single(option =>
                option.Kind == ConversationBehaviorKind.Conversation);
            editor.Value.SelectedBehavior = merchant;
            editor.Value.MerchantGreeting = "Fresh stock every day.";

            editor.Value.SelectedBehavior = conversation;
            editor.Value.SelectedBehavior = merchant;

            editor.Value.MerchantGreeting.Should().Be("Fresh stock every day.");
        }

        [Test]
        public void PreviewResolvesFriendlyDynamicTextSamples()
        {
            ReplaceWithCleanConversation("Welcome, <FirstName>. It is <Day/Night>.");
            using var editor = new Disposable(Open());

            editor.Value.PreviewLineText.Should().Be("Welcome, Kori. It is day.");
            editor.Value.LineText.Should().Contain("<FirstName>",
                "the stored NWN token remains editable on the Write tab");
        }

        [Test]
        public void SavingAnUntouchedConversationLeavesTheFileByteForByte()
        {
            var before = File.ReadAllBytes(_workingCopy);
            using var editor = new Disposable(Open());

            editor.Value.SaveCommand.Execute(null);

            File.ReadAllBytes(_workingCopy).Should().Equal(before);
        }

        // ---------- the scaffold ----------

        [Test]
        public void PickingAQuestToSetUpPreviewsWhatItWouldCreate()
        {
            using var editor = new Disposable(Open());

            editor.Value.QuestToScaffold = editor.Value.ScaffoldableQuests
                .Single(option => option.Value == "fetch_pet_treat");

            editor.Value.CanScaffold.Should().BeTrue();
            editor.Value.ScaffoldPreview.Should().NotBeEmpty();
            editor.Value.ScaffoldPreview.Should()
                .NotContain(beat => beat.Title == "First meeting",
                    "this conversation already greets everybody");
        }

        [Test]
        public void SettingUpAQuestAddsItsSituationsAndItsPill()
        {
            using var editor = new Disposable(Open());
            var before = editor.Value.Situations.Count;

            editor.Value.QuestToScaffold = editor.Value.ScaffoldableQuests
                .Single(option => option.Value == "fetch_pet_treat");
            editor.Value.ScaffoldQuestCommand.Execute(null);

            editor.Value.Situations.Count.Should().BeGreaterThan(before);
            editor.Value.QuestPills.Select(pill => pill.Name).Should().Contain("Fetch Pet Treat Quest");
            editor.Value.Coverage.Select(row => row.Name).Should().Contain("Fetch Pet Treat Quest");
        }

        /// <summary>Disposes the editor's document session, which holds a file handle.</summary>
        private sealed class Disposable : IDisposable
        {
            public Disposable(ConversationEditorViewModel value) => Value = value;

            public ConversationEditorViewModel Value { get; }

            public void Dispose() => Value.OnClose();
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            private readonly ExternalChangeChoice _externalChangeChoice;

            public StubPrompts(ExternalChangeChoice externalChangeChoice = ExternalChangeChoice.Overwrite)
            {
                _externalChangeChoice = externalChangeChoice;
            }

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Discard);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(_externalChangeChoice);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(true);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
