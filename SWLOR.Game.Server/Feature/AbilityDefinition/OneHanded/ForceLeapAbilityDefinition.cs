using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class ForceLeapAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceLeap1(builder);
            ForceLeap2(builder);
            ForceLeap3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            var rightHandBaseItemType = GetBaseItemType(weapon);
            var leftHandBaseItemType = GetBaseItemType(offHand);
            
            if (rightHandBaseItemType == BaseItem.Lightsaber
                && (leftHandBaseItemType == BaseItem.SmallShield ||
                    leftHandBaseItemType == BaseItem.LargeShield ||
                    leftHandBaseItemType == BaseItem.TowerShield ||
                    leftHandBaseItemType == BaseItem.Invalid))
            {
                return "This is a one-handed ability.";
            }

            return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var damage = 0;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = Random.D4(1);
                    break;
                case 2:
                    damage = Random.D6(1);
                    break;
                case 3:
                    damage = Random.D4(2);
                    break;
                default:
                    break;
            }

            const float Delay = 3f;
            ClearAllActions();
            ApplyEffectToObject(DurationType.Temporary, EffectDisappearAppear(GetLocation(target)), activator, Delay);

            DelayCommand(Delay, () =>
            {                
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 2f);
            });

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceLeap1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ForceLeap2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap2, PerkType.ForceLeap)
                .Name("Force Leap II")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ForceLeap3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap3, PerkType.ForceLeap)
                .Name("Force Leap III")
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}