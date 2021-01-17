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
            var obj = OBJECT_SELF;

            var vfxId = GetLocalInt(obj, "PERMANENT_VFX_ID");
            var vfx = vfxId > 0 ? (VisualEffect) vfxId : VisualEffect.None;
            
            if (vfx != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(vfx), obj);
            }

            SetEventScript(obj, EventScript.Placeable_OnHeartbeat, string.Empty);
        }
        
    }
}
