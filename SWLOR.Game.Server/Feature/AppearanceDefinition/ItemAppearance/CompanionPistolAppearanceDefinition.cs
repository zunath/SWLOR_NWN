namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>The left-hand pistol appearance verified against the inert companion carrier.</summary>
public sealed class CompanionPistolAppearanceDefinition : WeaponAppearanceBaseDefinition
{
    public override bool IsSimple => false;
    public override int[] TopParts { get; } = { 11 };
    public override int[] MiddleParts { get; } = { 201 };
    public override int[] BottomParts { get; } = { 11 };
}
