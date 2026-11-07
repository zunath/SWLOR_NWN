using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which abilities a crew member may use from a ship's station (plan section 10): their own Piloting abilities,
    /// which the ship activates for them. A viewport watcher has no controls.
    /// </summary>
    public static class SpaceCrewAbilityRules
    {
        public const string ViewportError = "You cannot use abilities from a viewport.";
        public const string NotPilotingError = "Only Piloting abilities can be used at a crew station.";

        /// <summary>Why a character at a station or viewport cannot use an ability, or an empty string when they can.</summary>
        public static string GetStationAbilityError(bool isSeated, bool canBeUsedInSpace, SkillType skill)
        {
            if (!isSeated)
                return ViewportError;

            return canBeUsedInSpace && skill == SkillType.Piloting
                ? string.Empty
                : NotPilotingError;
        }
    }
}
