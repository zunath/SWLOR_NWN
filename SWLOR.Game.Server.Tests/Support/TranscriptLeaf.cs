#nullable enable
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A recording stand-in for a leaf service of a combat transcript: every member writes "Label.Member(arguments)"
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

    /// <summary>
    /// Answers the members no script covers: it returns whether it answered and the answer, and when it declines the member answers
    /// with the empty value of its return type. A transcript that varies what the leaves report (every check true, every other check true)
    /// sets it once, instead of scripting each member.
    /// </summary>
    public Func<MethodInfo, object?[], (bool Handled, object? Value)>? Fallback { get; set; }

    /// <summary>
    /// Describes a call as the line to write instead of the default one: null leaves the default, an empty string writes nothing.
    /// Receives the label, the member name and the arguments.
    /// </summary>
    public Func<string, string, object?[], string?>? Describe { get; set; }

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
        var described = Describe?.Invoke(Label, name, arguments);
        if (described != null)
        {
            if (described.Length > 0)
                Lines.Add(described);
        }
        else if (!_silent.Contains(name))
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

        if (Fallback != null)
        {
            var (handled, value) = Fallback(targetMethod, arguments);
            if (handled)
                return value;
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
