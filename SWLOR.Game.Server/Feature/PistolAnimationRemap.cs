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

        /// <summary>
        /// Synchronizes a player creature's pistol animation when it enters the module.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            ResetTransientSuspensionAndSyncAnimationState(GetEnteringObject());
        }

        /// <summary>
        /// Synchronizes a spawned creature's pistol animation.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void OnCreatureSpawn()
        {
            ResetTransientSuspensionAndSyncAnimationState(OBJECT_SELF);
        }

        /// <summary>
        /// Reapplies the correct pistol animation after a player respawns.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn()
        {
            ResetTransientSuspensionAndSyncAnimationState(GetLastRespawnButtonPresser());
        }

        /// <summary>
        /// Synchronizes the pistol animation after an item is equipped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        public static void OnItemEquip()
        {
            SyncAnimationState(OBJECT_SELF, false);
        }

        /// <summary>
        /// Synchronizes the pistol animation after an item is unequipped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void OnItemUnequip()
        {
            SyncAnimationState(OBJECT_SELF, false);
        }

        /// <summary>
        /// Determines whether the equipped loadout can safely use the former two-handed pistol attack.
        /// </summary>
        public static bool ShouldUseFormerPistolAttackAnimation(
            BaseItem? rightHandBaseItem,
            BaseItem? leftHandBaseItem)
        {
            return rightHandBaseItem.HasValue &&
                   Item.PistolBaseItemTypes.Contains(rightHandBaseItem.Value) &&
                   !leftHandBaseItem.HasValue;
        }

        /// <summary>
        /// Determines whether an explicit animation needs the persistent pistol remap suspended.
        /// </summary>
        public static bool ShouldSuspendForExplicitThrow(
            Animation animation,
            bool isPistolRemapActive)
        {
            return animation == Animation.ThrowGrenade && isPistolRemapActive;
        }

        /// <summary>
        /// Plays an animation while preserving explicit throws from the persistent pistol remap.
        /// </summary>
        public static void PlayAnimationPreservingExplicitThrow(
            uint creature,
            Animation animation)
        {
            var suspendedRemap = SuspendForExplicitThrow(creature, animation);
            ActionPlayAnimation(animation);

            if (suspendedRemap)
                ScheduleRemapAfterExplicitThrow(creature, MinimumExplicitThrowRestoreDelaySeconds);
        }

        /// <summary>
        /// Plays a timed animation while preserving explicit throws from the persistent pistol remap.
        /// </summary>
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

        /// <summary>
        /// Plays an animation with its configured temporary replacement while preserving explicit
        /// throws from the persistent pistol remap.
        /// </summary>
        public static void PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(
            uint creature,
            Animation animation,
            float speed,
            float durationSeconds,
            string sourceAnimationName,
            string replacementAnimationName,
            float replacementRestoreDelaySeconds)
        {
            var suspendedRemap = SuspendForExplicitThrow(creature, animation);
            ReplaceObjectAnimation(creature, sourceAnimationName, replacementAnimationName);
            ActionPlayAnimation(animation, speed, durationSeconds);
            DelayCommand(replacementRestoreDelaySeconds, () =>
            {
                ReplaceObjectAnimation(creature, sourceAnimationName);
            });

            if (suspendedRemap)
            {
                ScheduleRemapAfterExplicitThrow(
                    creature,
                    Math.Max(
                        Math.Max(durationSeconds, replacementRestoreDelaySeconds),
                        MinimumExplicitThrowRestoreDelaySeconds));
            }
        }

        /// <summary>
        /// Clears throw state that cannot survive an export or lifecycle transition, then reapplies
        /// the persistent remap for the creature's current loadout.
        /// </summary>
        private static void ResetTransientSuspensionAndSyncAnimationState(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            DeleteLocalInt(creature, ExplicitThrowSuspendCountVariable);
            SyncAnimationState(creature, true);
        }

        /// <summary>
        /// Applies or removes the persistent pistol remap to match the creature's current loadout.
        /// </summary>
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

        /// <summary>
        /// Determines whether a creature's current equipped loadout qualifies for the remap.
        /// </summary>
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

        /// <summary>
        /// Temporarily removes the persistent remap when the requested animation uses its carrier.
        /// </summary>
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

        /// <summary>
        /// Restores a suspended remap after all overlapping explicit throws have completed.
        /// </summary>
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
