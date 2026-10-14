using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

public class PowerGloveAppearanceDefinition : WeaponAppearanceBaseDefinition
{
    public override bool IsSimple => false;

    // Powered glove, vibroknuckler, blastknuckle, shock glove, power fist, bare-fist appearance.
    public override int[] TopParts { get; } = { 101 };
    public override int[] MiddleParts { get; } = { 101, 102, 103, 104, 105, 106 };
    public override int[] BottomParts { get; } = { 101 };

    public override string GetPartName(AppearanceWeapon part, int partId) => part == AppearanceWeapon.Middle
        ? partId switch
        {
            101 => "Power Glove",
            102 => "Vibroknuckler",
            103 => "Blastknuckle",
            104 => "Shock Glove",
            105 => "Power Fist",
            106 => "Bare Fists (Invisible)",
            _ => base.GetPartName(part, partId)
        }
        : base.GetPartName(part, partId);
}
