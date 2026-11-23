using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seam the granted feat code calls: the ability details by feat.</summary>
public sealed partial class TestPerkService
{
    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _bench.Abilities;

    public IReadOnlyDictionary<FeatType, AbilityDetail> DefaultAllAbilityDetails() => base.GetAllAbilityDetails();
}
