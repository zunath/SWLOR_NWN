using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public abstract class WeaponAppearanceBaseDefinition : IWeaponAppearanceDefinition
    {
        public abstract bool IsSimple { get; }
        public virtual int[] SimpleParts { get; } = { };
        public virtual int[] TopParts { get; } = { };
        public virtual int[] MiddleParts { get; } = { };
        public virtual int[] BottomParts { get; } = { };
        public virtual string GetPartName(AppearanceWeapon part, int partId) => DefaultPartName(partId);

        /// <summary>The name of a part the definition does not name; the editor shows the picture's name instead.</summary>
        public static string DefaultPartName(int partId) => $"Part #{partId}";
    }
}
