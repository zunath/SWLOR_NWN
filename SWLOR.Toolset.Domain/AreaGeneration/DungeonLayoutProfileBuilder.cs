#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>Fluent builder for layout profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonLayoutProfileBuilder
    {
        private readonly Dictionary<string, DungeonLayoutProfile> _profiles = new();
        private DungeonLayoutProfile _active;

        public DungeonLayoutProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonLayoutProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            return this;
        }

        /// <summary>
        /// Configures the layout style and tuning knobs. Width/height/terrain labels are stamped
        /// per-request; leave AccentTerrain empty — set AccentDensity to express accent intent and
        /// the composed tileset profile supplies the terrain name.
        /// </summary>
        public DungeonLayoutProfileBuilder Configure(Action<MacroLayoutParameters> configure)
        {
            configure(_active.Template);
            return this;
        }

        public Dictionary<string, DungeonLayoutProfile> Build()
        {
            return _profiles;
        }
    }
}
