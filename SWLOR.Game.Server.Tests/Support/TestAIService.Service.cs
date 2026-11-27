#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-1, AIService (stays): the profile search and the leash's Enmity calls.
public sealed partial class TestAIService
{
    protected override IEnumerable<Type> GetProfileDefinitionTypes() => Definitions;

    protected override Dictionary<uint, int> GetEnmityTable(uint enemy) =>
        Tables.TryGetValue(enemy, out var table) ? new Dictionary<uint, int>(table) : new Dictionary<uint, int>();

    /// <summary>Runs while an enemys enmity table is cleared, as the status effects removed by a leash reset do.</summary>
    public Action<uint>? WhenEnmityIsCleared { get; set; }

    protected override void ClearEnmityTable(uint enemy)
    {
        _bench.Engine.Note($"ClearEnmityTable({enemy})");
        Tables.Remove(enemy);
        Proximity.Remove(enemy);
        WhenEnmityIsCleared?.Invoke(enemy);
    }

    protected override void RemoveEnmity(uint creature, uint enemy)
    {
        _bench.Engine.Note($"RemoveEnmity({creature}, {enemy})");
        if (Tables.TryGetValue(enemy, out var table))
            table.Remove(creature);
        if (Proximity.TryGetValue(enemy, out var set))
            set.Remove(creature);
    }

    /// <summary>The instance's own search for profile definitions, for the test that proves it finds the server's.</summary>
    public IEnumerable<Type> FindDefinitionsWithTheOriginalSearch() => base.GetProfileDefinitionTypes();
}
