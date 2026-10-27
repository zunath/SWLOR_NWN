using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Turns plain definition data into the text cells the definition snapshots hold. Every value has exactly one
/// rendering, so a snapshot line changes only when the data does.
/// </summary>
internal static class DefinitionCell
{
    /// <summary>Renders a value for use inside a text cell: numbers in invariant form, enums by name, null as <c>null</c>.</summary>
    public static string Text(object? value)
    {
        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            string text => text,
            Enum item => item.ToString(),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            Type type => type.Name,
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            IEnumerable items => "[" + string.Join(",", items.Cast<object?>().Select(Text)) + "]",
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>A short, stable fingerprint of a long text (a description) so the snapshot can lock it without holding it; null for no text.</summary>
    public static string? Fingerprint(string? text)
    {
        if (text == null)
            return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }

    /// <summary>The numeric value of an enum member, for the id column of a snapshot row.</summary>
    public static int Id(Enum value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);
}
