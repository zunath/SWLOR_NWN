using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The technique loadout: how many technique slots a player has, equipping and unequipping learned techniques, the slot
    /// budget enforced after progression changes and on login, the saved builds, and the learned and equipped reads. Learned
    /// techniques are equipped within the slot budget as ordinary active ability feats; the technique catalog they come from is
    /// <see cref="IMimicryService"/>'s.
    /// </summary>
    public interface IMimicryLoadoutService
    {
        /// <summary>
        /// Returns the maximum number of technique slots available to a player.
        /// Zero if the player does not have the Combat Analyzer perk.
        /// </summary>
        int GetMaxSlots(uint player);

        /// <summary>
        /// Returns the maximum number of technique slots available to a player's database record.
        /// </summary>
        int GetMaxSlots(Player dbPlayer);

        /// <summary>
        /// Returns the number of technique slots currently used by a player's equipped techniques.
        /// </summary>
        int GetUsedSlots(uint player);

        /// <summary>
        /// Returns the number of technique slots currently used by a player database record's equipped techniques.
        /// </summary>
        int GetUsedSlots(Player dbPlayer);

        /// <summary>
        /// Determines whether a player can equip a given technique right now.
        /// </summary>
        bool CanEquip(uint player, FeatType feat, out string error);

        /// <summary>
        /// Determines whether a player can equip a given technique right now, using an
        /// already-fetched player database record to avoid duplicate round trips.
        /// </summary>
        bool CanEquip(uint player, Player dbPlayer, FeatType feat, out string error);

        /// <summary>
        /// Equips a learned technique, granting the underlying feat and adding it to the player's hotbar.
        /// </summary>
        bool EquipTechnique(uint player, FeatType feat);

        /// <summary>
        /// Unequips a technique, removing the underlying feat and any hotbar slot referencing it.
        /// </summary>
        bool UnequipTechnique(uint player, FeatType feat);

        /// <summary>
        /// Unequips techniques that exceed the player's current Mimicry rank, then removes the
        /// newest-equipped techniques until the remaining loadout fits the current slot budget.
        /// Used after progression changes and on login to keep persisted loadouts valid.
        /// </summary>
        void EnforceSlotBudget(uint player);

        /// <summary>
        /// Enforces Mimicry rank requirements and the slot budget against an already-fetched player
        /// database record, avoiding a duplicate round trip when the caller has one in hand.
        /// </summary>
        void EnforceSlotBudget(uint player, Player dbPlayer);

        /// <summary>
        /// Immediately removes equipped techniques whose rank requirement is no longer met when
        /// Mimicry loses a rank. This must run for every Mimicry decay, including rank losses that
        /// do not cross a perk requirement and therefore do not trigger a perk refund callback.
        /// </summary>
        void OnMimicrySkillDecay();

        /// <summary>
        /// Unequips every equipped technique. Used when the Combat Analyzer perk is refunded.
        /// </summary>
        void UnequipAllTechniques(uint player);

        /// <summary>
        /// On login, re-grants every equipped technique's feat (in case it was lost, e.g. a fresh
        /// character load) and enforces rank and slot limits in case progression changed since logout.
        /// </summary>
        void OnPlayerLogin();

        /// <summary>Validates the entire draft before changing any equipped state.</summary>
        bool ValidateLoadout(Player dbPlayer, IReadOnlyCollection<FeatType> techniques, out string error);

        void CaptureLoadoutHotbar(uint player, MimicryLoadout loadout);

        bool ApplyLoadout(uint player, string loadoutId, IReadOnlyCollection<FeatType> techniques, out string error);

        /// <summary>
        /// Returns every technique a player has learned, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(uint player);

        /// <summary>
        /// Returns every technique a player database record has learned, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(Player dbPlayer);

        /// <summary>
        /// Returns every technique a player currently has equipped, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(uint player);

        /// <summary>
        /// Returns every technique a player database record has equipped, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(Player dbPlayer);

        /// <summary>
        /// Returns true when a technique feat is present in a player's equipped loadout.
        /// </summary>
        bool IsTechniqueEquipped(uint player, FeatType feat);

        /// <summary>
        /// Returns true when a technique feat is present in an already-fetched player record's
        /// equipped loadout.
        /// </summary>
        bool IsTechniqueEquipped(Player dbPlayer, FeatType feat);
    }
}
