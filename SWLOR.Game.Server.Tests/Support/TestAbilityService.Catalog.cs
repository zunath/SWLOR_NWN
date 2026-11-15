using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The answers of <see cref="TestAbilityService"/> for the catalog of definitions (part 1). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    /// <summary>Puts one ability in the catalog, without building the real definitions.</summary>
    public void Register(FeatType feat, AbilityDetail ability) =>
        ((Dictionary<FeatType, AbilityDetail>)GetAllAbilityDetails())[feat] = ability;

    /// <summary>The default of the definition search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetAbilityListDefinitionTypes();

    protected override IEnumerable<Type> GetAbilityListDefinitionTypes() => _bench.DefinitionTypes ?? Array.Empty<Type>();

    protected override void CacheAbilityTargeting(IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
    {
        _bench.CachedTargeting = abilities;
        Log("CacheAbilityTargeting");
    }
}
