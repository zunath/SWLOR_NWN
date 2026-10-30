using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A station-only building's own data (plan section 25): its settings, the droid assigned to it and the Asteroid
    /// Drill's stored ore. The id is <see cref="KeyFor"/> of the building's structure property id, never the property id
    /// itself: the DB's per-frame entity cache is keyed by id alone, so sharing the building's id made a Get of this state
    /// in the frame that loaded the building return the WorldProperty and throw (W11r). A new type, so no migration.
    /// </summary>
    public class StationBuildingState : EntityBase
    {
        public StationBuildingState()
        {
            StationId = string.Empty;
            ShipyardAccess = ShipyardAccessType.Citizens;
            BeaconAccess = JumpBeaconAccessType.Citizens;
            BeaconAllyStationIds = new List<string>();
            DrillStorage = new Dictionary<string, int>();
            DroidOwnerPlayerId = string.Empty;
            DroidName = string.Empty;
            SerializedDroid = string.Empty;
        }

        /// <summary>The state's key for a building's structure property id.</summary>
        public static string KeyFor(string buildingId) => "station-building:" + buildingId;

        /// <summary>The building's structure property id.</summary>
        public string BuildingId { get; set; }

        /// <summary>The station (city) the building stands on.</summary>
        [Indexed]
        public string StationId { get; set; }

        [Indexed]
        public StructureType StructureType { get; set; }

        public ShipyardAccessType ShipyardAccess { get; set; }

        public JumpBeaconAccessType BeaconAccess { get; set; }

        /// <summary>The stations whose citizens count as allies at this beacon.</summary>
        public List<string> BeaconAllyStationIds { get; set; }

        /// <summary>The Asteroid Drill's stored ore: item resref to units.</summary>
        public Dictionary<string, int> DrillStorage { get; set; }

        /// <summary>When the drill's next unit of ore counts from.</summary>
        public DateTime DrillLastAccrual { get; set; }

        /// <summary>The assigned droid's controller item (ObjectPlugin.Serialize), or empty.</summary>
        public string SerializedDroid { get; set; }
        public string DroidOwnerPlayerId { get; set; }
        public string DroidName { get; set; }

        /// <summary>The assigned droid's Industry rating (plan section 27), read when it was assigned.</summary>
        public int DroidIndustry { get; set; }

        /// <summary>The assigned droid's Efficiency Protocol percent, read when it was assigned.</summary>
        public int DroidEfficiencyPercent { get; set; }

        /// <summary>
        /// The assignment of the droid, matching the mark on its controller while a restart could leave the controller in
        /// the owner's character file too. Older records load empty.
        /// </summary>
        public string DroidAssignmentId { get; set; }
    }
}
