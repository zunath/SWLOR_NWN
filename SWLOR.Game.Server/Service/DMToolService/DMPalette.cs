using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// One palette (for example the custom creature palette) flattened for the DM creator.
    /// </summary>
    public class DMPalette
    {
        /// <summary>
        /// Category paths in palette order, such as "World Creatures / Dathomir". Only categories holding blueprints are listed.
        /// </summary>
        public IReadOnlyList<string> CategoryPaths { get; }

        /// <summary>
        /// Every blueprint in the palette, in palette order.
        /// </summary>
        public IReadOnlyList<DMBlueprint> Blueprints { get; }

        public DMPalette(IReadOnlyList<string> categoryPaths, IReadOnlyList<DMBlueprint> blueprints)
        {
            CategoryPaths = categoryPaths;
            Blueprints = blueprints;
        }
    }
}
