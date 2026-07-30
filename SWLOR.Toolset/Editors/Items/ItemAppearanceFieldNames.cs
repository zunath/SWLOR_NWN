namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// GFF field names the Appearance tab reads and writes, verified against
    /// Module\uti\adren_harness.uti.json (armor) and <see cref="Domain.Render.Icons.ItemIconResolver"/>'s
    /// bottom/middle/top mapping (ModelPart1 = bottom, ModelPart2 = middle, ModelPart3 = top).
    /// </summary>
    /// <remarks>
    /// A field's "x" twin exists because a byte-typed <c>ArmorPart_*</c> field caps at 255; nwn_gff
    /// also carries a word-typed <c>xArmorPart_*</c> twin for the same value, truncated to fit GFF's
    /// 16-character label limit whenever "x" plus the full name would exceed it. Both spellings below
    /// were read directly out of the corpus file rather than derived from the truncation rule, since
    /// several (LBicep -> LBice, LShoul -> LShou, LThigh -> LThig, Pelvis -> Pelvi, and their R twins)
    /// drop a different letter than a naive "chop to 16" would suggest.
    /// </remarks>
    public static class ItemAppearanceFieldNames
    {
        /// <summary>ModelType 0's and 1's one part field.</summary>
        public const string SimplePart = "ModelPart1";
        public const string SimplePartTwin = "xModelPart1";

        /// <summary>ModelType 2's bottom layer - the hilt/grip end of a composite weapon.</summary>
        public const string Bottom = "ModelPart1";
        public const string BottomTwin = "xModelPart1";

        /// <summary>ModelType 2's middle layer.</summary>
        public const string Middle = "ModelPart2";
        public const string MiddleTwin = "xModelPart2";

        /// <summary>ModelType 2's top layer - the blade/head end.</summary>
        public const string Top = "ModelPart3";
        public const string TopTwin = "xModelPart3";

        public const string Neck = "ArmorPart_Neck";
        public const string NeckTwin = "xArmorPart_Neck";
        public const string Torso = "ArmorPart_Torso";
        public const string TorsoTwin = "xArmorPart_Torso";
        public const string Belt = "ArmorPart_Belt";
        public const string BeltTwin = "xArmorPart_Belt";
        public const string Pelvis = "ArmorPart_Pelvis";
        public const string PelvisTwin = "xArmorPart_Pelvi";
        public const string Robe = "ArmorPart_Robe";
        public const string RobeTwin = "xArmorPart_Robe";

        public const string Cloth1Color = "Cloth1Color";
        public const string Cloth2Color = "Cloth2Color";
        public const string Leather1Color = "Leather1Color";
        public const string Leather2Color = "Leather2Color";
        public const string Metal1Color = "Metal1Color";
        public const string Metal2Color = "Metal2Color";

        public static readonly ItemArmorPartFieldPair Shoulder = new(
            "Shoulder", "ArmorPart_LShoul", "xArmorPart_LShou", "ArmorPart_RShoul", "xArmorPart_RShou");

        public static readonly ItemArmorPartFieldPair Bicep = new(
            "Bicep", "ArmorPart_LBicep", "xArmorPart_LBice", "ArmorPart_RBicep", "xArmorPart_RBice");

        public static readonly ItemArmorPartFieldPair Forearm = new(
            "Forearm", "ArmorPart_LFArm", "xArmorPart_LFArm", "ArmorPart_RFArm", "xArmorPart_RFArm");

        public static readonly ItemArmorPartFieldPair Hand = new(
            "Hand", "ArmorPart_LHand", "xArmorPart_LHand", "ArmorPart_RHand", "xArmorPart_RHand");

        public static readonly ItemArmorPartFieldPair Thigh = new(
            "Thigh", "ArmorPart_LThigh", "xArmorPart_LThig", "ArmorPart_RThigh", "xArmorPart_RThig");

        public static readonly ItemArmorPartFieldPair Shin = new(
            "Shin", "ArmorPart_LShin", "xArmorPart_LShin", "ArmorPart_RShin", "xArmorPart_RShin");

        public static readonly ItemArmorPartFieldPair Foot = new(
            "Foot", "ArmorPart_LFoot", "xArmorPart_LFoot", "ArmorPart_RFoot", "xArmorPart_RFoot");

        /// <summary>Every mirrored left/right body-part field, with each side's verified x-twin.</summary>
        public static readonly IReadOnlyList<ItemArmorPartFieldPair> Pairs = new[]
        {
            Shoulder, Bicep, Forearm, Hand, Thigh, Shin, Foot
        };
    }
}
