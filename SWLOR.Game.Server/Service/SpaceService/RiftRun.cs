using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A Rift Run in progress (plan section 31). In memory only: a restart empties every pocket.</summary>
    public class RiftRun
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RiftKeyDetail Key { get; set; }

        /// <summary>The ships that entered; every chamber's waves scale with it.</summary>
        public int Ships { get; set; } = 1;

        public DateTime StartedAt { get; set; }
        public int Room { get; set; }
        public int Wave { get; set; }
        public RiftRunPhase Phase { get; set; } = RiftRunPhase.Waiting;
        public DateTime NextAt { get; set; }

        /// <summary>Waves in a row none of whose ships could be created; the run ends after a few.</summary>
        public int FailedSpawns { get; set; }
        public HashSet<int> Warned { get; set; } = new();

        /// <summary>Seconds left on the timer when the last chamber was cleared.</summary>
        public int FinishedSecondsLeft { get; set; }

        public uint Area { get; set; }
        public List<uint> Pilots { get; set; } = new();
        public Dictionary<uint, Location> Origins { get; set; } = new();

        /// <summary>The ship each pilot flies in, so a pilot who logs out can rejoin with the same ship (W11k).</summary>
        public Dictionary<uint, string> ShipIds { get; set; } = new();

        /// <summary>
        /// The ships whose pilots logged out while the run was open, by ship ID, with the location the run returns them
        /// to. Their pilot rejoins at the current chamber on taking the controls again while the run is open.
        /// </summary>
        public Dictionary<string, Location> AwayShips { get; set; } = new();
        public List<uint> Enemies { get; set; } = new();
    }
}
