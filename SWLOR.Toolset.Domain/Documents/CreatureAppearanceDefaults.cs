using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Supplies the neutral body that a segmented (<c>MODELTYPE P</c>) creature needs in order to
    /// resolve to visible geometry.
    /// </summary>
    /// <remarks>
    /// Dynamic appearance rows identify a race/phenotype family rather than one complete model.
    /// The row still decides whether the result is a dwarf, elf, human, and so on; these values only
    /// choose the base variant of each body piece within that family. Keeping the defaults here lets
    /// new creature blueprints and appearance-only previews assemble the same honest generic body.
    /// </remarks>
    public static class CreatureAppearanceDefaults
    {
        public static void ApplyGenericSegmentedBody(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);

            root.SetInt("Appearance_Head", GffFieldType.Byte, 1);
            root.SetInt("ArmorPart_RFoot", GffFieldType.Byte, 1);

            foreach (var part in new[]
                     {
                         "BodyPart_LBicep", "BodyPart_LFArm", "BodyPart_LFoot", "BodyPart_LHand",
                         "BodyPart_LShin", "BodyPart_LThigh", "BodyPart_Neck", "BodyPart_Pelvis",
                         "BodyPart_RBicep", "BodyPart_RFArm", "BodyPart_RHand", "BodyPart_RShin",
                         "BodyPart_RThigh", "BodyPart_Torso"
                     })
            {
                root.SetInt(part, GffFieldType.Byte, 1);
            }

            foreach (var accessory in new[] { "BodyPart_Belt", "BodyPart_LShoul", "BodyPart_RShoul" })
                root.SetInt(accessory, GffFieldType.Byte, 0);

            foreach (var color in new[] { "Color_Hair", "Color_Skin", "Color_Tattoo1", "Color_Tattoo2" })
                root.SetInt(color, GffFieldType.Byte, 1);
        }
    }
}
