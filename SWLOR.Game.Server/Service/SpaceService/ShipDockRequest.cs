namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What <see cref="ShipDockingRules.GetDockRefusal"/> checks when a pilot docks with a host ship.</summary>
    public sealed class ShipDockRequest
    {
        public bool SameShip { get; init; }
        public ShipSize HostSize { get; init; }
        public ShipSize VisitorSize { get; init; }

        /// <summary>The pilot holds the host's Dock permission (owners always do).</summary>
        public bool HasDockPermission { get; init; }

        /// <summary>The distance between the ships in meters; <see cref="float.MaxValue"/> when they are in different areas.</summary>
        public float Distance { get; init; }

        public bool HostInCombat { get; init; }
        public bool VisitorInCombat { get; init; }

        /// <summary>How many ships are docked aboard the host already.</summary>
        public int HostDockedCount { get; init; }
    }
}
