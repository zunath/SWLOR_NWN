using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles your emote style between regular and novel.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class EmoteStyle : IChatCommand
    {
        private readonly IEmoteStyleService _emoteStyle;

        public EmoteStyle(
            IEmoteStyleService emoteStyle)
        {
            _emoteStyle = emoteStyle;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            Enumeration.EmoteStyle curStyle = _emoteStyle.GetEmoteStyle(user);
            Enumeration.EmoteStyle newStyle = curStyle == Enumeration.EmoteStyle.Novel ? Enumeration.EmoteStyle.Regular : Enumeration.EmoteStyle.Novel;
            _emoteStyle.SetEmoteStyle(user, newStyle);
            user.SendMessage($"Toggled emote style to {newStyle.ToString()}.");
        }

        public bool RequiresTarget => false;
    }
}
