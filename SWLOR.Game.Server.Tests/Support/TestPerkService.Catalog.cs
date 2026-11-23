using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seams the catalog code calls: the definition search, the droid test and the droid perk level.</summary>
public sealed partial class TestPerkService
{
    protected override IEnumerable<Type> GetPerkListDefinitionTypes() => new[] { typeof(BenchPerkList) };

    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);

    protected override int GetDroidPerkLevel(uint droid, PerkType perkType) => _bench.DroidPerks.GetValueOrDefault(perkType);

    /// <summary>The seams' default bodies, for the test that proves each one is the original call.</summary>
    public IEnumerable<Type> DefaultPerkListDefinitionTypes() => base.GetPerkListDefinitionTypes();

    public bool DefaultIsDroid(uint creature) => base.IsDroid(creature);

    public int DefaultDroidPerkLevel(uint droid, PerkType perkType) => base.GetDroidPerkLevel(droid, perkType);
}
