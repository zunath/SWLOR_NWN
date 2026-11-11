using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Loads the ability definitions the way the server does at boot, into the ability service and the targeting cache of the
/// <see cref="FacadeContainer"/> installed for the current flow. The catalog lives as long as that container, so a fixture
/// builds one container, loads it once inside <c>Install()</c> and installs it again for each test.
/// </summary>
public static class AbilityCache
{
    public static void Load() => Ability.CacheData();
}
