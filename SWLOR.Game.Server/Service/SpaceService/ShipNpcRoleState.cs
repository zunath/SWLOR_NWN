using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What an NPC ship's role behavior remembers between AI ticks.</summary>
    public class ShipNpcRoleState
    {
        /// <summary>When the role last used its periodic action (a tank retarget, a jam or paint, a web).</summary>
        public DateTime LastAction { get; set; } = DateTime.MinValue;

        /// <summary>The target a tank holds between retargets.</summary>
        public uint HeldTarget { get; set; } = OBJECT_INVALID;

        /// <summary>The distance to each enemy at the last decision, for an interceptor's flee check.</summary>
        public Dictionary<uint, float> Distances { get; } = new();

        public DateTime LastWave { get; set; } = DateTime.MinValue;

        public int WavesLaunched { get; set; }

        /// <summary>The fighters a carrier has launched that may still be flying.</summary>
        public List<uint> Fighters { get; } = new();

        public DateTime LastMine { get; set; } = DateTime.MinValue;
    }
}