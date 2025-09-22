using SWLOR.Component.World.Service;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;

namespace SWLOR.Component.World.Dialog
{
    public class JukeboxDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public JukeboxDialog(IDialogService dialogService) : base(dialogService)
        {
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, (page) =>
                {
                    page.Header = "Please select a song.";

                    foreach (var song in Music.GetAllSongs())
                    {
                        page.AddResponse(song.DisplayName, () =>
                        {
                            var area = GetArea(player);
                            FloatingTextStringOnCreature($"Song Selected: {song.DisplayName}", player, false);

                            MusicBackgroundChangeDay(area, song.ID);
                            MusicBackgroundChangeNight(area, song.ID);
                            MusicBackgroundPlay(area);
                        });
                    }

                });

            return builder.Build();
        }
    }
}
