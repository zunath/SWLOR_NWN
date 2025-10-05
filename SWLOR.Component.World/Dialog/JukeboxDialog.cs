using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Component.World.Dialog
{
    public class JukeboxDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        private readonly IMusicService _musicService;

        public JukeboxDialog(
            IDialogService dialogService,
            IMusicService musicService,
            IServiceProvider serviceProvider) 
            : base(dialogService, serviceProvider)
        {
            _musicService = musicService;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .AddPage(MainPageId, (page) =>
                {
                    page.Header = "Please select a song.";

                    foreach (var song in _musicService.GetAllSongs())
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
