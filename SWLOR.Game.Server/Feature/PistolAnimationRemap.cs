using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;
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
        private const string ExplicitThrowSuspendCountVariable = "PISTOL_ANIM_THROW_SUSPEND_COUNT";
        private const float MinimumExplicitThrowRestoreDelaySeconds = 1.1f;
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

        public static bool ShouldSuspendForExplicitThrow(
            Animation animation,
            bool isPistolRemapActive)
        {
            return animation == Animation.ThrowGrenade && isPistolRemapActive;
        }

        public static void PlayAnimationPreservingExplicitThrow(
            uint creature,
            Animation animation)
        {
            var suspendedRemap = SuspendForExplicitThrow(creature, animation);
            ActionPlayAnimation(animation);

            if (suspendedRemap)
                ScheduleRemapAfterExplicitThrow(creature, MinimumExplicitThrowRestoreDelaySeconds);
        }

        public static void PlayAnimationPreservingExplicitThrow(
            uint creature,
            Animation animation,
            float speed,
            float durationSeconds)
        {
            var suspendedRemap = SuspendForExplicitThrow(creature, animation);
            ActionPlayAnimation(animation, speed, durationSeconds);

            if (suspendedRemap)
            {
                ScheduleRemapAfterExplicitThrow(
                    creature,
                    Math.Max(durationSeconds, MinimumExplicitThrowRestoreDelaySeconds));
            }
        }

        private static void SyncAnimationState(uint creature, bool forceApply)
        {
            if (!GetIsObjectValid(creature))
                return;

            var shouldUseFormerAnimation = ShouldUseFormerPistolAttackAnimation(creature);
            var isRemapActive = GetLocalBool(creature, PistolAnimationRemapActiveVariable);

            if (shouldUseFormerAnimation && (forceApply || !isRemapActive))
            {
                ReplaceObjectAnimation(
                    creature,
                    SlingAttackAnimation,
                    FormerPistolAttackAnimation);
                SetLocalBool(creature, PistolAnimationRemapActiveVariable, true);
                Log.WriteStructured(
                    LogGroup.Server,
                    "Pistol animation remap changed: Creature={Creature} Action={Action} Animation={Animation}",
                    creature,
                    "Apply",
                    FormerPistolAttackAnimation);
            }
            else if (!shouldUseFormerAnimation && isRemapActive)
            {
                ReplaceObjectAnimation(creature, SlingAttackAnimation);
                DeleteLocalBool(creature, PistolAnimationRemapActiveVariable);
                Log.WriteStructured(
                    LogGroup.Server,
                    "Pistol animation remap changed: Creature={Creature} Action={Action} Animation={Animation}",
                    creature,
                    "Restore",
                    SlingAttackAnimation);
            }
        }

        private static bool ShouldUseFormerPistolAttackAnimation(uint creature)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            var rightHandBaseItem = GetIsObjectValid(rightHand)
                ? GetBaseItemType(rightHand)
                : (BaseItem?)null;
            var leftHandBaseItem = GetIsObjectValid(leftHand)
                ? GetBaseItemType(leftHand)
                : (BaseItem?)null;

            return ShouldUseFormerPistolAttackAnimation(
                rightHandBaseItem,
                leftHandBaseItem);
        }

        private static bool SuspendForExplicitThrow(uint creature, Animation animation)
        {
            var isRemapActive = GetIsObjectValid(creature) &&
                                GetLocalBool(creature, PistolAnimationRemapActiveVariable);
            if (!ShouldSuspendForExplicitThrow(animation, isRemapActive))
                return false;

            var suspendCount = GetLocalInt(creature, ExplicitThrowSuspendCountVariable);
            if (suspendCount == 0)
                ReplaceObjectAnimation(creature, SlingAttackAnimation);

            SetLocalInt(creature, ExplicitThrowSuspendCountVariable, suspendCount + 1);
            return true;
        }

        private static void ScheduleRemapAfterExplicitThrow(uint creature, float delaySeconds)
        {
            DelayCommand(delaySeconds, () =>
            {
                if (!GetIsObjectValid(creature))
                    return;

                var suspendCount = GetLocalInt(creature, ExplicitThrowSuspendCountVariable);
                if (suspendCount > 1)
                {
                    SetLocalInt(creature, ExplicitThrowSuspendCountVariable, suspendCount - 1);
                    return;
                }

                DeleteLocalInt(creature, ExplicitThrowSuspendCountVariable);
                if (GetLocalBool(creature, PistolAnimationRemapActiveVariable) &&
                    ShouldUseFormerPistolAttackAnimation(creature))
                {
                    ReplaceObjectAnimation(
                        creature,
                        SlingAttackAnimation,
                        FormerPistolAttackAnimation);
                }
            });
        }
    }
}
