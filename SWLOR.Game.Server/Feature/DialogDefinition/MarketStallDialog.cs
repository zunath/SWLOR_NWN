using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A market stall vendor: browse the stall's stock, and for the mayor (or anyone who may edit the city's structures) pick
    /// which of the three stock themes the stall sells. <see cref="MarketStalls"/> keeps the stall and its vendor,
    /// <see cref="CityStores"/> opens the store.
    /// </summary>
    public class MarketStallDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";
        private const string ThemePageId = "THEME_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ThemePageId, ThemePageInit);

            return builder.Build();
        }

        private WorldProperty GetStall()
        {
            var stallId = MarketStalls.GetStallId(Owner);
            return string.IsNullOrWhiteSpace(stallId) ? null : DB.Get<WorldProperty>(stallId);
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var stall = GetStall();
            if (stall == null)
            {
                page.Header = "This stall is closed.";
                return;
            }

            var city = DB.Get<WorldProperty>(stall.ParentPropertyId);
            page.Header = $"{stall.CustomName}\n\nSells: {MarketStallRules.DescribeTheme(stall.MarketStallTheme)}";

            page.AddResponse("Browse the wares", () =>
            {
                Close();
                CityStores.OpenNearest(Player, Owner);
            });

            if (MarketStalls.CanPickTheme(Player, city))
                page.AddResponse("Change what this stall sells", () => GoToPage(ThemePageId));
        }

        private void ThemePageInit(ConversationMenuPage page)
        {
            var stall = GetStall();
            if (stall == null)
            {
                page.Header = "This stall is closed.";
                return;
            }

            var current = MarketStallRules.NormalizeTheme(stall.MarketStallTheme);
            page.Header = $"{stall.CustomName}\n\nWhat should this stall sell? Its vendor changes at once. Stock already sold this week stays sold if you change back.";

            foreach (var theme in new[] { MarketStallTheme.GeneralGoods, MarketStallTheme.CraftingSupplies, MarketStallTheme.Decor })
            {
                var chosen = theme;
                var label = MarketStallRules.DescribeTheme(chosen);
                page.AddResponse(chosen == current ? $"{label} (current)" : label, () =>
                {
                    if (chosen == current)
                    {
                        GoToPage(MainPageId, false);
                        return;
                    }

                    Close();
                    var failure = MarketStalls.ChangeTheme(Player, stall.Id, chosen);
                    if (!string.IsNullOrEmpty(failure))
                        SendMessageToPC(Player, failure);
                });
            }

            page.AddResponse("Back", () => GoToPage(MainPageId, false));
        }
    }
}
