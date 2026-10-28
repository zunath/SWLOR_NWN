using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A trait a ship component can carry into the module built from it. Rows come from RevampComponentCatalog.</summary>
    public class ShipComponentTrait
    {
        public ShipComponentTraitType Type { get; set; }
        public string Name { get; set; }
        public ShipTraitKind Kind { get; set; }

        /// <summary>A blueprint trait's tier (1-3); 0 for every other kind.</summary>
        public int Tier { get; set; }

        /// <summary>The component families that can carry the trait.</summary>
        public IReadOnlyList<ShipComponentFamily> Families { get; set; } = new ShipComponentFamily[0];

        public IReadOnlyList<ShipTraitEffect> Effects { get; set; } = new ShipTraitEffect[0];

        /// <summary>The effects as players read them, for example "+6% Accuracy, -3% DMG".</summary>
        public string Description { get; set; }
    }
}
