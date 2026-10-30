using System.Runtime.Serialization;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Entity
{
    public class InventoryItem : EntityBase
    {
        [Indexed]
        public string StorageId { get; set; }
        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Tag { get; set; }
        [Indexed]
        public string Resref { get; set; }
        public int Quantity { get; set; }
        public string Data { get; set; }
        public string IconResref { get; set; }
        /// <summary>
        /// UTC ticks of the deposit, used to page items oldest first. Items deposited before this was recorded
        /// read and index as 0, so they stay ahead of newer deposits.
        /// </summary>
        [Indexed]
        public long DepositedTicks { get; set; }

        /// <summary>Shows the renamed default name of a former Display Plinth restored after the release migration.</summary>
        [OnDeserialized]
        internal void RenameFormerFurniture(StreamingContext context) =>
            Name = FurnitureRenameMigration.GetRenamedItemText(Resref, Name);
    }
}
