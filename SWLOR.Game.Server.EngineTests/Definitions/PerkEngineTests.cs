using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Validates NPC perk level resolution: an NPC with no PERK_LEVEL_{id} local defaults to the
    /// perk's max level, and EngineTestContext.SetNPCPerkLevel caps the effective level via that
    /// local variable. Renewal (Force / Light Consular) has three levels, so it exercises both
    /// the default-to-max path and an explicit non-max cap.
    /// </summary>
    public static class PerkEngineTests
    {
        private const int RenewalMaxLevel = 3;

        [EngineTest("NPC perk level defaults to max and honors an explicit cap", Category = "Perk")]
        public static async Task NpcPerkLevelDefaultsToMaxAndRespectsCap(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");
            // Lets spawn-init settle and keeps the test on the Task-only contract.
            await ctx.WaitFrameAsync();

            ctx.AssertEqual(RenewalMaxLevel, Perk.GetPerkLevel(npc, PerkType.Renewal), "Uncapped NPC Renewal perk level");

            ctx.SetNPCPerkLevel(npc, PerkType.Renewal, 1);
            ctx.AssertEqual(1, Perk.GetPerkLevel(npc, PerkType.Renewal), "Renewal perk level after SetNPCPerkLevel(1)");
        }
    }
}
