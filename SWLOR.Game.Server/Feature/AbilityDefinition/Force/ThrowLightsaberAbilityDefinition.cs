//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

            var validWeapon = GetIsObjectValid(weapon) &&
                                 (Item.LightsaberBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  Item.VibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  Item.FinesseVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)));

            if (distance > 15)
                return "You must be within 15 meters of your target.";
            if (!validWeapon)
                return "You cannot force throw your currently held object.";
            else return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            const float Range = 15.0f;
            var count = 1;
            var delay = GetDistanceBetween(activator, target) / 10.0f;
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);

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
                    dmg = 8;
                    break;
                case 2:
                    dmg = 17;
                    break;
                case 3:
                    dmg = 24;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);

            // apply to target
            DelayCommand(delay, () =>
            {
                var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                var damage = Combat.CalculateDamage(
                    attack, 
                    dmg, 
                    attackerStat, 
                    defense, 
                    defenderStat, 
                    0);
                ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), target);
                Enmity.ModifyEnmity(activator, target, damage + 200 * level);
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
                        var defense = Stat.GetDefense(nearbyCopy, CombatDamageType.Physical, AbilityType.Vitality);
                        var defenderStat = GetAbilityModifier(AbilityType.Willpower, nearbyCopy);
                        var damage = Combat.CalculateDamage(
                            attack,
                            dmg, 
                            attackerStat,
                            defense, 
                            defenderStat, 
                            0);
                        ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), nearbyCopy);
                        CombatPoint.AddCombatPoint(activator, nearbyCopy, SkillType.Force, 3);
                        Enmity.ModifyEnmity(activator, nearbyCopy, damage + 200 * level);
                    });

                    count++;
                }
                nearby = GetNextObjectInShape(Shape.SpellCylinder, Range, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            }

        }

        private static void ThrowLightsaber1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber1, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ThrowLightsaber2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber2, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void ThrowLightsaber3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber3, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}