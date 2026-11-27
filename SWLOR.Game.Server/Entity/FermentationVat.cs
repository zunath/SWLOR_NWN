using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// What one Fermentation Vat structure remembers (Player City Revamp, "Farming for other crafts"): the enzyme colors it has
    /// copied. The first batch of each type and color uses up one hunted enzyme of that kind as its starter and the vat keeps
    /// the kind from then on. The record goes with the vat when it is picked up or lost with its house or city. The id is
    /// <see cref="KeyFor"/> of the vat's structure id, never the structure id itself, since the DB's per-frame entity cache is
    /// keyed by id alone. A new type, so no migration.
    /// </summary>
    public class FermentationVat : EntityBase
    {
        public FermentationVat()
        {
            VatId = string.Empty;
            Starters = new List<string>();
        }

        /// <summary>The record's key for a vat structure's property id.</summary>
        public static string KeyFor(string vatId) => "fermentation-vat:" + vatId;

        /// <summary>The vat structure's property id.</summary>
        [Indexed]
        public string VatId { get; set; }

        /// <summary>The enzymes the vat has copied, by item resref (<c>hydrolase_blue</c>).</summary>
        public List<string> Starters { get; set; }
    }
}
