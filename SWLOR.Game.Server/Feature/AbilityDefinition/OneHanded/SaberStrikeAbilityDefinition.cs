using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class SaberStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            SaberStrike1(builder);
            SaberStrike2(builder);
            SaberStrike3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (Item.LightsaberBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A lightsaber must be equipped in your right hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var inflict = false;
            var breachTime = 0f;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    dmg = 6;
                    if (d2() == 1) inflict = true;
                    breachTime = 30f;
                    break;
                case 2:
                    dmg = 15;
                    if (d4() > 1) inflict = true;
                    breachTime = 60f;
                    break;
                case 3:
                    dmg = 22;
                    inflict = true;
                    breachTime = 60f;
                    break;
                default:
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            var stat = AbilityType.Perception;
            if (Ability.IsAbilityToggled(activator, AbilityToggleType.StrongStyleLightsaber))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = GetAbilityScore(activator, stat);
            var attack = Stat.GetAttack(activator, stat, SkillType.OneHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            
            if (inflict)
            {
                RemoveEffectByTag(target, "SABER_STRIKE");
                var eBreach = TagEffect(EffectACDecrease(2), "SABER_STRIKE");
                ApplyEffectToObject(DurationType.Temporary, eBreach, target, breachTime);
            }
            
            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.RiotBlade));

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void SaberStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike1, PerkType.SaberStrike)
                .Name("Saber Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void SaberStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike2, PerkType.SaberStrike)
                .Name("Saber Strike II")
                .Level(2)
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void SaberStrike3(AbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike3, PerkType.SaberStrike)
                .Name("Saber Strike III")
                .Level(3)
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}