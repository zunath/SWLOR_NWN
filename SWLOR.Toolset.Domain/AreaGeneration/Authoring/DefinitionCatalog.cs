#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Reflects over the linked theme, tileset-profile, and layout-profile definitions so the
    /// generator's pickers stay aligned with the imported definition catalog.
    /// </summary>
    public sealed class DefinitionCatalog
    {
        public IReadOnlyList<DungeonDetail> Themes { get; }
        public IReadOnlyDictionary<string, DungeonTilesetProfile> TilesetProfiles { get; }
        public IReadOnlyDictionary<string, DungeonLayoutProfile> LayoutProfiles { get; }

        public DefinitionCatalog()
        {
            var themes = Discover<IDungeonListDefinition, DungeonDetail>(d => d.BuildDungeons());
            Themes = themes.Values.OrderBy(t => t.DisplayName).ToList();
            var tilesetProfiles = Discover<IDungeonTilesetProfileListDefinition, DungeonTilesetProfile>(d => d.BuildTilesetProfiles());
            DungeonTilesetPaletteInheritance.Apply(tilesetProfiles);
            TilesetProfiles = tilesetProfiles;
            LayoutProfiles = Discover<IDungeonLayoutProfileListDefinition, DungeonLayoutProfile>(d => d.BuildLayoutProfiles());
        }

        private static Dictionary<string, TValue> Discover<TInterface, TValue>(Func<TInterface, Dictionary<string, TValue>> build)
        {
            var result = new Dictionary<string, TValue>();
            var types = typeof(TInterface).Assembly.GetTypes()
                .Where(t => typeof(TInterface).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                var instance = (TInterface)Activator.CreateInstance(type);
                foreach (var (key, value) in build(instance))
                    result[key] = value;
            }

            return result;
        }
    }
}
