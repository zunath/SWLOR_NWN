using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Writes a settings value as stable text, read by reflection: primitives and enums by value, strings quoted, types by name, delegates as
/// "set", sequences in order, dictionaries by sorted key, and objects as their type name with their public readable properties in name
/// order (to a depth, so a cyclic graph ends). No address, hash code or guid reaches the text.
/// </summary>
public static class WeaponAbilityValueWriter
{
    private const int MaximumDepth = 4;

    public static string Write(object? value) => Write(value, 0);

    /// <summary>The public readable instance properties of a type, by name.</summary>
    public static IReadOnlyList<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

    private static string Write(object? value, int depth)
    {
        switch (value)
        {
            case null:
                return "null";
            case bool flag:
                return flag ? "true" : "false";
            case string text:
                return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\r").Replace("\n", "\n").Replace(" ", "_") + "\"";
            case float number:
                return number.ToString("0.####", CultureInfo.InvariantCulture);
            case double number:
                return number.ToString("0.####", CultureInfo.InvariantCulture);
            case Type type:
                return "typeof(" + type.Name + ")";
            case Enum constant:
                return constant.ToString().Replace(", ", "|");
            case Delegate:
                return "set";
            case IFormattable formattable when value.GetType().IsPrimitive:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        if (depth >= MaximumDepth)
            return value.GetType().Name + "{...}";

        if (value is IDictionary dictionary)
        {
            var pairs = dictionary.Keys.Cast<object>()
                .Select(key => (Key: Write(key, depth + 1), Value: Write(dictionary[key], depth + 1)))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + ":" + pair.Value);
            return "{" + string.Join(",", pairs) + "}";
        }

        if (value is IEnumerable sequence)
            return "[" + string.Join(",", sequence.Cast<object?>().Select(item => Write(item, depth + 1))) + "]";

        var parts = new StringBuilder();
        foreach (var property in Properties(value.GetType()))
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (parts.Length > 0)
                parts.Append(',');
            parts.Append(property.Name).Append('=').Append(Write(propertyValue, depth + 1));
        }

        return value.GetType().Name + "{" + parts + "}";
    }
}
