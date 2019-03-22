using System;
using System.Linq;
using System.Reflection;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets the XP bonus on a particular player.", CommandPermissionType.DM)]
    public class GetXPBonus : IChatCommand
    {
        
        private readonly IDataService _data;

        public GetXPBonus(
            
            IDataService data)
        {
            
            _data = data;
        }
        
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only players may be targeted with this command.");
                return;
            }

            NWPlayer player = target.Object;
            var dbPlayer = _data.Get<Player>(player.GlobalID);

            user.SendMessage(player.Name +  " XP Bonus = " + dbPlayer.XPBonus);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
