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
    [CommandDetails("Enables or disables Discord chat channel.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class ToggleDiscord : IChatCommand
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;

        public ToggleDiscord(
            INWScript script,
            IDataContext db,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _color = color;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var player = _db.PlayerCharacters.Single(x => x.PlayerID == user.GlobalID);
            player.DisplayDiscord = !player.DisplayDiscord;
            user.SetLocalInt("DISPLAY_DISCORD", player.DisplayDiscord ? 1 : 0);
            _db.SaveChanges();

            if (player.DisplayDiscord)
            {
                user.SendMessage("Discord chat: " + _color.Green("ENABLED"));
            }
            else
            {
                user.SendMessage("Discord chat: " + _color.Red("DISABLED"));
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
