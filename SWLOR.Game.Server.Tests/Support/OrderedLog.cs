#nullable enable
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// One ordered list of what the code under test did, for the transcripts that do not run on the combat bench: every call that reaches an
/// engine fake or a recording neighbour is written as "Label.Member(arguments)" in call order, with creature and item ids read as the names
/// the scenario gave them. <see cref="Wrap{T}"/> puts a logging proxy in front of a fake (the call is written, then forwarded);
/// <see cref="ScriptedProxy.Observer"/> writes the calls a scripted neighbour receives.
/// </summary>
public sealed class OrderedLog
{
    private readonly Dictionary<uint, string> _names = new();

    /// <summary>Members that are never written, because a scenario reads them in loops that say nothing about the order of the work (the property reads of one item).</summary>
    public HashSet<string> Quiet { get; } = new(StringComparer.Ordinal);

    /// <summary>The lines, in call order.</summary>
    public List<string> Lines { get; } = new();

    /// <summary>Reads an object id as this name in every line that follows.</summary>
    public void Name(uint id, string name) => _names[id] = name;

    /// <summary>Names an id the scenario creates while it runs (the droid), after the names given with <see cref="Name"/>.</summary>
    public Func<uint, string?>? Resolver { get; set; }

    public string Format(object? argument) => argument switch
    {
        uint id when _names.TryGetValue(id, out var name) => name,
        uint id when Resolver?.Invoke(id) is { } resolved => resolved,
        _ => TranscriptFormat.Plain(argument)
    };

    public void Add(string label, string member, IReadOnlyList<object?> arguments)
    {
        if (!Quiet.Contains(member))
            Lines.Add($"{label}.{member}({string.Join(", ", arguments.Select(Format))})");
    }

    /// <summary>A proxy that writes every call to this log, then forwards it to <paramref name="target"/>.</summary>
    public T Wrap<T>(T target, string label)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, LoggingProxy>();
        var logging = (LoggingProxy)(object)proxy;
        logging.Target = target;
        logging.Label = label;
        logging.Log = this;
        return proxy;
    }

    /// <summary>The proxy <see cref="Wrap{T}"/> builds.</summary>
    public class LoggingProxy : DispatchProxy
    {
        public object Target { get; set; } = null!;
        public string Label { get; set; } = string.Empty;
        public OrderedLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var arguments = args ?? Array.Empty<object?>();
            Log.Add(Label, targetMethod!.Name, arguments);
            try
            {
                return targetMethod.Invoke(Target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                throw;
            }
        }
    }
}
