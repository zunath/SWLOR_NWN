using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The one-time bounty notice: cities can post bounties on an identity that shows the PvP flag, and nobody can be
    /// targeted before accepting. <see cref="BountyConsent"/> holds the acceptance.
    /// </summary>
    public class BountyNoticeDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialization()
        {
            if (!BountyConsent.ShouldPrompt(Player))
                Close();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            page.Header = BountyText.BuildNoticeText();

            page.AddResponse(ColorToken.Green("I accept"), () =>
            {
                BountyConsent.Accept(Player);
                Close();
            });

            page.AddResponse("Turn my PvP flag off", () =>
            {
                PlayerList.SetFlag(Player, PlayerListFlagType.PvP, false);
                Close();
            });

            page.AddResponse("Not now", Close);
        }
    }
}
