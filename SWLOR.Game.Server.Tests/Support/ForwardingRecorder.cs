#nullable enable
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A decorator for a real service of the container: the calls whose member names match a filter are written to the
/// transcript, then forwarded to the real instance, which keeps its real behaviour and state.
/// </summary>
public class ForwardingRecorder : DispatchProxy
{
    public object Target { get; set; } = null!;

    public string Label { get; set; } = string.Empty;

    public List<string> Lines { get; set; } = null!;

    public Func<object?, string> Format { get; set; } = TranscriptFormat.Plain;

    public Func<string, bool> Records { get; set; } = _ => true;

    /// <summary>
    /// Describes a call as the line to write instead of the default one: null leaves the default, an empty string writes nothing.
    /// Receives the label, the member name and the arguments.
    /// </summary>
    public Func<string, string, object?[], string?>? Describe { get; set; }

    /// <summary>The real instance behind a decorated service (or the service itself when it was not decorated).</summary>
    public static object Unwrap(object service) => service is ForwardingRecorder recorder ? recorder.Target : service;

    /// <summary>
    /// Replaces the registration of <typeparamref name="T"/> with a decorator over a real instance built from the same
    /// implementation type, so every holder in the container receives the decorator.
    /// </summary>
    public static void Decorate<T>(IServiceCollection services, string label, List<string> lines, Func<object?, string> format, Func<string, bool> records,
        Func<string, string, object?[], string?>? describe = null)
        where T : class
    {
        var descriptor = services.Last(d => d.ServiceType == typeof(T));
        var implementation = descriptor.ImplementationType ?? throw new InvalidOperationException(typeof(T).Name + " is not registered by type.");
        services.Replace(ServiceDescriptor.Singleton(typeof(T), provider =>
        {
            var real = ActivatorUtilities.CreateInstance(provider, implementation);
            var proxy = DispatchProxy.Create<T, ForwardingRecorder>();
            var recorder = (ForwardingRecorder)(object)proxy;
            recorder.Target = real;
            recorder.Label = label;
            recorder.Lines = lines;
            recorder.Format = format;
            recorder.Records = records;
            recorder.Describe = describe;
            return proxy;
        }));
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var arguments = args ?? Array.Empty<object?>();
        var described = Describe?.Invoke(Label, targetMethod!.Name, arguments);
        if (described != null)
        {
            if (described.Length > 0)
                Lines.Add(described);
        }
        else if (Records(targetMethod!.Name))
            Lines.Add($"{Label}.{targetMethod.Name}({string.Join(", ", arguments.Select(Format))})");

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
