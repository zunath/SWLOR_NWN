#nullable enable
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The spawn instance with its clock, its definition search and its error log under the test's control:
/// <see cref="Now"/> is the time the instance sees, <see cref="Definitions"/> the spawn list definitions the module
/// load finds, and <see cref="Errors"/> what the instance reported to the error log.
/// </summary>
public sealed class TestSpawnService : SpawnService
{
    public TestSpawnService(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    public List<Type> Definitions { get; } = new();

    public List<string> Errors { get; } = new();

    protected override DateTime UtcNow() => Now;

    protected override IEnumerable<Type> GetSpawnListDefinitionTypes() => Definitions;

    protected override void WriteError(string message) => Errors.Add(message);

    /// <summary>The instance's own search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetSpawnListDefinitionTypes();

    /// <summary>The instance's own clock, for the test that proves it reads the system clock.</summary>
    public DateTime ReadSystemClock() => base.UtcNow();

    /// <summary>The instance's own error sink, for the test that proves it writes to the error log.</summary>
    public void WriteToTheErrorLog(string message) => base.WriteError(message);
}
