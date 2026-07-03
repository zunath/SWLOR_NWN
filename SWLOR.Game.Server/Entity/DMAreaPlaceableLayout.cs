using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class DMAreaPlaceableLayout: EntityBase
    {
        [Indexed]
        public string AreaResref { get; set; }

        [Indexed]
        public string Name { get; set; }

        public List<DMAreaPlaceableLayoutEntry> Entries { get; set; }

        public DMAreaPlaceableLayout()
        {
            AreaResref = string.Empty;
            Name = string.Empty;
            Entries = new List<DMAreaPlaceableLayoutEntry>();
        }
    }

    public class DMAreaPlaceableLayoutEntry
    {
        public string Tag { get; set; }
        public string Resref { get; set; }
        public int MatchIndex { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Facing { get; set; }

        public DMAreaPlaceableLayoutEntry()
        {
            Tag = string.Empty;
            Resref = string.Empty;
        }
    }
}
