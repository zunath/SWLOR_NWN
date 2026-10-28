using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A Rift Run chamber: its waves for one ship, and the ship each extra party ship adds to every wave.</summary>
    public class RiftRoom
    {
        public string Name { get; set; }
        public IReadOnlyList<IReadOnlyList<string>> Waves { get; set; } = Array.Empty<IReadOnlyList<string>>();
        public IReadOnlyList<string> Extras { get; set; } = Array.Empty<string>();
    }
}
