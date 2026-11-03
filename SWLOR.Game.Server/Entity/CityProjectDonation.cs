namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A City Project donation in flight (Player City Revamp, "City Projects"): the journal of one donor's gift from the moment it is
    /// decided until the project holds it. The materials leave the donor's inventory in the character file and enter the project's
    /// record in the database, which cannot be saved together, so the record is written first, the character file is saved without the
    /// materials and with a mark naming this record, the project is saved with the donation and its id, and the record goes. A
    /// restart in between is settled at the donor's next login from the record, the mark and the project
    /// (<see cref="Service.CityProjectService.CityProjectDonationRules.Settle"/>). A new type, so no migration.
    /// </summary>
    public class CityProjectDonation : EntityBase
    {
        public CityProjectDonation()
        {
            PlayerId = string.Empty;
            ProjectId = string.Empty;
            Resref = string.Empty;
            DonorLabel = string.Empty;
        }

        /// <summary>The donor's player id.</summary>
        [Indexed]
        public string PlayerId { get; set; }

        [Indexed]
        public string ProjectId { get; set; }

        /// <summary>The material.</summary>
        public string Resref { get; set; }

        /// <summary>How many of it leave the donor's inventory.</summary>
        public int Count { get; set; }

        /// <summary>The name strangers know the donor by when they gave, for the Gazette.</summary>
        public string DonorLabel { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
