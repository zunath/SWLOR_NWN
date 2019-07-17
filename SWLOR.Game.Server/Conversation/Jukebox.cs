using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class Jukebox: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Please select a song.\n\n((Music files are a separate download that must be installed manually. Refer to our website for downloading and installing these files: https://starwarsnwn.com/ ))"); // Responses dynamically generated

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            ClearPageResponses("MainPage");

            var songs = DataService.JukeboxSong.GetAll().Where(x => x.IsActive).OrderBy(o => o.DisplayName);
            foreach (var song in songs)
            {
                AddResponseToPage("MainPage", song.DisplayName, true, song.ID);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            int jukeboxSongID = (int)response.CustomData;
            JukeboxSong song = DataService.JukeboxSong.GetByID(jukeboxSongID);

            player.FloatingText("Song Selected: " + song.DisplayName);

            _.MusicBackgroundChangeDay(player.Area, song.AmbientMusicID);
            _.MusicBackgroundChangeNight(player.Area, song.AmbientMusicID);
            _.MusicBackgroundPlay(player.Area);
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
