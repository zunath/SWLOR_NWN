using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    // Remaps selected one-handed combat animations to "no weapon" animations while a player is using katars.
    public static class KatarAnimationRemap
    {
        private const string KatarAnimationRemapActiveVariable = "KATAR_ANIM_REMAP_ACTIVE";
        private const string BaseItems2DA = "baseitems";
        private const string ItemClassColumn = "ItemClass";
        private const string SwordItemClassPrefix = "WSw";
        private static readonly HashSet<BaseItem> _swordFamilyBaseItems = BuildSwordFamilyBaseItems();

        private static class AnimationKey
        {
            public const string OneHandReadyR = "1hreadyr";
            public const string OneHandReadyL = "1hreadyl";
            public const string OneHandSlashL = "1hslashl";
            public const string OneHandSlashR = "1hslashr";
            public const string OneHandStab = "1hstab";
            public const string OneHandCloseH = "1hcloseh";
            public const string OneHandCloseL = "1hclosel";
            public const string OneHandReach = "1hreach";
            public const string OneHandSlashO = "1hslasho";
            public const string OneHandParryL = "1hparryl";
            public const string OneHandParryR = "1hparryr";

            public const string UnarmedReadyR = "nwreadyr";
            public const string UnarmedReadyL = "nwreadyl";
            public const string UnarmedSlashL = "nwslashl";
            public const string UnarmedSlashR = "nwslashr";
            public const string UnarmedStab = "nwstab";
            public const string UnarmedCloseH = "nwcloseh";
            public const string UnarmedCloseL = "nwclosel";
            public const string UnarmedReach = "nwreach";
            public const string UnarmedSlashO = "nwslasho";
            public const string UnarmedDodgeS = "dodges";
            public const string UnarmedDodgeLR = "dodgelr";
        }

        private static readonly (string Old, string New)[] _remapPairs =
        {
            // Core 1h attack chain -> unarmed equivalents.
            (AnimationKey.OneHandReadyR, AnimationKey.UnarmedReadyR),
            (AnimationKey.OneHandReadyL, AnimationKey.UnarmedReadyL),
            (AnimationKey.OneHandSlashL, AnimationKey.UnarmedSlashL),
            (AnimationKey.OneHandSlashR, AnimationKey.UnarmedSlashR),
            (AnimationKey.OneHandStab, AnimationKey.UnarmedStab),
            (AnimationKey.OneHandCloseH, AnimationKey.UnarmedCloseH),
            (AnimationKey.OneHandCloseL, AnimationKey.UnarmedCloseL),
            (AnimationKey.OneHandReach, AnimationKey.UnarmedReach),
            (AnimationKey.OneHandSlashO, AnimationKey.UnarmedSlashO),
            // 1h parries are mapped to unarmed-style dodges.
            (AnimationKey.OneHandParryL, AnimationKey.UnarmedDodgeS),
            (AnimationKey.OneHandParryR, AnimationKey.UnarmedDodgeLR),
        };

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip()
        {
            SyncFromEvent(GetPCItemLastEquippedBy());
        }  

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            var player = GetEnteringObject();
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

        // Determines whether remaps should be active right now and applies/restores exactly once per state change.
        private static void SyncKatarRemapState(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            BaseItem? rightHandBaseItem = GetIsObjectValid(rightHand) ? GetBaseItemType(rightHand) : null;
            BaseItem? leftHandBaseItem = GetIsObjectValid(leftHand) ? GetBaseItemType(leftHand) : null;

            // Keep remap on for dual-katar explicitly, otherwise only when main hand is katar and
            // offhand is not sword/dagger-family (those combos should keep regular 1h piercing behavior).
            var shouldUseKatarRemap =
                HasDualWieldKatars(creature, rightHandBaseItem, leftHandBaseItem) ||
                (HasMainHandKatar(rightHandBaseItem) && !HasOffHandDaggerOrSword(leftHandBaseItem));
            var isRemapActive = GetLocalBool(creature, KatarAnimationRemapActiveVariable);

            if (shouldUseKatarRemap && !isRemapActive)
            {
                ApplyKatarRemap(creature);
                SetLocalBool(creature, KatarAnimationRemapActiveVariable, true);
            }
            else if (!shouldUseKatarRemap && isRemapActive)
            {
                RestoreKatarRemap(creature);
                DeleteLocalBool(creature, KatarAnimationRemapActiveVariable);
            }
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

        // Applies every configured animation replacement for this creature.
        private static void ApplyKatarRemap(uint creature)
        {
            foreach (var (oldAnimation, newAnimation) in _remapPairs)
            {
                ReplaceObjectAnimation(creature, oldAnimation, newAnimation);
            }

            Log.Write(LogGroup.Server, $"Applied katar animation remap to {GetName(creature)} ({creature}).");
        }

        // Restores all replaced animation keys
        private static void RestoreKatarRemap(uint creature)
        {
            foreach (var (oldAnimation, _) in _remapPairs)
            {
                ReplaceObjectAnimation(creature, oldAnimation);
            }

            Log.Write(LogGroup.Server, $"Restored default animations after katar unequip for {GetName(creature)} ({creature}).");
        }
    }
}
