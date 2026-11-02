using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Tells every player about the tournaments that are taking sign-ups (Player City Revamp, "Conflict and PvP"). A
    /// player sees one notice per open tournament at each login while sign-ups are open, wherever their character lives,
    /// so visitors can come to compete or watch.
    /// </summary>
    public static class TournamentNotices
    {
        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float DeliveryDelaySeconds = 5f;

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            DelayCommand(DeliveryDelaySeconds, () => Deliver(player));
        }

        private static void Deliver(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var now = DateTime.UtcNow;
            foreach (var tournament in Tournaments.GetOpenForSignUp())
            {
                var city = DB.Get<WorldProperty>(tournament.CityId);
                if (city == null)
                    continue;

                var ring = Property.GetPlaceableByPropertyId(tournament.RingPropertyId);
                var areaName = GetIsObjectValid(ring) ? GetName(GetArea(ring)) : string.Empty;
                SendMessageToPC(player, ColorToken.Green(TournamentText.BuildSignUpNotice(
                    city.CustomName, areaName, tournament.Prize, tournament.EntryFee, tournament.StartsUtc, now)));
            }
        }
    }
}
