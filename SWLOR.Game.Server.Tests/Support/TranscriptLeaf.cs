#nullable enable
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A recording stand-in for a leaf service of a combat transcript (P5-7b): every member writes "Label.Member(arguments)"
/// to the one transcript in call order, then answers with a scripted value or, when none is scripted, with the empty
/// value of its return type (zero, false, an empty string, an empty collection, null). It forwards to nothing, so what
/// a transcript shows is the call a service made, not what a real leaf did with it.
/// </summary>
public class TranscriptLeaf : DispatchProxy
{
    private readonly Dictionary<string, Func<object?[], object?>> _answers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _silent = new(StringComparer.Ordinal);

    /// <summary>The label written before each member ("Stat", "StatusEffect").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>The transcript the call is written to.</summary>
    public List<string> Lines { get; set; } = null!;

    /// <summary>Writes an argument as text; set by the bench so creature ids read as names.</summary>
    public Func<object?, string> Format { get; set; } = TranscriptFormat.Plain;

    /// <summary>Answers calls to <paramref name="member"/> with the result of <paramref name="answer"/>.</summary>
    public TranscriptLeaf Script(string member, Func<object?[], object?> answer)
    {
        _answers[member] = answer;
        return this;
    }

    /// <summary>A member that is read so often that its calls are left out of the transcript (the answer still happens).</summary>
    public TranscriptLeaf Silence(string member)
    {
        _silent.Add(member);
        return this;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var arguments = args ?? Array.Empty<object?>();
        var name = targetMethod!.Name;
        if (!_silent.Contains(name))
            Lines.Add($"{Label}.{name}({string.Join(", ", arguments.Select(Format))})");

        if (_answers.TryGetValue(name, out var answer))
        {
            try
            {
                return answer(arguments);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                throw;
            }
        }

        return EmptyValue(targetMethod.ReturnType);
    }

    public static object? EmptyValue(Type type)
    {
        if (type == typeof(void))
            return null;
        if (type == typeof(string))
            return string.Empty;
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 0);
        if (!type.IsGenericType)
            return type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null ? Activator.CreateInstance(type) : null;

        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments();
        if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IList<>) ||
            definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(List<>))
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(arguments));
        if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(Dictionary<,>))
            return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments));
        if (definition == typeof(ISet<>) || definition == typeof(HashSet<>))
            return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(arguments));
        return null;
    }
}

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
