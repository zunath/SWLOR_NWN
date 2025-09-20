using System.Numerics;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Core.Entity
{
    public class ModuleCache: EntityBase
    {
        public const string DefaultId = "SWLOR_CACHE";

        public ModuleCache()
        {
            Id = DefaultId;
            WalkmeshesByArea = new Dictionary<string, List<Vector3>>();
        }

        public int LastModuleMTime { get; set; }
        public Dictionary<string, List<Vector3>> WalkmeshesByArea { get; set; }
        public Dictionary<string, string> ItemNamesByResref { get; set; }
    }
}
