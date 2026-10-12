using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class MusicPickerPayload : GuiPayloadBase
    {
        public MusicPickerMode Mode { get; set; }
        public uint Jukebox { get; set; }
        public uint MusicPlayer { get; set; }

        public MusicPickerPayload(MusicPickerMode mode, uint jukebox = OBJECT_INVALID, uint musicPlayer = OBJECT_INVALID)
        {
            Mode = mode;
            Jukebox = jukebox;
            MusicPlayer = musicPlayer;
        }
    }
}
