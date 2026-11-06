using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Loads the ability definitions the way the server does at boot. <c>Ability.CacheData</c> also fills the targeting cache,
/// which lives in a container instance, so the load runs inside a container of its own; the definitions stay cached in
/// <c>Ability</c> after the container is gone.
/// </summary>
public static class AbilityCache
{
    public static void Load()
    {
        using var facades = new FacadeContainer();
        using var scope = facades.Install();
        Ability.CacheData();
    }
}
