using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;

namespace SWLOR.Shared.Domain.Ability.Contracts
{
    public interface IAbilityService
    {
        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        void CacheData();

        void CacheAbilities();
        void CacheToggleActions();

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        bool IsFeatRegistered(FeatType featType);

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        AbilityDetail GetAbilityDetail(FeatType featType);

        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation);

        /// <summary>
        /// Toggles an ability on or off for a given player.
        /// If additional logic is defined in an AbilityToggleDefinition, that will be run after this is performed.
        /// </summary>
        /// <param name="player">The player to toggle on or off.</param>
        /// <param name="toggleType">The type of toggle to turn on or off.</param>
        /// <param name="isToggled">true if the ability should be enabled, false otherwise</param>
        void ToggleAbility(uint player, AbilityToggleType toggleType, bool isToggled);

        /// <summary>
        /// Retrieves whether a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        bool IsAbilityToggled(uint player, AbilityToggleType toggleType);

        /// <summary>
        /// Retrieves whether  a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="playerId">The player Id to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        bool IsAbilityToggled(string playerId, AbilityToggleType toggleType);

        /// <summary>
        /// Determines if any ability is toggled by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if any ability is toggled, false otherwise</returns>
        bool IsAnyAbilityToggled(uint player);

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        void AddLeadershipCombatPoint();

        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// It will NOT remove any existing effects.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity);
    }
}