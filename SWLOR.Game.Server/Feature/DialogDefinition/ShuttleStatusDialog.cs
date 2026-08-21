using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ShuttleStatusDialog: ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        /// <summary>
        /// Renders the shuttle's destination and remaining arrival time from the console.
        /// </summary>
        private void MainPageInit(ConversationMenuPage page)
        {
            var console = Owner;

            if (!Shuttle.TryGetFlightInfo(console, out var destination, out var arrivalUtc))
            {
                page.Header = "This console is offline.";
                return;
            }

            var destinationName = Planet.GetPlanetByType(destination).Name;
            var remaining = arrivalUtc - DateTime.UtcNow;
            var countdown = remaining <= TimeSpan.Zero
                ? "momentarily"
                : Time.GetTimeShortIntervals(remaining, false);

            page.Header = BuildStatusHeader(destinationName, countdown);

            page.AddResponse("Refresh", () =>
            {
                GoToPage(MainPageId, false);
            });
        }

        private static string BuildStatusHeader(string destinationName, string countdown) =>
            ColorToken.Cyan($"Destination: {destinationName}\nArriving in: {countdown}");
    }
}
