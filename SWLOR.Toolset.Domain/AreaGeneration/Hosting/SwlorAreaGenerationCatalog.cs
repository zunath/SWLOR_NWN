using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>
/// SWLOR's generator content: reflects over the theme, tileset-profile and layout-profile definitions in this assembly
/// and hands them to the shared generator as an <see cref="IAreaGenerationCatalog"/>.
/// </summary>
public static class SwlorAreaGenerationCatalog
{
    public static AreaGenerationCatalog Create() => new(
        Discover<IDungeonListDefinition, DungeonDetail>(definition => definition.BuildDungeons()),
        Discover<IDungeonTilesetProfileListDefinition, DungeonTilesetProfile>(definition => definition.BuildTilesetProfiles()),
        Discover<IDungeonLayoutProfileListDefinition, DungeonLayoutProfile>(definition => definition.BuildLayoutProfiles()));

    private static IEnumerable<TValue> Discover<TInterface, TValue>(Func<TInterface, Dictionary<string, TValue>> build)
    {
        var result = new Dictionary<string, TValue>();
        var types = typeof(TInterface).Assembly.GetTypes()
            .Where(type => typeof(TInterface).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        foreach (var type in types)
        {
            var instance = (TInterface)Activator.CreateInstance(type)!;
            foreach (var (key, value) in build(instance))
                result[key] = value;
        }

        return result.Values;
    }
}
