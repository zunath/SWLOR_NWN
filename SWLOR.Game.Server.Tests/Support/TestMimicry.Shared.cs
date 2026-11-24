#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// The seams several parts call: the witness code (analysis, witness, learn) and the loadout code (the login restore) both read perk levels.
public sealed partial class TestMimicry
{
    public Dictionary<PerkType, int> PerkLevels { get; } = new();

    protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);
}
