using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays your current coordinates in the area.", CommandPermissionType.Player | CommandPermissionType.DM)]
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
            NWArea area = user.Area;
            Vector position = user.Position;
            int cellX = (int)(position.m_X / 10);
            int cellY = (int)(position.m_Y / 10);
            string sector = "N/A"; 

            if(area.Width == 32 && area.Height == 32)
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
