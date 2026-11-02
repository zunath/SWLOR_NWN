using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A job saved in Redis opens as a fresh window after a restart, so the Complete Job button must follow the saved
    /// stage and end time alone.
    /// </summary>
    public static class IncubatorCompleteJobEngineTests
    {
        [EngineTest("A finished incubation job opened in a new window can be completed", Category = "Incubation")]
        public static async Task FinishedJobCanBeCompleted(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var payload = new IncubatorPayload("engine-incubator-complete/" + fixture.Id, string.Empty);
            var job = SaveJob(payload.PropertyId, fixture, 3, DateTime.UtcNow.AddMinutes(-5));
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    ctx.AssertEqual("STAGE 3 COMPLETE", model.JobProgressTime, "The finished job shows its last stage complete");
                    ctx.Assert(model.IsCompleteJobEnabled, "Complete Job is enabled for a finished job");

                    model = ReopenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    ctx.Assert(model.IsCompleteJobEnabled, "Complete Job stays enabled after the window reopens");

                    var eggs = CountCarried(player, BeastMastery.BeastEggResref);
                    model.OnClickCompleteJob()();
                    model.OnModalConfirmClick()();
                    ctx.AssertEqual(eggs + 1, CountCarried(player, BeastMastery.BeastEggResref), "Completing the job gives the egg");
                    ctx.Assert(!DB.Exists<IncubationJob>(job.Id), "Completing the job removes it");
                });
            }
            finally
            {
                CloseWindows(fixture);
                DB.Delete<IncubationJob>(job.Id);
            }
        }

        [EngineTest("An unfinished incubation job cannot be completed", Category = "Incubation")]
        public static async Task UnfinishedJobCannotBeCompleted(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var payload = new IncubatorPayload("engine-incubator-unfinished/" + fixture.Id, string.Empty);
            var running = SaveJob(payload.PropertyId, fixture, 3, DateTime.UtcNow.AddHours(1));
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    ctx.Assert(!model.IsCompleteJobEnabled, "Complete Job is disabled while the last stage runs");

                    var eggs = CountCarried(player, BeastMastery.BeastEggResref);
                    model.OnClickCompleteJob()();
                    model.OnModalConfirmClick()();
                    ctx.AssertEqual(eggs, CountCarried(player, BeastMastery.BeastEggResref), "A running job gives no egg");
                    ctx.Assert(DB.Exists<IncubationJob>(running.Id), "A running job stays");

                    running.CurrentStage = 1;
                    running.DateCompleted = DateTime.UtcNow.AddMinutes(-5);
                    DB.Set(running);
                    model = ReopenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    ctx.AssertEqual("STAGE 1 COMPLETE", model.JobProgressTime, "A finished early stage shows complete");
                    ctx.Assert(!model.IsCompleteJobEnabled, "Complete Job stays disabled until the last stage finishes");
                });
            }
            finally
            {
                CloseWindows(fixture);
                DB.Delete<IncubationJob>(running.Id);
            }
        }

        private static IncubationJob SaveJob(string propertyId, PlayerAbilityFixture fixture, int stage, DateTime completed)
        {
            var job = new IncubationJob
            {
                ParentPropertyId = propertyId,
                PlayerId = fixture.Id,
                BeastDNAType = BeastType.Viper,
                CurrentStage = stage,
                DateStarted = completed.AddHours(-36),
                DateCompleted = completed
            };
            DB.Set(job);
            return job;
        }
    }
}
