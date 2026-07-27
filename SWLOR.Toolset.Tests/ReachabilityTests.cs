using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The evaluator is the editor's navigation model, so these are the tests that decide whether
    /// what a writer is shown matches what a player would get. Asserted against dantherbs, a real
    /// two-quest giver whose six openings cover every shape the module uses.
    /// </summary>
    public class ReachabilityTests
    {
        private static readonly SnippetCatalog Snippets = SnippetCatalog.Build();
        private static readonly IGameCodeIndex GameCode = new GameCodeIndex(GameServerSourceRoot);
        private static readonly ReachabilityEvaluator Evaluator = new(Snippets, GameCode);

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

        private static SituationModel Model(DlgDocument document) =>
            new(document, Evaluator, GameCode);

        // ---------- which opening wins ----------

        [Test]
        public void ANewPlayerGetsTheGreeting()
        {
            var document = DantHerbs();
            var opening = Evaluator.ResolveOpening(document, new PretendPlayer());

            opening.Should().NotBeNull();
            opening!.Target.Text.Should().StartWith("Greetings, traveler.");
        }

        [Test]
        public void APlayerPartwayThroughTheSecondQuestGetsTheReminder()
        {
            var document = DantHerbs();
            var player = new PretendPlayer()
                .WithQuest("harvest_herbs", QuestProgress.Completed)
                .WithQuest("field_tinctures", QuestProgress.OnStep(1));

            var opening = Evaluator.ResolveOpening(document, player);

            opening!.Target.Text.Should().StartWith("The bottles are lined up and empty.");
        }

        [Test]
        public void APlayerReadyToHandInGetsTheTurnInLine()
        {
            var document = DantHerbs();
            var player = new PretendPlayer()
                .WithQuest("harvest_herbs", QuestProgress.Completed)
                .WithQuest("field_tinctures", QuestProgress.OnStep(2));

            var opening = Evaluator.ResolveOpening(document, player);

            opening!.Target.Text.Should().StartWith("That's enough.");
        }

        [Test]
        public void APlayerWhoFinishedEverythingGetsTheClosingLine()
        {
            var document = DantHerbs();
            var player = new PretendPlayer()
                .WithQuest("harvest_herbs", QuestProgress.Completed)
                .WithQuest("field_tinctures", QuestProgress.Completed);

            var opening = Evaluator.ResolveOpening(document, player);

            opening!.Target.Text.Should().StartWith("The tinctures held.");
        }

        [Test]
        public void APlayerWhoFinishedTheFirstQuestIsOfferedTheSecond()
        {
            var document = DantHerbs();
            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);

            var opening = Evaluator.ResolveOpening(document, player);

            opening!.Target.Text.Should().StartWith("Hold a moment.");
        }

        [Test]
        public void TheFirstMatchWins_NotTheMostSpecific()
        {
            // Both the "finished Harvesting Herbs" line and the offer fit this player. The offer is
            // listed first, so that is what they hear - the ordering IS the meaning.
            var document = DantHerbs();
            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);

            var winner = Evaluator.ResolveOpening(document, player);
            var alsoFits = document.Openings
                .Where(opening => Evaluator.Evaluate(opening, player).IsOpen)
                .ToList();

            alsoFits.Count.Should().BeGreaterThan(1);
            winner!.Struct.Should().BeSameAs(alsoFits[0].Struct);
        }

        // ---------- guards ----------

        [Test]
        public void ANegatedGuardInvertsItsVerdict()
        {
            var document = DantHerbs();
            var offer = document.Openings.Single(o => o.Target.Text.StartsWith("Hold a moment."));

            var eligible = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);
            var alreadyDoing = new PretendPlayer()
                .WithQuest("harvest_herbs", QuestProgress.Completed)
                .WithQuest("field_tinctures", QuestProgress.OnStep(1));

            Evaluator.Evaluate(offer, eligible).IsOpen.Should().BeTrue();
            Evaluator.Evaluate(offer, alreadyDoing).IsOpen.Should()
                .BeFalse("the offer is guarded by !condition-has-quest");
        }

        [Test]
        public void EveryGuardMustPass_ThereIsNoOr()
        {
            var document = DantHerbs();
            var offer = document.Openings.Single(o => o.Target.Text.StartsWith("Hold a moment."));

            // Satisfies the negated pair but not the prerequisite quest.
            var result = Evaluator.Evaluate(offer, new PretendPlayer());

            result.Guards.Should().HaveCount(3);
            result.Guards.Count(guard => guard.Outcome == GuardOutcome.Passes).Should().Be(2);
            result.IsOpen.Should().BeFalse();
        }

        [Test]
        public void AGuardReadsAsASentenceWithTheQuestsRealName()
        {
            var document = DantHerbs();
            var reminder = document.Openings.Single(o => o.Target.Text.StartsWith("The bottles are lined up"));

            Evaluator.Describe(reminder.Conditions[0])
                .Should().Be("the player is doing Field Tinctures");
        }

        [Test]
        public void AnUnknownRuleIsReportedRatherThanGuessed()
        {
            var document = DantHerbs();
            var opening = document.Openings[0];
            opening.AddCondition("condition-invented", "whatever");

            var result = Evaluator.Evaluate(opening, new PretendPlayer());

            result.Guards[^1].Outcome.Should().Be(GuardOutcome.NotSimulated);
            result.IsUncertain.Should().BeTrue();
            result.Guards[^1].Sentence.Should().Contain("unknown rule");
        }

        // ---------- walking ----------

        [Test]
        public void AcceptingAQuestPutsThePlayerOnItsFirstStep()
        {
            var document = DantHerbs();
            var accept = document.Replies.Single(r => r.Text == "I'll bring the innards and blood sample.");

            var after = Evaluator.ApplyActions(accept, new PretendPlayer());

            after.GetQuest("field_tinctures").CurrentState.Should().Be(1);
            after.GetQuest("field_tinctures").IsInProgress.Should().BeTrue();
        }

        [Test]
        public void AdvancingPastTheLastStepCompletesTheQuest()
        {
            var document = DantHerbs();
            var collect = document.Replies.Single(r => r.Text == "[Take the clinic's hazard pay.]");

            var player = new PretendPlayer().WithQuest("field_tinctures", QuestProgress.OnStep(2));
            var after = Evaluator.ApplyActions(collect, player);

            after.GetQuest("field_tinctures").IsCompleted.Should()
                .BeTrue("Field Tinctures has two steps, so advancing from the second finishes it");
        }

        [Test]
        public void FactionActionsChangeTheStateReadByLaterGuards()
        {
            var document = DantHerbs();
            var reply = document.AddReply("Adjust faction.");
            reply.AddAction("action-give-faction-standing", "7 12");
            reply.AddAction("action-take-faction-standing", "7 -2");
            reply.AddAction("action-give-faction-points", "7 8");
            reply.AddAction("action-take-faction-points", "7 3");

            var after = Evaluator.ApplyActions(
                reply,
                new PretendPlayer().WithFactionStanding(7, 5).WithFactionPoints(7, 2));

            after.GetFactionStanding(7).Should().Be(15);
            after.GetFactionPoints(7).Should().Be(7);
        }

        [Test]
        public void AWholeQuestArcCanBeWalkedWithoutAServer()
        {
            var document = DantHerbs();
            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);

            // Offer -> ask -> her answer -> accept. A conversation alternates, so picking a choice
            // leads to an NPC line, and that line carries the next set of choices.
            var offer = Evaluator.ResolveOpening(document, player)!;
            var ask = Evaluator.VisibleChoices(offer.Target, player)
                .Single(choice => choice.Target.Text == "What do you need this time?");
            var herAnswer = Evaluator.ResolveNextLine(ask.Target, player)!;
            herAnswer.Target.Text.Should().StartWith("Three Wild Innards");

            var accept = Evaluator.VisibleChoices(herAnswer.Target, player)
                .Single(choice => choice.Target.Text.StartsWith("I'll bring"));
            player = Evaluator.ApplyActions(accept.Target, player);

            // Now the reminder is what she opens with.
            Evaluator.ResolveOpening(document, player)!.Target.Text
                .Should().StartWith("The bottles are lined up");

            // Hand in, and the turn-in opening takes over.
            player.WithQuest("field_tinctures", QuestProgress.OnStep(2));
            var turnIn = Evaluator.ResolveOpening(document, player)!;
            turnIn.Target.Text.Should().StartWith("That's enough.");

            var collect = Evaluator.VisibleChoices(turnIn.Target, player).Single();
            player = Evaluator.ApplyActions(collect.Target, player);

            Evaluator.ResolveOpening(document, player)!.Target.Text
                .Should().StartWith("The tinctures held.");
        }

        // ---------- situations ----------

        [Test]
        public void TheSituationsAreTheOpeningsInOrder()
        {
            var document = DantHerbs();
            var situations = Model(document).Situations();

            situations.Should().HaveCount(8);
            situations.Select(situation => situation.Order).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
            situations[^1].IsCatchAll.Should().BeTrue();
            situations[^1].Title.Should().Be("First meeting");
        }

        [Test]
        public void SituationsAreTitledByWhatThePlayerIsDoing()
        {
            var document = DantHerbs();
            var titles = Model(document).Situations().Select(situation => situation.Title).ToList();

            titles.Should().Contain("Finished Field Tinctures");
            titles.Should().Contain("On step 2 of Field Tinctures");
            titles.Should().Contain("Doing Field Tinctures");
            titles.Should().Contain("Offering Field Tinctures");
        }

        [Test]
        public void ASituationsWhenClauseReadsAsPlainEnglish()
        {
            var document = DantHerbs();
            var offer = Model(document).Situations().Single(s => s.Title == "Offering Field Tinctures");

            offer.When.Should().Be(
                "the player has finished Harvesting Herbs, and the player has not finished Field Tinctures, "
                + "and the player is not doing Field Tinctures");
        }

        [Test]
        public void AShippedConversationCarriesAnOpeningNoPlayerCanEverReach()
        {
            // A real finding, not a fixture. "Thanks for your help!" is guarded on having finished
            // Harvesting Herbs - but the offer above it catches every such player who has not
            // engaged with Field Tinctures, and the three above THAT catch everyone who has. No
            // combination of quest states reaches it, so nobody has ever seen this line.
            var document = DantHerbs();
            var model = Model(document);

            var unreachable = model.Situations()
                .Where(situation => situation.State == SituationState.Unreachable)
                .ToList();

            unreachable.Should().ContainSingle();
            unreachable[0].Order.Should().Be(5);
            unreachable[0].Opening.Target.Text.Should().StartWith("Thanks for your help!");
            model.PlayerFor(unreachable[0]).Should().BeNull();
        }

        [Test]
        public void EveryOtherSituationInThatConversationIsWritten()
        {
            var document = DantHerbs();

            Model(document).Situations()
                .Where(situation => situation.Order != 5)
                .Should().OnlyContain(situation => situation.State == SituationState.Written);
        }

        [Test]
        public void AnOpeningAddedBelowTheCatchAllIsReportedAsUnreachable()
        {
            // The one mistake this format makes easy, and the reason the rail exists.
            var document = DantHerbs();
            var entry = document.AddEntry("Nobody will ever hear this.");
            var opening = document.AddOpening(entry);
            opening.AddCondition("condition-has-quest", "field_tinctures");

            var situations = Model(document).Situations();

            situations[^1].State.Should().Be(SituationState.Unreachable);
            situations[^1].Order.Should().Be(9);
        }

        [Test]
        public void AnEmptyOpeningIsReportedAsNotYetWritten()
        {
            // Isolating "empty" takes some care, because unreachable is reported first as the more
            // urgent fact. The catch-all has to go, and the new guard has to be one no earlier
            // opening already claims - reusing an existing quest guard would make this genuinely
            // unreachable rather than merely unwritten.
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(opening => opening.Conditions.Count == 0));

            var entry = document.AddEntry();
            var opening = document.AddOpening(entry);
            opening.AddCondition("condition-has-completed-tutorial");

            var situation = Model(document).Situations()[^1];
            situation.State.Should().Be(SituationState.Empty);
            situation.When.Should().Be("the player has finished the tutorial on some character");
        }

        [Test]
        public void SelectingASituationProducesAPlayerWhoActuallyReachesIt()
        {
            // What clicking a situation in the rail has to do: not merely satisfy that opening, but
            // break every earlier one that would have caught the player first.
            var document = DantHerbs();
            var model = Model(document);

            foreach (var situation in model.Situations())
            {
                var player = model.PlayerFor(situation);
                if (situation.State == SituationState.Unreachable)
                {
                    player.Should().BeNull($"situation {situation.Order} ({situation.Title}) cannot be reached");
                    continue;
                }

                player.Should().NotBeNull($"situation {situation.Order} ({situation.Title}) should be reachable");
                var reached = Evaluator.ResolveOpening(document, player!);

                reached.Should().NotBeNull($"situation {situation.Order} ({situation.Title}) should be reachable");
                reached!.Struct.Should().BeSameAs(situation.Opening.Struct,
                    $"situation {situation.Order} ({situation.Title}) is what that player should get");
            }
        }

        // ---------- coverage ----------

        [Test]
        public void CoverageReportsACellPerQuestStep()
        {
            var document = DantHerbs();
            var coverage = Model(document).Coverage();

            var tinctures = coverage.Single(quest => quest.QuestId == "field_tinctures");
            tinctures.Name.Should().Be("Field Tinctures");
            tinctures.IsRepeatable.Should().BeFalse();
            tinctures.Cells.Select(cell => cell.Label).Should().Equal("OFFER", "1", "2", "DONE");
        }

        [Test]
        public void AFullyWrittenQuestIsFullyCovered()
        {
            var document = DantHerbs();
            var coverage = Model(document).Coverage();

            coverage.Should().OnlyContain(quest => quest.IsComplete);
        }

        [Test]
        public void RemovingTheTurnInLineLeavesAGapInTheCoverage()
        {
            var document = DantHerbs();
            var turnIn = document.Openings.Single(o => o.Target.Text.StartsWith("That's enough."));
            document.RemoveLink(turnIn);

            var tinctures = Model(document).Coverage().Single(quest => quest.QuestId == "field_tinctures");

            tinctures.IsComplete.Should().BeFalse();
            tinctures.Cells.Single(cell => cell.Label == "2").IsCovered.Should().BeFalse();
        }

        /// <summary>
        /// Every opening in the module that no player can reach. Two shapes, both real:
        /// <list type="bullet">
        /// <item>a second unguarded greeting — the first answers everybody, so nothing below it
        /// fires. Four conversations do this, <c>dmfi_universal</c> twenty times;</item>
        /// <item><c>dantherbs</c> opening 5, where no combination of quest states escapes the
        /// openings above it.</item>
        /// </list>
        /// Pinned as a set so it cannot drift quietly in either direction: a new entry means
        /// someone shipped a dead line, and a vanished one means the detector stopped seeing
        /// something it used to.
        /// </summary>
        private static IEnumerable<string> KnownUnreachableOpenings()
        {
            yield return "dantherbs.dlg.json: opening 5 (Finished Harvesting Herbs)";
            yield return "dt_doc_velpo.dlg.json: opening 2 (First meeting)";

            foreach (var order in new[] { 2, 3 })
                yield return $"q1_nikka_larson.dlg.json: opening {order} (First meeting)";

            foreach (var order in new[] { 2, 3, 4, 5 })
                yield return $"zomb_telconv.dlg.json: opening {order} (First meeting)";

            for (var order = 2; order <= 21; order++)
                yield return $"dmfi_universal.dlg.json: opening {order} (First meeting)";
        }

        [Test]
        public void TheWholeModuleIsSweptForOpeningsNoPlayerCanReach()
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "dlg"), "*.json"))
            {
                var document = DlgDocument.Load(path);
                var model = new SituationModel(document, Evaluator, GameCode);
                foreach (var situation in model.Situations())
                {
                    if (situation.State == SituationState.Unreachable)
                        found.Add($"{Path.GetFileName(path)}: opening {situation.Order} ({situation.Title})");
                }
            }

            found.Should().BeEquivalentTo(KnownUnreachableOpenings());
        }

        [Test]
        public void TheDetectorDoesNotMistakeAQuestChainForADeadEnd()
        {
            // A capstone chain is five offers stacked in one conversation, each gated on finishing
            // the rung below. They are all reachable, and reading them as dead was the first thing
            // this detector got wrong - the prerequisites are declared through constants, so a scan
            // that only read string literals saw a chain with no links and concluded that the top
            // offer swallowed the rest.
            var document = DlgDocument.Load(
                Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "cq_absdef.dlg.json"));

            var situations = new SituationModel(document, Evaluator, GameCode).Situations();

            situations.Should().HaveCount(17);
            situations.Where(situation => situation.Title.StartsWith("Offering"))
                .Should().HaveCount(5)
                .And.OnlyContain(situation => situation.State == SituationState.Written);
        }

        [Test]
        public void CoverageNamesTheRepeatableQuestAsRepeatable()
        {
            var document = DantHerbs();
            var herbs = Model(document).Coverage().Single(quest => quest.QuestId == "harvest_herbs");

            herbs.Name.Should().Be("Harvesting Herbs");
            herbs.IsRepeatable.Should().BeTrue();
        }
    }
}
