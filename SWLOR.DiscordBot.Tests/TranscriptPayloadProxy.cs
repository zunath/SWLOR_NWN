using System.Reflection;

namespace SWLOR.DiscordBot.Tests;

public class TranscriptPayloadProxy : DispatchProxy
{
    public Dictionary<string, object?> Properties { get; set; } = [];
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is { Name: var name } && name.StartsWith("get_", StringComparison.Ordinal) &&
            Properties.TryGetValue(name[4..], out var value)) return value;
        throw new InvalidOperationException("Unexpected SDK payload access: " + targetMethod?.Name);
    }
}
