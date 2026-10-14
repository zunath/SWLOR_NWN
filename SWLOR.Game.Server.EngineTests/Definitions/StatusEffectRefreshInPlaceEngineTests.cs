using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Hobble opts into in-place refresh: the same source re-applying it restarts the active effect with
    /// a fresh application's timing and triggers, and any other source still replaces it.
    /// </summary>
    public static class StatusEffectRefreshInPlaceEngineTests
    {
        private const string Resref = "nw_bandit001";

        [EngineTest("Hobble re-applied by the same source restarts in place with fresh timing and triggers", Category = "StatusEffect", TimeoutSeconds = 60f)]
        public static async Task HobbleRefreshesInPlace(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature(Resref);
            var otherCaster = ctx.SpawnCreature(Resref, -2f);
            var target = ctx.SpawnCreature(Resref, 2f);
            // Spawn initialization resets NPC stats and pools after the first frame.
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(target);
            foreach (var creature in new[] { caster, otherCaster, target })
            {
                SetAILevel(creature, AILevel.VeryLow);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
            }

            ctx.SetNPCResources(target, 100, 100);
            TemporaryStatModifier.Add(caster, StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Control, 60f);
            TemporaryStatModifier.Add(caster, StatType.StatusAppliedTargetStaminaDrain, 3, 60f);
            TemporaryStatModifier.Add(caster, StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 1, 60f);

            var staminaBefore = Stat.GetCurrentStamina(target);
            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, target, typeof(HobbleStatusEffect), 3f), "Hobble applies");
            var applied = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            ctx.Assert(applied != null, "the target has Hobble");
            ctx.AssertEqual(staminaBefore - 3, Stat.GetCurrentStamina(target), "the first application drains stamina");
            var slow = StatusEffect.GetStatAdjustment(target, StatType.MovementSpeedPercentAdjustment);
            ctx.Assert(slow < 0, "Hobble slows the target");

            // Past the stamina drain cooldown, so the refresh must trigger it again.
            await ctx.DelaySecondsAsync(1.5f);
            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, target, typeof(HobbleStatusEffect), 3f), "Hobble re-applies");
            var refreshedAt = DateTime.UtcNow;
            var refreshed = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            ctx.Assert(ReferenceEquals(applied, refreshed), "the same source keeps the active Hobble");
            ctx.AssertEqual(slow, StatusEffect.GetStatAdjustment(target, StatType.MovementSpeedPercentAdjustment),
                "the refresh does not add the slow again");
            ctx.AssertEqual(staminaBefore - 6, Stat.GetCurrentStamina(target), "the refresh still triggers the stamina drain");

            // A fresh application expires on the tick at its full duration; the refresh must too.
            var remaining = refreshed.GetRemainingDurationSeconds(refreshedAt);
            ctx.Assert(remaining > 0.9f, $"the refresh restarts the duration ({remaining:0.00} s left)");
            await ctx.DelaySecondsAsync(remaining - 0.5f);
            ctx.Assert(ReferenceEquals(applied, StatusEffect.GetStatusEffect<HobbleStatusEffect>(target)),
                "Hobble lasts its restarted duration");
            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<HobbleStatusEffect>(target), 1.3f,
                "Hobble to expire on time after the refresh");

            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, target, typeof(HobbleStatusEffect), 3f), "Hobble applies again");
            var fromCaster = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            ctx.Assert(StatusEffect.ApplyStatusEffect(otherCaster, target, typeof(HobbleStatusEffect), 3f), "another source applies Hobble");
            var fromOther = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            ctx.Assert(!ReferenceEquals(fromCaster, fromOther), "another source replaces the active Hobble");
            ctx.AssertEqual(otherCaster, fromOther.Source, "the replacement belongs to the new source");
            ctx.AssertEqual(slow, StatusEffect.GetStatAdjustment(target, StatType.MovementSpeedPercentAdjustment),
                "only one Hobble slows the target");
        }
    }
}
