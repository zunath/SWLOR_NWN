using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class WorldMap
    {
        /// <summary>
        /// When a player enters the world map, shrink them down and reduce movement speed to 50% of normal.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void EnterWorldMap()
        {
            if (!GetLocalBool(OBJECT_SELF, "WORLD_MAP")) return;

            var player = GetEnteringObject();

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, 0.75f);
            var effect = EffectMovementSpeedDecrease(35);
            effect = TagEffect(effect, "WORLD_MAP_EFFECT");

            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }

        /// <summary>
        /// When a player exits the world map, return them to normal size and set movement speed back to normal.
        /// </summary>
        [NWNEventHandler("area_exit")]
        public static void ExitWorldMap()
        {
            if (!GetLocalBool(OBJECT_SELF, "WORLD_MAP")) return;

            var player = GetExitingObject();

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, 1.0f);
            RemoveEffectByTag(player, "WORLD_MAP_EFFECT");
        }

        /// <summary>
        /// When a player uses a teleport object, they are sent to the waypoint with a tag matching
        /// the DESTINATION variable on the object.
        /// </summary>
        [NWNEventHandler("teleport")]
        public static void PlaceableTeleport()
        {
            var waypointTag = GetLocalString(OBJECT_SELF, "DESTINATION");
            if (string.IsNullOrWhiteSpace(waypointTag)) return;

            var player = GetLastUsedBy();
            var waypoint = GetWaypointByTag(waypointTag);
            var location = GetLocation(waypoint);
            AssignCommand(player, () => ActionJumpToLocation(location));
        }
    }
}
