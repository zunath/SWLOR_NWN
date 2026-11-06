using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Validates the simulated companion without treating its native carrier as a melee weapon. The logic lives in
/// <see cref="ITwinPistolEquipmentService"/>; the constants stay here and every member forwards to the container's
/// instance.
/// </summary>
public static class TwinPistolEquipment
{
    private static ITwinPistolEquipmentService Instance => FacadeAccessor.Resolve<ITwinPistolEquipmentService>();

    public const string PairVariable = "TWIN_PISTOL_PAIR";
    public const string SpareVariable = "TWIN_PISTOL_SPARE";
    public const string MainWeaponVariable = "TWIN_PISTOL_MAIN";
    public const string OwnerVariable = "TWIN_PISTOL_OWNER";

    public static bool IsChanging(uint creature) =>
        Instance.IsChanging(creature);

    public static bool IsRevalidating(uint creature) =>
        Instance.IsRevalidating(creature);

    public static bool TryBeginRevalidation(uint creature) =>
        Instance.TryBeginRevalidation(creature);

    public static void EndRevalidation(uint creature) =>
        Instance.EndRevalidation(creature);

    public static bool TryGetFailedReturn(uint creature, out uint item, out InventorySlot slot) =>
        Instance.TryGetFailedReturn(creature, out item, out slot);

    public static void SetFailedReturn(uint creature, uint item, InventorySlot slot) =>
        Instance.SetFailedReturn(creature, item, slot);

    public static void ClearFailedReturn(uint creature) =>
        Instance.ClearFailedReturn(creature);

    public static bool IsPairItem(uint item) =>
        Instance.IsPairItem(item);

    public static bool IsPairItem(BaseItem type, bool marked) =>
        Instance.IsPairItem(type, marked);

    public static bool IsGeneratedCompanion(uint item) =>
        Instance.IsGeneratedCompanion(item);

    public static bool IsDestroyingCompanion(uint item) =>
        Instance.IsDestroyingCompanion(item);

    public static bool IsLinkedPair(uint creature, uint main, uint off) =>
        Instance.IsLinkedPair(creature, main, off);

    /// <summary>Includes the required rank used by native weapon attribute-delta caps.</summary>
    public static bool IsMirroredProperty(ItemPropertyType type) =>
        Instance.IsMirroredProperty(type);

    /// <summary>Creates the disposable visual/weapon profile for one owned pair item.</summary>
    public static void EnsurePair(uint creature) =>
        Instance.EnsurePair(creature);

    /// <summary>The generated gun has no appearance of its own; edits go to the owned pistol it mirrors.</summary>
    public static uint ResolveAppearanceOwner(uint creature, uint item) =>
        Instance.ResolveAppearanceOwner(creature, item);

    /// <summary>Copies the owned pistol's model parts and colors onto its equipped generated gun.</summary>
    public static void RefreshAppearance(uint creature) =>
        Instance.RefreshAppearance(creature);

    /// <summary>Safely returns the owned item and destroys only its generated second gun.</summary>
    public static bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID) =>
        Instance.RemovePair(creature, removedItem);

    /// <summary>Saved spare objects are disposable; rebuild exactly one from the equipped owner.</summary>
    public static void RestorePair(uint creature) =>
        Instance.RestorePair(creature);

    /// <summary>Exit cleanup retains the owned equipped item for character saving.</summary>
    public static void ReleaseCompanions(uint creature) =>
        Instance.ReleaseCompanions(creature);

    /// <summary>Destroyed NPC owners never run module-exit cleanup; drop their cached pair.</summary>
    public static void ReleaseDestroyedOwner(uint creature) =>
        Instance.ReleaseDestroyedOwner(creature);

    public static void DestroySpare(uint spare) =>
        Instance.DestroySpare(spare);

    public static bool IsPair(BaseItem? mainHandType, BaseItem? offHandType) =>
        Instance.IsPair(mainHandType, offHandType);

    public static string GetPairCompatibilityError(BaseItem? mainHandType, BaseItem? offHandType) =>
        Instance.GetPairCompatibilityError(mainHandType, offHandType);

    public static (uint MainHand, uint OffHand) GetProspectiveHands(uint creature, uint item, InventorySlot slot) =>
        Instance.GetProspectiveHands(creature, item, slot);

    public static bool IsActivePair(uint creature) =>
        Instance.IsActivePair(creature);

    /// <summary>The simulated hand, available only when both equipped items satisfy the pair's requirements.</summary>
    public static uint GetCompanionWeapon(uint creature) =>
        Instance.GetCompanionWeapon(creature);

    /// <summary>Only a paired main-hand pistol's stored Pistol requirement is reinterpreted.</summary>
    public static SkillType ResolveRequirementSkill(BaseItem itemType, SkillType requirement, bool paired) =>
        Instance.ResolveRequirementSkill(itemType, requirement, paired);

    /// <summary>Pure classification for an already validated arrangement; explicit ability identities win.</summary>
    public static SkillType ResolveSkill(BaseItem weaponType, BaseItem? mainHandType, BaseItem? offHandType, SkillType? explicitOverride = null) =>
        Instance.ResolveSkill(weaponType, mainHandType, offHandType, explicitOverride);

    /// <summary>Companion properties are profile data only; no native or creature-wide equipped bonuses.</summary>
    public static bool IsSupportedCompanionProperty(ItemPropertyType type) =>
        Instance.IsSupportedCompanionProperty(type);

    public static string GetCompanionPropertyError(uint item) =>
        Instance.GetCompanionPropertyError(item);
}
