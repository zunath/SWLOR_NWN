using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>Validates the simulated companion without treating its native carrier as a melee weapon.</summary>
public interface ITwinPistolEquipmentService
{
    bool IsChanging(uint creature);

    /// <summary>True while the creature's equipment is being revalidated; revalidation does not nest.</summary>
    bool IsRevalidating(uint creature);

    /// <summary>Marks the creature as being revalidated; false when it already is.</summary>
    bool TryBeginRevalidation(uint creature);

    void EndRevalidation(uint creature);

    /// <summary>The item that could not be returned to storage, and the slot it still occupies.</summary>
    bool TryGetFailedReturn(uint creature, out uint item, out InventorySlot slot);

    void SetFailedReturn(uint creature, uint item, InventorySlot slot);

    void ClearFailedReturn(uint creature);

    bool IsPairItem(uint item);

    bool IsPairItem(BaseItem type, bool marked);

    bool IsGeneratedCompanion(uint item);

    bool IsDestroyingCompanion(uint item);

    bool IsLinkedPair(uint creature, uint main, uint off);

    /// <summary>Includes the required rank used by native weapon attribute-delta caps.</summary>
    bool IsMirroredProperty(ItemPropertyType type);

    /// <summary>Creates the disposable visual/weapon profile for one owned pair item.</summary>
    void EnsurePair(uint creature);

    /// <summary>The generated gun has no appearance of its own; edits go to the owned pistol it mirrors.</summary>
    uint ResolveAppearanceOwner(uint creature, uint item);

    /// <summary>Copies the owned pistol's model parts and colors onto its equipped generated gun.</summary>
    void RefreshAppearance(uint creature);

    /// <summary>Safely returns the owned item and destroys only its generated second gun.</summary>
    bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID);

    /// <summary>Saved spare objects are disposable; rebuild exactly one from the equipped owner.</summary>
    void RestorePair(uint creature);

    /// <summary>Exit cleanup retains the owned equipped item for character saving.</summary>
    void ReleaseCompanions(uint creature);

    /// <summary>Destroyed NPC owners never run module-exit cleanup; drop their cached pair.</summary>
    void ReleaseDestroyedOwner(uint creature);

    void DestroySpare(uint spare);

    bool IsPair(BaseItem? mainHandType, BaseItem? offHandType);

    string GetPairCompatibilityError(BaseItem? mainHandType, BaseItem? offHandType);

    (uint MainHand, uint OffHand) GetProspectiveHands(uint creature, uint item, InventorySlot slot);

    bool IsActivePair(uint creature);

    /// <summary>The simulated hand, available only when both equipped items satisfy the pair's requirements.</summary>
    uint GetCompanionWeapon(uint creature);

    /// <summary>Only a paired main-hand pistol's stored Pistol requirement is reinterpreted.</summary>
    SkillType ResolveRequirementSkill(BaseItem itemType, SkillType requirement, bool paired);

    /// <summary>Pure classification for an already validated arrangement; explicit ability identities win.</summary>
    SkillType ResolveSkill(BaseItem weaponType, BaseItem? mainHandType, BaseItem? offHandType, SkillType? explicitOverride = null);

    /// <summary>Companion properties are profile data only; no native or creature-wide equipped bonuses.</summary>
    bool IsSupportedCompanionProperty(ItemPropertyType type);

    string GetCompanionPropertyError(uint item);
}
