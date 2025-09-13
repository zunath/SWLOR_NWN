//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Core.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class CripplingShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CripplingShot1(builder);
            CripplingShot2(builder);
            CripplingShot3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            const float Duration = 6f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 12;
                    dc = 10;
                    break;
                case 2:
                    dmg = 21;
                    dc = 15;
                    break;
                case 3:
                    dmg = 34;
                    dc = 20;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Ranged);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            var attackerStat = Combat.GetPerkAdjustedAbilityScore(activator);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Ranged);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(99), target, Duration);
            }

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void CripplingShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                .Name("Crippling Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CripplingShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                .Name("Crippling Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CripplingShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                .Name("Crippling Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}