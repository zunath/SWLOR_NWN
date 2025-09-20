using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Player
{
    /// <summary>
    /// Event fired when a player enters the server.
    /// </summary>
    public class PlayerEnteredEvent : BaseEvent
    {
        /// <summary>
        /// The player's character ID.
        /// </summary>
        public string PlayerId { get; }

        /// <summary>
        /// The player's character name.
        /// </summary>
        public string PlayerName { get; }

        /// <summary>
        /// The area the player entered.
        /// </summary>
        public string AreaName { get; }

        public PlayerEnteredEvent(string playerId, string playerName, string areaName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            AreaName = areaName;
        }
    }
}
