using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>One colour swatch of the appearance editor and the names of its four bindings.</summary>
    public sealed class ArmorColorRegion
    {
        public ArmorColorRegion(AppearanceColorTarget target, AppearanceArmorColor channel)
        {
            Target = target;
            Channel = channel;
            Name = target.ToString() + channel;
            RegionName = Name + "Region";
            SelectedName = Name + "Selected";
            TintName = Name + "Tint";
            CustomName = Name + "Custom";
        }

        public AppearanceColorTarget Target { get; }
        public AppearanceArmorColor Channel { get; }

        /// <summary>The swatch name, for example <c>ChestCloth1</c>.</summary>
        public string Name { get; }

        /// <summary>The palette cell shown on the swatch.</summary>
        public string RegionName { get; }

        /// <summary>Whether the swatch carries the active-target glow.</summary>
        public string SelectedName { get; }

        /// <summary>The fill colour previewed over the swatch.</summary>
        public string TintName { get; }

        /// <summary>Whether the swatch shows a custom (RGB) colour instead of a palette preset.</summary>
        public string CustomName { get; }
    }
}
