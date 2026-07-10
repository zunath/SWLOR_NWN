using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ShuttleStatusDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var console = GetDialogTarget();

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

            page.Header = ColorToken.Cyan("Destination: ") + destinationName + "\n" +
                          ColorToken.Cyan("Arriving in: ") + countdown;

            page.AddResponse("Refresh", () =>
            {
                ChangePage(MainPageId, false);
            });
        }
    }
}
