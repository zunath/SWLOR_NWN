using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Feature
{
    // One owner remaps selected one-handed combat animations for katars and Power Gloves.
    public static class KatarAnimationRemap
    {
        private const string BaseItems2DA = "baseitems";
        private const string ItemClassColumn = "ItemClass";
        private const string SwordItemClassPrefix = "WSw";
        private static readonly HashSet<BaseItem> _swordFamilyBaseItems = BuildSwordFamilyBaseItems();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void RegisterEquipmentAnimationRestoration()
        {
            QueuedAttackAnimation.RestoreEquipmentAnimations -= RestoreCurrentEquipmentAnimations;
            QueuedAttackAnimation.RestoreEquipmentAnimations += RestoreCurrentEquipmentAnimations;
        }

        private static void RestoreCurrentEquipmentAnimations(uint creature) =>
            SyncKatarRemapState(creature);

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip()
        {
            SyncFromEvent(GetPCItemLastEquippedBy());
        }

        /// <summary>
        /// Login synchronization, run by <see cref="SavedAnimationMappings"/> after it clears the
        /// mappings a character loaded from the server vault brought back.
        /// </summary>
        public static void SynchronizeOnLogin(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            SyncKatarRemapState(player);
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip()
        {
            SyncFromEvent(GetPCItemLastUnequippedBy());
        }

        // Shared guard path for equip/unequip module events.
        private static void SyncFromEvent(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            SyncKatarRemapState(player);
        }

        // Brings the creature's animation mappings in line with its equipment. The engine saves these
        // mappings with the character, so a login or equipment change can find stale ones to remove.
        private static void SyncKatarRemapState(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            BaseItem? rightHandBaseItem = GetIsObjectValid(rightHand) ? GetBaseItemType(rightHand) : null;
            BaseItem? leftHandBaseItem = GetIsObjectValid(leftHand) ? GetBaseItemType(leftHand) : null;

            // Keep remap on for dual-katar explicitly, otherwise only when main hand is katar and
            // offhand is not sword/dagger-family (those combos should keep regular 1h piercing behavior).
            var shouldUseKatarRemap =
                EquipmentPredicates.HasUnarmedGrip(rightHandBaseItem, leftHandBaseItem) ||
                HasDualWieldKatars(creature, rightHandBaseItem, leftHandBaseItem) ||
                (HasMainHandKatar(rightHandBaseItem) && !HasOffHandDaggerOrSword(leftHandBaseItem));
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject();
            var changes = KatarAnimationRemapPlan.GetChanges(shouldUseKatarRemap,
                WeaponAttackAnimation.ReadAnimationReplacements(native), QueuedAttackAnimation.GetActiveSwingKeys(creature));
            if (changes.Count == 0)
                return;

            foreach (var (source, replacement) in changes)
            {
                ReplaceObjectAnimation(creature, source, replacement);
            }

            Log.Write(LogGroup.Server, shouldUseKatarRemap
                ? $"Applied katar animation remap to {GetName(creature)} ({creature})."
                : $"Restored default animations after katar unequip for {GetName(creature)} ({creature}).");
        }

        private static bool HasMainHandKatar(BaseItem? rightHandBaseItem)
        {
            return rightHandBaseItem.HasValue && Item.KatarBaseItemTypes.Contains(rightHandBaseItem.Value);
        }

        // Explicit dual-katar override: always use unarmed remap in this setup.
        private static bool HasDualWieldKatars(uint creature, BaseItem? rightHandBaseItem, BaseItem? leftHandBaseItem)
        {
            if (HasDualWieldKatars(rightHandBaseItem, leftHandBaseItem))
                return true;

            // Equip/unequip events can briefly report stale inventory state. Re-check once using
            // fresh slot reads so dual-katar users keep unarmed remaps during transition frames.
            var latestRightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var latestLeftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(latestRightHand) || !GetIsObjectValid(latestLeftHand))
                return false;

            var latestRightBaseItem = GetBaseItemType(latestRightHand);
            var latestLeftBaseItem = GetBaseItemType(latestLeftHand);
            return HasDualWieldKatars(latestRightBaseItem, latestLeftBaseItem);
        }

        private static bool HasDualWieldKatars(BaseItem? rightHandBaseItem, BaseItem? leftHandBaseItem)
        {
            return rightHandBaseItem.HasValue &&
                   leftHandBaseItem.HasValue &&
                   Item.KatarBaseItemTypes.Contains(rightHandBaseItem.Value) &&
                   Item.KatarBaseItemTypes.Contains(leftHandBaseItem.Value);
        }

        // Offhand sword/dagger check is 2da driven to avoid hardcoding.
        // Any custom base item that uses the sword ItemClass family (WSw*) is treated the same.
        // Katars are excluded so dual-katar can still force unarmed remap.
        private static bool HasOffHandDaggerOrSword(BaseItem? leftHandBaseItem)
        {
            if (!leftHandBaseItem.HasValue)
                return false;

            var baseItemType = leftHandBaseItem.Value;
            if (Item.KatarBaseItemTypes.Contains(baseItemType))
                return false;

            return _swordFamilyBaseItems.Contains(baseItemType);
        }

        // Precompute all sword-family (WSw*) base items once to avoid runtime 2da scans.
        private static HashSet<BaseItem> BuildSwordFamilyBaseItems()
        {
            var set = new HashSet<BaseItem>();
            var rowCount = Get2DARowCount(BaseItems2DA);
            for (var row = 0; row < rowCount; row++)
            {
                var itemClass = Get2DAString(BaseItems2DA, ItemClassColumn, row);
                if (!string.IsNullOrWhiteSpace(itemClass) &&
                    itemClass.StartsWith(SwordItemClassPrefix, StringComparison.Ordinal))
                {
                    set.Add((BaseItem)row);
                }
            }

            return set;
        }
    }
}
