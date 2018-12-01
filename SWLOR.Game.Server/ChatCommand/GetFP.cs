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
    [CommandDetails("Gets the Current FP of player.",CommandPermissionType.Player)]
    public class GetFP : IChatCommand
    {

        public GetFP(
            INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
                              if (!user.IsPlayer) return;
                              Player entity = _data.Get<Player>(oPC.GlobalID);
                             user.SendMessage(_color.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219)););
    }
}
