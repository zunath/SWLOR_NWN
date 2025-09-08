//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class ElectricFistAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ElectricFist1(builder);
            ElectricFist2(builder);
            ElectricFist3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.KatarBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a katar ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;
            float duration;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    dc = 10;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 17;
                    dc = 15;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 24;
                    dc = 20;
                    duration = 60f;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            CombatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffect.Apply(activator, target, StatusEffectType.Shock, duration);
            }

            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void ElectricFist1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist1, PerkType.ElectricFist)
                .Name("Electric Fist I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ElectricFist2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist2, PerkType.ElectricFist)
                .Name("Electric Fist II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ElectricFist3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist3, PerkType.ElectricFist)
                .Name("Electric Fist III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}