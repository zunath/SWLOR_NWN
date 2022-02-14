using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public abstract class ExplosiveBaseAbilityDefinition: IAbilityListDefinition
    {
        private const string ExplosiveItemResref = "explosives";

        public abstract Dictionary<FeatType, AbilityDetail> BuildAbilities();

        protected static void TakeExplosives(uint activator)
        {
            if (!GetIsPC(activator))
                return;

            var chanceToNotConsume = 10 * Perk.GetEffectivePerkLevel(activator, PerkType.DemolitionExpert);
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
    }
}
