using SWLOR.Game.Server.Domain.Administration;
using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// DM-hosted events: DMs schedule, start and end them; players see them with /events, join them when
    /// the host allows it, and go back to where they joined from with /leaveevent.
    /// </summary>
    public interface IHostedEventsService
    {
        List<HostedEvent> GetEvents();

        HostedEvent GetEvent(string eventId);

        HostedEventReturnPoint GetReturnPoint(uint player);

        string SaveDetails(
            uint dm,
            string eventId,
            string name,
            string hostName,
            string description,
            string startText,
            DateTime? shownStartUtc,
            PlayerTimeZone zone,
            bool isJoinOpen,
            bool announceOnStart,
            out HostedEvent saved);

        string SetArrivalPoint(uint dm, string eventId);

        string ClearArrivalPoint(uint dm, string eventId);

        string GetArrivalAreaName(HostedEvent hostedEvent);

        string GoToArrivalPoint(uint dm, string eventId);

        string Start(uint dm, string eventId);

        string End(uint dm, string eventId);

        string SendEveryoneBack(uint dm, string eventId);

        (int Total, int Online) CountParticipants(HostedEvent hostedEvent);

        string Join(uint player, string eventId);

        string Leave(uint player, bool ignoreCombat = false);

        HostedEventTravelState GetTravelState(uint player);

        void RefreshWindows(uint player);

        /// <summary>
        /// The zone a player picked in Settings, or a DM in DM Settings, or UTC.
        /// </summary>
        PlayerTimeZone GetPlayerTimeZone(uint player);
    }
}
