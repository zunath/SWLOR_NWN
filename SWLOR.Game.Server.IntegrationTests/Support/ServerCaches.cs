using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The caches the server fills at the module-cache boot step. A converted service holds its cache on its instance, and
/// each test builds its own container, so a test whose game code reads them calls this from its <c>[SetUp]</c>.
/// </summary>
public static class ServerCaches
{
    /// <summary>Registers the skill definitions, as <c>Skill.CacheData</c> does at boot.</summary>
    public static void EnsureSkills() => Skill.CacheData();

    /// <summary>Registers the perk definitions, as <c>Perk.CacheData</c> does at boot (it needs the skill definitions too).</summary>
    public static void EnsurePerks()
    {
        EnsureSkills();
        Perk.CacheData();
    }
}
