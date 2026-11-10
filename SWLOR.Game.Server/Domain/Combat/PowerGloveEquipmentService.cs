using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>
/// One owned Power Glove supplies the weapon; its other hand is disposable appearance only. The logic that used to be
/// static in <c>Service.PowerGloveEquipment</c> is moved here unchanged; the cached pairs, the re-entrancy guards and the
/// companion-creation flag are this instance's own. The item service is injected; calls into the unconverted
/// <c>Droid</c> service, the equipment storage and the glove fit code sit behind one-line <c>protected virtual</c>
/// methods whose bodies are the original calls.
/// </summary>
public class PowerGloveEquipmentService : IPowerGloveEquipmentService
{
    public const string SpareVariable = "POWER_GLOVE_SPARE";
    public const string MainWeaponVariable = "POWER_GLOVE_MAIN";
    public const string OwnerVariable = "POWER_GLOVE_OWNER";
    public const string CompanionVariable = "POWER_GLOVE_COMPANION";
    private const string DestroyingVariable = "POWER_GLOVE_REMOVING";
    private const string CompanionResref = "pg_companion";

    private readonly Dictionary<uint, (uint Main, uint Companion)> _pairs = new();
    private readonly HashSet<uint> _changing = new();
    private readonly HashSet<uint> _refreshingAppearance = new();
    private readonly ObjectVisualTransformDataScopeType[] _transformScopes =
    {
        ObjectVisualTransformDataScopeType.Base,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart1,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart3
    };

    private readonly IItemService _item;

    private readonly Lazy<IDroidService> _droid;

    public PowerGloveEquipmentService(IItemService item, Lazy<IDroidService> droid)
    {
        _droid = droid;
        _item = item;
    }

    public bool IsCreatingCompanion { get; private set; }

    public bool IsChanging(uint creature) => _changing.Contains(creature);

    // A droid has no client; its controlling player gets the explanation.
    private uint MessageRecipient(uint creature) => IsDroid(creature) ? GetMaster(creature) : creature;

    public bool IsPairItem(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && GetBaseItemType(item) == BaseItem.PowerGlove;

    // The dedicated public type remains disposable even when saved local references are stale.
    public bool IsGeneratedCompanion(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && GetBaseItemType(item) == BaseItem.CompanionPowerGlove;

    public bool IsDestroyingCompanion(uint item) =>
        IsGeneratedCompanion(item) && GetLocalInt(item, DestroyingVariable) == 1;

    public bool IsLinkedPair(uint creature, uint main, uint companion) =>
        GetIsObjectValid(creature) && IsPairItem(main) && IsGeneratedCompanion(companion) &&
        !IsDestroyingCompanion(companion) &&
        GetLocalObject(main, CompanionVariable) == companion &&
        GetLocalObject(companion, MainWeaponVariable) == main &&
        GetLocalObject(companion, OwnerVariable) == creature &&
        GetItemPossessor(main) == creature && GetItemPossessor(companion) == creature;

    public bool IsEquippedCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion)) return false;
        var owner = GetLocalObject(companion, OwnerVariable);
        var main = GetLocalObject(companion, MainWeaponVariable);
        return IsLinkedPair(owner, main, companion) &&
               GetItemInSlot(InventorySlot.RightHand, owner) == main &&
               GetItemInSlot(InventorySlot.LeftHand, owner) == companion;
    }

    /// <summary>Rebinds saved item references only while a player's equipped inventory is loading.</summary>
    public bool TryRestoreSavedCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion) || IsDestroyingCompanion(companion)) return false;
        var owner = GetItemPossessor(companion);
        if (!GetIsObjectValid(owner)) return false;
        var main = GetItemInSlot(InventorySlot.RightHand, owner);
        var cachedPairMatches = !_pairs.TryGetValue(owner, out var existing) ||
                                existing.Main == main && existing.Companion == companion;
        if (!CanRestoreSavedPair(GetIsPC(owner), GetIsObjectValid(GetArea(owner)),
                IsPairItem(main) && GetItemInSlot(InventorySlot.LeftHand, owner) == companion,
                GetItemPossessor(main) == owner && GetLocalObject(companion, OwnerVariable) == owner,
                cachedPairMatches))
            return false;

        // BIC acquire events can precede remapping saved cross-item references to the equipped pair.
        SetLocalObject(main, CompanionVariable, companion);
        SetLocalObject(companion, MainWeaponVariable, main);
        SanitizeCompanion(companion);
        return IsEquippedCompanion(companion);
    }

    public bool CanRestoreSavedPair(bool isPlayer, bool hasArea, bool hasEquippedPair,
        bool hasMatchingOwners, bool cachedPairMatches) =>
        isPlayer && !hasArea && hasEquippedPair && hasMatchingOwners && cachedPairMatches;

    public bool IsActivePair(uint creature)
    {
        if (!GetIsObjectValid(creature)) return false;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        return IsLinkedPair(creature, main, companion) &&
               !GetIsItemPropertyValid(GetFirstItemProperty(companion)) &&
               string.IsNullOrWhiteSpace(_item.GetCreatureItemUseError(creature, main));
    }

    /// <summary>The line every Power Glove's description carries, so players can see why its bonuses count twice.</summary>
    public const string PairDescriptionNote =
        "One Power Glove covers both hands: each hand strikes with its DMG, and its other bonuses count once per hand, like two identical Katars.";

    /// <summary>Appends <see cref="PairDescriptionNote"/> to a description that lacks it, keeping the existing text.</summary>
    public string GetPairDescription(string description) =>
        description.Contains(PairDescriptionNote, StringComparison.Ordinal) ? description
        : string.IsNullOrWhiteSpace(description) ? PairDescriptionNote
        : $"{description.TrimEnd()}\n{PairDescriptionNote}";

    /// <summary>
    /// A Power Glove fills both hands, so its equipment stats count once per hand: one glove grants what two
    /// identical Katars do. Every equipped-stat sum uses this, so equipping and removing a glove stay symmetric.
    /// </summary>
    public int GetEquipmentStatCopies(BaseItem type) => type == BaseItem.PowerGlove ? 2 : 1;

    public uint GetCompanion(uint creature) => IsActivePair(creature)
        ? GetItemInSlot(InventorySlot.LeftHand, creature) : OBJECT_INVALID;

    public uint ResolveAppearanceOwner(uint creature, uint item)
    {
        if (!IsGeneratedCompanion(item)) return item;
        var main = GetLocalObject(item, MainWeaponVariable);
        return IsLinkedPair(creature, main, item) ? main : OBJECT_INVALID;
    }

    /// <summary>Reconciles completed native equips, including failures, without cloning the real item.</summary>
    public void EnsurePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature || IsChanging(creature))
            return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsPairItem(main))
        {
            if (IsGeneratedCompanion(companion) || _pairs.ContainsKey(creature))
                RemovePair(creature);
            return;
        }
        if (!string.IsNullOrWhiteSpace(_item.GetCreatureItemUseError(creature, main)))
        {
            RemovePair(creature, main);
            return;
        }
        if (IsLinkedPair(creature, main, companion))
        {
            _pairs[creature] = (main, companion);
            SanitizeCompanion(companion);
            RefreshAppearance(creature);
            return;
        }
        if (GetIsObjectValid(companion))
        {
            RemovePair(creature, main);
            return;
        }

        // Dispose of a detached old cosmetic before generating its replacement.
        if (_pairs.Remove(creature, out var previous)) DestroyCompanion(previous.Companion);
        DeleteLocalObject(main, CompanionVariable);
        _changing.Add(creature);
        companion = OBJECT_INVALID;
        try
        {
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(storage))
                throw new InvalidOperationException("Temporary item storage is unavailable.");
            IsCreatingCompanion = true;
            try { companion = CreateItemOnObject(CompanionResref, storage); }
            finally { IsCreatingCompanion = false; }
            if (!IsGeneratedCompanion(companion))
                throw new InvalidOperationException("The Power Glove cosmetic could not be created.");
            SetLocalObject(companion, MainWeaponVariable, main);
            SetLocalObject(companion, OwnerVariable, creature);
            SetLocalObject(main, CompanionVariable, companion);
            SetName(companion, GetName(main) + " (Other Hand)");
            SanitizeCompanion(companion);
            _pairs[creature] = (main, companion);
            if (!MoveIntoOwnedStorage(creature, companion, creature, new HashSet<uint>()))
                throw new InvalidOperationException("The other hand could not be stored safely.");
            CreaturePlugin.RunEquip(creature, companion, InventorySlot.LeftHand);
            if (GetItemInSlot(InventorySlot.LeftHand, creature) != companion ||
                GetItemPossessor(companion) != creature)
                throw new InvalidOperationException("The other hand could not be equipped safely.");
            // Acquiring an item can normalize its native loot flags.
            SanitizeCompanion(companion);
            RefreshAppearance(creature);
        }
        catch (InvalidOperationException)
        {
            DestroyCompanion(companion);
            _pairs.Remove(creature);
            DeleteLocalObject(main, CompanionVariable);
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red("The Power Glove could not equip both hands. Free inventory or bag space and try again."));
        }
        finally { _changing.Remove(creature); }

        // A failed generated-hand equip must not leave a functioning lone weapon.
        if (!IsActivePair(creature)) RemovePair(creature, main);
    }

    /// <summary>Reserves space for the real item before either hand is removed.</summary>
    public bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return false;
        if (!_pairs.TryGetValue(creature, out var pair))
        {
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!IsPairItem(main) && IsGeneratedCompanion(companion))
                main = GetLocalObject(companion, MainWeaponVariable);
            pair = (main, companion);
        }
        if (GetIsObjectValid(removedItem) && removedItem != pair.Main && removedItem != pair.Companion)
            return false;
        _changing.Add(creature);
        try
        {
            if (IsPairItem(pair.Main) && GetItemInSlot(InventorySlot.RightHand, creature) == pair.Main)
                UnequipOwnedItem(creature, pair.Main, InventorySlot.RightHand);
            DestroyCompanion(pair.Companion);
            if (IsPairItem(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            _pairs.Remove(creature);
            return true;
        }
        catch (InvalidOperationException)
        {
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red("Free inventory or bag space before removing the Power Glove."));
            return false;
        }
        finally { _changing.Remove(creature); }
    }

    /// <summary>Saved cosmetic objects are discarded; the owned weapon and its upgrades are retained.</summary>
    public void RestorePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        ReleaseCompanions(creature);
        DelayCommand(.1f, () =>
        {
            if (!GetIsObjectValid(creature)) return;
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var left = GetItemInSlot(InventorySlot.LeftHand, creature);
            // Native saves can contain an invalid hand combination. Preserve that real item too.
            if (IsPairItem(main) && GetIsObjectValid(left))
            {
                _changing.Add(creature);
                try { UnequipOwnedItem(creature, left, InventorySlot.LeftHand); }
                catch (InvalidOperationException)
                {
                    SendMessageToPC(MessageRecipient(creature), ColorToken.Red("Free inventory or bag space before equipping the Power Glove."));
                }
                finally { _changing.Remove(creature); }
            }
            EnsurePair(creature);
        });
    }

    public void ReleaseCompanions(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        _changing.Add(creature);
        try
        {
            var companions = new HashSet<uint>();
            if (_pairs.Remove(creature, out var pair))
            {
                companions.Add(pair.Companion);
                if (IsPairItem(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            }
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                CollectCompanions(GetItemInSlot((InventorySlot)slot, creature), companions, new HashSet<uint>());
            CollectCompanions(creature, companions, new HashSet<uint>());
            foreach (var companion in companions)
            {
                var main = GetLocalObject(companion, MainWeaponVariable);
                if (IsPairItem(main) && GetItemPossessor(main) == creature)
                    DeleteLocalObject(main, CompanionVariable);
                DestroyCompanion(companion);
            }
        }
        finally { _changing.Remove(creature); }
    }

    public (uint Main, uint Companion)? RemoveOwnerState(uint creature)
    {
        _changing.Remove(creature);
        _refreshingAppearance.Remove(creature);
        return _pairs.Remove(creature, out var pair) ? pair : null;
    }

    /// <summary>Forgets a destroyed owner before native object IDs can be reused.</summary>
    public void ReleaseDestroyedOwner(uint creature)
    {
        var pair = RemoveOwnerState(creature);
        if (!pair.HasValue) return;

        if (IsPairItem(pair.Value.Main) &&
            GetLocalObject(pair.Value.Main, CompanionVariable) == pair.Value.Companion)
            DeleteLocalObject(pair.Value.Main, CompanionVariable);
        if (IsGeneratedCompanion(pair.Value.Companion) &&
            GetLocalObject(pair.Value.Companion, OwnerVariable) == creature)
            DestroyCompanion(pair.Value.Companion);
    }

    public void DestroyCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion) || IsDestroyingCompanion(companion)) return;
        SetLocalInt(companion, DestroyingVariable, 1);
        SetPlotFlag(companion, false);
        DestroyObject(companion);
    }

    /// <summary>Copies only appearance state; the companion never receives gameplay properties.</summary>
    public void RefreshAppearance(uint creature)
    {
        if (!GetIsObjectValid(creature) || _refreshingAppearance.Contains(creature)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsLinkedPair(creature, main, companion)) return;
        _refreshingAppearance.Add(creature);
        try
        {
            var changedAppearance = false;
            foreach (var part in new[] { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top })
            foreach (var type in new[] { ItemAppearanceType.WeaponColor, ItemAppearanceType.WeaponModel })
            {
                var value = GetItemAppearance(main, type, (int)part);
                if (GetItemAppearance(companion, type, (int)part) == value) continue;
                ItemPlugin.SetItemAppearance(companion, type, (int)part, value, updateCreatureAppearance: false);
                changedAppearance = true;
            }
            if (GetHiddenWhenEquipped(companion) != GetHiddenWhenEquipped(main))
            {
                SetHiddenWhenEquipped(companion, GetHiddenWhenEquipped(main) != 0);
                changedAppearance = true;
            }
            foreach (var scope in _transformScopes)
            foreach (var transform in Enum.GetValues<ObjectVisualTransform>())
            {
                var value = GetMirroredTransform(main, companion, transform, scope);
                if (Math.Abs(GetObjectVisualTransform(companion, transform, nScope: scope) - value) > .0001f)
                    SetObjectVisualTransform(companion, transform, value, nScope: scope);
            }
            // Invalidate this same-ID item's observer caches after changing its held model.
            // TintMap's nested fit pass may run normally, but cannot enter this sync twice.
            if (changedAppearance) RefreshEquippedItemAppearance(creature, companion);
        }
        finally { _refreshingAppearance.Remove(creature); }
    }

    public void SanitizeCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion) || IsDestroyingCompanion(companion)) return;
        SetLocalInt(companion, SpareVariable, 1);
        SetLocalInt(companion, Domain.Items.ItemService.NoEconomyVariable, 1);
        SetPlotFlag(companion, true);
        SetDroppableFlag(companion, false);
        SetPickpocketableFlag(companion, false);
        ItemPlugin.SetWeight(companion, 0);
        ItemPlugin.SetBaseGoldPieceValue(companion, 0);
        ItemPlugin.SetAddGoldPieceValue(companion, 0);
        var properties = new List<ItemProperty>();
        for (var ip = GetFirstItemProperty(companion); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(companion))
            properties.Add(ip);
        foreach (var property in properties) RemoveItemProperty(companion, property);
    }

    public void CollectCompanions(uint container, HashSet<uint> companions, HashSet<uint> visited)
    {
        if (!GetIsObjectValid(container) || !visited.Add(container)) return;
        if (IsGeneratedCompanion(container)) companions.Add(container);
        if (!GetHasInventory(container)) return;
        var contents = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            contents.Add(item);
        foreach (var item in contents) CollectCompanions(item, companions, visited);
    }

    private bool MoveIntoOwnedStorage(uint creature, uint companion, uint container, HashSet<uint> visited)
    {
        if (!visited.Add(container)) return false;
        if (ItemPlugin.MoveTo(companion, container, true) && GetItemPossessor(companion) == creature) return true;
        var contents = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            if (GetHasInventory(item) && GetItemPossessor(item) == creature) contents.Add(item);
        foreach (var bag in contents)
            if (MoveIntoOwnedStorage(creature, companion, bag, visited)) return true;
        return false;
    }

    private void UnequipOwnedItem(uint creature, uint item, InventorySlot slot)
    {
        // A recovery bag would let a full inventory duplicate the pair's storage; keep the item equipped instead.
        if (!TryUnequip(creature, item, slot, allowRecoveryBag: false))
            throw new InvalidOperationException("No owned storage can hold the Power Glove.");
    }

    /// <summary>The unconverted <c>Droid</c> service.</summary>
    protected virtual bool IsDroid(uint creature) => _droid.Value.IsDroid(creature);

    /// <summary>The equipment storage that returns an item to the owner's inventory.</summary>
    protected virtual bool TryUnequip(uint creature, uint item, InventorySlot slot, bool allowRecoveryBag) =>
        PlayerEquipmentStorage.TryUnequip(creature, item, slot, allowRecoveryBag: allowRecoveryBag);

    /// <summary>The held-item appearance refresh for observers.</summary>
    protected virtual void RefreshEquippedItemAppearance(uint creature, uint item) =>
        EquippedItemAppearance.Refresh(creature, item);

    /// <summary>The glove fit rule that mirrors the main glove's visual transform onto the cosmetic hand.</summary>
    protected virtual float GetMirroredTransform(uint main, uint companion, ObjectVisualTransform transform,
        ObjectVisualTransformDataScopeType scope) =>
        PowerGloveFit.GetMirroredTransform(main, companion, transform, scope);
}
