using System;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipRefinement
    {
        public static int Subtype(ShipQualityDimension dimension)
        {
            var value = (int)dimension;
            if (value < 1 || value > (int)ShipQualityDimension.CycleDuration || (value & (value - 1)) != 0)
                throw new ArgumentException("Choose one ship refinement dimension.", nameof(dimension));
            return (int)Math.Log2(value);
        }

        public static ShipQualityDimension Dimension(int subtype) => subtype >= 0 && subtype < 6
            ? (ShipQualityDimension)(1 << subtype) : throw new ArgumentOutOfRangeException(nameof(subtype));

        public static string Validate(ShipQualityDimension eligible, ShipQualityDimension selected, int magnitude,
            ShipModuleProfile module = null, ShipModuleVariant variant = null)
        {
            if (selected == ShipQualityDimension.None || (eligible & selected) != selected)
                return "That refinement does not tune an eligible property on this ship item.";
            Subtype(selected);
            if (magnitude < 1 || magnitude > 100) return "Ship refinement magnitude must be between 1 and 100.";
            if (selected == ShipQualityDimension.ActivationCost && (module == null || variant == null ||
                ShipModuleTuning.Refine(module, variant, selected, magnitude).Capacitor >= variant.Capacitor))
                return "That refinement does not reduce this module's paid capacitor cost.";
            return null;
        }
    }
}
