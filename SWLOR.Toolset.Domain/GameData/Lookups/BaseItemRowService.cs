using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Reads baseitems.2da's label, ModelType, and StorePanel columns, indexed by row. Mirrors
    /// <see cref="BaseItemIconService"/>'s shape but surfaces the columns item-family
    /// classification needs rather than icon naming.
    /// </summary>
    public sealed class BaseItemRowService
    {
        private const string TableName = "baseitems";

        private readonly Lazy<IReadOnlyDictionary<int, BaseItemRow>> _byId;

        public BaseItemRowService(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _byId = new Lazy<IReadOnlyDictionary<int, BaseItemRow>>(() => Build(twoDa));
        }

        /// <summary>The row for a uti's BaseItem value, or null when the row is absent or reserved.</summary>
        public BaseItemRow? GetOrNull(int baseItem) =>
            _byId.Value.TryGetValue(baseItem, out var row) ? row : null;

        public IReadOnlyList<BaseItemRow> All => _byId.Value.Values.ToList();

        private static IReadOnlyDictionary<int, BaseItemRow> Build(TwoDaService twoDa)
        {
            if (!twoDa.TryGetTable(TableName, out var table) || table == null)
                return new Dictionary<int, BaseItemRow>();

            var rows = new Dictionary<int, BaseItemRow>(table.RowCount);
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "label");
                if (string.IsNullOrWhiteSpace(label))
                    continue; // Reserved/deleted row: nothing to classify.

                int modelType;
                try
                {
                    modelType = table.GetInt(row, "ModelType") ?? -1;
                }
                catch (FormatException)
                {
                    modelType = -1; // A malformed ModelType just means the classifier falls back to the label.
                }

                int storePanel;
                try
                {
                    storePanel = table.GetInt(row, "StorePanel") ?? 4;
                }
                catch (FormatException)
                {
                    storePanel = 4;
                }

                // Aurora has exactly five panes. Unknown/custom values belong in Miscellaneous,
                // which is also what baseitems.2da uses for uncategorized item types.
                if (storePanel is < 0 or > 4)
                    storePanel = 4;

                rows[row] = new BaseItemRow(row, label, modelType, storePanel);
            }

            return rows;
        }
    }
}
