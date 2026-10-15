using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.RoleplayXPService;
using SWLOR.NWN.API.NWScript.Enum;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class RoleplayXPLoggingEngineTests
    {
        [EngineTest("RPXP OOC counts persist through skipped and completed distributions", Category = "RoleplayXP", TimeoutSeconds = 45f)]
        public static async Task DistributionCountsSurviveReload(EngineTestContext ctx)
        {
            using var owner = await PlayerAbilityFixture.CreateAsync(ctx);
            using var other = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            var recordMessage = typeof(RoleplayXP).GetMethod("ProcessOOCMessage", BindingFlags.NonPublic | BindingFlags.Static);
            var settings = ApplicationSettings.Get();
            var area = GetArea(owner.Creature);
            var wasEventArea = GetLocalBool(area, Communication.EventCommsAreaVariable);
            SetLocalBool(area, Communication.EventCommsAreaVariable, false);
            var expectedBalance = DB.Get<Player>(owner.Id).UnallocatedXP;
            var expectedLifetimeOOC = DB.Get<Player>(owner.Id).RoleplayProgress.OOCMessageCount;
            var expectedLifetimeXP = DB.Get<Player>(owner.Id).RoleplayProgress.TotalRPExpGained;
            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    var player = Reload(ctx, owner.Id);
                    player.RoleplayProgress.RPPoints = 49;
                    // Exercise a saved balance from before penalties were disabled.
                    player.RoleplayProgress.PendingOOCPenaltySteps = settings.RoleplayXPOOCPenaltyEnabled ? 0 : 3;
                    player.RoleplayProgress.OOCAllowanceDay = OOCAllowance.GetAllowanceDay(DateTime.UtcNow);
                    player.RoleplayProgress.DailyOOCMessageCount = settings.RoleplayXPDailyOOCAllowance;
                    DB.Set(player);
                    await ctx.ExecuteInCreatureContextAsync(owner.Creature, () =>
                    {
                        for (var index = 0; index < 3; index++)
                            recordMessage.Invoke(null, new object[] { owner.Creature, DB.Get<Player>(owner.Id), DateTime.UtcNow,
                                ChatChannel.PlayerTalk, "//RPXP persistence test" });
                    });
                    expectedLifetimeOOC += 3;
                    player = Reload(ctx, owner.Id);
                    ctx.AssertEqual(3UL, player.RoleplayProgress.OOCMessagesSinceLastDistribution, "All OOC messages survive save/reload");
                    ctx.AssertEqual(3, player.RoleplayProgress.PendingOOCPenaltySteps, "OOC penalties survive save/reload");
                    ctx.AssertEqual(expectedLifetimeOOC, player.RoleplayProgress.OOCMessageCount, "Lifetime count remains cumulative");

                    await Distribute(ctx, owner.Creature);
                    player = Reload(ctx, owner.Id);
                    ctx.AssertEqual(3UL, player.RoleplayProgress.OOCMessagesSinceLastDistribution, "Skipped payout preserves distribution count");
                    ctx.AssertEqual(3, player.RoleplayProgress.PendingOOCPenaltySteps, "Skipped payout preserves penalties");
                    ctx.AssertEqual(expectedBalance, player.UnallocatedXP, "Skipped payout does not award XP");

                    player.RoleplayProgress.RPPoints = 50;
                    // The second cycle covers a full reduction when enabled, and full XP when disabled.
                    if (cycle == 1) player.RoleplayProgress.PendingOOCPenaltySteps = OOCAllowance.MaxPenaltySteps;
                    var penaltyBalance = player.RoleplayProgress.PendingOOCPenaltySteps;
                    var xpBeforePenalty = 1500 + GetAbilityModifier(AbilityType.Social, owner.Creature) * 375;
                    var awarded = OOCAllowance.ApplyPenalty(xpBeforePenalty, player.RoleplayProgress.PendingOOCPenaltySteps,
                        settings.RoleplayXPOOCPenaltyEnabled ? settings.RoleplayXPOOCPenaltyPercent : 0);
                    expectedBalance += awarded;
                    expectedLifetimeXP += (ulong)awarded;
                    DB.Set(player);
                    await Distribute(ctx, owner.Creature);
                    player = Reload(ctx, owner.Id);
                    ctx.AssertEqual(0UL, player.RoleplayProgress.OOCMessagesSinceLastDistribution, "Completed payout resets the saved count");
                    ctx.AssertEqual(settings.RoleplayXPOOCPenaltyEnabled ? 0 : penaltyBalance,
                        player.RoleplayProgress.PendingOOCPenaltySteps, "Only enabled penalties consume the saved balance");
                    ctx.AssertEqual(0, player.RoleplayProgress.RPPoints, "Completed payout consumes RP points");
                    ctx.AssertEqual(expectedBalance, player.UnallocatedXP, "Saved balance matches actual award");
                    ctx.AssertEqual(expectedLifetimeXP, player.RoleplayProgress.TotalRPExpGained, "Saved lifetime XP matches actual award");
                    ctx.AssertEqual(expectedLifetimeOOC, player.RoleplayProgress.OOCMessageCount, "Payout preserves lifetime OOC count");
                    var otherPlayer = Reload(ctx, other.Id);
                    ctx.AssertEqual(0UL, otherPlayer.RoleplayProgress.OOCMessagesSinceLastDistribution, "Other character's count stays independent");
                    ctx.AssertEqual(0, otherPlayer.UnallocatedXP, "Other character receives no XP");
                }
            }
            finally
            {
                SetLocalBool(area, Communication.EventCommsAreaVariable, wasEventArea);
            }
        }

        private static async Task Distribute(EngineTestContext ctx, uint player)
        {
            SetLocalInt(player, "RP_SYSTEM_TICKS", 299);
            await ctx.ExecuteInCreatureContextAsync(player, RoleplayXP.DistributeRoleplayXP);
        }

        private static Player Reload(EngineTestContext ctx, string playerId)
        {
            // Read Redis JSON directly so the in-process entity cache cannot satisfy persistence assertions.
            var player = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(playerId));
            ctx.Assert(player != null, "Saved player record exists in Redis");
            ctx.AssertEqual(playerId, player.Id, "Reload retains the owning character ID");
            DB.Set(player);
            return player;
        }
    }
}
