using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A revamp module fitted to a hull, as the stat calculator sees it.
    /// </summary>
    public class ShipFittedModule
    {
        /// <summary>The row the module sits in.</summary>
        public ShipModulePowerType Row { get; set; }

        /// <summary>The slot number within the row, starting at 1.</summary>
        public int Slot { get; set; }

        public ShipModuleStep Step { get; set; }

        /// <summary>The module item's component traits. Null or empty when it has none.</summary>
        public IReadOnlyCollection<ShipComponentTraitType> Traits { get; set; }
    }
}
