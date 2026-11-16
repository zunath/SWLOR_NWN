using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// One astromech body: its appearance row, the model and tint material behind it, and the paint
    /// it wears until the player picks their own.
    /// </summary>
    public class AstromechChassis
    {
        public AstromechChassis(
            AppearanceType appearance,
            string model,
            string name,
            TintMapColor primary,
            TintMapColor secondary,
            TintMapColor trim)
        {
            Appearance = appearance;
            Model = model;
            Name = name;
            DefaultPrimary = primary;
            DefaultSecondary = secondary;
            DefaultTrim = trim;
        }

        public AppearanceType Appearance { get; }

        /// <summary>The model resref, which is also the name of its tint material.</summary>
        public string Model { get; }

        public string Name { get; }

        /// <summary>The portrait of this chassis in the paint it comes with: the resref without its size letter.</summary>
        public string Portrait => "po_" + Model.Replace("c_astro_", "astro") + "_";

        public TintMapColor DefaultPrimary { get; }
        public TintMapColor DefaultSecondary { get; }
        public TintMapColor DefaultTrim { get; }

        public TintMapColor GetDefaultPaint(AstromechPaintZone zone)
        {
            return zone switch
            {
                AstromechPaintZone.Secondary => DefaultSecondary,
                AstromechPaintZone.Trim => DefaultTrim,
                _ => DefaultPrimary
            };
        }
    }
}
