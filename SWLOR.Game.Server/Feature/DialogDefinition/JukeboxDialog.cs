using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class JukeboxDialog: ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";


        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, (page) =>
                {
                    page.Header = "Please select a song.";

                    foreach (var song in Music.GetAllSongs())
                    {
                        page.AddResponse(song.DisplayName, () =>
                        {
                            var area = GetArea(Player);
                            FloatingTextStringOnCreature($"Song Selected: {song.DisplayName}", Player, false);

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
