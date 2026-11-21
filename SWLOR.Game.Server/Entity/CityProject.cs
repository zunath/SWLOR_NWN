using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityProjectService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A City Project (Player City Revamp, "City Projects"): a facility upgrade, Home Ground, or on a station the Jump Beacon
    /// level, built by the city's credits and by materials that anyone, visitors included, donates at City Hall. The credits sit in
    /// the city's <see cref="WorldProperty.Escrow"/> under this record's id from the moment the project starts, with the part from
    /// each balance remembered. Once every material is in, the project completes 2 days later, applied at the next server start. The
    /// record is deleted when the project completes or is cancelled; the Gazette keeps the story. A new type, so no migration.
    /// </summary>
    public class CityProject : EntityBase
    {
        public CityProject()
        {
            CityId = string.Empty;
            StartedByPlayerId = string.Empty;
            RequiredMaterials = new Dictionary<string, int>();
            DonatedMaterials = new Dictionary<string, int>();
            DonorMaterials = new Dictionary<string, Dictionary<string, int>>();
            DonorLabels = new Dictionary<string, string>();
            AppliedDonationIds = new List<string>();
        }

        /// <summary>The property id of the city or station that is building it.</summary>
        [Indexed]
        public string CityId { get; set; }

        [Indexed]
        public CityProjectType ProjectType { get; set; }

        [Indexed]
        public CityProjectState State { get; set; }

        /// <summary>The facility level the project builds, or 0 for Home Ground.</summary>
        public int TargetLevel { get; set; }

        /// <summary>The credits held in the city's escrow for it. Paid out when it completes, returned when it is cancelled.</summary>
        public long CreditCost { get; set; }

        /// <summary>Item resref to the number the project needs.</summary>
        public Dictionary<string, int> RequiredMaterials { get; set; }

        /// <summary>Item resref to the number donated so far. Never more than the project needs.</summary>
        public Dictionary<string, int> DonatedMaterials { get; set; }

        /// <summary>Donor's player id, then item resref to the number that donor gave. Cancelling returns each donor's share.</summary>
        public Dictionary<string, Dictionary<string, int>> DonorMaterials { get; set; }

        /// <summary>Donor's player id to the name strangers know them by, as it stood when they gave, for the Gazette.</summary>
        public Dictionary<string, string> DonorLabels { get; set; }

        /// <summary>
        /// The ids of the <see cref="CityProjectDonation"/> records this project has taken. Saved with the donation itself, so it is the
        /// receipt that tells a restarted login the donation already counts. Never more than one entry for an id.
        /// </summary>
        public List<string> AppliedDonationIds { get; set; }

        public string StartedByPlayerId { get; set; }

        public DateTime StartedUtc { get; set; }

        /// <summary>When the last material arrived, or null while some are still needed.</summary>
        public DateTime? SuppliedUtc { get; set; }

        /// <summary>When the project completes: two days after it was fully supplied. Null while it is not supplied.</summary>
        public DateTime? CompletesUtc { get; set; }
    }
}
