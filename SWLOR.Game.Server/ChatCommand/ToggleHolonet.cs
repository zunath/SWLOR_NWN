using System.Linq;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Enables or disables holonet chat channel.", CommandPermissionType.Player)]
    public class ToggleHolonet : IChatCommand
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IColorTokenService _color;

        public ToggleHolonet(
            INWScript script,
            IDataService data,
            IColorTokenService color)
        {
            _ = script;
            _data = data;
            _color = color;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var player = _data.Get<PlayerCharacter>(user.GlobalID);
            player.DisplayHolonet = !player.DisplayHolonet;
            user.SetLocalInt("DISPLAY_HOLONET", player.DisplayHolonet ? 1 : 0);

            _data.SubmitDataChange(player, DatabaseActionType.Update);

            if (player.DisplayHolonet)
            {
                user.SendMessage("Holonet chat: " + _color.Green("ENABLED"));
            }
            else
            {
                user.SendMessage("Holonet chat: " + _color.Red("DISABLED"));
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
