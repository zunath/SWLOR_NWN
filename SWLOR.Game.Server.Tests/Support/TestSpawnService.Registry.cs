#nullable enable
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

// Part SP-2, SpawnRegistryService: the spawn list definition search that LoadSpawnTables runs.
public sealed partial class TestSpawnService
{
    protected override IEnumerable<Type> GetSpawnListDefinitionTypes() => Definitions;

    /// <summary>The instance's own search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetSpawnListDefinitionTypes();
}
