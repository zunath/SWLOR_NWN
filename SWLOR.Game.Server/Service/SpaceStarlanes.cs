using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Dangerous starlanes (plan section 12). When a pilot enters a space area, each lane in it that leads to a higher
    /// tier, a Small and Medium hull zone or Wild Space gets a danger tag on its name and a warning in its description,
    /// so the pilot sees it before jumping. Jumping one repeats the warning (see <see cref="StarlaneDangerRules"/>).
    /// </summary>
    public static partial class Space
    {
        private const string StarlaneTag = "tele_obj";
        private const string StarlaneDestinationVariable = "DESTINATION";
        private const string StarlaneDangerMarkedVariable = "STARLANE_DANGER_MARKED";

        /// <summary>Tags the dangerous lanes of an area the first time anyone enters it.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void MarkDangerousStarlanesOnEnter()
        {
            var area = OBJECT_SELF;
            if (GetObjectType(GetEnteringObject()) != ObjectType.Creature || !IsSpaceArea(area) || GetLocalBool(area, StarlaneDangerMarkedVariable))
                return;

            SetLocalBool(area, StarlaneDangerMarkedVariable, true);
            var fromResref = GetResRef(area);
            for (var lane = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(lane); lane = GetNextObjectInArea(area, ObjectType.Placeable))
            {
                if (GetTag(lane) != StarlaneTag)
                    continue;

                var destinationArea = GetStarlaneDestinationArea(lane);
                var danger = StarlaneDangerRules.Classify(fromResref, GetResRef(destinationArea));
                if (danger == StarlaneDanger.None)
                    continue;

                var name = GetName(lane);
                if (!name.Contains(StarlaneDangerRules.NameMarker))
                    SetName(lane, name + StarlaneDangerRules.GetNameTag(danger, GetResRef(destinationArea)));

                SetDescription(lane, StarlaneDangerRules.GetDescription(GetDescription(lane), danger, fromResref, GetResRef(destinationArea)));
            }
        }

        /// <summary>The area a lane's DESTINATION waypoint (or object) is in, or invalid.</summary>
        public static uint GetStarlaneDestinationArea(uint lane)
        {
            var destination = GetLocalString(lane, StarlaneDestinationVariable);
            if (string.IsNullOrWhiteSpace(destination))
                return OBJECT_INVALID;

            var waypoint = GetWaypointByTag(destination);
            if (!GetIsObjectValid(waypoint))
                waypoint = GetObjectByTag(destination);

            return GetIsObjectValid(waypoint) ? GetArea(waypoint) : OBJECT_INVALID;
        }

        /// <summary>
        /// Warns a creature jumping a lane from <paramref name="fromArea"/> to <paramref name="destinationArea"/> when the
        /// lane is dangerous. Nothing is said for a ground teleporter or a safe lane.
        /// </summary>
        public static void WarnOfDangerousStarlane(uint creature, uint fromArea, uint destinationArea)
        {
            if (!IsSpaceArea(fromArea))
                return;

            var fromResref = GetResRef(fromArea);
            var toResref = GetResRef(destinationArea);
            var warning = StarlaneDangerRules.GetWarning(StarlaneDangerRules.Classify(fromResref, toResref), fromResref, toResref, GetName(destinationArea));
            if (!string.IsNullOrEmpty(warning))
                SendMessageToPC(creature, ColorToken.Orange(warning));
        }
    }
}
