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
    public class StrikingCobraAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            StrikingCobra1(builder);
            StrikingCobra2(builder);
            StrikingCobra3(builder);

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
            var dmg = 0;
            var duration = 0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 6;
                    if (d2() == 1) inflict = true;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 15;
                    if (d4() > 1) inflict = true;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 22;
                    inflict = true;
                    duration = 60f;
                    break;
                default:
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
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);
            if (inflict) 
                StatusEffect.Apply(activator, target, StatusEffectType.Poison, duration);

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void StrikingCobra1(AbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra)
                .Name("Striking Cobra I")
                .Level(1)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void StrikingCobra2(AbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra)
                .Name("Striking Cobra II")
                .Level(2)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void StrikingCobra3(AbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra)
                .Name("Striking Cobra III")
                .Level(3)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}