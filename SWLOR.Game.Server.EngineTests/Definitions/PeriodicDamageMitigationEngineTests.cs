using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PeriodicDamageMitigationEngineTests
    {
        [EngineTest("Toxin and Shock ticks honor shared damage reduction and its cap", Category = "StatusEffect", TimeoutSeconds = 60f)]
        public static async Task ElementalTicks(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("nw_rat001", -5f);
            await ctx.DelaySecondsAsync(1f);
            CreaturePlugin.SetRawAbilityScore(source, AbilityType.Perception, 26);
            CreaturePlugin.SetRawAbilityScore(source, AbilityType.Agility, 26);
            ctx.SuppressNPCNaturalRegen(source);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), source, 120f);

            foreach (var reduction in new[] { 0, 50, 100 })
            foreach (var effect in new StatusEffectBase[] { new ToxinStatusEffect(), new ShockStatusEffect() })
            {
                var target = ctx.SpawnCreature("nw_rat001", 5f);
                await ctx.DelaySecondsAsync(1f);
                ctx.SuppressNPCNaturalRegen(target);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
                CreatureStat.SetNPCMaxHitPoints(target, 1000, true);
                if (reduction == 50)
                {
                    ctx.Assert(StatusEffect.ApplyStatusEffect(source, target, new PainSuppressant2StatusEffect(), 60f), "Pain Suppressant applies");
                    ctx.Assert(StatusEffect.ApplyStatusEffect(source, target, new ShieldWallStatusEffect(35), 60f), "Shield Wall applies");
                }
                else if (reduction > 0)
                    TemporaryStatModifier.Add(target, StatType.DamageTakenPercentAdjustment, -reduction, 60f);
                ctx.AssertEqual(-reduction, Stat.GetStatAdjustment(target, StatType.DamageTakenPercentAdjustment), "shared reduction");
                ctx.AssertEqual(0, Resistance.GetResistance(target, effect.ResistanceType), "elemental resistance is isolated");
                var before = GetCurrentHitPoints(target);
                effect.ApplyEffect(source, target, 5);
                await ctx.ExecuteInCreatureContextAsync(source, () =>
                    effect.GetType().GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(effect, new object[] { target }));
                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < before, 5f, "attributed periodic damage");
                var observed = before - GetCurrentHitPoints(target);
                var factor = (100 - Math.Min(85, reduction)) / 100f;
                // Toxin ticks for 6% of 1000 HP, capped at 30 + 3 per Agility modifier of its source (33bf375c8e).
                var toxinTick = ToxinStatusEffect.CalculateTickDamage(1000, GetAbilityModifier(AbilityType.Agility, source));
                var minimum = (int)((effect is ToxinStatusEffect ? toxinTick : 17) * factor);
                // A fractional reduced tick rounds up (54 at 15% is 8.1 and lands as 9), so the bound is the rounded-up value.
                var maximum = (int)Math.Ceiling(Math.Round((effect is ToxinStatusEffect ? toxinTick : 20) * factor, 3));
                ctx.Assert(observed >= minimum && observed <= maximum,
                    $"{effect.Name} with {reduction}% reduction: expected {minimum}-{maximum}, observed {observed}");
                ctx.Log($"{effect.Name}: reduction={reduction}%, native damage={observed}");
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
        }
    }
}
