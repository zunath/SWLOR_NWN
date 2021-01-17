using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class PlaceableScripts
    {
        /// <summary>
        /// When a teleport placeable is used, send the user to the configured waypoint.
        /// Checks are made for required key items, if specified as local variables on the placeable.
        /// </summary>
        [NWNEventHandler("teleport")]
        public static void UseTeleportDevice()
        {
            var user = GetLastUsedBy();

            if (GetIsInCombat(user))
            {
                SendMessageToPC(user, "You are in combat.");
                return;
            }

            var device = OBJECT_SELF;
            var destination = GetLocalString(device, "DESTINATION");
            var vfxId = GetLocalInt(device, "VISUAL_EFFECT");
            var vfx = vfxId > 0 ? (VisualEffect) vfxId : VisualEffect.None;
            var requiredKeyItemId = GetLocalInt(device, "KEY_ITEM_ID");
            var missingKeyItemMessage = GetLocalString(device, "MISSING_KEY_ITEM_MESSAGE");
            if (string.IsNullOrWhiteSpace(missingKeyItemMessage))
                missingKeyItemMessage = "You don't have the necessary key item to access this object.";

            if (requiredKeyItemId > 0)
            {
                var keyItem = (KeyItemType) requiredKeyItemId;

                if (!KeyItem.HasKeyItem(user, keyItem))
                {
                    SendMessageToPC(user, missingKeyItemMessage);

                    return;
                }
            }

            if (vfx != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(vfx), user);
            }

            var waypoint = GetWaypointByTag(destination);

            if (!GetIsObjectValid(waypoint))
            {
                SendMessageToPC(user, "Cannot locate waypoint. Inform an admin this teleporter is broken.");
                return;
            }

            var location = GetLocation(waypoint);
            AssignCommand(user, () => ActionJumpToLocation(location));
        }

        /// <summary>
        /// Applies a permanent VFX on a placeable on heartbeat, then removes the heartbeat script.
        /// </summary>
        [NWNEventHandler("permanent_vfx")]
        public static void ApplyPermanentVisualEffect()
        {
            var placeable = OBJECT_SELF;

            var vfxId = GetLocalInt(placeable, "PERMANENT_VFX_ID");
            var vfx = vfxId > 0 ? (VisualEffect) vfxId : VisualEffect.None;
            
            if (vfx != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(vfx), placeable);
            }

            SetEventScript(placeable, EventScript.Placeable_OnHeartbeat, string.Empty);
        }

        /// <summary>
        /// Handles starting a generic conversation when a placeable is clicked or used by a player or DM.
        /// </summary>
        [NWNEventHandler("generic_convo")]
        public static void GenericConversation()
        {
            var placeable = OBJECT_SELF;
            var user = GetObjectType(placeable) == ObjectType.Placeable ? GetLastUsedBy() : GetClickingObject();

            if (!GetIsPC(user) && !GetIsDM(user)) return;

            var conversation = GetLocalString(placeable, "CONVERSATION");
            var target = GetLocalBool(placeable, "TARGET_PC") ? user : placeable;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                Dialog.StartConversation(user, target, conversation);
            }
            else
            {
                AssignCommand(user, () => ActionStartConversation(target, string.Empty, true, false));
            }
        }
        
    }
}
