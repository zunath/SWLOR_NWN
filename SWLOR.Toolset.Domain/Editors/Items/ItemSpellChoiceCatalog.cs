using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// The iprp_spells.2da rows a CastSpell property (itempropdef.2da row 15) can name. A choice's
    /// <see cref="BehaviorChoice.Value"/> is the row id, which is exactly the Subtype value a
    /// Consumable/Grenade's property 15 entry stores - see <see cref="ItemValueStore"/>.
    /// </summary>
    public static class ItemSpellChoiceCatalog
    {
        private const string TableName = "iprp_spells";
        private const string LabelColumn = "Label";
        private const string NameColumn = "Name";

        /// <param name="twoDa">Null-tolerant, matching <see cref="Triggers.LoadScreenCatalog"/>.</param>
        /// <param name="tlk">
        /// Resolves a TLK StrRef to display text; null or a miss falls back to the Label column.
        /// </param>
        public static IReadOnlyList<BehaviorChoice> Read(TwoDaService? twoDa, Func<int, string?>? tlk)
        {
            if (twoDa == null || !twoDa.TryGetTable(TableName, out var table) || table == null)
                return Array.Empty<BehaviorChoice>();

            var spells = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, LabelColumn);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                spells.Add(new BehaviorChoice(row, ResolveDisplay(table, row, tlk) ?? label));
            }

            return spells;
        }

        private static string? ResolveDisplay(TwoDaTable table, int row, Func<int, string?>? tlk)
        {
            if (tlk == null)
                return null;

            var strRef = table.GetInt(row, NameColumn);
            if (strRef is null)
                return null;

            var text = tlk(strRef.Value);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
