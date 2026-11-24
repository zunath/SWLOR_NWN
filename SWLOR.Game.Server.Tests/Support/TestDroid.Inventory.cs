#nullable enable
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Tests.Support;

// Part DR-3, DroidInventoryService: the read of the generated ammunition flags, which the item snapshot and the rejected-item return call.
public sealed partial class TestDroid
{
    public (bool Arrows, bool Bullets, bool Bolts)? Flags { get; set; }

    protected override (bool Arrows, bool Bullets, bool Bolts)? ReadGeneratedAmmunitionFlags(uint droid) => Flags;
}
