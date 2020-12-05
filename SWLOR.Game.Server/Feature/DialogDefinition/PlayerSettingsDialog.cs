using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlayerSettingsDialog: DialogBase
    {
        private class Model
        {
            public int SelectedBattleThemeId { get; set; }
            public int CurrentDayTimeTheme { get; set; }
            public int CurrentNightTimeTheme { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ChangeBattleThemeId = "CHANGE_BATTLE_THEME_PAGE";
        private const string SongDetailId = "SONG_DETAIL_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ChangeBattleThemeId, ChangeBattleThemeInit)
                .AddPage(SongDetailId, SongDetailInit)
                .AddBackAction(BackAction)
                .AddEndAction(EndAction);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var toggleAchievementNotificationText = dbPlayer.Settings.DisplayAchievementNotification ? "Disable Achievement Notification" : "Enable Achievement Notification";

            page.Header =
                ColorToken.Green("Achievement Notifications: ") + (dbPlayer.Settings.DisplayAchievementNotification ? "ENABLED" : "DISABLED") + "\n" +
                "\nYou may adjust your personal character settings here.";

            page.AddResponse("Change Battle Theme", () => ChangePage(ChangeBattleThemeId));
            page.AddResponse(toggleAchievementNotificationText, () =>
            {
                dbPlayer.Settings.DisplayAchievementNotification = !dbPlayer.Settings.DisplayAchievementNotification;
                DB.Set(playerId, dbPlayer);
            });
        }

        private void ChangeBattleThemeInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var currentBattleThemeId = dbPlayer.Settings.BattleThemeId;
            var battleSongs = Music.GetBattleSongs();
            var header = ColorToken.Green("Current Battle Theme: ");

            if (currentBattleThemeId == null)
            {
                header += "(Area determines theme)";
            }
            else
            {
                var song = battleSongs[(int) currentBattleThemeId];
                header += song.DisplayName;
            }

            page.Header = header;

            page.AddResponse(ColorToken.Green("Use Area Theme"), () =>
            {
                dbPlayer.Settings.BattleThemeId = null;
                Core.NWNX.Player.MusicBattleChange(player, 0);
                DB.Set(playerId, dbPlayer);
            });

            foreach (var song in battleSongs)
            {
                page.AddResponse(song.Value.DisplayName, () =>
                {
                    model.SelectedBattleThemeId = song.Key;
                    ChangePage(SongDetailId);
                });
            }
        }

        private void SongDetailInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var currentBattleThemeId = dbPlayer.Settings.BattleThemeId;
            var battleSongs = Music.GetBattleSongs();
            var header = ColorToken.Green("Current Battle Theme: ");

            if (currentBattleThemeId == null)
            {
                header += "(Area determines theme)";
            }
            else
            {
                var song = battleSongs[(int)currentBattleThemeId];
                header += song.DisplayName;
            }

            page.Header = header + "\n\n" + 
                          ColorToken.Green("Selecting Theme: ") + battleSongs[model.SelectedBattleThemeId].DisplayName;

            page.AddResponse("Preview Song", () =>
            {
                var area = GetArea(player);
                model.CurrentDayTimeTheme = MusicBackgroundGetDayTrack(area);
                model.CurrentNightTimeTheme = MusicBackgroundGetNightTrack(area);

                Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.SelectedBattleThemeId, false);
                Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.SelectedBattleThemeId, true);
            });

            page.AddResponse("Select Song", () =>
            {
                dbPlayer.Settings.BattleThemeId = model.SelectedBattleThemeId;
                DB.Set(playerId, dbPlayer);

                Core.NWNX.Player.MusicBattleChange(player, model.SelectedBattleThemeId);
            });
        }

        private void BackAction(string oldPage, string newPage)
        {
            if (oldPage != SongDetailId) return;

            var player = GetPC();
            var model = GetDataModel<Model>();
            Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.CurrentDayTimeTheme, false);
            Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.CurrentNightTimeTheme, true);
        }

        private void EndAction()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.CurrentDayTimeTheme, false);
            Core.NWNX.Player.MusicBackgroundChangeTimeToggle(player, model.CurrentNightTimeTheme, true);
        }
    }
}
