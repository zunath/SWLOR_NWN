using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Entity;

public sealed class MimicryLoadout
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<FeatType> Techniques { get; set; } = new();
    public Dictionary<int, FeatType> HotbarSlots { get; set; } = new();
}
