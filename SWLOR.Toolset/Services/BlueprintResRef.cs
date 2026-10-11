using NwnResRef = Nwn.Formats.Resources.ResourceReferenceRules;
using SWLOR.NWN.Formats.Common;
using Nwn.Authoring.Editing;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
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
            if (!NwnResRef.IsCanonical(normalized))
            {
                problem =
                    $"ResRef '{raw}' must be 1-{NwnResRef.MaxLength} characters " +
                    "of a-z, 0-9, or underscore.";
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
