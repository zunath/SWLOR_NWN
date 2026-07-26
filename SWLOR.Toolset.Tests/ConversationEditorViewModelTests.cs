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

        private ConversationEditorViewModel Open() =>
            new(_workingCopy, "dantherbs", Snippets, GameCode, new OutputLogService(), new StubPrompts());

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
            using var editor = new Disposable(Open());
            var dead = editor.Value.Situations.Single(row => row.IsUnreachable);

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
        public void AddingAChoiceAppearsUnderTheCurrentLine()
        {
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            var before = editor.Value.Choices.Count;

            editor.Value.AddChoiceCommand.Execute(null);

            editor.Value.Choices.Should().HaveCount(before + 1);
            editor.Value.Choices.Last().Text.Should().Be(QuestConversationScaffold.Placeholder);
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
            // The fix the design offers for an unreachable opening, exercised end to end.
            using var editor = new Disposable(Open());
            var dead = editor.Value.Situations.Single(row => row.IsUnreachable);

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
            using var editor = new Disposable(Open());
            editor.Value.SelectSituationCommand.Execute(
                editor.Value.Situations.Single(row => row.Title == "Doing Field Tinctures"));
            editor.Value.LineText = "Saved text.";
            editor.Value.CommitLineCommand.Execute(null);

            editor.Value.SaveCommand.Execute(null);

            editor.Value.IsDirty.Should().BeFalse();
            DlgDocument.Parse(File.ReadAllBytes(_workingCopy)).Entries
                .Should().Contain(entry => entry.Text == "Saved text.");
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
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Discard);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Overwrite);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(true);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
