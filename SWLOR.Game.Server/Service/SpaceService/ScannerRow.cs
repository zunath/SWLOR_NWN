namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One signature as the Scanner tab of the Ship HUD lists it for a viewer.</summary>
    public class ScannerRow
    {
        public string SignatureId { get; init; }
        public string Name { get; init; }
        public string Detail { get; init; }

        /// <summary>The full detail: rough or exact location, threat and loot class.</summary>
        public string Tooltip { get; init; }
        public int Percent { get; init; }
        public bool CanJump { get; init; }

        /// <summary>Whether the row is a boardable derelict the party can reach: its Board and Salvage buttons work.</summary>
        public bool CanBoard { get; init; }
    }
}
