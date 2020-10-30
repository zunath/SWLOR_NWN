using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles your emote style between regular and novel.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class EmoteStyle : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var curStyle = EmoteStyleService.GetEmoteStyle(user);
            var newStyle = curStyle == Enumeration.EmoteStyle.Novel ? Enumeration.EmoteStyle.Regular : Enumeration.EmoteStyle.Novel;
            EmoteStyleService.SetEmoteStyle(user, newStyle);
            user.SendMessage($"Toggled emote style to {newStyle.ToString()}.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
