using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class Resource
    {
        /// <summary>
        /// When a resource is used, display an error message indicating that a harvester is needed.
        /// </summary>
        [NWNEventHandler("res_used")]
        public static void OnUsed()
        {
            var user = GetLastUsedBy();
            SendMessageToPC(user, "Use a harvester to retrieve resources from this object.");
        }

        /// <summary>
        /// When a resource's heartbeat fires for the first time, spawn a prop object if specified.
        /// </summary>
        [NWNEventHandler("res_heartbeat")]
        public static void OnHeartbeat()
        {
            var placeable = OBJECT_SELF;

            // Remove the heartbeat script no matter what.
            SetEventScript(placeable, EventScript.Placeable_OnHeartbeat, string.Empty);

            if (GetLocalBool(placeable, "RESOURCE_PROP_SPAWNED")) return;

            var propResref = GetLocalString(placeable, "RESOURCE_PROP");
            if (string.IsNullOrWhiteSpace(propResref)) return;

            // Spawn the prop and link it back to the actual placeable.
            var location = GetLocation(placeable);
            var prop = CreateObject(ObjectType.Placeable, propResref, location);
            SetLocalObject(placeable, "RESOURCE_PROP_OBJ", prop);
            SetLocalBool(placeable, "RESOURCE_PROP_SPAWNED", true);
        }
    }
}
