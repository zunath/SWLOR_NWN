using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Services
{
    internal static class BlueprintResRef
    {
        public static bool TryNormalize(
            DocumentSession session,
            string fieldName,
            out string normalized,
            out string? problem)
        {
            var raw = session.Document.Root.GetStringOrNull(fieldName) ?? string.Empty;
            normalized = raw.Trim().ToLowerInvariant();
            if (normalized.Length is < 1 or > 16 ||
                normalized.Any(character =>
                    character is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            {
                problem =
                    $"ResRef '{raw}' must be 1-16 characters of a-z, 0-9, or underscore.";
                return false;
            }

            if (!string.Equals(raw, normalized, StringComparison.Ordinal))
            {
                var value = normalized;
                session.Execute(
                    "Normalize ResRef",
                    () => session.Document.Root.SetString(
                        fieldName,
                        GffFieldType.ResRef,
                        value));
            }

            problem = null;
            return true;
        }
    }
}
