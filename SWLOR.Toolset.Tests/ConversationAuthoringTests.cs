using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The two things the editor does for a writer beyond letting them type: telling them what is
    /// wrong in words they can act on, and laying out a correct quest conversation so there is
    /// nothing structural left to get wrong.
    /// </summary>
    public class ConversationAuthoringTests
    {
        private static readonly SnippetCatalog Snippets = SnippetCatalog.Build();
        private static readonly IGameCodeIndex GameCode = new GameCodeIndex(GameServerSourceRoot);
        private static readonly ReachabilityEvaluator Evaluator = new(Snippets, GameCode);
        private static readonly ConversationAnalyzer Analyzer = new(Snippets, Evaluator, GameCode);

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

        private static DlgDocument DantHerbs() =>
            DlgDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json"));

        /// <summary>A brand-new dialog, exactly as Module Contents' "New Dialog…" creates it.</summary>
        private static DlgDocument NewConversation() =>
            DlgDocument.Parse(ModuleResourceTemplateFactory.CreateFileContent(
                Domain.Workspace.ResourceType.Dlg, "test_convo", "Test"));

        // ---------- problems, in words ----------

        [Test]
        public void TheDeadOpeningIsReportedAsAConsequenceNotARuleName()
        {
            var problems = Analyzer.Analyze(DantHerbs());

            var dead = problems.Single(problem => problem.RuleId == "unreachable-opening");
            dead.Severity.Should().Be(ProblemSeverity.Broken);
            dead.Message.Should().StartWith("“Finished Harvesting Herbs” can never happen.");
            dead.Anchor.Should().Be(ProblemAnchor.Situation);
            dead.Situation.Should().NotBeNull();
        }

        [Test]
        public void BannedScaffoldingIsAHintAgainstTheLineThatCarriesIt()
        {
            var problems = Analyzer.Analyze(DantHerbs());

            var hint = problems.First(problem =>
                problem.RuleId == "house-style" && problem.Message.Contains("traveler"));

            hint.Severity.Should().Be(ProblemSeverity.Hint);
            hint.Node.Should().NotBeNull();
            hint.Node!.Text.Should().StartWith("Greetings, traveler.");
        }

        [Test]
        public void ConditionsWithNothingToRunThemAreReportedAsAlwaysShowing()
        {
            var document = DantHerbs();
            var opening = document.Openings[0];
            opening.Active = string.Empty;

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "condition-never-runs" && problem.Severity == ProblemSeverity.Broken);
        }

        [Test]
        public void AnActionWithNothingToRunItIsReported()
        {
            var document = DantHerbs();
            document.Replies.First(reply => reply.Actions.Count > 0).Script = string.Empty;

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem => problem.RuleId == "action-never-runs");
        }

        [Test]
        public void AStepBeyondTheQuestsLastIsReportedWithTheRealCount()
        {
            var document = DantHerbs();
            var opening = document.Openings[0];
            opening.AddCondition("condition-on-quest-state", "field_tinctures 7");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "impossible-step"
                && problem.Message == "Field Tinctures has 2 step(s), so step 7 will never match.");
        }

        [Test]
        public void AQuestThatDoesNotExistIsReported()
        {
            var document = DantHerbs();
            document.Openings[0].AddCondition("condition-has-quest", "no_such_quest");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "unknown-quest"
                && problem.Message.Contains("no quest called “no_such_quest”"));
        }

        [Test]
        public void AnUnknownKeyItemIsReported()
        {
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-all-key-items", "99999999");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "unknown-key-item"
                && problem.Message.Contains("no key item called “99999999”"));
        }

        [Test]
        public void AKeyItemThatExistsIsNotReported()
        {
            // 79 is KeyItemType.DantooineShovel - a real key item, so nothing should flag it.
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-all-key-items", "79");

            var problems = Analyzer.Analyze(document);

            problems.Should().NotContain(problem => problem.RuleId == "unknown-key-item");
        }

        [Test]
        public void AnUnknownFactionIsReported()
        {
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-has-faction-standing", "99999999 5");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "unknown-faction"
                && problem.Message.Contains("no faction called “99999999”"));
        }

        [Test]
        public void AFactionThatExistsIsNotReported()
        {
            // 7 is FactionType.Czerka - a real faction, so nothing should flag it.
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-has-faction-standing", "7 5");

            var problems = Analyzer.Analyze(document);

            problems.Should().NotContain(problem => problem.RuleId == "unknown-faction");
        }

        [Test]
        public void AnUnknownSkillIsReported()
        {
            // The exact shape from the review: an unknown skill with a required rank of 0 used to
            // preview as passing, because ReachabilityEvaluator treated the raw identifier as a
            // valid key with GetSkillRank defaulting to 0. It is now reported as broken instead.
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-any-skill", "no_such_skill 0");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "unknown-skill"
                && problem.Message.Contains("no skill called “no_such_skill”"));
        }

        [Test]
        public void ASkillNamedByItsEnumMemberIsNotReported()
        {
            // Skills are stored by name in the corpus - the same form SkillType.TryParse reads.
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-any-skill", "Force 10");

            var problems = Analyzer.Analyze(document);

            problems.Should().NotContain(problem => problem.RuleId == "unknown-skill");
        }

        [Test]
        public void ASkillNamedByItsNumberIsNotReported()
        {
            // 5 is SkillType.Force - skills may also be stored by their integer value.
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-any-skill", "5 10");

            var problems = Analyzer.Analyze(document);

            problems.Should().NotContain(problem => problem.RuleId == "unknown-skill");
        }

        [Test]
        public void AnIncompleteRepeatingArgumentGroupIsBroken()
        {
            var document = NewConversation();
            document.Openings[0].AddCondition("condition-all-skills", "1 10 2");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "incomplete-repeat-group" &&
                problem.Severity == ProblemSeverity.Broken &&
                problem.Message.Contains("incomplete repeating set"));
        }

        [Test]
        public void ALineNothingLeadsToIsReportedWithItsText()
        {
            var document = DantHerbs();
            document.AddEntry("Nobody can hear this.");

            var problems = Analyzer.Analyze(document);

            problems.Should().Contain(problem =>
                problem.RuleId == "nothing-leads-here"
                && problem.Message.Contains("Nobody can hear this."));
        }

        [Test]
        public void AShippedConversationIsNotDrownedInFindings()
        {
            // If a clean-ish file produces a wall of findings, the panel gets ignored. dantherbs has
            // one real break and a handful of style hints, and that ratio is the point.
            var problems = Analyzer.Analyze(DantHerbs());

            problems.Count(problem => problem.Severity == ProblemSeverity.Broken).Should().Be(1);
            problems.Should().OnlyContain(problem =>
                problem.Severity != ProblemSeverity.Untidy || problem.RuleId == "quest-beat-missing");
        }

        [Test]
        public void EveryConversationInTheModuleCanBeAnalyzedWithoutThrowing()
        {
            var failures = new List<string>();
            foreach (var path in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "dlg"), "*.json"))
            {
                try
                {
                    Analyzer.Analyze(DlgDocument.Load(path));
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            failures.Should().BeEmpty();
        }

        // ---------- the scaffold ----------

        [Test]
        public void ThePreviewNamesEverySituationItWillCreate()
        {
            var scaffold = new QuestConversationScaffold(GameCode);

            var beats = scaffold.Preview("field_tinctures");

            beats.Select(beat => beat.Title).Should().Equal(
                "Finished Field Tinctures",
                "Ready to hand in Field Tinctures",
                "On step 1 of Field Tinctures",
                "Offering Field Tinctures",
                "First meeting");
        }

        [Test]
        public void ThePreviewExplainsEachSituationFromTheQuestItself()
        {
            var beats = new QuestConversationScaffold(GameCode).Preview("field_tinctures");

            beats[0].Explanation.Should().Contain("Not repeatable");

            // The gated case lives on the catch-all rather than in an opening of its own, because a
            // guard that is the exact complement of the offer's leaves one of the two dead.
            beats[^1].Explanation.Should().Contain("has not finished Harvesting Herbs");
        }

        [Test]
        public void ARepeatableQuestIsExplainedDifferently()
        {
            var beats = new QuestConversationScaffold(GameCode).Preview("harvest_herbs");

            beats[0].Explanation.Should().Contain("Repeatable");
            beats[^1].Explanation.Should().NotContain("has not finished");
        }

        [Test]
        public void ScaffoldingAQuestProducesSituationsInWorkingOrder()
        {
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var situations = new SituationModel(document, Evaluator, GameCode).Situations();

            // The template's own placeholder greeting is the catch-all, so the scaffold adds none of
            // its own and everything it creates is lifted above it.
            situations.Select(situation => situation.Title).Should().Equal(
                "Finished Field Tinctures",
                "On step 2 of Field Tinctures",
                "On step 1 of Field Tinctures",
                "Offering Field Tinctures",
                "First meeting");
        }

        [Test]
        public void ScaffoldingARepeatableQuestLeavesTheOfferReachableAfterCompletion()
        {
            // harvest_herbs is repeatable: a completed player still satisfies
            // condition-can-accept-quest, and openings are first-match-wins, so a finished
            // placeholder above the offer would lock them out of ever restarting the quest.
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "harvest_herbs");

            var situations = new SituationModel(document, Evaluator, GameCode).Situations();

            situations.Select(situation => situation.Title).Should().NotContain(
                title => title.StartsWith("Finished"),
                "a repeatable quest must not shadow its own offer with a finished opening");

            var completed = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);
            var resolved = Evaluator.ResolveOpening(document, completed);
            resolved.Should().NotBeNull();
            resolved!.Conditions.Select(condition => condition.SnippetKey).Should().Contain(
                "condition-can-accept-quest",
                "the completed player must land on the offer opening and be able to restart");
        }

        [Test]
        public void TheScaffoldDoesNotAddASecondGreeting()
        {
            var document = NewConversation();
            var scaffold = new QuestConversationScaffold(GameCode);

            scaffold.Preview("field_tinctures", document).Should()
                .NotContain(beat => beat.Title == "First meeting");

            scaffold.Apply(document, "field_tinctures");

            document.Openings.Count(opening => opening.Conditions.Count == 0).Should().Be(1);
        }

        [Test]
        public void EverySituationTheScaffoldCreatesIsReachable()
        {
            // The whole point: a writer finds a shape that already works rather than assembling one.
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var model = new SituationModel(document, Evaluator, GameCode);
            foreach (var situation in model.Situations())
            {
                situation.State.Should().NotBe(SituationState.Unreachable,
                    $"situation {situation.Order} ({situation.Title}) must be reachable");
                model.PlayerFor(situation).Should().NotBeNull();
            }
        }

        [Test]
        public void TheScaffoldCoversEveryStepOfTheQuest()
        {
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var coverage = new SituationModel(document, Evaluator, GameCode).Coverage();

            coverage.Single(quest => quest.QuestId == "field_tinctures").IsComplete.Should().BeTrue();
        }

        [Test]
        public void TheScaffoldWiresTheQuestActionsToTheRightReplies()
        {
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var actions = document.Entries.Concat(document.Replies)
                .SelectMany(node => node.Actions)
                .Select(action => $"{action.SnippetKey} {action.Value}")
                .ToList();

            actions.Should().BeEquivalentTo(
                "action-accept-quest field_tinctures",
                "action-request-quest-items field_tinctures",
                "action-advance-quest field_tinctures");
        }

        [Test]
        public void EveryLineTheScaffoldLeavesBlankIsMarkedForTheWriter()
        {
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var unwritten = document.Entries.Concat(document.Replies)
                .Count(node => node.Text == QuestConversationScaffold.Placeholder);

            unwritten.Should().BeGreaterThan(8);
            document.Entries.Concat(document.Replies).Should()
                .OnlyContain(node => !string.IsNullOrWhiteSpace(node.Text));
        }

        [Test]
        public void ScaffoldingIntoAnExistingConversationLiftsTheNewSituationsAboveTheGreeting()
        {
            // Appended openings would sit below the existing catch-all, where none of them could
            // ever fire. This is the case that made reordering part of the scaffold.
            var document = DantHerbs();
            var before = document.Openings.Count;

            new QuestConversationScaffold(GameCode).Apply(document, "fetch_pet_treat");

            var situations = new SituationModel(document, Evaluator, GameCode).Situations();
            var scaffolded = situations.Take(document.Openings.Count - before).ToList();

            scaffolded.Should().OnlyContain(situation => situation.State != SituationState.Unreachable);
            scaffolded[0].Title.Should().Be("Finished Fetch Pet Treat Quest");
        }

        [Test]
        public void TheScaffoldedConversationSurvivesASaveAndReload()
        {
            var document = NewConversation();
            new QuestConversationScaffold(GameCode).Apply(document, "field_tinctures");

            var reloaded = DlgDocument.Parse(document.ToBytes());

            reloaded.Openings.Should().HaveCount(document.Openings.Count);
            reloaded.FindDanglingLinks().Should().BeEmpty();
            new SituationModel(reloaded, Evaluator, GameCode).Situations()
                .Should().OnlyContain(situation => situation.State != SituationState.Unreachable);
        }
    }
}
