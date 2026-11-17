#nullable enable
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The spawn registry (part SP-2) with its definition search and its error log under the test's control:
/// <see cref="Definitions"/> are the spawn list definitions the module load finds, and <see cref="Errors"/> is the list the
/// registry and the spawn runtime of the same <see cref="TestSpawnService"/> report to.
/// </summary>
public sealed class TestSpawnRegistryService : SpawnRegistryService
{
    public TestSpawnRegistryService(List<string> errors)
    {
        Errors = errors;
    }

    public List<Type> Definitions { get; } = new();

    public List<string> Errors { get; }

    protected override IEnumerable<Type> GetSpawnListDefinitionTypes() => Definitions;

    protected override void WriteError(string message) => Errors.Add(message);

    /// <summary>The registry's own search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetSpawnListDefinitionTypes();

    /// <summary>The registry's own error sink, for the test that proves it writes to the error log.</summary>
    public void WriteToTheErrorLog(string message) => base.WriteError(message);
}
