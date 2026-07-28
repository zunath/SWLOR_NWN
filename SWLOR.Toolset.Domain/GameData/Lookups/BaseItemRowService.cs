using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Reads baseitems.2da's label and ModelType columns, indexed by row. Mirrors
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

                rows[row] = new BaseItemRow(row, label, modelType);
            }

            return rows;
        }
    }
}
