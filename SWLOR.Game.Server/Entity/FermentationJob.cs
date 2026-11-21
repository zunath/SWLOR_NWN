using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The batch in a Fermentation Vat (Player City Revamp, "Farming for other crafts"): the enzyme being made, who started it,
    /// and when it finishes. A vat holds one batch, from the moment it starts until its owner collects it, so the id is
    /// <see cref="KeyFor"/> of the vat. The batch runs on the clock, not on a timer, so it finishes at <see cref="CompleteUtc"/>
    /// whether the server stayed up or restarted in between. The inputs are kept so that a vat that goes away can send them back.
    /// A new type, so no migration.
    /// </summary>
    public class FermentationJob : EntityBase
    {
        public FermentationJob()
        {
            VatId = string.Empty;
            OwnerPlayerId = string.Empty;
            OwnerCdKey = string.Empty;
            CropResref = string.Empty;
            ManureResref = string.Empty;
            WaterResref = string.Empty;
            StarterData = string.Empty;
        }

        /// <summary>The record's key for a vat structure's property id.</summary>
        public static string KeyFor(string vatId) => "fermentation-job:" + vatId;

        /// <summary>The vat structure's property id.</summary>
        [Indexed]
        public string VatId { get; set; }

        /// <summary>The character who started the batch and collects it.</summary>
        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>The owner's public CD key. A CD key has one batch running at a time, across all its characters and vats.</summary>
        [Indexed]
        public string OwnerCdKey { get; set; }

        public EnzymeType Type { get; set; }

        public EnzymeColorType Color { get; set; }

        public DateTime StartedUtc { get; set; }

        /// <summary>When the batch finishes: <c>EnzymeCultureRules.Hours</c> after <see cref="StartedUtc"/>.</summary>
        public DateTime CompleteUtc { get; set; }

        /// <summary>The crop used, <c>EnzymeCultureRules.CropsPerBatch</c> of it.</summary>
        public string CropResref { get; set; }

        public string ManureResref { get; set; }

        public string WaterResref { get; set; }

        /// <summary>The hunted enzyme the batch used as its starter, as saved by the engine, or empty when the vat had the color already.</summary>
        public string StarterData { get; set; }
    }
}
