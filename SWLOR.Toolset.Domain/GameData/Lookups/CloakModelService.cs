using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// The geometry, surface, and wearer-part visibility selected by one cloak appearance.
    /// </summary>
    public readonly record struct CloakModelMapping(
        int Model,
        int Texture,
        bool HideLeftShoulder,
        bool HideRightShoulder);

    /// <summary>
    /// Resolves a cloak appearance (a UTI's ModelPart1) to the shared geometry and texture numbers
    /// named by cloakmodel.2da. Multiple appearances intentionally reuse one model with different
    /// textures, so neither half of the mapping may be discarded.
    /// </summary>
    public sealed class CloakModelService
    {
        private const string TableName = "cloakmodel";
        private readonly ReloadableLazy<IReadOnlyDictionary<int, CloakModelMapping>> _mappings;

        public CloakModelService(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _mappings = new ReloadableLazy<IReadOnlyDictionary<int, CloakModelMapping>>(() => Build(twoDa));
            twoDa.TablesReloaded += _mappings.Reset;
        }

        public CloakModelMapping? GetOrNull(int appearance) =>
            _mappings.Value.TryGetValue(appearance, out var mapping) ? mapping : null;

        private static IReadOnlyDictionary<int, CloakModelMapping> Build(TwoDaService twoDa)
        {
            if (!twoDa.TryGetTable(TableName, out var table) || table == null)
                return new Dictionary<int, CloakModelMapping>();

            var mappings = new Dictionary<int, CloakModelMapping>();
            for (var row = 0; row < table.RowCount; row++)
            {
                try
                {
                    var model = table.GetInt(row, "MODEL");
                    if (model is not > 0)
                        continue;

                    var texture = row;
                    try
                    {
                        texture = table.GetInt(row, "TEXTURE") is > 0 and var mappedTexture
                            ? mappedTexture
                            : row;
                    }
                    catch (FormatException)
                    {
                        // Keep valid geometry usable; the appearance row is the safest texture fallback.
                    }

                    mappings[row] = new CloakModelMapping(
                        model.Value,
                        texture,
                        ReadFlag(table, row, "HIDESHOL"),
                        ReadFlag(table, row, "HIDESHOR"));
                }
                catch (FormatException)
                {
                    // A malformed/reserved row cannot identify wearable geometry.
                }
            }

            return mappings;
        }

        private static bool ReadFlag(TwoDaTable table, int row, string column)
        {
            try
            {
                return table.GetInt(row, column) == 1;
            }
            catch (FormatException)
            {
                // A malformed optional flag must not discard otherwise usable cloak geometry.
                return false;
            }
        }
    }
}
