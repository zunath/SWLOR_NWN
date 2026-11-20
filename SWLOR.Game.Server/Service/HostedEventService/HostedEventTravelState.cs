namespace SWLOR.Game.Server.Service.HostedEventService
{
    /// <summary>
    /// What blocks a player from traveling to or from an event, read from the engine by the caller.
    /// </summary>
    public readonly struct HostedEventTravelState
    {
        public bool IsDM { get; init; }
        public bool IsDead { get; init; }
        public bool IsInCombat { get; init; }
        public bool IsInSpace { get; init; }
        public bool IsInOutOfCharacterArea { get; init; }
        // The event the player is at, or empty when they are not at one.
        public string CurrentEventId { get; init; }
    }
}
