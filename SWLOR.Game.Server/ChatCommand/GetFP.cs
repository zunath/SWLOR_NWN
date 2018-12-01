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
    [CommandDetails("Gets the current FP of player.", CommandPermissionType.Player)]
    public class FP : IChatCommand
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        public FP(
            INWScript script,
            IColorTokenService color,
            IDataService data)
        {
            _ = script;
            _color = color;
            _data = data;
        }
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!user.IsPlayer) return;
            Player entity = _data.Get<Player>(user.GlobalID);
            user.SendMessage(_color.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));
        }
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
