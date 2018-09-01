using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the world time to 8 PM.", CommandPermissionType.DM)]
    public class Night : IChatCommand
    {
        private readonly INWScript _;
        
        public Night(INWScript script)
        {
            _ = script;
        }
        
        public void DoAction(NWPlayer user, params string[] args)
        {
            _.SetTime(20, 0, 0, 0);
        }
    }
}
