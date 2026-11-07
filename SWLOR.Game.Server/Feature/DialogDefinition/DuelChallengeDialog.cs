using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DuelService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The target's side of a duel challenge: who challenged them, the rules, then Accept or Decline.
    /// <see cref="Duels"/> holds the challenge and checks everything again on Accept.
    /// </summary>
    public class DuelChallengeDialog : ConversationMenuDefinition
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
            if (Duels.GetChallenge(Player) == null)
                Close();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var challenge = Duels.GetChallenge(Player);
            if (challenge == null || !GetIsObjectValid(challenge.Challenger))
            {
                page.Header = "The challenge has expired.";
                return;
            }

            page.Header = $"{PlayerName.GetDisplayName(Player, challenge.Challenger)} challenges you to a duel at the Dueling Ring.\n\n" +
                          "While it lasts, any blow that would kill either of you subdues you instead, whoever lands it. " +
                          "Leaving the ring or logging out forfeits. " +
                          "If someone else joins the fight, the duel is void, and you both stay protected for " +
                          $"{DuelRules.ProtectionSeconds:0} seconds.";

            page.AddResponse(ColorToken.Green("Accept the duel"), () =>
            {
                Duels.Accept(Player);
                Close();
            });

            page.AddResponse("Decline", () =>
            {
                Duels.Decline(Player);
                Close();
            });
        }
    }
}
