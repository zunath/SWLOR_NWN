using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    [TestFixture]
    public class ProspectorPolicyTests
    {
        private static ProspectingEngine Engine(int pool, string seams, int stability = 100, int charges = 50, int rank = 0, int seed = 3)
        {
            var list = ImmutableArray.CreateBuilder<ProspectingSeam>();
            foreach (var letter in seams)
            {
                list.Add(new ProspectingSeam(
                    char.ToLowerInvariant(letter) == 'g' ? ProspectingSeamKind.GemPocket : ProspectingSeamKind.Rich, char.IsUpper(letter)));
            }

            var node = new ProspectingNode(pool, stability, list.ToImmutable());
            return new ProspectingEngine(SimMinigameRandom.ForRun(seed, 1), ProspectingSetup.ForNode(node, charges, rank, 1));
        }

        private static string Choose(ProspectorPlan plan, ProspectingEngine engine) => new ProspectorPolicy(plan).Choose(engine);

        [Test]
        public void TheCasualPlayerOnlyStrikes()
        {
            var engine = Engine(3, "rg");

            Choose(ProspectorPlan.Casual, engine).Should().Be(ProspectingActionIds.Strike);
            Choose(ProspectorPlan.Casual, Engine(3, "RG")).Should().Be(ProspectingActionIds.Strike, "a casual player ignores the seams they can see");
        }

        [Test]
        public void ACarefulPlayerSurveysFirst_ThenExtractsWhatIsFound_ThenStrikes()
        {
            var plan = ProspectorPlan.Skilled[0];
            var engine = Engine(4, "rg");
            var policy = new ProspectorPolicy(plan);

            policy.Choose(engine).Should().Be(ProspectingActionIds.Survey);
            engine.Perform(ProspectingActionIds.Survey);
            policy.Choose(engine).Should().Be(ProspectingActionIds.Extract, "the plan surveys once, and extracts what it found");
            engine.Perform(ProspectingActionIds.Extract);
            policy.Choose(engine).Should().Be(ProspectingActionIds.Strike);
        }

        [Test]
        public void ACarefulPlayerKeepsEnoughStabilityToMineThePool()
        {
            // A pool of 4 takes about three Strikes; with 60 Stability left a Survey and an Extract would leave too little.
            var engine = Engine(4, "r", stability: 70);

            Choose(ProspectorPlan.Skilled[0], engine).Should().Be(ProspectingActionIds.Strike);
        }

        [Test]
        public void TheRecklessPlayerSpendsTheStabilityAnyway()
        {
            var engine = Engine(4, "r", stability: 70);

            Choose(ProspectorPlan.Reckless, engine).Should().Be(ProspectingActionIds.Survey);
        }

        [Test]
        public void ACarefulPlayerStopsSurveyingOnceASurveyFindsFewerSeamsThanItCanReveal()
        {
            var engine = Engine(2, "r");
            var policy = new ProspectorPolicy(ProspectorPlan.Skilled[2]);
            policy.Choose(engine).Should().Be(ProspectingActionIds.Survey);
            engine.Perform(ProspectingActionIds.Survey);
            policy.Choose(engine).Should().Be(ProspectingActionIds.Extract);
            engine.Perform(ProspectingActionIds.Extract);

            // The first Survey could reveal 1 and did: another may find more. The second finds nothing, so the player stops.
            policy.Choose(engine).Should().Be(ProspectingActionIds.Survey);
            engine.Perform(ProspectingActionIds.Survey);
            policy.Choose(engine).Should().Be(ProspectingActionIds.Strike);
        }

        [Test]
        public void AHeavyPlayerUsesHeavyStrikesOnlyOnceNoGemPocketCanBeShattered()
        {
            var plan = ProspectorPlan.Fast[1];
            var hidden = Engine(4, "rg", stability: 100);
            var policy = new ProspectorPolicy(plan);

            // Still surveying.
            policy.Choose(hidden).Should().Be(ProspectingActionIds.Survey);

            // With every seam found and claimed, a pool of 3 or more is taken with a Heavy Strike.
            var claimed = Engine(3, "");
            var finished = new ProspectorPolicy(plan);
            finished.Choose(claimed).Should().Be(ProspectingActionIds.Survey);
            claimed.Perform(ProspectingActionIds.Survey);
            finished.Choose(claimed).Should().Be(ProspectingActionIds.HeavyStrike);
        }

        [Test]
        public void APlayerWhoNeverExtractsMayHeavyStrikeAtOnce()
        {
            Choose(ProspectorPlan.Fast[2], Engine(4, "rg")).Should().Be(ProspectingActionIds.HeavyStrike);
        }

        [Test]
        public void APlayerWhoCannotAffordAHeavyStrikeStrikesInstead()
        {
            Choose(ProspectorPlan.Fast[2], Engine(4, "", charges: 1)).Should().Be(ProspectingActionIds.Strike);
        }

        [Test]
        public void ThePlayerWillNotSpendChargesOnASeamWhenTheyCannotKeepTheReserve()
        {
            // One unit left and two charges: an Extract would leave no charge to mine the pool, so the player strikes it.
            var engine = Engine(1, "R", charges: 2);

            Choose(ProspectorPlan.Skilled[0], engine).Should().Be(ProspectingActionIds.Strike);
        }

        [Test]
        public void APolicyWorksOnlyTheProspectingEngine()
        {
            var act = () => new ProspectorPolicy(ProspectorPlan.Casual).Choose(new SWLOR.CraftSim.Tests.Support.ToyEngine(SimMinigameRandom.ForRun(1, 1)));

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void EverySkilledPlanIsDistinct_AndTheFastOnesAreNotAmongThem()
        {
            ProspectorPlan.Skilled.Distinct().Should().HaveCount(ProspectorPlan.Skilled.Count);
            ProspectorPlan.Skilled.Should().NotContain(plan => plan.HeavyStrikes);
            ProspectorPlan.Fast.Should().OnlyContain(plan => plan.HeavyStrikes);
            ProspectorPlan.Skilled.Should().Contain(ProspectorPlan.Casual);
            ProspectorPlan.All.Distinct().Should().HaveCount(ProspectorPlan.All.Count);
            ProspectorPlan.Cautious.Should().OnlyContain(plan => plan.Reserve == ProspectorReserve.Cautious && plan.ExtractSeams);
            ProspectorPlan.Reckless.Reserve.Should().Be(ProspectorReserve.None);
        }

        [Test]
        public void EveryPlanPlaysANodeToAnEnd()
        {
            foreach (var plan in ProspectorPlan.All.Append(ProspectorPlan.Reckless))
            {
                for (var seed = 1; seed <= 20; seed++)
                {
                    var random = SimMinigameRandom.ForRun(seed, 5);
                    var engine = new ProspectingEngine(
                        random, ProspectingSetup.ForNode(ProspectingNode.Create(random, 0.1), 50, 25, 3));

                    CraftPlayer.Play(engine, new ProspectorPolicy(plan), ProspectingPlays.ActionLimit);

                    engine.Status.Should().NotBe(MinigameStatus.InProgress, $"{plan.Name}, seed {seed}");
                    engine.Items.Should().BeGreaterThan(0);
                }
            }
        }
    }
}
