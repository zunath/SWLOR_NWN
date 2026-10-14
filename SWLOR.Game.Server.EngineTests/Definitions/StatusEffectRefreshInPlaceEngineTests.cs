using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Hobble opts into in-place refresh: re-applying it restarts the active effect with a fresh
    /// application's timing and triggers, and another source takes it over as a replacement would.
    /// </summary>
    public static class StatusEffectRefreshInPlaceEngineTests
    {
        private const string Resref = "nw_bandit001";

        [EngineTest("Zone buffs re-applied by their pulses restart in place", Category = "StatusEffect", TimeoutSeconds = 60f)]
        public static async Task ZoneBuffsRefreshInPlace(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature(Resref);
            var ally = ctx.SpawnCreature(Resref, 2f);
            await ctx.DelaySecondsAsync(1f);
            foreach (var creature in new[] { caster, ally })
                SetAILevel(creature, AILevel.VeryLow);

            // Zone pulses re-apply these with 3.2 s every 3 s.
            foreach (var type in new[] { typeof(ForceSanctuary1StatusEffect), typeof(EmergencyBunker1StatusEffect), typeof(KoltoMistHealingStatusEffect) })
            {
                ctx.Assert(StatusEffect.ApplyStatusEffect(caster, ally, type, 3.2f), $"{type.Name} applies");
                var applied = StatusEffect.GetStatusEffect(ally, type);
                var stats = applied.StatGroup.Stats.Where(stat => stat.Value != 0)
                    .ToDictionary(stat => stat.Key, stat => StatusEffect.GetStatAdjustment(ally, stat.Key));

                await ctx.DelaySecondsAsync(1f);
                ctx.Assert(StatusEffect.ApplyStatusEffect(caster, ally, type, 3.2f), $"{type.Name} re-applies");
                ctx.Assert(ReferenceEquals(applied, StatusEffect.GetStatusEffect(ally, type)), $"the next pulse keeps the active {type.Name}");
                ctx.Assert(applied.GetRemainingDurationSeconds(DateTime.UtcNow) > 3f, $"the pulse restarts {type.Name}'s duration");
                foreach (var (stat, value) in stats)
                    ctx.AssertEqual(value, StatusEffect.GetStatAdjustment(ally, stat), $"{type.Name} does not add {stat} again");

                StatusEffect.RemoveStatusEffect(type, ally);
            }
        }

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
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(StatusEffect.ApplyStatusEffect(otherCaster, target, typeof(HobbleStatusEffect), 3f), "another source applies Hobble");
            var fromOther = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            ctx.Assert(ReferenceEquals(fromCaster, fromOther), "another source takes over the active Hobble");
            ctx.AssertEqual(otherCaster, fromOther.Source, "the Hobble now belongs to the new source, as a replacement would");
            ctx.Assert(fromOther.GetRemainingDurationSeconds(DateTime.UtcNow) > remaining - 0.2f,
                "the new source's application restarts the duration");
            ctx.AssertEqual(slow, StatusEffect.GetStatAdjustment(target, StatType.MovementSpeedPercentAdjustment),
                "only one Hobble slows the target");
        }
    }
}
