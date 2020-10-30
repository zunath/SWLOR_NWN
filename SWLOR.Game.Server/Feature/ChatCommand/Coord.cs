using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays your current coordinates in the area.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Coord : IChatCommand
    {
        /// <summary>
        /// Returns the X and Y position, in tiles, of the user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var area = user.Area;
            var width = NWScript.GetAreaSize(Dimension.Width, area);
            var height = NWScript.GetAreaSize(Dimension.Height, area);
            var position = user.Position;
            var cellX = (int)(position.X / 10);
            var cellY = (int)(position.Y / 10);
            var sector = "N/A"; 

            if(width == 32 && height == 32)
                sector = BaseService.GetSectorOfLocation(user.Location);

            user.SendMessage($"Current Area Coordinates: ({cellX}, {cellY}) [Sector: " + sector + "]");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
