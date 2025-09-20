using System.Collections.Generic;
using System.Numerics;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class ModuleCache: EntityBase
    {
        public ModuleCache()
        {
            Id = "SWLOR_CACHE";
            WalkmeshesByArea = new Dictionary<string, List<Vector3>>();
        }

        public int LastModuleMTime { get; set; }
        public Dictionary<string, List<Vector3>> WalkmeshesByArea { get; set; }
        public Dictionary<string, string> ItemNamesByResref { get; set; }
    }
}
