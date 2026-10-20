using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class RepeatedTargetDamageEngineTests
    {
        [EngineTest("Sustained Fire III and Rundown I add independent auto-attack bonuses", Category = "RepeatedTargetDamage", TimeoutSeconds = 30f)]
        public static async Task AutoAttackBonusesStack(EngineTestContext ctx)
        {
            foreach (var skill in new[] { SkillType.Rifle, SkillType.Vibroblade, SkillType.Pistol, SkillType.Lightsaber, SkillType.TwinPistol })
            {
                var attacker = ctx.SpawnCreature("nw_bandit001");
                var target = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.WaitFrameAsync();
                Prepare(ctx, attacker, target);

                for (var hit = 1; hit <= 6; hit++)
                {
                    var damage = Combat.ApplyDamageDealtModifiers(attacker, target, 100, skill,
                        CombatDamageType.Physical, isAbilityDamage: false, canApplyRandomFlatBonuses: false);
                    var stacks = Math.Min(hit, 5);
                    ctx.AssertEqual(100 + stacks * 4, damage, $"{skill} auto-attack {hit} adds both bonuses");
                    AssertStacks(ctx, attacker, stacks, stacks);
                }
            }
        }

        [EngineTest("Ability hits preserve Rundown while independently advancing Sustained Fire", Category = "RepeatedTargetDamage", TimeoutSeconds = 30f)]
        public static async Task AbilityHitsPreserveAutoAttackStacks(EngineTestContext ctx)
        {
            foreach (var weaponSkill in new[] { SkillType.Rifle, SkillType.TwinPistol })
            {
                var attacker = ctx.SpawnCreature("nw_bandit001");
                var target = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.WaitFrameAsync();
                Prepare(ctx, attacker, target);

                for (var hit = 1; hit <= 6; hit++)
                {
                    var autoAttackStacks = Math.Min(hit, 5);
                    var weaponHitStacks = Math.Min(hit * 2 - 1, 5);
                    var autoAttackDamage = Combat.ApplyDamageDealtModifiers(attacker, target, 100, weaponSkill,
                        CombatDamageType.Physical, isAbilityDamage: false, canApplyRandomFlatBonuses: false);
                    ctx.AssertEqual(100 + autoAttackStacks + weaponHitStacks * 3, autoAttackDamage,
                        $"{weaponSkill} auto-attack {hit} retains Rundown between abilities");
                    AssertStacks(ctx, attacker, autoAttackStacks, weaponHitStacks);

                    weaponHitStacks = Math.Min(hit * 2, 5);
                    var abilityDamage = Combat.ApplyDamageDealtModifiers(attacker, target, 100, weaponSkill,
                        CombatDamageType.Physical, isAbilityDamage: true, canApplyRandomFlatBonuses: false);
                    ctx.AssertEqual(100 + weaponHitStacks * 3, abilityDamage,
                        $"{weaponSkill} weapon ability {hit} benefits only from Sustained Fire");
                    AssertStacks(ctx, attacker, autoAttackStacks, weaponHitStacks);
                }

                foreach (var skill in new[] { SkillType.Force, SkillType.Devices })
                {
                    var damage = Combat.ApplyDamageDealtModifiers(attacker, target, 100, skill,
                        CombatDamageType.Force, isAbilityDamage: true, canApplyRandomFlatBonuses: false);
                    ctx.AssertEqual(100, damage, $"{skill} does not receive weapon-hit bonuses");
                    AssertStacks(ctx, attacker, 5, 5);
                }
            }
        }

        private static void Prepare(EngineTestContext ctx, uint attacker, uint target)
        {
            foreach (var creature in new[] { attacker, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
            }

            ctx.SetNPCPerkLevel(attacker, PerkType.SustainedFire, 3);
            ctx.SetNPCPerkLevel(attacker, PerkType.Rundown, 1);
            ctx.AssertEqual(3, Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusPerHit),
                "Sustained Fire III supplies +3 DMG per stack");
            ctx.AssertEqual(1, Stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit),
                "Rundown I supplies +1 DMG per stack");
        }

        private static void AssertStacks(EngineTestContext ctx, uint attacker, int autoAttackStacks, int weaponHitStacks)
        {
            var rundown = StatusEffect.GetStatusEffect<MeleeRepeatedTargetDamageStatusEffect>(attacker);
            var sustainedFire = StatusEffect.GetStatusEffect<SustainedFireStatusEffect>(attacker);
            ctx.Assert(rundown != null, "Rundown remains active alongside Sustained Fire");
            ctx.Assert(sustainedFire != null, "Sustained Fire remains active alongside Rundown");
            ctx.AssertEqual(autoAttackStacks, rundown.Stacks, "Rundown stack count");
            ctx.AssertEqual(weaponHitStacks, sustainedFire.Stacks, "Sustained Fire stack count");
            ctx.AssertEqual(weaponHitStacks * 3, sustainedFire.DamageBonus, "Sustained Fire damage bonus");
            ctx.AssertEqual(EffectIconType.RundownStatusEffect, rundown.Icon, "Rundown keeps its own status icon");
        }
    }
}
