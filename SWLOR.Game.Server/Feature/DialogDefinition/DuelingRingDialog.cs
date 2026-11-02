using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DuelService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A Dueling Ring's terminal: a player standing in the ring picks another player in it to challenge. The target
    /// answers in <see cref="DuelChallengeDialog"/>. <see cref="Duels"/> holds the challenge and the duel.
    /// </summary>
    public class DuelingRingDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var ring = Owner;

            page.Header = "Dueling Ring\n\n" +
                          "Challenge another player standing in the ring. If they accept, any blow that would kill either of you " +
                          "subdues you instead, whoever lands it. Leaving the ring or logging out forfeits. " +
                          "If someone else joins the fight, the duel is void, and you both stay protected for " +
                          $"{DuelRules.ProtectionSeconds:0} seconds.\n\n";

            if (Duels.GetDuel(player) != null)
            {
                page.Header += "You are in a duel.";
                return;
            }

            if (!Duels.IsInsideRing(ring, player))
            {
                page.Header += "Step inside the ring to challenge someone.";
                return;
            }

            var others = Duels.GetChallengeablePlayers(player, ring);
            if (others.Count == 0)
            {
                page.Header += "No one else is standing in the ring.";
                return;
            }

            page.Header += "Whom do you challenge?";
            foreach (var other in others)
            {
                var target = other;
                page.AddResponse($"Challenge {PlayerName.GetDisplayName(player, target)}", () =>
                {
                    if (Duels.Challenge(player, target, ring))
                        Close();
                });
            }
        }
    }
}
