using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Reads the icon-naming columns of baseitems.2da, indexed by row so a uti's BaseItem field can be
    /// looked up directly. Separate from <see cref="TwoDaLookupService"/>, which reads the same table
    /// for its display names only: this one is about resource names, has no TLK dependency, and keeps
    /// reserved rows out of the way rather than mapping them to a label.
    /// </summary>
    public sealed class BaseItemIconService
    {
        private const string TableName = "baseitems";

        private readonly ReloadableLazy<IReadOnlyDictionary<int, BaseItemIconRow>> _byId;

        public BaseItemIconService(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _byId = new ReloadableLazy<IReadOnlyDictionary<int, BaseItemIconRow>>(() => Build(twoDa));
            twoDa.TablesReloaded += _byId.Reset;
        }

        /// <summary>The row for a uti's BaseItem value, or null when the row is absent or reserved.</summary>
        public BaseItemIconRow? GetOrNull(int baseItem) =>
            _byId.Value.TryGetValue(baseItem, out var row) ? row : null;

        private static IReadOnlyDictionary<int, BaseItemIconRow> Build(TwoDaService twoDa)
        {
            if (!twoDa.TryGetTable(TableName, out var table) || table == null)
                return new Dictionary<int, BaseItemIconRow>();

            var rows = new Dictionary<int, BaseItemIconRow>(table.RowCount);
            for (var row = 0; row < table.RowCount; row++)
            {
                var itemClass = table.GetString(row, "ItemClass");
                if (string.IsNullOrWhiteSpace(itemClass))
                    continue; // Reserved/deleted row: nothing to name an icon after.

                int modelType;
                try
                {
                    modelType = table.GetInt(row, "ModelType") ?? -1;
                }
                catch (FormatException)
                {
                    modelType = -1; // A malformed ModelType just means "try every naming pattern".
                }

                rows[row] = new BaseItemIconRow(row, modelType, itemClass, table.GetString(row, "DefaultIcon"));
            }

            return rows;
        }
    }
}
