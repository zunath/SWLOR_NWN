using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Feature
{
    public static class Jukebox
    {
        /// <summary>
        /// When a jukebox is used, open the music window for its owner's collection.
        /// DMs get every track instead.
        /// </summary>
        [NWNEventHandler(ScriptName.OnJukeboxUsed)]
        public static void UseJukebox()
        {
            var user = GetLastUsedBy();
            var jukebox = OBJECT_SELF;
            if (!GetIsPC(user))
                return;

            var player = user;
            var uiTarget = OBJECT_INVALID;
            if (GetIsDMPossessed(player))
            {
                uiTarget = player;
                player = GetMaster(player);
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.MusicPicker, new MusicPickerPayload(MusicPickerMode.Jukebox, jukebox), jukebox, uiTarget);
        }
    }
}
