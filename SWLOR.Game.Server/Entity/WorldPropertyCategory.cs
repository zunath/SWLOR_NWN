using System.Collections.Generic;
using System.Runtime.Serialization;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Entity
{
    public class WorldPropertyCategory: EntityBase
    {
        public WorldPropertyCategory()
        {
            Items = new Dictionary<string, WorldPropertyItem>();
        }

        [Indexed]
        public string ParentPropertyId { get; set; }

        [Indexed]
        public string Name { get; set; }

        public Dictionary<string, WorldPropertyItem> Items { get; set; }

    }

    public class WorldPropertyItem
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Resref { get; set; }
        public string IconResref { get; set; }
        public int Quantity { get; set; }

        public string Data { get; set; }

        /// <summary>Shows the renamed default name of a former Display Plinth restored after the release migration.</summary>
        [OnDeserialized]
        internal void RenameFormerFurniture(StreamingContext context) =>
            Name = FurnitureRenameMigration.GetRenamedItemText(Resref, Name);
    }
}
