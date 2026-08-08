using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Reads and writes item appearance fields that have an NWN:EE word-sized <c>x*</c> companion.
    /// </summary>
    /// <remarks>
    /// The original fields are bytes, but custom content can use part numbers above 255. NWN:EE
    /// stores the complete number in a word-sized companion while retaining the byte field for
    /// compatibility. The companion is authoritative when present. Writes keep an existing
    /// companion synchronized and create one when the value cannot fit in the legacy field.
    /// </remarks>
    public static class ItemAppearanceValues
    {
        private static readonly IReadOnlyDictionary<string, string> ExtendedFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ModelPart1"] = "xModelPart1",
                ["ModelPart2"] = "xModelPart2",
                ["ModelPart3"] = "xModelPart3",
                ["ArmorPart_Neck"] = "xArmorPart_Neck",
                ["ArmorPart_Torso"] = "xArmorPart_Torso",
                ["ArmorPart_Belt"] = "xArmorPart_Belt",
                ["ArmorPart_Pelvis"] = "xArmorPart_Pelvi",
                ["ArmorPart_Robe"] = "xArmorPart_Robe",
                ["ArmorPart_LShoul"] = "xArmorPart_LShou",
                ["ArmorPart_RShoul"] = "xArmorPart_RShou",
                ["ArmorPart_LBicep"] = "xArmorPart_LBice",
                ["ArmorPart_RBicep"] = "xArmorPart_RBice",
                ["ArmorPart_LFArm"] = "xArmorPart_LFArm",
                ["ArmorPart_RFArm"] = "xArmorPart_RFArm",
                ["ArmorPart_LHand"] = "xArmorPart_LHand",
                ["ArmorPart_RHand"] = "xArmorPart_RHand",
                ["ArmorPart_LThigh"] = "xArmorPart_LThig",
                ["ArmorPart_RThigh"] = "xArmorPart_RThig",
                ["ArmorPart_LShin"] = "xArmorPart_LShin",
                ["ArmorPart_RShin"] = "xArmorPart_RShin",
                ["ArmorPart_LFoot"] = "xArmorPart_LFoot",
                ["ArmorPart_RFoot"] = "xArmorPart_RFoot",
            };

        public static string? ExtendedFieldFor(string primaryField) =>
            ExtendedFields.GetValueOrDefault(primaryField);

        /// <summary>
        /// The complete stored part number, preferring its extended field when one exists.
        /// </summary>
        public static int? Read(JsonGffStruct item, string primaryField)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentException.ThrowIfNullOrWhiteSpace(primaryField);

            var primary = item.TryGet(primaryField, out var primaryValue)
                ? checked((int)primaryValue.GetInteger())
                : (int?)null;

            if (ExtendedFieldFor(primaryField) is { } extended &&
                item.TryGet(extended, out var extendedValue))
            {
                // The companion is authoritative only for values above the byte range. Write keeps
                // the pair in sync, but a file whose primary byte was edited directly (Aurora, hand
                // edits) can carry a stale companion of 0 - which must not shadow the real value.
                return Math.Max(primary ?? 0, checked((int)extendedValue.GetInteger()));
            }

            return primary;
        }

        /// <summary>
        /// Stores a complete part number without overflowing the legacy byte field.
        /// </summary>
        public static void Write(ItemValueStore store, string primaryField, int value)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentException.ThrowIfNullOrWhiteSpace(primaryField);
            if (value is < 0 or > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Appearance values must fit in a GFF word.");

            // Keep the old field valid for readers which do not understand the EE companion.
            // The companion below remains authoritative for values above the byte range.
            store.SetInteger(
                BehaviorFieldStorage.Field,
                primaryField,
                GffFieldType.Byte,
                Math.Min(value, byte.MaxValue));

            var extended = ExtendedFieldFor(primaryField);
            if (extended != null && (value > byte.MaxValue || store.Item.Contains(extended)))
            {
                store.SetInteger(
                    BehaviorFieldStorage.Field,
                    extended,
                    GffFieldType.Word,
                    value);
            }
        }
    }
}
