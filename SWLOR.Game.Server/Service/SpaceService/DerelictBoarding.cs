using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A derelict being worked (plan section 28): a party boarded it on foot, or a ship's droids strip it. Held in memory,
    /// keyed by the derelict's signature; a restart ends it (the boot returns every ship to its dock).
    /// </summary>
    public class DerelictBoarding
    {
        public string SignatureId { get; init; }
        public DerelictMode Mode { get; init; }

        /// <summary>The content tier (<see cref="DerelictRules.TierIndex"/>): 1-6 the space tiers, 7 Wild Space.</summary>
        public int TierIndex { get; init; }

        public bool WildSpace { get; init; }

        /// <summary>The space tier of its area (1-6), for the waves' squads and the site's loot table.</summary>
        public int SpaceTier { get; init; }

        public bool Contested { get; init; }

        /// <summary>Where the derelict lies: the space area and position ships undock beside and waves arrive around.</summary>
        public string AreaResref { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }

        /// <summary>The player IDs of the party working it (the first pilot's party).</summary>
        public HashSet<string> PartyIds { get; init; } = new();

        public DateTime StartedAt { get; set; }

        /// <summary>
        /// The player IDs of boarders who fought aboard: on the enmity table of an enemy that died here, or its killer.
        /// Only they (and whoever opens the strongbox) are paid from it.
        /// </summary>
        public HashSet<string> Participants { get; init; } = new();

        /// <summary>The strongbox was opened or the salvage team finished: nothing is left to take.</summary>
        public bool Looted { get; set; }

        // Boarding

        public string TemplateResref { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;

        /// <summary>The instanced interior, once made.</summary>
        public uint Interior { get; set; }

        public uint Strongbox { get; set; }

        /// <summary>The ground enemies aboard, reinforcements included; the strongbox opens once none is left.</summary>
        public List<uint> Enemies { get; } = new();

        /// <summary>The ships docked at the derelict, in docking order.</summary>
        public List<string> DockedShipIds { get; } = new();

        /// <summary>The players who came aboard with each docked ship, its pilot first.</summary>
        public Dictionary<string, List<string>> CrewByShipId { get; } = new();

        // Contested waves

        public int WavesSpawned { get; set; }
        public int WavesCleared { get; set; }

        /// <summary>A wave is out: its ships still fly.</summary>
        public bool WaveOut { get; set; }

        public DateTime NextWaveAt { get; set; }
        public List<uint> WaveShips { get; } = new();
        public int Reinforcements { get; set; }
        public DateTime LastReinforcementAt { get; set; }

        // Salvage team

        public string SalvageShipId { get; init; } = string.Empty;
        public string SalvagePilotId { get; init; } = string.Empty;
        public int SalvageIndustry { get; init; }
        public int SalvageSecondsNeeded { get; init; }

        /// <summary>The seconds the team has worked; a wave out pauses it.</summary>
        public double SalvageProgress { get; set; }
    }
}
