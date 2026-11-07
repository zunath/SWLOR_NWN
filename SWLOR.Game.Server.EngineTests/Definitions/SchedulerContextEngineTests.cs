using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What a scheduler task (the space AI tick, the mine check, the launch check) may rely on. A DelayCommand called
    /// straight from a scheduler task has no script to attach to and its closure never runs, so scheduled space code
    /// uses Scheduler.Schedule, or calls DelayCommand inside AssignCommand.
    /// </summary>
    public static class SchedulerContextEngineTests
    {
        [EngineTest("A scheduled task's follow-up runs through Scheduler.Schedule and an assigned DelayCommand, not a bare DelayCommand", Category = "SchedulerContext", TimeoutSeconds = 30f)]
        public static async Task DelayedFollowUpsFromAScheduledTask(EngineTestContext ctx)
        {
            var actor = ctx.SpawnCreature("nw_rat001");
            var scheduled = false;
            var assigned = false;
            var bare = false;
            Scheduler.Schedule(() =>
            {
                Scheduler.Schedule(() => scheduled = true, TimeSpan.FromSeconds(0.1));
                AssignCommand(actor, () => DelayCommand(0.1f, () => assigned = true));
                DelayCommand(0.1f, () => bare = true);
            }, TimeSpan.Zero);

            await ctx.WaitUntilAsync(() => scheduled && assigned, 5f, "the scheduled and the assigned follow-ups to run");
            await ctx.DelaySecondsAsync(2f);
            ctx.Assert(!bare, "a bare DelayCommand from a scheduled task started running: scheduled space code may use it again");
        }
    }
}
