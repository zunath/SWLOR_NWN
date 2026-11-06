#nullable enable
using System.Reflection;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The enmity tables of the installed container's <see cref="EnmityService"/>, which the AI context reads for its
/// current target through the <c>Enmity</c> facade. The AI instance tests keep their own model of the tables
/// (<see cref="TestAIService.Tables"/>) and copy it here before a context is built, so the context sees the same threat
/// the instance does. The tables are private to the instance, so they are reached by reflection.
/// </summary>
public static class RealEnmityTables
{
    /// <summary>Makes the static tables hold exactly <paramref name="tables"/>.</summary>
    public static void Replace(Dictionary<uint, Dictionary<uint, int>> tables)
    {
        Clear();
        var enemyTables = Field<Dictionary<uint, Dictionary<uint, int>>>("_enemyEnmityTables");
        var creatureToEnemies = Field<Dictionary<uint, List<uint>>>("_creatureToEnemies");
        foreach (var (enemy, table) in tables)
        {
            enemyTables[enemy] = new Dictionary<uint, int>(table);
            foreach (var creature in table.Keys)
            {
                if (!creatureToEnemies.TryGetValue(creature, out var enemies))
                    creatureToEnemies[creature] = enemies = new List<uint>();
                enemies.Add(enemy);
            }
        }
    }

    public static void Clear()
    {
        Field<Dictionary<uint, Dictionary<uint, int>>>("_enemyEnmityTables").Clear();
        Field<Dictionary<uint, List<uint>>>("_creatureToEnemies").Clear();
    }

    private static T Field<T>(string name) =>
        (T)typeof(EnmityService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(FacadeContainer.Current.Get<IEnmityService>())!;
}
