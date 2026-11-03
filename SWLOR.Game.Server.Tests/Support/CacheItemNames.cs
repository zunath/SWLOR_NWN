using System.Reflection;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Fills the item name cache of a fixture's <see cref="FacadeContainer"/> without the engine, for the tests of quest and
/// guild definitions that resolve a reward's name through <c>Cache.GetItemNameByResref</c>. In the server the cache is
/// filled at boot by spawning every item blueprint; a unit test has no module, so it reads the names from the blueprints.
/// The cache belongs to the container's <see cref="CacheService"/>, so the names go away with the container.
/// </summary>
public static class CacheItemNames
{
    private static readonly PropertyInfo NamesProperty = typeof(CacheService)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Makes <paramref name="names"/> (blueprint resref to item name) the container's cached item names.</summary>
    public static void Seed(FacadeContainer facades, Dictionary<string, string> names)
    {
        NamesProperty.SetValue(facades.Get<ICacheService>(), names);
    }
}
