using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// One owned Power Glove supplies the weapon; its other hand is disposable appearance only. The logic lives in
/// <see cref="IPowerGloveEquipmentService"/>; the constants stay here and every member forwards to the container's
/// instance.
/// </summary>
public static class PowerGloveEquipment
{
    private static IPowerGloveEquipmentService Instance => FacadeAccessor.Resolve<IPowerGloveEquipmentService>();

    public const string SpareVariable = "POWER_GLOVE_SPARE";
    public const string MainWeaponVariable = "POWER_GLOVE_MAIN";
    public const string OwnerVariable = "POWER_GLOVE_OWNER";
    public const string CompanionVariable = "POWER_GLOVE_COMPANION";

    /// <summary>The line every Power Glove's description carries, so players can see why its bonuses count twice.</summary>
    public const string PairDescriptionNote =
        "One Power Glove covers both hands: each hand strikes with its DMG, and its other bonuses count once per hand, like two identical Katars.";

    /// <summary>True while a cosmetic hand is being generated.</summary>
    internal static bool IsCreatingCompanion => Instance.IsCreatingCompanion;

    public static bool IsChanging(uint creature) =>
        Instance.IsChanging(creature);

    public static bool IsPairItem(uint item) =>
        Instance.IsPairItem(item);

    public static bool IsGeneratedCompanion(uint item) =>
        Instance.IsGeneratedCompanion(item);

    public static bool IsDestroyingCompanion(uint item) =>
        Instance.IsDestroyingCompanion(item);

    public static bool IsLinkedPair(uint creature, uint main, uint companion) =>
        Instance.IsLinkedPair(creature, main, companion);

    internal static bool IsEquippedCompanion(uint companion) =>
        Instance.IsEquippedCompanion(companion);

    /// <summary>Rebinds saved item references only while a player's equipped inventory is loading.</summary>
    public static bool TryRestoreSavedCompanion(uint companion) =>
        Instance.TryRestoreSavedCompanion(companion);

    internal static bool CanRestoreSavedPair(bool isPlayer, bool hasArea, bool hasEquippedPair, bool hasMatchingOwners, bool cachedPairMatches) =>
        Instance.CanRestoreSavedPair(isPlayer, hasArea, hasEquippedPair, hasMatchingOwners, cachedPairMatches);

    public static bool IsActivePair(uint creature) =>
        Instance.IsActivePair(creature);

    /// <summary>Appends <see cref="PairDescriptionNote"/> to a description that lacks it, keeping the existing text.</summary>
    public static string GetPairDescription(string description) =>
        Instance.GetPairDescription(description);

    /// <summary>
    /// A Power Glove fills both hands, so its equipment stats count once per hand: one glove grants what two
    /// identical Katars do. Every equipped-stat sum uses this, so equipping and removing a glove stay symmetric.
    /// </summary>
    public static int GetEquipmentStatCopies(BaseItem type) =>
        Instance.GetEquipmentStatCopies(type);

    public static uint GetCompanion(uint creature) =>
        Instance.GetCompanion(creature);

    public static uint ResolveAppearanceOwner(uint creature, uint item) =>
        Instance.ResolveAppearanceOwner(creature, item);

    /// <summary>Reconciles completed native equips, including failures, without cloning the real item.</summary>
    public static void EnsurePair(uint creature) =>
        Instance.EnsurePair(creature);

    /// <summary>Reserves space for the real item before either hand is removed.</summary>
    public static bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID) =>
        Instance.RemovePair(creature, removedItem);

    /// <summary>Saved cosmetic objects are discarded; the owned weapon and its upgrades are retained.</summary>
    public static void RestorePair(uint creature) =>
        Instance.RestorePair(creature);

    public static void ReleaseCompanions(uint creature) =>
        Instance.ReleaseCompanions(creature);

    internal static (uint Main, uint Companion)? RemoveOwnerState(uint creature) =>
        Instance.RemoveOwnerState(creature);

    /// <summary>Forgets a destroyed owner before native object IDs can be reused.</summary>
    public static void ReleaseDestroyedOwner(uint creature) =>
        Instance.ReleaseDestroyedOwner(creature);

    public static void DestroyCompanion(uint companion) =>
        Instance.DestroyCompanion(companion);

    /// <summary>Copies only appearance state; the companion never receives gameplay properties.</summary>
    public static void RefreshAppearance(uint creature) =>
        Instance.RefreshAppearance(creature);

    public static void SanitizeCompanion(uint companion) =>
        Instance.SanitizeCompanion(companion);

    internal static void CollectCompanions(uint container, HashSet<uint> companions, HashSet<uint> visited) =>
        Instance.CollectCompanions(container, companions, visited);
}
