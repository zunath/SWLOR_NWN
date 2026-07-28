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

        /// <summary>A conversation with no openings at all, for testing one guard shape in isolation.</summary>
        private static DlgDocument Blank()
        {
            var document = DantHerbs();
            foreach (var opening in document.Openings.ToList())
                document.RemoveLink(opening);

            return document;
        }

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

            // Field Tinctures declares harvest_herbs as a prerequisite - CanAccept would refuse
            // without it, so a player who could actually reach this reply already has it.
            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);
            var after = Evaluator.ApplyActions(accept, player);

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
        public void AcceptingAQuestAlreadyInProgressLeavesItsStepAlone()
        {
            // QuestDetail.CanAccept refuses once a quest is already under way. Firing anyway would
            // rewind harvest_herbs from step 2 back to step 1.
            var document = DantHerbs();
            var accept = document.Replies.Single(r => r.Text == "Of course, I'll gather the herbs for you.");

            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.OnStep(2));
            var after = Evaluator.ApplyActions(accept, player);

            after.GetQuest("harvest_herbs").CurrentState.Should().Be(2);
        }

        [Test]
        public void AcceptingAFinishedNonRepeatableQuestDoesNotRestartIt()
        {
            // Field Tinctures is not repeatable, so CanAccept refuses once it is completed - the
            // reply must not convert that completion back into step 1.
            var document = DantHerbs();
            var accept = document.Replies.Single(r => r.Text == "I'll bring the innards and blood sample.");

            var player = new PretendPlayer()
                .WithQuest("harvest_herbs", QuestProgress.Completed)
                .WithQuest("field_tinctures", QuestProgress.Completed);
            var after = Evaluator.ApplyActions(accept, player);

            after.GetQuest("field_tinctures").IsCompleted.Should().BeTrue();
            after.GetQuest("field_tinctures").CurrentState.Should().BeNull();
        }

        [Test]
        public void AcceptingAQuestWithoutItsPrerequisiteDoesNothing()
        {
            // Field Tinctures declares harvest_herbs as a prerequisite. CanAccept refuses without
            // it, so this reply must leave the player exactly as unstarted as it found them.
            var document = DantHerbs();
            var accept = document.Replies.Single(r => r.Text == "I'll bring the innards and blood sample.");

            var after = Evaluator.ApplyActions(accept, new PretendPlayer());

            after.GetQuest("field_tinctures").IsInProgress.Should().BeFalse();
            after.GetQuest("field_tinctures").CurrentState.Should().BeNull();
        }

        [Test]
        public void AcceptingASkillGatedQuestWithoutTheRankDoesNothing()
        {
            // primal_overrun_foundation carries a Beast Mastery rank-50 prerequisite; runtime
            // QuestDetail.CanAccept refuses below it, so the simulated accept must too.
            var document = DlgDocument.Load(
                Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "cq_primover.dlg.json"));
            var accept = document.Replies.Single(r => r.Text == "I'll clear the six and bring your slate.");

            var unqualified = Evaluator.ApplyActions(accept, new PretendPlayer());
            unqualified.GetQuest("primal_overrun_foundation").IsInProgress.Should().BeFalse(
                "a rank-0 pretend player cannot pass the rank-50 skill gate");

            var qualified = Evaluator.ApplyActions(
                accept, new PretendPlayer().WithSkill("BeastMastery", 50));
            qualified.GetQuest("primal_overrun_foundation").IsInProgress.Should().BeTrue(
                "the same accept succeeds once the prerequisite rank is met");
        }

        [Test]
        public void AcceptingARepeatableQuestAfterCompletionRestartsIt()
        {
            // Unlike Field Tinctures, harvest_herbs is repeatable - CanAccept allows a fresh accept
            // once it is completed, so this reply must still put the player back on step 1.
            var document = DantHerbs();
            var accept = document.Replies.Single(r => r.Text == "Of course, I'll gather the herbs for you.");

            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);
            var after = Evaluator.ApplyActions(accept, player);

            after.GetQuest("harvest_herbs").CurrentState.Should().Be(1);
            after.GetQuest("harvest_herbs").IsInProgress.Should().BeTrue();
        }

        [Test]
        public void AdvancingAnUnacceptedQuestDoesNothing()
        {
            // QuestDetail.Advance refuses when the player has not accepted the quest yet.
            var document = DantHerbs();
            var advance = document.Replies.Single(r => r.Text == "[Collect your reward]");

            var after = Evaluator.ApplyActions(advance, new PretendPlayer());

            after.GetQuest("harvest_herbs").IsInProgress.Should().BeFalse();
            after.GetQuest("harvest_herbs").CurrentState.Should().BeNull();
        }

        [Test]
        public void AdvancingAFinishedQuestDoesNothing()
        {
            // QuestDetail.Advance also refuses a quest that is already completed.
            var document = DantHerbs();
            var advance = document.Replies.Single(r => r.Text == "[Collect your reward]");

            var player = new PretendPlayer().WithQuest("harvest_herbs", QuestProgress.Completed);
            var after = Evaluator.ApplyActions(advance, player);

            after.GetQuest("harvest_herbs").IsCompleted.Should().BeTrue();
        }

        [Test]
        public void AKeyItemGivenByNameIsRecognizedWhenCheckedByItsNumericId()
        {
            // CZ220ShuttlePass is KeyItemType 5. An imported conversation may give a key item by its
            // enum member name and later check the same item by numeric id - the runtime resolves
            // both to the same KeyItemType (KeyItem.GetKeyItemTypeById/GetKeyItemTypeByName), so a
            // walk that stored the raw strings instead would see two different key items and report
            // the later guard as failing when the game would not.
            var document = DantHerbs();
            var reply = document.AddReply("Take the shuttle pass, named.");
            reply.AddAction("action-give-key-items", "CZ220ShuttlePass");

            var after = Evaluator.ApplyActions(reply, new PretendPlayer());

            after.HasKeyItem("5").Should().BeTrue();
        }

        [Test]
        public void AKeyItemGivenByNumericIdIsRecognizedWhenCheckedByName()
        {
            // The same canonicalization the other direction: given by id, checked by name.
            var document = DantHerbs();
            var reply = document.AddReply("Take the shuttle pass, by id.");
            reply.AddAction("action-give-key-items", "5");

            var after = Evaluator.ApplyActions(reply, new PretendPlayer());

            after.HasKeyItem("CZ220ShuttlePass").Should().BeTrue();
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
        public void TakingMoreFactionPointsThanOwnedFloorsAtZero()
        {
            // Faction.AdjustPlayerFactionPoints floors at zero rather than going negative - a
            // simulated take that ignores this would leave a later "has at least 0 points" guard
            // reading differently than the runtime does.
            var document = DantHerbs();
            var reply = document.AddReply("Take more points than she has.");
            reply.AddAction("action-take-faction-points", "7 50");

            var after = Evaluator.ApplyActions(reply, new PretendPlayer().WithFactionPoints(7, 10));

            after.GetFactionPoints(7).Should().Be(0);
        }

        [Test]
        public void GivingFactionStandingClampsToTheRuntimeMaximum()
        {
            // Faction.AdjustPlayerFactionStanding clamps to MaximumFaction rather than overshooting.
            var document = DantHerbs();
            var reply = document.AddReply("Give more standing than the scale allows.");
            reply.AddAction("action-give-faction-standing", "7 500");

            var after = Evaluator.ApplyActions(reply, new PretendPlayer().WithFactionStanding(7, 4900));

            after.GetFactionStanding(7).Should().Be(SWLOR.Game.Server.Service.Faction.MaximumFaction);
        }

        [Test]
        public void TakingFactionStandingClampsToTheRuntimeMinimum()
        {
            // Same clamp, the other direction.
            var document = DantHerbs();
            var reply = document.AddReply("Take more standing than the scale allows.");
            reply.AddAction("action-take-faction-standing", "7 500");

            var after = Evaluator.ApplyActions(reply, new PretendPlayer().WithFactionStanding(7, -4900));

            after.GetFactionStanding(7).Should().Be(SWLOR.Game.Server.Service.Faction.MinimumFaction);
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

        [Test]
        public void ANonQuestGuardCanBeBrokenSoALaterUnguardedOpeningIsReachable()
        {
            // A real gap TryBreak used to have: it only knew how to break quest guards, so an
            // earlier opening guarded only by "!condition-all-key-items K" reported the unguarded
            // opening below it as Unreachable even though a player who is given K bypasses the
            // first route and reaches the second in game.
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var guardedEntry = document.AddEntry("Only for those without the token.");
            var guarded = document.AddOpening(guardedEntry);
            guarded.AddCondition("!condition-all-key-items", "5001");

            var catchAllEntry = document.AddEntry("Nice to meet you.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.HasKeyItem("5001").Should().BeTrue();
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void ATutorialGuardCanBeBrokenSoALaterUnguardedOpeningIsReachable()
        {
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var guardedEntry = document.AddEntry("New to town?");
            var guarded = document.AddOpening(guardedEntry);
            guarded.AddCondition("!condition-has-completed-tutorial");

            var catchAllEntry = document.AddEntry("Welcome back.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.HasCompletedTutorial.Should().BeTrue();
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void ASkillGuardCanBeBrokenSoALaterUnguardedOpeningIsReachable()
        {
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var guardedEntry = document.AddEntry("Not trained enough to hear this yet.");
            var guarded = document.AddOpening(guardedEntry);
            guarded.AddCondition("!condition-any-skill", "7 3");

            var catchAllEntry = document.AddEntry("Everyone else.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetSkillRank("7").Should().BeGreaterThanOrEqualTo(3);
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void AFactionGuardCanBeBrokenSoALaterUnguardedOpeningIsReachable()
        {
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var guardedEntry = document.AddEntry("Not trusted enough to hear this yet.");
            var guarded = document.AddOpening(guardedEntry);
            guarded.AddCondition("!condition-has-faction-points", "7 50");

            var catchAllEntry = document.AddEntry("Everyone else.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetFactionPoints(7).Should().Be(50);
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void ANegatedQuestStateGuardCanBeBrokenByLandingOnOneOfItsListedSteps()
        {
            // !condition-on-quest-state q 3 4 used to be broken by treating "3" and "4" as bogus
            // quest ids of their own, each set to step 1 - q itself was never moved, the guard kept
            // passing on every retry, and the solver exhausted its budget reporting the catch-all
            // unreachable. Only q is a quest id here; 3 and 4 are the steps that break it.
            var document = Blank();

            var guardedEntry = document.AddEntry("Not on the right step yet.");
            var guarded = document.AddOpening(guardedEntry);
            guarded.AddCondition("!condition-on-quest-state", "q 3 4");

            var catchAllEntry = document.AddEntry("Everyone else.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetQuest("q").CurrentState.Should().BeOneOf(3, 4);
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void APositiveQuestStateGuardCanBeMovedToADifferentStepWhenTheTargetOnlyNeedsInProgress()
        {
            // The earlier opening requires q on step 1 specifically. The target only asks for q "in
            // progress" via condition-has-quest, so ApplyToSatisfy defaults it to step 1 too -
            // coinciding with the earlier guard. Because q is protected, the old breaker skipped it
            // and tried to clear the state argument "1" as if it were a second quest id, which did
            // nothing to either quest, so the earlier guard kept passing.
            var document = Blank();

            var firstEntry = document.AddEntry("Still on the very first step.");
            var first = document.AddOpening(firstEntry);
            first.AddCondition("condition-on-quest-state", "q 1");

            var secondEntry = document.AddEntry("Doing it, whatever the step.");
            var second = document.AddOpening(secondEntry);
            second.AddCondition("condition-has-quest", "q");

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == second.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetQuest("q").IsInProgress.Should().BeTrue();
            player.GetQuest("q").CurrentState.Should().NotBe(1);
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(second.Struct);
        }

        [Test]
        public void AQuestOfferGuardCanBeBrokenForARepeatableQuestByPuttingItInProgress()
        {
            // condition-can-accept-quest used to be broken by marking the quest Completed - but
            // CanAcceptQuest (both the runtime and ReachabilityEvaluator) still lets a completed
            // REPEATABLE quest be accepted again, so harvest_herbs's offer stayed open on every
            // retry and the solver exhausted its budget reporting the catch-all unreachable.
            var document = Blank();

            var offerEntry = document.AddEntry("Care to help me gather herbs?");
            var offer = document.AddOpening(offerEntry);
            offer.AddCondition("condition-can-accept-quest", "harvest_herbs");

            var catchAllEntry = document.AddEntry("Everyone else.");
            var catchAll = document.AddOpening(catchAllEntry);

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == catchAll.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetQuest("harvest_herbs").IsInProgress.Should().BeTrue(
                "in progress makes CanAcceptQuest false whether or not the quest is repeatable");
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(catchAll.Struct);
        }

        [Test]
        public void AFactionGuardCanBeBrokenWithinTheTargetsOwnAllowedRange()
        {
            // The earlier opening requires standing >= 10 (blocking below that). The target itself
            // requires standing < 20 - reachable anywhere from the runtime minimum through 19,
            // which includes 0-9. Identifier-only protection used to refuse touching faction 7 at
            // all just because the target also names it, even though dropping below 10 stays
            // entirely inside the range the target still allows.
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var firstEntry = document.AddEntry("Only the well-regarded may pass.");
            var first = document.AddOpening(firstEntry);
            first.AddCondition("condition-has-faction-standing", "7 10");

            var secondEntry = document.AddEntry("Not fully trusted, but welcome.");
            var second = document.AddOpening(secondEntry);
            second.AddCondition("!condition-has-faction-standing", "7 20");

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == second.Struct);
            situation.State.Should().Be(SituationState.Written);

            var player = model.PlayerFor(situation);
            player.Should().NotBeNull();
            player!.GetFactionStanding(7).Should().BeLessThan(10);
            Evaluator.ResolveOpening(document, player!)!.Struct.Should().BeSameAs(second.Struct);
        }

        [Test]
        public void ANonQuestGuardThatWouldContradictTheTargetLeavesItUnreachable()
        {
            // The other half of the fix: TryBreak must not remove a key item the target situation
            // depends on just to unblock an earlier opening. Here both openings need the same key
            // item present, so nobody who reaches the second was ever not caught by the first -
            // breaking the earlier one is only possible by taking away what the target needs too,
            // and that is refused.
            var document = DantHerbs();
            document.RemoveLink(document.Openings.Single(o => o.Conditions.Count == 0));

            var firstEntry = document.AddEntry("Ah, you carry the seal.");
            var first = document.AddOpening(firstEntry);
            first.AddCondition("condition-all-key-items", "5001");

            var secondEntry = document.AddEntry("The seal-bearer returns.");
            var second = document.AddOpening(secondEntry);
            second.AddCondition("condition-all-key-items", "5001");
            second.AddCondition("condition-has-completed-tutorial");

            var model = Model(document);
            var situation = model.Situations().Single(s => s.Opening.Struct == second.Struct);

            situation.State.Should().Be(SituationState.Unreachable);
            model.PlayerFor(situation).Should().BeNull();
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
