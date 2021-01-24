//using Random = SWLOR.Game.Server.Service.Random;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public class QuickDrawAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            QuickDraw1(builder);
            QuickDraw2(builder);
            QuickDraw3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand);

            if (!Item.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a pistol ability.";
            }
            else
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
                    damage = d8();
                    break;
                case 2:
                    damage = d6(2);
                    break;
                case 3:
                    damage = d6(3);
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void QuickDraw1(AbilityBuilder builder)
        {
            builder.Create(Feat.QuickDraw1, PerkType.QuickDraw)
                .Name("Quick Draw I")
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void QuickDraw2(AbilityBuilder builder)
        {
            builder.Create(Feat.QuickDraw2, PerkType.QuickDraw)
                .Name("Quick Draw II")
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasCustomValidation((activator, target, level) =>
                {
                    return Validation(activator, target, level);
                })
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
        private static void QuickDraw3(AbilityBuilder builder)
        {
            builder.Create(Feat.QuickDraw3, PerkType.QuickDraw)
                .Name("Quick Draw III")
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
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