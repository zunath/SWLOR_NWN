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
        string GetPartName(AppearanceWeapon part, int partId);
    }
}
