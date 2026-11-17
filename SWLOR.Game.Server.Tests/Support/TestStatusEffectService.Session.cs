using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part 7, StatusEffectSessionService: the ability details the toggles are found in.
public sealed partial class TestStatusEffectService
{
    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _bench.AbilityDetails;
}
