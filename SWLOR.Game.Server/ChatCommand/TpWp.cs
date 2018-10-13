using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Teleports you to a waypoint with a specified tag.", CommandPermissionType.DM)]
    public class TpWp : IChatCommand
    {
        private readonly INWScript _;

        public TpWp(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 1)
            {
                user.SendMessage("You must specify a waypoint tag. Example: /tpwp MY_WAYPOINT_TAG");
                return;
            }

            string tag = args[0];
            NWObject wp = _.GetWaypointByTag(tag);

            if (!wp.IsValid)
            {
                user.SendMessage("Invalid waypoint tag. Did you enter the right tag?");
                return;
            }

            user.AssignCommand(() =>
            {
                _.ActionJumpToLocation(wp.Location);
            });
        }

        public bool RequiresTarget => false;
    }
}
