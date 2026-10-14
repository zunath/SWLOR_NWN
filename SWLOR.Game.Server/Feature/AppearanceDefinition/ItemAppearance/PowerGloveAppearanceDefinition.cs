using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

public class PowerGloveAppearanceDefinition : WeaponAppearanceBaseDefinition
{
    public override bool IsSimple => false;

    public override int[] TopParts { get; } = { 101 };
    public override int[] MiddleParts { get; } =
    {
        101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
        112, 113, 114, 115, 116, 117, 118, 119, 120, 121
    };
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
            107 => "Rivet Knuckler",
            108 => "Arc Knuckler",
            109 => "Breacher Cestus",
            110 => "Fang Claws",
            111 => "Pulse Cestus",
            112 => "Talon Knuckler",
            113 => "Aegis Guard",
            114 => "Cinder Knuckler",
            115 => "Obsidian Cestus",
            116 => "Rail Claws",
            117 => "Thunder Knuckle",
            118 => "Spine Cestus",
            119 => "Ion Guard",
            120 => "Impact Knuckler",
            121 => "Relic Claws",
            _ => base.GetPartName(part, partId)
        }
        : base.GetPartName(part, partId);
}
