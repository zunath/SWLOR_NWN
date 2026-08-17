using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Restores the former pistol attack animation while retaining the native sling base item
    /// behavior required for one-handed pistols and bullet ammunition. The former animation is
    /// only safe with an empty offhand because it uses both hands.
    /// </summary>
    public static class PistolAnimationRemap
    {
        private const string PistolAnimationRemapActiveVariable = "PISTOL_ANIM_REMAP_ACTIVE";
        public const string SlingAttackAnimation = "throwr";
        public const string FormerPistolAttackAnimation = "bowshot";

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            SyncAnimationState(GetEnteringObject(), true);
        }

        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void OnCreatureSpawn()
        {
            SyncAnimationState(OBJECT_SELF, true);
        }

        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn()
        {
            SyncAnimationState(GetLastRespawnButtonPresser(), true);
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        public static void OnItemEquip()
        {
            SyncAnimationState(OBJECT_SELF, false);
        }

        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void OnItemUnequip()
        {
            SyncAnimationState(OBJECT_SELF, false);
        }

        public static bool ShouldUseFormerPistolAttackAnimation(
            BaseItem? rightHandBaseItem,
            BaseItem? leftHandBaseItem)
        {
            return rightHandBaseItem.HasValue &&
                   Item.PistolBaseItemTypes.Contains(rightHandBaseItem.Value) &&
                   !leftHandBaseItem.HasValue;
        }

        private static void SyncAnimationState(uint creature, bool forceApply)
        {
            if (!GetIsObjectValid(creature))
                return;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            var rightHandBaseItem = GetIsObjectValid(rightHand)
                ? GetBaseItemType(rightHand)
                : (BaseItem?)null;
            var leftHandBaseItem = GetIsObjectValid(leftHand)
                ? GetBaseItemType(leftHand)
                : (BaseItem?)null;
            var shouldUseFormerAnimation = ShouldUseFormerPistolAttackAnimation(
                rightHandBaseItem,
                leftHandBaseItem);
            var isRemapActive = GetLocalBool(creature, PistolAnimationRemapActiveVariable);

            if (shouldUseFormerAnimation && (forceApply || !isRemapActive))
            {
                ReplaceObjectAnimation(
                    creature,
                    SlingAttackAnimation,
                    FormerPistolAttackAnimation);
                SetLocalBool(creature, PistolAnimationRemapActiveVariable, true);
            }
            else if (!shouldUseFormerAnimation && isRemapActive)
            {
                ReplaceObjectAnimation(creature, SlingAttackAnimation);
                DeleteLocalBool(creature, PistolAnimationRemapActiveVariable);
            }
        }
    }
}
