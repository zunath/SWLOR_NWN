using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>One owned Power Glove supplies the weapon; its other hand is disposable appearance only.</summary>
public interface IPowerGloveEquipmentService
{
    /// <summary>True while a cosmetic hand is being generated.</summary>
    bool IsCreatingCompanion { get; }

    bool IsChanging(uint creature);

    bool IsPairItem(uint item);

    bool IsGeneratedCompanion(uint item);

    bool IsDestroyingCompanion(uint item);

    bool IsLinkedPair(uint creature, uint main, uint companion);

    bool IsEquippedCompanion(uint companion);

    /// <summary>Rebinds saved item references only while a player's equipped inventory is loading.</summary>
    bool TryRestoreSavedCompanion(uint companion);

    bool CanRestoreSavedPair(bool isPlayer, bool hasArea, bool hasEquippedPair, bool hasMatchingOwners, bool cachedPairMatches);

    bool IsActivePair(uint creature);

    /// <summary>Appends <see cref="PairDescriptionNote"/> to a description that lacks it, keeping the existing text.</summary>
    string GetPairDescription(string description);

    /// <summary>
    /// A Power Glove fills both hands, so its equipment stats count once per hand: one glove grants what two
    /// identical Katars do. Every equipped-stat sum uses this, so equipping and removing a glove stay symmetric.
    /// </summary>
    int GetEquipmentStatCopies(BaseItem type);

    uint GetCompanion(uint creature);

    uint ResolveAppearanceOwner(uint creature, uint item);

    /// <summary>Reconciles completed native equips, including failures, without cloning the real item.</summary>
    void EnsurePair(uint creature);

    /// <summary>Reserves space for the real item before either hand is removed.</summary>
    bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID);

    /// <summary>Saved cosmetic objects are discarded; the owned weapon and its upgrades are retained.</summary>
    void RestorePair(uint creature);

    void ReleaseCompanions(uint creature);

    (uint Main, uint Companion)? RemoveOwnerState(uint creature);

    /// <summary>Forgets a destroyed owner before native object IDs can be reused.</summary>
    void ReleaseDestroyedOwner(uint creature);

    void DestroyCompanion(uint companion);

    /// <summary>Copies only appearance state; the companion never receives gameplay properties.</summary>
    void RefreshAppearance(uint creature);

    void SanitizeCompanion(uint companion);

    void CollectCompanions(uint container, HashSet<uint> companions, HashSet<uint> visited);
}
