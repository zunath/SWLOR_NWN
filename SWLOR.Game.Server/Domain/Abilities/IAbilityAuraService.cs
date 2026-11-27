using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Player auras and the Leadership combat point (part 8 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md).
    /// Static callers reach the container's instance through <c>GameServices.AbilityAura</c>; the event handlers are on
    /// <see cref="AbilityAuraService"/>. The death service reaches it as one of its <see cref="ICreatureRevivedListener"/>s.
    /// </summary>
    public interface IAbilityAuraService : ICreatureRevivedListener
    {
        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        void AddLeadershipCombatPoint();

        void RecordLeadershipWeaponHit(uint player, uint target);

        void ApplyAura(uint activator, Type type, bool targetsSelf, bool targetsParty, bool targetsEnemies, VisualEffect successfulImpactVisualEffect = VisualEffect.None);

        /// <summary>
        /// Retrieves the creatures a self-and-party aura applies to: the activator and the party
        /// members currently inside its aura range.
        /// </summary>
        /// <param name="activator">The creature whose aura is checked.</param>
        /// <returns>The activator followed by its party members in aura range.</returns>
        List<uint> GetSelfAndPartyAuraRecipients(uint activator);

        bool RemoveAura(uint activator, Type type, bool sendsWornOffMessage = false);

        bool ToggleAura(uint activator, Type type);

        /// <summary>
        /// Deactivates every aura the player is currently projecting and strips any aura effects they are
        /// receiving as a recipient. Used by flows that must return a player to a clean state, such as the
        /// character rebuilder before a reset.
        /// </summary>
        void ClearAllPlayerAuras(uint player);

        /// <summary>
        /// Removes a creature from all active aura range lists and strips any aura effects they received
        /// as a recipient. Used when a creature leaves the game world in a way that bypasses the normal
        /// AOE exit event (e.g., entering space, being teleported).
        /// </summary>
        /// <param name="target">The creature to remove from all aura ranges.</param>
        void RemoveCreatureFromAllAuraRanges(uint target);

        /// <summary>
        /// Refreshes party-aura eligibility for a creature against all active aura leaders.
        /// If the creature is still tracked in a party aura range but is no longer in the leader's party,
        /// remove the cached range entry and strip relevant aura effects.
        /// </summary>
        /// <param name="target">The creature whose party aura eligibility should be refreshed.</param>
        void RefreshPartyAuraEligibility(uint target);

        /// <summary>
        /// Enrolls a creature that just joined a party in the party auras it is already standing in,
        /// and enrolls the party members already standing in its own aura. The aura enter event only
        /// fires on entry, so without this they would wait until they leave and re-enter the aura.
        /// </summary>
        /// <param name="member">The creature that joined the party.</param>
        void EnrollPartyMemberInAuraRanges(uint member);

        /// <summary>
        /// Re-applies any aura effects that a creature should be receiving based on their current position
        /// in active aura range lists. Used after in-place resurrection (subdual, revive) where the
        /// AOE enter event may not re-fire for a creature that never physically left the AOE.
        /// </summary>
        /// <param name="target">The creature to re-enroll in active auras.</param>
        void ReapplyAuraEffectsForCreature(uint target);

        void ReapplyPlayerAuraAOE(uint player);

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        void ApplyAuraAOE();

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnExit();

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnDeath();

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        void ReapplyAuraOnRespawn();

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnSpaceEntry();

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        void AuraEnter();

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        void AuraExit();
    }
}
