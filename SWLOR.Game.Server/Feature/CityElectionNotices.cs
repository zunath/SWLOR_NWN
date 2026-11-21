using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Tells citizens about their city's election at login (Player City Revamp, "Election changes"). City processing
    /// runs only at server start, so nothing in game announces an election otherwise. A citizen sees one notice at each
    /// login while registration or voting is open, and the result once after it posts.
    /// </summary>
    public static class CityElectionNotices
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

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                return;

            var city = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);
            if (city == null)
                return;

            var now = DateTime.UtcNow;

            // The result of the last election, once.
            var result = city.LastElectionResult;
            if (ElectionNoticeText.IsResultUnread(result, dbPlayer.SeenElectionResultId, dbPlayer.CitizenSinceUtc, now))
            {
                var mayor = DB.Get<Player>(result.WinnerPlayerId);
                var mayorName = PlayerName.GetDisplayNameByPlayerId(player, result.WinnerPlayerId, mayor?.Name);
                var resultNotice = ElectionNoticeText.BuildResultNotice(result, mayorName);
                if (!string.IsNullOrEmpty(resultNotice))
                    SendMessageToPC(player, ColorToken.Green(resultNotice));

                dbPlayer.SeenElectionResultId = result.Id;
                DB.Set(dbPlayer);
            }

            // The open election's stage and the days left, at every login.
            var election = ElectionBallot.GetCityElection(city.Id);
            if (election != null)
            {
                var stageNotice = ElectionNoticeText.BuildStageNotice(election, now, ElectionBallot.IsEligibleVoter(dbPlayer, election));
                if (!string.IsNullOrEmpty(stageNotice))
                    SendMessageToPC(player, ColorToken.Green(stageNotice));
            }
        }
    }
}
