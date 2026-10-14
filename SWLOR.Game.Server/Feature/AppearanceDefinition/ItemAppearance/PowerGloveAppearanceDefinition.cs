namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

public class PowerGloveAppearanceDefinition : WeaponAppearanceBaseDefinition
{
    public override bool IsSimple => false;

    // Powered glove, vibroknuckler, blastknuckle, shock glove, power fist, bare-fist appearance.
    public override int[] TopParts { get; } = { 101 };
    public override int[] MiddleParts { get; } = { 101, 102, 103, 104, 105, 106 };
    public override int[] BottomParts { get; } = { 101 };
}
