using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the world time to 8 AM.", CommandPermissionType.DM)]
    public class Day: IChatCommand
    {
        private readonly INWScript _;
        
        public Day(INWScript script)
        {
            _ = script;
        }
        
        public void DoAction(NWPlayer user, params string[] args)
        {
            _.SetTime(8, 0, 0, 0);
        }
    }
}
