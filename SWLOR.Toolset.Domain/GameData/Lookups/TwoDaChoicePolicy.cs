namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>Shared gate between raw 2DA rows and builder-facing option lists.</summary>
    public static class TwoDaChoicePolicy
    {
        /// <summary>
        /// True only for labels that name real content rather than an engine, BioWare, CEP, or
        /// custom-content placeholder slot.
        /// </summary>
        public static bool IsSelectableLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            var trimmed = label.Trim();
            if (trimmed.All(character => character == '*'))
                return false;

            // appearance.2da uses wrapper punctuation for internal null models such as
            // "(Null Human)". Treat a separated Null prefix as a sentinel while retaining real
            // content words such as "Nullifier".
            var unwrapped = trimmed.TrimStart('(', '[', '{').TrimStart();
            if (unwrapped.StartsWith("null", StringComparison.OrdinalIgnoreCase) &&
                (unwrapped.Length == "null".Length ||
                 char.IsWhiteSpace(unwrapped["null".Length]) ||
                 unwrapped["null".Length] is '_' or '-' ||
                 char.IsDigit(unwrapped["null".Length])))
            {
                return false;
            }

            var normalized = new string(trimmed
                .Where(character => !char.IsWhiteSpace(character) && character is not '_' and not '-')
                .ToArray());
            if (normalized.Contains("reserved", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("deleted", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "padding", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("unused", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (normalized.StartsWith("user", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = normalized["user".Length..];
                if (suffix.Length == 0 || suffix.All(char.IsDigit))
                    return false;
            }

            if (normalized.StartsWith("null", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = normalized["null".Length..];
                if (suffix.Length == 0 || suffix.All(char.IsDigit))
                    return false;
            }

            return true;
        }
    }
}
