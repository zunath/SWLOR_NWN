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

        /// <summary>
        /// The player whose spent Rift Key opened the run (OBJECT_INVALID for a run opened without one). Kept apart from
        /// <see cref="Pilots"/>, since a key user who leaves the controls is no longer a pilot but their key still counts.
        /// </summary>
        public uint KeyUser { get; set; } = OBJECT_INVALID;

        /// <summary>
        /// The character id of <see cref="KeyUser"/>, or empty for a run opened without a key. The spend's state is kept by it,
        /// so a key user who disconnects (an invalid handle) or rejoins (a new handle) never turns an unconfirmed spend into
        /// a confirmed one.
        /// </summary>
        public string KeyUserId { get; set; } = string.Empty;

        /// <summary>A save of the key user's character file holds the spent key as spent: the run pays its spoils.</summary>
        public bool KeySpendSaved { get; set; }

        /// <summary>The spent key goes back to the key user instead: the run pays no spoils.</summary>
        public bool KeyRefunded { get; set; }

        public List<uint> Pilots { get; set; } = new();
        public Dictionary<uint, Location> Origins { get; set; } = new();

        /// <summary>The ship each pilot flies in, so a pilot who logs out can rejoin with the same ship (W11k).</summary>
        public Dictionary<uint, string> ShipIds { get; set; } = new();

        /// <summary>
        /// The ships whose pilots logged out while the run was open, by ship ID, with the location the run returns them
        /// to. Their pilot rejoins at the current chamber on taking the controls again while the run is open.
        /// </summary>
        public Dictionary<string, Location> AwayShips { get; set; } = new();

        /// <summary>The player ID of the pilot flying each ship in the run, by ship ID, so an away ship's losses find them.</summary>
        public Dictionary<string, string> PilotIds { get; set; } = new();

        /// <summary>
        /// The run collapsed or was abandoned unfinished. Ships whose pilots logged out then take the same losses as
        /// ships destroyed in the collapse, so logging out never escapes a failing run.
        /// </summary>
        public bool Failed { get; set; }
        public List<uint> Enemies { get; set; } = new();
    }
}
