//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class HammerShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            HammerShot1(builder);
            HammerShot2(builder);
            HammerShot3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.CannonBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a cannon ability.";
            }
            else 
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var damage = 0;
            var duration = 0f;
            var inflict = false;
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            switch (level)
            {
                case 1:
                    damage = d6();
                    duration = 3f;
                    if (d2() == 1) inflict = true;
                    break;
                case 2:
                    damage = d6(2);
                    duration = 6f;
                    if (d4() > 1 ) inflict = true;
                    break;
                case 3:
                    damage = d6(3);
                    duration = 6f;
                    inflict = true;
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            if (inflict) ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, duration);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void HammerShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.HammerShot1, PerkType.HammerShot)
                .Name("Hammer Shot I")
                .HasRecastDelay(RecastGroup.HammerShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void HammerShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.HammerShot2, PerkType.HammerShot)
                .Name("Hammer Shot II")
                .HasRecastDelay(RecastGroup.HammerShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void HammerShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.HammerShot3, PerkType.HammerShot)
                .Name("Hammer Shot III")
                .HasRecastDelay(RecastGroup.HammerShot, 60f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
    }
}