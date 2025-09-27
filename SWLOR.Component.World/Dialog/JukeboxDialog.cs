using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Component.World.Dialog
{
    public class JukeboxDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IMusicService MusicService => _serviceProvider.GetRequiredService<IMusicService>();

        public JukeboxDialog(
            IDialogService dialogService,
            IDialogBuilder dialogBuilder,
            IServiceProvider serviceProvider) 
            : base(dialogService, dialogBuilder)
        {
            _serviceProvider = serviceProvider;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .AddPage(MainPageId, (page) =>
                {
                    page.Header = "Please select a song.";

                    foreach (var song in MusicService.GetAllSongs())
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
