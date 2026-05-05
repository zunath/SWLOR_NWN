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

        private static readonly (string Old, string New)[] _remapPairs =
        {
            // Core 1h attack chain -> unarmed equivalents.
            ("1hreadyr", "nwreadyr"),
            ("1hreadyl", "nwreadyl"),
            ("1hslashl", "nwslashl"),
            ("1hslashr", "nwslashr"),
            ("1hstab", "nwstab"),
            ("1hcloseh", "nwcloseh"),
            ("1hclosel", "nwclosel"),
            ("1hreach", "nwreach"),
            ("1hslasho", "nwslasho"),
            // 1h parries are mapped to unarmed-style dodges.
            ("1hparryl", "dodges"),
            ("1hparryr", "dodgelr"),
        };

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip()
        {
            SyncFromEvent(GetPCItemLastEquippedBy(), GetPCItemLastEquipped());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip()
        {
            SyncFromEvent(GetPCItemLastUnequippedBy(), GetPCItemLastUnequipped());
        }

        /// <summary>
        /// Shared guard path for equip/unequip module events.
        /// If either object is invalid, we do nothing.
        /// </summary>
        private static void SyncFromEvent(uint player, uint item)
        {
            if (!GetIsObjectValid(player) || !GetIsObjectValid(item))
                return;

            SyncKatarRemapState(player);
        }

        
        // Determines whether remaps should be active right now and applies/restores exactly once per state change.
        private static void SyncKatarRemapState(uint creature)
        {
            // Keep remap on for dual-katar explicitly, otherwise only when main hand is katar and
            // offhand is not sword/dagger-family (those combos should keep regular 1h piercing behavior).
            var shouldUseKatarRemap =
                HasDualWieldKatars(creature) ||
                (HasMainHandKatar(creature) && !HasOffHandDaggerOrSword(creature));
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

        private static bool HasMainHandKatar(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            return GetIsObjectValid(rightHand) && Item.KatarBaseItemTypes.Contains(GetBaseItemType(rightHand));
        }

        /// Explicit dual-katar override: always use unarmed remap in this setup.
        private static bool HasDualWieldKatars(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);

            return GetIsObjectValid(rightHand) &&
                   GetIsObjectValid(leftHand) &&
                   Item.KatarBaseItemTypes.Contains(GetBaseItemType(rightHand)) &&
                   Item.KatarBaseItemTypes.Contains(GetBaseItemType(leftHand));
        }


        /// Offhand sword/dagger check is 2da driven to avoid hardcoding.
        /// Any custom base item that uses the sword ItemClass family (WSw*) is treated the same.
        /// Katars are excluded so dual-katar can still force unarmed remap.
        private static bool HasOffHandDaggerOrSword(uint creature)
        {
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return false;

            var baseItemType = GetBaseItemType(leftHand);
            if (Item.KatarBaseItemTypes.Contains(baseItemType))
                return false;

            // Use baseitems.2da to check if the item is a sword or dagger.
            var itemClass = Get2DAString(BaseItems2DA, ItemClassColumn, (int)baseItemType);
            return !string.IsNullOrWhiteSpace(itemClass) &&
                   itemClass.StartsWith(SwordItemClassPrefix);
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
