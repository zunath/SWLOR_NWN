#nullable enable
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The enmity table part, with the one read of its private tables the tests need.</summary>
public sealed class TestEnmity : EnmityService
{
    public void RemoveEnmityAndKeepProximity(uint creature, uint enemy)
    {
        var table = (Dictionary<uint, Dictionary<uint, int>>)typeof(EnmityService)
            .GetField("_enemyEnmityTables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this)!;
        table[enemy].Remove(creature);
    }
}
