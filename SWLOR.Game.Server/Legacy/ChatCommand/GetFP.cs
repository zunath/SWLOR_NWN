using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Gets the current FP of player.", CommandPermissionType.Player)]
    public class FP : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!user.IsPlayer) return;
            var entity = DataService.Player.GetByID(user.GlobalID);
            user.SendMessage(ColorToken.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));
        }
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
