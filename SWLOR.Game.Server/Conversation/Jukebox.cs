using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class Jukebox: ConversationBase
    {
        private class Song
        {
            public int ID { get; set; }
            public string DisplayName { get; set; }
            public string Resource { get; set; }

            public Song(int id, string displayName, string resource)
            {
                ID = id;
                DisplayName = displayName;
                Resource = resource;
            }
        }

        private class Model
        {
            public List<Song> Songs { get; set; } = new List<Song>();
        }

        private static Dictionary<int, Song> _songs = new Dictionary<int, Song>();
        static Jukebox()
        {
            LoadSongs();
        }

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

            foreach (var song in _songs.Values)
            {
                AddResponseToPage("MainPage", song.DisplayName, true, song.ID);
            }
        }

        private static void LoadSongs()
        {
            var model = new Model();
            const string File = "ambientmusic";
            int rowCount = NWNXUtil.Get2DARowCount(File);

            for (int row = 0; row < rowCount; row++)
            {
                string description = _.Get2DAString(File, "Description", row);
                string resource = _.Get2DAString(File, "Resource", row);
                string displayName = _.Get2DAString(File, "DisplayName", row);

                string name = description == "****" ? displayName : _.GetStringByStrRef(Convert.ToInt32(description));

                model.Songs.Add(new Song(row, name, resource));
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            int jukeboxSongID = (int)response.CustomData;
            var song = _songs[jukeboxSongID];

            player.FloatingText("Song Selected: " + song.DisplayName);

            _.MusicBackgroundChangeDay(player.Area, song.ID);
            _.MusicBackgroundChangeNight(player.Area, song.ID);
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
