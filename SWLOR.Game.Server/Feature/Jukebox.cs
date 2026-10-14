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

            var payload = new MusicPickerPayload(MusicPickerMode.Jukebox, jukebox);

            // DMs open the picker the way the toolbar's Music button does, so the two share one window across
            // possession changes.
            if (GetIsDM(user) || GetIsDMPossessed(user))
                DMPresence.ToggleWindow(user, GuiWindowType.MusicPicker, payload, jukebox);
            else
                Gui.TogglePlayerWindow(user, GuiWindowType.MusicPicker, payload, jukebox);
        }
    }
}
