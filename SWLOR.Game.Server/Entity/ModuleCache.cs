using System.Collections.Generic;
using System.Numerics;

namespace SWLOR.Game.Server.Entity
{
    public class ModuleCache: EntityBase
    {
        public ModuleCache()
        {
            WalkmeshesByArea = new Dictionary<string, List<Vector3>>();
        }

        public int LastModuleMTime { get; set; }
        public Dictionary<string, List<Vector3>> WalkmeshesByArea { get; set; }
        public Dictionary<string, string> ItemNamesByResref { get; set; }
    }
}
