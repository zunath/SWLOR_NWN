#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// The perk levels the witness code (analysis, witness, learn) reads through the stay-part; the loadout double answers its own perk read from the same table.
public sealed partial class TestMimicry
{
    public Dictionary<PerkType, int> PerkLevels { get; } = new();

    protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);
}
