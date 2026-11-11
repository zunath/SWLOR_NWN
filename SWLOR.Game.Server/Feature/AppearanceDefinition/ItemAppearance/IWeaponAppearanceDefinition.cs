using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    internal interface IWeaponAppearanceDefinition
    {
        bool IsSimple { get; }
        int[] SimpleParts { get; }
        int[] TopParts { get; }

        int[] MiddleParts { get; }

        int[] BottomParts { get; }

        /// <summary>Whether a weapon of this type keeps a part missing from these lists as its own (see <see cref="WeaponOriginalParts"/>).</summary>
        bool KeepsOwnParts { get; }
        string GetPartName(AppearanceWeapon part, int partId);
    }
}
