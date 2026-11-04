using System.Collections.Generic;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A value transfer in flight between a character's inventory and a database record (Player City Revamp, farming and the Civic
    /// Exchange): the journal of one action from the moment it is decided until both sides hold it. A character file saves about once
    /// a minute and the database at once, so a server stop between the two writes would keep the items in both places or in neither.
    /// The entry is written first, the character file is saved with a mark naming it, and the entry goes once the database side is
    /// done and receipted. A restart in between is settled at the character's next login from the entry, the mark and the database
    /// (<see cref="TransferJournalRules.Settle"/>). Follows the City Project donation journal (<see cref="CityProjectDonation"/>).
    /// A new type, so no migration.
    /// </summary>
    public class TransferEntry : EntityBase
    {
        public TransferEntry()
        {
            PlayerId = string.Empty;
            Kind = string.Empty;
            TargetId = string.Empty;
            Payload = string.Empty;
            Summary = string.Empty;
            Items = new List<TransferItem>();
        }

        /// <summary>The character whose inventory the items leave or enter.</summary>
        [Indexed]
        public string PlayerId { get; set; }

        /// <summary>Names the feature's handler, which applies the database side and tells whether it is applied.</summary>
        public string Kind { get; set; }

        public TransferDirection Direction { get; set; }

        /// <summary>The record that carries the receipt, for the logs.</summary>
        public string TargetId { get; set; }

        /// <summary>The items that leave the inventory (a deposit) or enter it (a withdrawal).</summary>
        public List<TransferItem> Items { get; set; }

        /// <summary>What the feature's handler needs to apply the database side again, as <see cref="TransferJournalRules.BuildPayload"/> writes it.</summary>
        public string Payload { get; set; }

        /// <summary>What the character was doing, for their message when a restart interrupted it: "loading feed into the farmhand's hopper".</summary>
        public string Summary { get; set; }

        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// When the database side was applied and this entry was told so, or null. It outlives the target's own receipt: a record that is
        /// deleted later takes its receipts with it, but an applied entry stays applied until the character's next login settles it.
        /// </summary>
        public DateTime? AppliedUtc { get; set; }
    }
}
