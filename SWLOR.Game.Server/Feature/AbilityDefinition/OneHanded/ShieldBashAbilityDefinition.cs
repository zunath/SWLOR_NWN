//using Random = SWLOR.Game.Server.Service.Random;

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
    public class ShieldBashAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ShieldBash1(builder);
            ShieldBash2(builder);
            ShieldBash3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.LeftHand, activator);
            var leftHandType = GetBaseItemType(weapon);
            
            if (Item.ShieldBaseItemTypes.Contains(leftHandType))
            {
                return string.Empty;
            }
            else
                return "A shield must be equipped in your left hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            int dmg;
            const float Duration = 3f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    dc = 12;
                    break;
                case 2:
                    dmg = 16;
                    dc = 14;
                    break;
                case 3:
                    dmg = 24;
                    dc = 16;
                    break;
            }


            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var might = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(attack, dmg, might, defense, vitality, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Will, dc, AbilityType.Might);
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectDazed(), target, Duration);
                Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Dazed);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.ShieldWall));

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void ShieldBash1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash1, PerkType.ShieldBash)
                .Name("Shield Bash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ShieldBash2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash2, PerkType.ShieldBash)
                .Name("Shield Bash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ShieldBash3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash3, PerkType.ShieldBash)
                .Name("Shield Bash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}