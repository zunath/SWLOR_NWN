#nullable enable
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A stand-in for a service interface that writes each call (the member name and its arguments) to a shared list and answers
/// with the default of the return type, or with what <see cref="Answer"/> gives. A test uses it to see which service a seam of a
/// part forwards a call to.
/// </summary>
public class RecordingServiceProxy : DispatchProxy
{
    public string Label { get; set; } = string.Empty;

    public List<string> Calls { get; set; } = null!;

    public Func<string, object?[], object?>? Answer { get; set; }

    public static TService For<TService>(string label, List<string> calls, Func<string, object?[], object?>? answer = null)
        where TService : class
    {
        var proxy = Create<TService, RecordingServiceProxy>();
        var recorder = (RecordingServiceProxy)(object)proxy;
        recorder.Label = label;
        recorder.Calls = calls;
        recorder.Answer = answer;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var arguments = args ?? Array.Empty<object?>();
        Calls.Add($"{Label}.{targetMethod!.Name}({string.Join(", ", arguments.Select(argument => argument is Type type ? type.Name : argument?.ToString()))})");
        var answered = Answer?.Invoke(targetMethod.Name, arguments);
        if (answered != null)
            return answered;

        var returnType = targetMethod.ReturnType;
        return returnType == typeof(void) || !returnType.IsValueType ? null : Activator.CreateInstance(returnType);
    }
}
