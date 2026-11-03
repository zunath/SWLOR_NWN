using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DevicesBlastDamageEngineTests
    {
        private const int Samples = 8;

        [EngineTest("Thermal Detonator adds gadget DMG and a Bleeding or Burning bonus, and Blast Radius raises blast damage", Category = "DevicesDamage", TimeoutSeconds = 400f)]
        public static async Task ThermalDetonatorAndBlastRadiusDamage(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            fixture.Update(record =>
            {
                record.CharacterType = CharacterType.Standard;
                record.Skills[SkillType.Devices].Rank = 50;
                record.Perks[PerkType.WristRocket] = 3;
                record.Perks[PerkType.FragGrenade] = 3;
                record.Perks[PerkType.ThermalDetonator] = 1;
            });
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Perception, 26);
            foreach (var feat in new[] { FeatType.WristRocket3, FeatType.FragGrenade3, FeatType.ThermalDetonator1 })
                CreaturePlugin.AddFeat(player, feat);
            await ctx.ExecuteInCreatureContextAsync(player, () => CreateItemOnObject("explosives", player, 99));

            // The Qion Slug is the Hutlar enemy from the player report.
            var target = ctx.SpawnCreature("qion_slug", 1.5f);
            await ctx.WaitFrameAsync();
            ctx.MakeHostile(target);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 30000, true);
            SetAILevel(target, AILevel.VeryLow);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 390f);

            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                var wristRocket = await MeasureAbility(ctx, fixture, target, FeatType.WristRocket3, false);
                var thermalDetonator = await MeasureAbility(ctx, fixture, target, FeatType.ThermalDetonator1, false);
                var bleedingThermalDetonator = await MeasureAbility(ctx, fixture, target, FeatType.ThermalDetonator1, true);
                var fragGrenade = await MeasureAbility(ctx, fixture, target, FeatType.FragGrenade3, false);

                fixture.Update(record => record.Perks[PerkType.BlastRadius] = 3);
                ctx.AssertEqual(12, Stat.GetStatAdjustment(player, StatType.BlastDamagePercentAdjustment), "Blast Radius III blast damage bonus");
                var blastRadiusFragGrenade = await MeasureAbility(ctx, fixture, target, FeatType.FragGrenade3, false);

                // Same seeds, so each pair differs only by its DMG input or percent bonus.
                // Wrist Rocket III: 32 + 28 gadget DMG. Thermal Detonator: 60 + 28, +40 on a Bleeding target.
                AssertRatio(ctx, thermalDetonator, wristRocket, 1.35, 1.48, "Thermal Detonator against Wrist Rocket III");
                AssertRatio(ctx, bleedingThermalDetonator, thermalDetonator, 1.36, 1.48, "Thermal Detonator on a Bleeding target against an unaffected one");
                AssertRatio(ctx, blastRadiusFragGrenade, fragGrenade, 1.10, 1.14, "Frag Grenade III with Blast Radius III against without");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                AssignCommand(player, () => ClearAllActions());
            }
        }

        private static void AssertRatio(EngineTestContext ctx, IReadOnlyList<int> numerator, IReadOnlyList<int> denominator, double minimum, double maximum, string label)
        {
            var ratio = numerator.Average() / denominator.Average();
            ctx.Log($"DEVICES_DAMAGE {label}: ratio={ratio:0.000}");
            ctx.Assert(ratio >= minimum && ratio <= maximum, $"{label}: damage ratio {ratio:0.000} is outside {minimum}-{maximum}.");
        }

        private static async Task<IReadOnlyList<int>> MeasureAbility(EngineTestContext ctx, PlayerAbilityFixture fixture, uint target, FeatType feat, bool bleedingTarget)
        {
            var player = fixture.Creature;
            var ability = Ability.GetAbilityDetail(feat);
            var damage = new List<int>();
            for (var sample = 0; sample < Samples; sample++)
            {
                ctx.SeedRandom(1000 + sample);
                // Capstone recasts ignore ReduceRecastDelay, so clear the saved deadline directly.
                fixture.Update(record => record.RecastTimes?.Remove(ability.RecastGroup));
                StatusEffect.RemoveStatusEffect(typeof(BleedStatusEffect), target);
                StatusEffect.RemoveStatusEffect(typeof(BurnStatusEffect), target);
                if (bleedingTarget)
                    StatusEffect.ApplyStatusEffect(player, target, new BleedStatusEffect(), 30f);
                AssignCommand(player, () => ClearAllActions());
                await ctx.WaitFrameAsync();
                var hitPoints = GetCurrentHitPoints(target);
                Ability.ClearLastCompletedAbilityImpactSummary(player);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    ctx.Assert(UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target)),
                        $"{ability.Name} starts: {Ability.GetLastActivationDenialReason()}"));
                await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player)?.Ability == ability,
                    8f, $"{ability.Name} to complete");
                // Telegraphed blasts land after the activation completes.
                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < hitPoints, 5f, $"{ability.Name} to damage the target");
                // Damage over time can tick inside this window, so read the impact's own damage.
                var dealt = Ability.GetLastCompletedAbilityImpactSummary(player)?.AttributedDamage ?? 0;
                ctx.Assert(dealt > 0, $"{ability.Name} records the damage of its own impact.");
                damage.Add(dealt);
            }

            ctx.Log($"DEVICES_DAMAGE {ability.Name}{(bleedingTarget ? " (Bleeding target)" : string.Empty)}: " +
                    $"mean={damage.Average():0.0} samples=[{string.Join(",", damage)}]");
            return damage;
        }
    }
}
