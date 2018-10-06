using System.Linq;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Contracts;
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
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;

        public ToggleHolonet(
            INWScript script,
            IDataContext db,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _color = color;
        }

        public void DoAction(NWPlayer user, params string[] args)
        {
            var player = _db.PlayerCharacters.Single(x => x.PlayerID == user.GlobalID);
            player.DisplayHolonet = !player.DisplayHolonet;
            user.SetLocalInt("DISPLAY_HOLONET", player.DisplayHolonet ? 1 : 0);
            _db.SaveChanges();

            if (player.DisplayHolonet)
            {
                user.SendMessage("Holonet chat: " + _color.Green("ENABLED"));
            }
            else
            {
                user.SendMessage("Holonet chat: " + _color.Red("DISABLED"));
            }
        }
    }
}
