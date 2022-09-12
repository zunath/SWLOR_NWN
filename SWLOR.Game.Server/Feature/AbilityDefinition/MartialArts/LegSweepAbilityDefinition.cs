using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class LegSweepAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            LegSweep1(builder);
            LegSweep2(builder);
            LegSweep3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.StaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a staff ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 4;
                    if (d4()==1) inflict = true;
                    break;
                case 2:
                    dmg = 13;
                    if (Random(100) < 40) inflict = true;
                    break;
                case 3:
                    dmg = 20;
                    if (d4() > 2) inflict = true;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            Enmity.ModifyEnmityOnAll(activator, 250 * level);
            CombatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            int attackerStat;
            int attack;

            if(GetHasFeat(FeatType.FlurryStyle, activator))
            {
                attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.MartialArts);
            } 
            else
            {
                attackerStat = GetAbilityScore(activator, AbilityType.Might);
                attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            }
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);
            if (inflict) ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
        }

        private static void LegSweep1(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep1, PerkType.LegSweep)
                .Name("Leg Sweep I")
                .Level(1)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void LegSweep2(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep2, PerkType.LegSweep)
                .Name("Leg Sweep II")
                .Level(2)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void LegSweep3(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep3, PerkType.LegSweep)
                .Name("Leg Sweep III")
                .Level(3)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}