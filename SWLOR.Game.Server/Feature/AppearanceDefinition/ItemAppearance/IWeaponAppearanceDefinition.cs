namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    internal interface IWeaponAppearanceDefinition
    {
        bool IsSimple { get; }
        int[] TopParts { get; }

        int[] MiddleParts { get; }

        int[] BottomParts { get; }
    }
}
