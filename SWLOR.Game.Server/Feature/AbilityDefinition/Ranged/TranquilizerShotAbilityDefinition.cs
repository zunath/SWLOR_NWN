using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
            var enmity = level * 500;

            Enmity.ModifyEnmity(activator, target, enmity);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            DelayCommand(0.1f, () =>
            {
                if (!GetIsObjectValid(target) || GetCurrentHitPoints(target) <= 0)
                    return;

                var vfx = EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue);
                vfx = TagEffect(vfx, effectTag);
                var sleep = TagEffect(EffectSleep(), effectTag);

                ApplyEffectToObject(DurationType.Temporary, sleep, target, duration);
                ApplyEffectToObject(DurationType.Temporary, vfx, target, duration);
                Ability.ApplyTemporaryImmunity(target, duration, ImmunityType.Sleep);
            });
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            
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
                {
                    // Cone must use the shooter's position as origin (vOrigin) and aim from activator → struck target.
                    // Omitting vOrigin defaults to (0,0,0); using GetLocation(target) alone aims the cone by the target's
                    // facing, so the primary hit can miss the AOE. Always tranquilize the creature that was shot, then
                    // up to two others in the cone.
                    const float coneLength = RadiusSize.Colossal;
                    var area = GetArea(activator);
                    var origin = GetPosition(activator);
                    var towardTarget = GetPosition(target) - origin;
                    var planar = new Vector3(towardTarget.X, towardTarget.Y, 0f);
                    if (VectorMagnitude(planar) < 0.01f)
                        planar = AngleToVector(GetFacing(activator));
                    var dir = VectorNormalize(planar);
                    var facing = VectorToAngle(dir);
                    var coneEnd = origin + dir * coneLength;
                    var coneLocation = Location(area, coneEnd, facing);

                    ApplyEffect(activator, target, 3, 12f);

                    var hitCount = 1;
                    var creature = GetFirstObjectInShape(Shape.SpellCone, coneLength, coneLocation, true, ObjectType.Creature, origin);
                    while (GetIsObjectValid(creature) && hitCount < 3)
                    {
                        if (creature != activator && creature != target)
                        {
                            ApplyEffect(activator, creature, 3, 12f);
                            hitCount++;
                        }
                        creature = GetNextObjectInShape(Shape.SpellCone, coneLength, coneLocation, true, ObjectType.Creature, origin);
                    }
                    break;
                }
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
                .BreaksStealth()
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
                .BreaksStealth()
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
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}