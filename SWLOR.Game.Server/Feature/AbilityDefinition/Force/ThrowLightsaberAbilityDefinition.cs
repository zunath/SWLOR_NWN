//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var distance = GetDistanceBetween(activator, target);

            if (distance > 15)
                return "You must be within 15 meters of your target.";
            if (!GetIsObjectValid(weapon) || !Item.LightsaberBaseItemTypes.Contains(GetBaseItemType(weapon)))
                return "You cannot force throw your currently held object.";
            else return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;
            const float Range = 15.0f;
            var count = 1;
            var delay = GetDistanceBetween(activator, target) / 10.0f;
            var willpower = GetAbilityModifier(AbilityType.Willpower, activator);

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);

            // Make the activator face their target.
            ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, activator);
            
            AssignCommand(activator, () => ActionPlayAnimation(Animation.SaberThrow, 2));

            switch (level)
            {
                case 1:
                    dmg = 5.0f;
                    break;
                case 2:
                    dmg = 7.5f;
                    break;
                case 3:
                    dmg = 9.0f;
                    break;
            }

            // apply to target
            DelayCommand(delay, () =>
            {
                CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
                var defense = Stat.GetDefense(target, CombatDamageType.Physical);
                var targetWillpower = GetAbilityModifier(AbilityType.Willpower, target);
                var damage = Combat.CalculateDamage(dmg, willpower, defense, targetWillpower, 0);
                ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), target);
            });
                        
            // apply to next nearest creature in the spellcylinder
            var nearby = GetFirstObjectInShape(Shape.SpellCylinder, Range, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            while (GetIsObjectValid(nearby) && count < level)
            {
                if (nearby != target && nearby != activator)
                {
                    delay = GetDistanceBetween(activator, nearby) / 10.0f;
                    var nearbyCopy = nearby;
                    DelayCommand(delay, () =>
                    {
                        CombatPoint.AddCombatPoint(activator, nearby, SkillType.Force, 3);
                        var defense = Stat.GetDefense(nearbyCopy, CombatDamageType.Physical);
                        var targetWillpower = GetAbilityModifier(AbilityType.Willpower, nearbyCopy);
                        var damage = Combat.CalculateDamage(dmg, willpower, defense, targetWillpower, 0);
                        ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), nearbyCopy);
                    });

                    count++;
                }
                nearby = GetNextObjectInShape(Shape.SpellCylinder, Range, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
        }

        private static void ThrowLightsaber1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber1, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber I")
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 60f)
                .HasActivationDelay(2.0f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
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
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
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
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}