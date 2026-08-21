using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Builds an independent custom blueprint from an existing blueprint, matching Aurora's
    /// Edit Copy operation.
    /// </summary>
    public static class BlueprintCopyFactory
    {
        private const int MaximumResRefLength = 16;
        private const int InitialCopyNumberWidth = 3;

        /// <summary>
        /// Returns the next available Aurora-style copy ResRef. A source without a three-or-more digit
        /// suffix gets <c>001</c>; an already numbered copy increments its suffix.
        /// </summary>
        public static string NextResRef(string sourceResRef, IEnumerable<string> existingResRefs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceResRef);
            ArgumentNullException.ThrowIfNull(existingResRefs);

            var normalized = NormalizeSourceResRef(sourceResRef);
            if (normalized.Length == 0)
                throw new ArgumentException("The source ResRef has no legal ResRef characters.", nameof(sourceResRef));

            var digitStart = normalized.Length;
            while (digitStart > 0 && char.IsAsciiDigit(normalized[digitStart - 1]))
                digitStart--;

            var trailingDigitCount = normalized.Length - digitStart;
            var prefix = normalized;
            ulong copyNumber = 1;
            var minimumNumberWidth = InitialCopyNumberWidth;

            if (trailingDigitCount >= InitialCopyNumberWidth &&
                ulong.TryParse(normalized[digitStart..], out var parsedNumber))
            {
                prefix = normalized[..digitStart];
                copyNumber = parsedNumber + 1;
                minimumNumberWidth = trailingDigitCount;
            }

            var existing = existingResRefs.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fellBackFromOversizedNumber = false;
            for (var attempt = 0; attempt < 1_000_000; attempt++, copyNumber++)
            {
                var digits = copyNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (digits.Length < minimumNumberWidth)
                    digits = digits.PadLeft(minimumNumberWidth, '0');

                // An all-numeric 16-character ResRef can overflow to 17 digits. At that point it is no
                // longer useful as a numeric sequence, so start a conventional 001 suffix from it.
                if (digits.Length > MaximumResRefLength)
                {
                    if (fellBackFromOversizedNumber)
                        break;

                    fellBackFromOversizedNumber = true;
                    prefix = normalized;
                    copyNumber = 1;
                    minimumNumberWidth = InitialCopyNumberWidth;
                    attempt--;
                    continue;
                }

                var prefixLength = Math.Min(prefix.Length, MaximumResRefLength - digits.Length);
                var candidate = prefix[..prefixLength] + digits;
                if (!existing.Contains(candidate))
                    return candidate;
            }

            throw new InvalidOperationException($"Could not generate an available copy ResRef for '{sourceResRef}'.");
        }

        /// <summary>
        /// Deep-copies the source document and changes only its blueprint identity. Tag, name, scripts,
        /// inventory, variables, unknown fields, and every other authored value are preserved.
        /// </summary>
        public static byte[] CreateFileContent(
            ResourceType type,
            JsonGffDocument source,
            string copyResRef)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!ModuleWorkspace.BlueprintTypes.Contains(type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type), type, "Edit Copy is only available for blueprint resource types.");
            }

            if (!IsValidResRef(copyResRef))
            {
                throw new ArgumentException(
                    "The copy ResRef must be 1-16 characters of a-z, 0-9, or underscore.",
                    nameof(copyResRef));
            }

            // Serializing and parsing produces an independent document while retaining the source's
            // unknown fields and JSON formatting characteristics. The copy is not on an undo stack yet.
            var copy = JsonGffDocument.Parse(source.ToBytes());
            using (EditScope.EnterConstruction())
            {
                copy.Root.SetString(
                    IdentityFieldName(type),
                    GffFieldType.ResRef,
                    copyResRef);
            }

            return copy.ToBytes();
        }

        /// <summary>The root field which identifies a blueprint of this type.</summary>
        public static string IdentityFieldName(ResourceType type)
        {
            if (!ModuleWorkspace.BlueprintTypes.Contains(type))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Not a blueprint resource type.");

            return type == ResourceType.Utm ? "ResRef" : "TemplateResRef";
        }

        private static string NormalizeSourceResRef(string sourceResRef)
        {
            var builder = new System.Text.StringBuilder(MaximumResRefLength);
            foreach (var character in sourceResRef.Trim())
            {
                if (char.IsAsciiLetterOrDigit(character) || character == '_')
                    builder.Append(char.ToLowerInvariant(character));

                if (builder.Length == MaximumResRefLength)
                    break;
            }

            return builder.ToString();
        }

        private static bool IsValidResRef(string resRef) =>
            !string.IsNullOrWhiteSpace(resRef) &&
            resRef.Length <= MaximumResRefLength &&
            resRef.All(character =>
                char.IsAsciiDigit(character) ||
                character == '_' ||
                character is >= 'a' and <= 'z');
    }
}
