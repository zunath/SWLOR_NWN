using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public abstract class ExplosiveBaseAbilityDefinition: IAbilityListDefinition
    {
        private const string ExplosiveItemResref = "explosives";

        public abstract Dictionary<FeatType, AbilityDetail> BuildAbilities();

        protected string ExplosiveValidation(uint activator, uint target, int level, Location location)
        {
            var activatorPosition = GetPosition(activator);
            var targetPosition = GetPositionFromLocation(location);

            if (!HasExplosives(activator))
            {
                return "You have no explosives.";
            }

            if (!LineOfSightVector(activatorPosition, targetPosition))
                return "You cannot see your target.";

            return string.Empty;
        }

        protected static void TakeExplosives(uint activator)
        {
            if (!GetIsPC(activator))
                return;

            var chanceToNotConsume = 10 * Perk.GetPerkLevel(activator, PerkType.DemolitionExpert);
            if (Random.D100(1) <= chanceToNotConsume)
                return;

            var item = GetItemPossessedBy(activator, ExplosiveItemResref);
            var stackSize = GetItemStackSize(item);

            if (stackSize <= 1)
            {
                DestroyObject(item);
            }
            else
            {
                SetItemStackSize(item, stackSize - 1);
            }
        }

        /// <summary>
        /// Determines whether the activator has at least one Explosives item in their inventory.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <returns>true if at least one explosives item is found, false otherwise.</returns>
        protected bool HasExplosives(uint activator)
        {
            if (!GetIsPC(activator))
                return true;

            var item = GetItemPossessedBy(activator, ExplosiveItemResref);

            return GetIsObjectValid(item) && GetItemStackSize(item) > 0;
        }

        protected void ExplosiveImpact(
            uint activator, 
            Location targetLocation,
            Effect vfxEffect,
            string sound,
            float radius,
            Action<uint> impactAction)
        {
            var activatorLocation = GetLocation(activator);
            var delay = GetDistanceBetweenLocations(activatorLocation, targetLocation) / 18f;

            DelayCommand(delay, () =>
            {
                if(!string.IsNullOrWhiteSpace(sound))
                    PlaySound(sound);
                ApplyEffectAtLocation(DurationType.Instant, vfxEffect, targetLocation);
            });

            DelayCommand(delay, () =>
            {
                var target = GetFirstObjectInShape(Shape.Sphere, radius, targetLocation, true);
                while (GetIsObjectValid(target))
                {
                    impactAction(target);

                    target = GetNextObjectInShape(Shape.Sphere, radius, targetLocation, true);
                }

            });

            TakeExplosives(activator);
        }

        protected void ExplosiveAOEImpact(
            uint activator,
            Location targetLocation,
            AreaOfEffect aoe,
            string enterScript,
            string heartbeatScript,
            float duration)
        {
            var activatorLocation = GetLocation(activator);
            var delay = GetDistanceBetweenLocations(activatorLocation, targetLocation) / 18f;

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var dmgBonus = Combat.GetAbilityDamageBonus(activator, SkillType.Devices);
            dmgBonus += attackerStat / 2;

            DelayCommand(delay, () =>
            {
                ApplyEffectAtLocation(
                    DurationType.Temporary, 
                    EffectAreaOfEffect(aoe, enterScript, heartbeatScript), 
                    targetLocation, 
                    duration);

                var AOEObject = GetNearestObjectToLocation(targetLocation, ObjectType.AreaOfEffect);
                if(AOEObject != OBJECT_INVALID)
                {
                    SetLocalInt(AOEObject, "DEVICE_ACC", attackerStat);
                    SetLocalInt(AOEObject, "DEVICE_ATK", attack);
                    SetLocalInt(AOEObject, "DEVICE_DMG", dmgBonus);
                }

            });

            TakeExplosives(activator);
        }
    }
}
