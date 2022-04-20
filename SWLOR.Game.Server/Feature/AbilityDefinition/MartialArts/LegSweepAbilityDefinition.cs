using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var dmg = 0.0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 2.0f;
                    if (d4()==1) inflict = true;
                    break;
                case 2:
                    dmg = 4.5f;
                    if (Random(100) < 40) inflict = true;
                    break;
                case 3:
                    dmg = 7.0f;
                    if (d4() > 2) inflict = true;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var might  = GetAbilityModifier(AbilityType.Might, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, might, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);
            if (inflict) ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
        }

        private static void LegSweep1(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep1, PerkType.LegSweep)
                .Name("Leg Sweep I")
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
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}