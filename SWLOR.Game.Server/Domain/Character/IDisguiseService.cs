using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DisguiseService;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IDisguiseService
    {
        /// <summary>Reapplies the active disguise appearance when a player logs in.</summary>
        void ApplyActiveDisguiseOnEnter();

        string BuildIdentityKey(string disguiseId);

        bool IsDisguiseIdentityKey(string identityKey);

        string GetIdentityKey(uint player);

        string GetDisplayDescriptor(uint player);

        bool ShouldScrambleAccountName(uint player);

        PlayerDisguise GetActiveDisguise(uint player);

        PlayerDisguise GetActiveDisguise(Player dbPlayer);

        List<PlayerDisguise> GetDisguises(string playerId, bool retired);

        int GetDisguiseSlotLimit(uint player, Player dbPlayer);

        int CalculateDisguiseSlotLimit(int baseLimit, int additionalSlots);

        /// <summary>
        /// The delay this player must wait between disguise activations, after perk reductions.
        /// </summary>
        TimeSpan GetActivationDelay(uint player);

        /// <summary>
        /// Applies a cooldown reduction percent to the base activation delay. The result never drops
        /// below the minimum activation delay so that stacked reduction sources cannot
        /// remove the delay entirely and let a player cycle identities faster than they can be observed.
        /// </summary>
        TimeSpan CalculateActivationDelay(int reductionPercent);

        int CountUsedSlots(string playerId);

        PlayerDisguise CreateDisguise(uint player);

        SaveDisguiseResult SaveDisguise(
            uint player,
            string disguiseId,
            string privateName,
            string descriptor,
            string biography,
            int portraitInternalId,
            int soundSetId,
            bool scrambleAccountId);

        ActivateDisguiseResult Activate(uint player, string disguiseId);

        bool Deactivate(uint player);

        bool Retire(uint player, string disguiseId);

        bool Unretire(uint player, string disguiseId);

        DeleteRetiredDisguiseResult DeleteRetiredDisguise(uint player, string disguiseId, DisguisePaymentMethod paymentMethod);

        int ResetActivationCooldowns(uint player, uint actor = OBJECT_INVALID);

        string GetPortraitResref(PlayerDisguise disguise);

        string GetSoundSetName(int soundSetId);
    }
}
