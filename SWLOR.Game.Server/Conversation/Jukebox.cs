using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class Jukebox: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage("Please select a song.\n\n((Music files are a separate download that must be installed manually. Refer to our website for downloading and installing these files: https://starwarsnwn.com/ ))"); // Responses dynamically generated

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
            var response = GetResponseByID("MainPage", responseID);
            var jukeboxSongID = (int)response.CustomData;
            var song = DataService.JukeboxSong.GetByID(jukeboxSongID);

            player.FloatingText("Song Selected: " + song.DisplayName);

            NWScript.MusicBackgroundChangeDay(player.Area, song.AmbientMusicID);
            NWScript.MusicBackgroundChangeNight(player.Area, song.AmbientMusicID);
            NWScript.MusicBackgroundPlay(player.Area);
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
