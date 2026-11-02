using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TravelerService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The conversation of a traveler: browse the wares, hear who they are, and for the information broker hear a rumor,
    /// hear where the other travelers are, and open the contract board. <see cref="Travelers"/> makes the traveler and
    /// <see cref="CityStores"/> opens the store, with the host city's Sales tax.
    /// </summary>
    public class TravelerDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";
        private const string StoryPageId = "STORY_PAGE";
        private const string NewsPageId = "NEWS_PAGE";

        private string _news = string.Empty;

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit)
                .AddPage(StoryPageId, StoryPageInit)
                .AddPage(NewsPageId, NewsPageInit);

            return builder.Build();
        }

        private TravelerDetail GetTraveler()
        {
            return TravelerCatalog.Get(Travelers.GetTravelerType(Owner));
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var traveler = GetTraveler();
            if (traveler == null)
            {
                page.Header = "This traveler has nothing to say.";
                return;
            }

            page.Header = $"{traveler.Name}\n{traveler.Title}\n\n{traveler.Greeting}";

            page.AddResponse("Browse the wares", () =>
            {
                Close();
                Travelers.OpenStore(Player, Owner);
            });

            if (traveler.Type == TravelerType.InformationBroker)
            {
                page.AddResponse("Ask for a rumor", () =>
                {
                    _news = traveler.Rumors.Count == 0
                        ? "Nothing worth repeating today."
                        : traveler.Rumors[Random.Next(traveler.Rumors.Count)];
                    GoToPage(NewsPageId);
                });

                page.AddResponse("Ask where the other travelers are", () =>
                {
                    _news = TravelerText.BuildOthersNews(traveler.Type, Travelers.GetLocations());
                    GoToPage(NewsPageId);
                });

                page.AddResponse("Look at the contract board", () =>
                {
                    Close();
                    Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractBoard, null, Owner);
                });
            }

            page.AddResponse("Who are you?", () => GoToPage(StoryPageId));
        }

        private void StoryPageInit(ConversationMenuPage page)
        {
            var traveler = GetTraveler();
            page.Header = traveler == null ? "This traveler has nothing to say." : $"{traveler.Name}\n\n{traveler.Story}";
            page.AddResponse("Back", () => GoToPage(MainPageId, false));
        }

        private void NewsPageInit(ConversationMenuPage page)
        {
            page.Header = _news;
            page.AddResponse("Back", () => GoToPage(MainPageId, false));
        }
    }
}
