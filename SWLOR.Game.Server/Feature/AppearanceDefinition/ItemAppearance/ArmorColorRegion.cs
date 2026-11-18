using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>One colour swatch of the appearance editor and the names of its four bindings.</summary>
    /// <param name="Target">The armor target the swatch colours.</param>
    /// <param name="Channel">The dye channel of that target.</param>
    public sealed record ArmorColorRegion(AppearanceColorTarget Target, AppearanceArmorColor Channel)
    {
        /// <summary>The swatch name, for example <c>ChestCloth1</c>.</summary>
        public string Name { get; } = Target.ToString() + Channel;

        /// <summary>The palette cell shown on the swatch.</summary>
        public string RegionName { get; } = Target.ToString() + Channel + "Region";

        /// <summary>Whether the swatch carries the active-target glow.</summary>
        public string SelectedName { get; } = Target.ToString() + Channel + "Selected";

        /// <summary>The fill colour previewed over the swatch.</summary>
        public string TintName { get; } = Target.ToString() + Channel + "Tint";

        /// <summary>Whether the swatch shows a custom (RGB) colour instead of a palette preset.</summary>
        public string CustomName { get; } = Target.ToString() + Channel + "Custom";
    }
}
