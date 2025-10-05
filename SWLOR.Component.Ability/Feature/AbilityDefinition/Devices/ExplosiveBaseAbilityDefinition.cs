using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public abstract class ExplosiveBaseAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        protected ExplosiveBaseAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        protected IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        protected IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        protected IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        protected IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        protected ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        protected ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        protected IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        protected IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private const string ExplosiveItemResref = "explosives";

        public abstract Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder);

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

        protected void TakeExplosives(uint activator)
        {
            if (!GetIsPC(activator))
                return;

            var chanceToNotConsume = 10 * PerkService.GetPerkLevel(activator, PerkType.DemolitionExpert);
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
                var target = GetFirstObjectInShape(ShapeType.Sphere, radius, targetLocation, true);
                while (GetIsObjectValid(target))
                {
                    impactAction(target);

                    target = GetNextObjectInShape(ShapeType.Sphere, radius, targetLocation, true);
                }

            });

            TakeExplosives(activator);
        }

        protected void ExplosiveAOEImpact(
            uint activator,
            Location targetLocation,
            AreaOfEffectType aoe,
            string enterScript,
            string heartbeatScript,
            float duration)
        {
            var activatorLocation = GetLocation(activator);
            var delay = GetDistanceBetweenLocations(activatorLocation, targetLocation) / 18f;

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var dmgBonus = CombatService.GetAbilityDamageBonus(activator, SkillType.Devices);
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
