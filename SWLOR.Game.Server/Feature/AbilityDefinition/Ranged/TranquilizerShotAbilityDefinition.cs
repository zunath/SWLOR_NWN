using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class TranquilizerShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            TranquilizerShot1(builder);
            TranquilizerShot2(builder);
            TranquilizerShot3(builder);

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

        private void ApplyEffect(uint activator, uint target, int level, float duration)
        {
            var effectTag = $"StatusEffectType.Tranquilize";
            var enmity = level * 1000;

            var vfx = EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue);
            vfx = TagEffect(vfx, effectTag);
            var sleep = TagEffect(EffectSleep(), effectTag);

            ApplyEffectToObject(DurationType.Temporary, sleep, target, duration);
            ApplyEffectToObject(DurationType.Temporary, vfx, target, duration);
            Ability.ApplyTemporaryImmunity(target, duration, ImmunityType.Sleep);

            Enmity.ModifyEnmity(activator, target, enmity);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);
            
            switch (level)
            {
                default:
                case 1:
                    ApplyEffect(activator, target, 1, 12f);
                    break;
                case 2:
                    ApplyEffect(activator, target, 2, 24f);
                    break;
                case 3:
                    var count = 0;
                    var creature = GetFirstObjectInShape(Shape.SpellCone, RadiusSize.Colossal, GetLocation(target), true);
                    while (GetIsObjectValid(creature) && count < 3)
                    {
                        if(creature != activator) 
                        {
                            ApplyEffect(activator, creature, 3, 12f);
                        }
                        count++;

                        creature = GetNextObjectInShape(Shape.SpellCone, RadiusSize.Colossal, GetLocation(target), true);
                    }
                    break;
            }
        }

        private void TranquilizerShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void TranquilizerShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void TranquilizerShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot3, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.TranquilizerShot, 300f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}