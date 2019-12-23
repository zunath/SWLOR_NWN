using System.Collections.Generic;

namespace SWLOR.Game.Server.Data
{
    public class Index
    {
        public List<object> IDs { get; set; }
        public Dictionary<string, object> SecondaryIndexes { get; set; }
        public Dictionary<string, List<object>> SecondaryListIndexes { get; set; }

        public Index()
        {
            IDs = new List<object>();
            SecondaryIndexes = new Dictionary<string, object>();
            SecondaryListIndexes = new Dictionary<string, List<object>>();
        }
    }
}
