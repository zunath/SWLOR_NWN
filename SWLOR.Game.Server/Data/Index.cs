using System;
using System.Collections.Generic;
using System.Text;

namespace SWLOR.Game.Server.Data
{
    public class Index
    {
        public List<object> IDs { get; set; }
        public Dictionary<string, object> SecondaryIndexes { get; set; }

        public Index()
        {
            IDs = new List<object>();
            SecondaryIndexes = new Dictionary<string, object>();
        }
    }
}
