using System.Threading.Tasks;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Drives Renewal I - a self-targetable, casted Force ability with an FP cost and a status
    /// effect impact - through the real UsePerkFeat.TryUseAbility pipeline and verifies the
    /// activator's FP pool is spent and the impact status effect lands after the activation
    /// delay elapses.
    /// </summary>
    public static class AbilityActivationEngineTests
    {
        private const int StartingFP = 50;
        private const int StartingStamina = 50;

        [EngineTest("Renewal I activation spends FP and applies its regeneration status effect", Category = "Ability", TimeoutSeconds = 30f)]
        public static async Task RenewalActivationSpendsFPAndAppliesStatusEffect(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");

            // Spawn initialization scripts reset the FP/STAMINA locals a frame after creation;
            // configuring resources before they run would let them overwrite the setup.
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(npc, StartingFP, StartingStamina);
            var fpBefore = Stat.GetCurrentFP(npc);

            // The activation must run in the caster's script context (line-of-sight and the
            // delayed impact both depend on OBJECT_SELF), matching the real feat-use event.
            var used = false;
            var attempted = false;
            AssignCommand(npc, () =>
            {
                used = UsePerkFeat.TryUseAbility(npc, npc, FeatType.Renewal1, GetLocation(npc));
                attempted = true;
            });
            await ctx.WaitUntilAsync(() => attempted, 5f, "the assigned activation command to execute");
            ctx.Assert(used, "TryUseAbility should report success activating Renewal I on its caster.");

            // Renewal I has a 1s activation delay before its impact (and cost deduction) apply;
            // give it generous margin.
            await ctx.WaitUntilAsync(
                () => StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc),
                10f,
                "Renewal I's regeneration status effect to appear on the caster after its activation delay");

            var remainingFP = Stat.GetCurrentFP(npc);
            ctx.Assert(remainingFP < fpBefore, $"FP should have decreased below its pre-activation value of {fpBefore} after casting Renewal I, but is {remainingFP}.");
        }
    }
}
