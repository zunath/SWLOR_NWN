using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Resolves the creature wing and tail appearance ids stored in UTC/GIT data to their MDL
    /// resrefs. These models are separate visuals at runtime and therefore must be composed into
    /// toolset previews rather than treated as metadata on the base appearance.
    /// </summary>
    public sealed class CreatureAttachmentModelService
    {
        private readonly ReloadableLazy<AttachmentModels> _models;

        public CreatureAttachmentModelService(TwoDaService twoDa)
        {
            ArgumentNullException.ThrowIfNull(twoDa);
            _models = new ReloadableLazy<AttachmentModels>(() => Build(twoDa));
            twoDa.TablesReloaded += _models.Reset;
        }

        public string? GetWingOrNull(int appearance) =>
            _models.Value.Wings.TryGetValue(appearance, out var model) ? model : null;

        public string? GetTailOrNull(int appearance) =>
            _models.Value.Tails.TryGetValue(appearance, out var model) ? model : null;

        private static AttachmentModels Build(TwoDaService twoDa) =>
            new(BuildTable(twoDa, "wingmodel"), BuildTable(twoDa, "tailmodel"));

        private static IReadOnlyDictionary<int, string> BuildTable(TwoDaService twoDa, string tableName)
        {
            if (!twoDa.TryGetTable(tableName, out var table) || table == null)
                return new Dictionary<int, string>();

            var models = new Dictionary<int, string>();
            for (var row = 0; row < table.RowCount; row++)
            {
                try
                {
                    var model = table.GetString(row, "MODEL");
                    if (!string.IsNullOrWhiteSpace(model) && model != "****")
                        models[row] = model;
                }
                catch (FormatException)
                {
                    // Reserved or malformed rows cannot identify an attachment model.
                }
            }

            return models;
        }

        private sealed record AttachmentModels(
            IReadOnlyDictionary<int, string> Wings,
            IReadOnlyDictionary<int, string> Tails);
    }
}
