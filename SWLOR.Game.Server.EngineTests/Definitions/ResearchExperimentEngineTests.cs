using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ResearchExperimentEngineTests
    {
        [EngineTest("Research experiments persist after every action and settle for a player who leaves", Category = "Research")]
        public static async Task ExperimentPersistsAndSettlesThroughTheDatabase(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();

            const string PlayerId = "research-experiment-player";
            var started = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
            var job = new ResearchJob
            {
                ParentPropertyId = "research-experiment-lab",
                PlayerId = PlayerId,
                DateStarted = started,
                DateCompleted = started.AddSeconds(3600),
                Level = 2,
                SerializedItem = string.Empty,
                Experiment = ResearchExperimentLifecycle.Open(3, ExperimentRules.Trials, 3600, true)
            };
            var legacy = new ResearchJob
            {
                ParentPropertyId = "research-experiment-legacy-lab",
                PlayerId = PlayerId,
                DateStarted = started,
                DateCompleted = started.AddSeconds(3600),
                Level = 3,
                SerializedItem = string.Empty
            };

            try
            {
                var engine = new ExperimentEngine(
                    ServiceMinigameRandom.Instance,
                    new ExperimentSetup(3, 30, 10, 200, true));
                DB.Set(job);
                DB.Set(legacy);

                // Play a few actions, saving the job after each as the window does.
                for (var step = 0; step < 4; step++)
                {
                    var saved = DB.Get<ResearchJob>(job.Id);
                    engine.Perform(ExperimentActionIds.TestCreditReduction);
                    ResearchExperimentLifecycle.Record(saved, engine.Snapshot());
                    DB.Set(saved);
                }

                var reloaded = DB.Get<ResearchJob>(job.Id);
                ctx.AssertEqual(4, reloaded.Experiment.TrialsUsed, "Every action is saved on the job");
                ctx.AssertEqual(engine.EvidenceOf(ResearchCandidate.CreditReduction),
                    reloaded.Experiment.Evidence[ResearchCandidate.CreditReduction], "Saved evidence matches the engine");
                ctx.Assert(ResearchExperimentLifecycle.IsOpen(reloaded), "The experiment is still open before the player leaves");
                ctx.Assert(DB.Get<ResearchJob>(legacy.Id).Experiment == null, "A job without an experiment loads without one");

                // A logout settles the open experiment, once, and leaves the legacy job alone.
                ctx.AssertEqual(1, ResearchExperimentService.SettleOpenExperiments(PlayerId), "One open experiment settles");
                ctx.AssertEqual(0, ResearchExperimentService.SettleOpenExperiments(PlayerId), "Settling again changes nothing");

                for (var reload = 0; reload < 2; reload++)
                {
                    var settled = JsonConvert.DeserializeObject<ResearchJob>(DB.GetRawJson<ResearchJob>(job.Id));
                    ctx.AssertEqual(ResearchExperimentStatus.Settled, settled.Experiment.Status, "The settled status is saved");
                    ctx.Assert(settled.Experiment.ExtraBonus, "The extra bonus rolled at the start is kept");
                    ctx.Assert(settled.Experiment.Winner != ResearchCandidate.None, "Evidence on a candidate stores a winner");

                    var blueprint = new BlueprintDetail { Level = 2 };
                    var bonuses = new List<int>();
                    var completion = ResearchGrantRules.Complete(blueprint, settled.Experiment, ServiceMinigameRandom.Instance, bonuses.Add);
                    ctx.Assert(completion.FromExperiment, "Completion grants the stored outcome");
                    ctx.AssertEqual(1, bonuses.Count, "The stored extra bonus is granted");
                }

                ctx.Assert(DB.Get<ResearchJob>(legacy.Id).Experiment == null, "The legacy job was not given an experiment");
            }
            finally
            {
                DB.Delete<ResearchJob>(job.Id);
                DB.Delete<ResearchJob>(legacy.Id);
            }
        }
    }
}
