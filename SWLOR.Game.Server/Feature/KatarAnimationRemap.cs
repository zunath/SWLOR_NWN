using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class KatarAnimationRemap
    {
        private const string KatarAnimationRemapActiveVariable = "KATAR_ANIM_REMAP_ACTIVE";
        private const string BaseItems2DA = "baseitems";
        private const string ItemClassColumn = "ItemClass";
        private const string SwordItemClassPrefix = "WSw";

        private static readonly (string Old, string New)[] _remapPairs =
        {
            ("1hreadyr", "nwreadyr"),
            ("1hreadyl", "nwreadyl"),
            ("1hslashl", "nwslashl"),
            ("1hslashr", "nwslashr"),
            ("1hstab", "nwstab"),
            ("1hcloseh", "nwcloseh"),
            ("1hclosel", "nwclosel"),
            ("1hreach", "nwreach"),
            ("1hslasho", "nwslasho"),
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

        private static void SyncFromEvent(uint player, uint item)
        {
            if (!GetIsObjectValid(player) || !GetIsObjectValid(item))
                return;

            SyncKatarRemapState(player);
        }

        private static void SyncKatarRemapState(uint creature)
        {
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

        private static bool HasDualWieldKatars(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);

            return GetIsObjectValid(rightHand) &&
                   GetIsObjectValid(leftHand) &&
                   Item.KatarBaseItemTypes.Contains(GetBaseItemType(rightHand)) &&
                   Item.KatarBaseItemTypes.Contains(GetBaseItemType(leftHand));
        }

        private static bool HasOffHandDaggerOrSword(uint creature)
        {
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return false;

            var baseItemType = GetBaseItemType(leftHand);
            if (Item.KatarBaseItemTypes.Contains(baseItemType))
                return false;

            // Use 2DA metadata to include custom sword/dagger variants without hardcoding base item IDs.
            var itemClass = Get2DAString(BaseItems2DA, ItemClassColumn, (int)baseItemType);
            return !string.IsNullOrWhiteSpace(itemClass) &&
                   itemClass.StartsWith(SwordItemClassPrefix);
        }

        private static void ApplyKatarRemap(uint creature)
        {
            foreach (var (oldAnimation, newAnimation) in _remapPairs)
            {
                ReplaceObjectAnimation(creature, oldAnimation, newAnimation);
            }

            Log.Write(LogGroup.Server, $"Applied katar animation remap to {GetName(creature)} ({creature}).");
        }

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
