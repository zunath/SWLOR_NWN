using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Resolves a cloak appearance (a UTI's ModelPart1) to the shared geometry number named by
    /// cloakmodel.2da. Multiple appearances intentionally reuse one model with different textures.
    /// </summary>
    public sealed class CloakModelService
    {
        private const string TableName = "cloakmodel";
        private readonly ReloadableLazy<IReadOnlyDictionary<int, int>> _models;

        public CloakModelService(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _models = new ReloadableLazy<IReadOnlyDictionary<int, int>>(() => Build(twoDa));
            twoDa.TablesReloaded += _models.Reset;
        }

        public int? GetModelOrNull(int appearance) =>
            _models.Value.TryGetValue(appearance, out var model) ? model : null;

        private static IReadOnlyDictionary<int, int> Build(TwoDaService twoDa)
        {
            if (!twoDa.TryGetTable(TableName, out var table) || table == null)
                return new Dictionary<int, int>();

            var models = new Dictionary<int, int>();
            for (var row = 0; row < table.RowCount; row++)
            {
                try
                {
                    var model = table.GetInt(row, "MODEL");
                    if (model is > 0)
                        models[row] = model.Value;
                }
                catch (FormatException)
                {
                    // A malformed/reserved row cannot identify wearable geometry.
                }
            }

            return models;
        }
    }
}
