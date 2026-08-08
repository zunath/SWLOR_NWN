using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Reads the tint-map locals from a blueprint without exposing raw VarTable plumbing.</summary>
    public static class TintMapOverrides
    {
        public static IReadOnlyDictionary<string, int> Read(VarTable variables)
        {
            ArgumentNullException.ThrowIfNull(variables);

            return variables
                .Where(entry =>
                    entry.Type == VarTable.TypeInt &&
                    entry.IntValue is > 0 &&
                    entry.Name.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal))
                .ToDictionary(
                    entry => entry.Name,
                    entry => entry.IntValue!.Value,
                    StringComparer.Ordinal);
        }
    }
}
