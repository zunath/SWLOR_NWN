using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Tells citizens about their city's leadership at login (Player City Revamp, "Acting mayor and stepping down" and
    /// "Planetary councils"): a step-down waiting for their answer, the Acting Mayor term they hold, and a planetary council
    /// a DM called that they have not read. City processing runs only at server start, so nothing in game announces these
    /// otherwise.
    /// </summary>
    public static class CityLeadershipNotices
    {
        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float DeliveryDelaySeconds = 6f;

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

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                return;

            var city = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);
            if (city == null || city.IsQueuedForDeletion)
                return;

            var now = DateTime.UtcNow;

            if (StepDownRules.IsWaitingForAnswerFrom(city.StepDown, playerId, now))
            {
                SendMessageToPC(player, ColorToken.Green(CityLeadershipNoticeText.BuildStepDownNotice(
                    city.CustomName,
                    ElectionRules.GetDaysLeft(now, city.StepDown.AcceptDeadlineUtc))));
            }

            if (city.ActingMayorPlayerId == playerId &&
                ActingMayorRules.IsActive(city.ActingMayorPlayerId, city.ActingMayorEndsUtc, now))
            {
                SendMessageToPC(player, ColorToken.Green(CityLeadershipNoticeText.BuildActingMayorNotice(
                    city.CustomName,
                    city.ActingMayorEndsUtc.Value,
                    now)));
            }

            // A planetary council called for the planet, once, for the mayor and officers it was meant for.
            if (PlanetaryCouncilRules.IsUnread(city.CouncilSummons, dbPlayer.SeenCouncilSummonsId, now) &&
                PlanetaryCouncilRules.IsRecipient(city, playerId, now))
            {
                SendMessageToPC(player, ColorToken.Green(city.CouncilSummons.Text));
                dbPlayer.SeenCouncilSummonsId = city.CouncilSummons.Id;
                DB.Set(dbPlayer);
            }
        }
    }
}
