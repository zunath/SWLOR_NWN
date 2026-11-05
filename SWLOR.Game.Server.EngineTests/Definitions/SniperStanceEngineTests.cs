using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SniperStanceEngineTests
    {
        [EngineTest("Sniper Stance adds +15% damage to ranged abilities at 10m or more", Category = "SniperStance", TimeoutSeconds = 30f)]
        public static async Task LongRangeAbilityDamage(EngineTestContext ctx)
        {
            var sniper = ctx.SpawnCreature("nw_bandit001");
            var plain = ctx.SpawnCreature("nw_bandit001", 0f, 2f);
            var nearTarget = ctx.SpawnCreature("nw_rat001", 5f);
            var farTarget = ctx.SpawnCreature("nw_rat001", 12f);
            await ctx.WaitFrameAsync();
            foreach (var creature in new[] { sniper, plain, nearTarget, farTarget })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
            }

            ctx.Assert(StatusEffect.ApplyStatusEffect<SniperStanceStatusEffect>(sniper, sniper, 60f),
                "Sniper Stance applies");
            ctx.AssertEqual(10, Stat.GetStatAdjustment(sniper, StatType.RangedAbilityLongRangeMinimumRangeMeters),
                "Sniper Stance supplies its own 10m long-range threshold");
            ctx.AssertEqual(15, Stat.GetStatAdjustment(sniper, StatType.RangedAbilityLongRangeDamagePercentAdjustment),
                "Sniper Stance supplies +15% long-range ability damage");

            foreach (var skill in new[] { SkillType.Rifle, SkillType.Pistol, SkillType.Throwing })
            {
                ctx.AssertEqual(115, AbilityDamage(sniper, farTarget, skill),
                    $"{skill} ability at 12m gains Sniper Stance's +15%");
                ctx.AssertEqual(100, AbilityDamage(sniper, nearTarget, skill),
                    $"{skill} ability at 5m is not long range");
                ctx.AssertEqual(100, AbilityDamage(plain, farTarget, skill),
                    $"{skill} ability without Sniper Stance is unchanged");
            }

            ctx.AssertEqual(100, Combat.ApplyDamageDealtModifiers(sniper, farTarget, 100, SkillType.Rifle,
                    CombatDamageType.Physical, isAbilityDamage: false, canApplyRandomFlatBonuses: false),
                "Rifle auto-attacks do not gain the ability bonus");
            ctx.AssertEqual(100, Combat.ApplyDamageDealtModifiers(sniper, farTarget, 100, SkillType.Force,
                    CombatDamageType.Force, isAbilityDamage: true, canApplyRandomFlatBonuses: false),
                "Force abilities are not ranged weapon abilities");

            // Scope Calibration also sets a 10m threshold; owning both must not push it to 20m.
            ctx.SetNPCPerkLevel(sniper, PerkType.ScopeCalibration, 1);
            ctx.AssertEqual(10, Stat.GetStatAdjustment(sniper, StatType.RangedAbilityLongRangeMinimumRangeMeters),
                "Scope Calibration and Sniper Stance keep a single 10m threshold");
            ctx.AssertEqual(115, AbilityDamage(sniper, farTarget, SkillType.Rifle),
                "the bonus still applies at 12m with Scope Calibration");
        }

        private static int AbilityDamage(uint attacker, uint target, SkillType skill)
        {
            return Combat.ApplyDamageDealtModifiers(attacker, target, 100, skill,
                CombatDamageType.Physical, isAbilityDamage: true, canApplyRandomFlatBonuses: false);
        }
    }
}
