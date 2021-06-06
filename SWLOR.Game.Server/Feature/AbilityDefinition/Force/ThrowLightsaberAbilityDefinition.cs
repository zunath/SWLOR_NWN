//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ThrowLightsaberAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ThrowLightsaber1(builder);
            ThrowLightsaber2(builder);
            ThrowLightsaber3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var distance = GetDistanceBetween(activator, target);

            if (distance > 15)
                return "You must be within 15 meters of your target.";
            if (!GetIsObjectValid(weapon) || !Item.LightsaberBaseItemTypes.Contains(GetBaseItemType(weapon)))
                return "You cannot force throw your currently held object.";
            else return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            var iDamage = GetAbilityModifier(AbilityType.Intelligence, activator) + (int) (GetAbilityModifier(AbilityType.Wisdom, activator) * 0.5f);
            var iRange = 15;
            var iCount = 1;
            var fDelay = GetDistanceBetween(activator, target) / 10.0f;
            var oTargetObject = target;

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            // Make the activator face their target.
            ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, activator);
            
            AssignCommand(activator, () => ActionPlayAnimation(Animation.LoopingCustom10, 2));

            switch (level)
            {
                case 1:
                    iDamage += d6();
                    break;
                case 2:
                    iDamage += d8();
                    break;
                case 3:
                    iDamage += d10();
                    break;
                default:
                    break;
            }

            // apply to target
            if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
            {
                DelayCommand(fDelay, () =>
                {                    
                    ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(iDamage, DamageType.Sonic)), target);
                });

                iCount += 1;
            }
            
            // apply to next nearest creature in the spellcylinder
            oTargetObject = GetFirstObjectInShape(Shape.SpellCylinder, iRange, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            while (GetIsObjectValid(oTargetObject) && iCount < level)
            {
                if (oTargetObject != target && oTargetObject != activator)
                {
                    fDelay = GetDistanceBetween(activator, oTargetObject) / 10.0f;
                    //var creature = oTargetObject;
                    // apply to target
                    if (!Ability.GetAbilityResisted(activator, oTargetObject, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        DelayCommand(fDelay, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(iDamage, DamageType.Sonic)), target);
                        });

                        iCount += 1;
                    }
                }
                oTargetObject = GetNextObjectInShape(Shape.SpellCylinder, iRange, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ThrowLightsaber1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber1, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber I")
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ThrowLightsaber2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber2, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber II")
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ThrowLightsaber3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber3, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber III")
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}