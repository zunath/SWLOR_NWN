using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles your emote style between regular and novel.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class EmoteStyle : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            Enumeration.EmoteStyle curStyle = EmoteStyleService.GetEmoteStyle(user);
            Enumeration.EmoteStyle newStyle = curStyle == Enumeration.EmoteStyle.Novel ? Enumeration.EmoteStyle.Regular : Enumeration.EmoteStyle.Novel;
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
