using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    /// <summary>
    /// Saved-data form of <see cref="RetiredAppearanceMigration"/> on one item or creature node. Nested items are the
    /// caller's to visit. <paramref name="species"/> replaces a creature's saved appearance when choosing its head: a
    /// character saved in space has its ship's appearance.
    /// </summary>
    internal static bool MigrateRetiredAppearances(Node node, bool isItem, int? species = null)
    {
        var changed = false;
        void Replace(string label, int replacement, int current)
        {
            if (replacement == current) return;
            WritePart(node, label, replacement);
            changed = true;
        }

        if (isItem)
        {
            switch (ReadBaseItem(node))
            {
                case BaseItem.Armor:
                    foreach (var (slot, part) in RetiredAppearanceMigration.Slots)
                    {
                        var label = "ArmorPart_" + slot;
                        var current = ReadPart(node, label);
                        Replace(label, RetiredAppearanceMigration.ReplaceArmorPart(part, current), current);
                    }
                    break;
                case BaseItem.Helmet:
                {
                    var current = ReadSimpleModel(node);
                    Replace("ModelPart1", RetiredAppearanceMigration.ReplaceHelmet(current), current);
                    break;
                }
                case BaseItem.Cloak:
                {
                    var current = ReadSimpleModel(node);
                    Replace("ModelPart1", RetiredAppearanceMigration.ReplaceCloak(current), current);
                    break;
                }
            }
            return changed;
        }

        foreach (var (slot, part) in RetiredAppearanceMigration.Slots)
        {
            if (part == RetiredArmorPart.Robe) continue;
            // Character files save the right foot's body part under the armor part name.
            var label = slot == "RFoot" ? "ArmorPart_RFoot" : "BodyPart_" + slot;
            var current = ReadPart(node, label);
            Replace(label, RetiredAppearanceMigration.ReplaceBodyPart(part, current), current);
        }
        var head = ReadPart(node, "Appearance_Head");
        Replace("Appearance_Head", RetiredAppearanceMigration.ReplaceHead(
            species ?? ReadInteger(node, "Appearance_Type"), ReadInteger(node, "Gender"), head), head);
        return changed;
    }
}
