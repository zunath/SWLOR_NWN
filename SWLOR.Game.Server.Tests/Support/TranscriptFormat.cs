#nullable enable
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>How a transcript writes an argument or a result: stable text, no addresses, no hash codes.</summary>
public static class TranscriptFormat
{
    public static string Plain(object? value) => value switch
    {
        null => "null",
        bool flag => flag ? "True" : "False",
        float number => number.ToString("0.####", CultureInfo.InvariantCulture),
        double number => number.ToString("0.####", CultureInfo.InvariantCulture),
        string text => text,
        Type type => type.Name,
        Enum constant => constant.ToString(),
        Delegate => "<delegate>",
        IFormattable formattable when value.GetType().IsPrimitive => formattable.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable sequence => "[" + string.Join(",", sequence.Cast<object?>().Select(Plain)) + "]",
        _ => Describe(value)
    };

    // A class or a record: its type name and its public readable properties of simple types, in name order.
    private static string Describe(object value)
    {
        var type = value.GetType();
        if (type.IsValueType && type.FullName!.StartsWith("System.ValueTuple", StringComparison.Ordinal))
            return value.ToString()!;

        var parts = new List<string>();
        // Only what the type itself adds (a status effect's constructor arguments), never what a base class or an override supplies (its id, name, icon).
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                continue;
            if (property.GetMethod!.GetBaseDefinition().DeclaringType != property.DeclaringType)
                continue;
            var propertyType = property.PropertyType;
            if (!(propertyType.IsPrimitive || propertyType.IsEnum || propertyType == typeof(string) || propertyType == typeof(Type)))
                continue;
            parts.Add(property.Name + "=" + Plain(property.GetValue(value)));
        }

        return type.Name + (parts.Count == 0 ? string.Empty : "{" + string.Join(",", parts) + "}");
    }
}
