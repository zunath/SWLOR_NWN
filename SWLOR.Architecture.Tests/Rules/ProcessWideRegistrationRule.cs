using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// Nothing in the server hooks itself into the process where a reload could not undo it. Two things are forbidden outside
/// a short allow list: subscribing to a process-wide event (<c>AppDomain</c> events, <c>AssemblyLoadContext</c> unloading
/// and resolving, <c>Console.CancelKeyPress</c>, <c>TaskScheduler.UnobservedTaskException</c>, <c>SystemEvents</c>) and
/// starting a dedicated thread (<c>new Thread(...)</c>). A retired build must be able to let go of everything that points
/// into it, and a subscription or a thread the build started would keep it alive.
/// </summary>
internal static class ProcessWideRegistrationRule
{
    private const string EventAdder = "add_";
    private const string Constructor = ".ctor";
    private const string ThreadType = "System.Threading.Thread";

    /// <summary>The types whose event subscriptions are always process-wide, whatever the event.</summary>
    private static readonly string[] AnyEventTypes =
    [
        "System.AppDomain",
        "Microsoft.Win32.SystemEvents"
    ];

    /// <summary>Process-wide events of types that also have ordinary members: type, then the subscribing method.</summary>
    private static readonly (string Type, string Adder)[] NamedEvents =
    [
        ("System.Runtime.Loader.AssemblyLoadContext", "add_Unloading"),
        ("System.Runtime.Loader.AssemblyLoadContext", "add_Resolving"),
        ("System.Runtime.Loader.AssemblyLoadContext", "add_ResolvingUnmanagedDll"),
        ("System.Console", "add_CancelKeyPress"),
        ("System.Threading.Tasks.TaskScheduler", "add_UnobservedTaskException")
    ];

    /// <param name="facts">The uses of other assemblies' members in the server.</param>
    /// <param name="subscribers">The types allowed to subscribe to a process-wide event.</param>
    /// <param name="threadStarters">The types allowed to start a thread.</param>
    public static IReadOnlyList<string> Find(
        AssemblyFacts facts,
        IReadOnlyCollection<TypeKey> subscribers,
        IReadOnlyCollection<TypeKey> threadStarters)
    {
        var problems = new List<string>();

        foreach (var use in facts.ExternalMemberUses)
        {
            if (IsProcessWideSubscription(use) && !subscribers.Contains(use.Source))
            {
                problems.Add($"{use.Source} subscribes to {use.ExternalType}.{use.MemberName[EventAdder.Length..]} ({Describe(use.Members)}): " +
                             "a process-wide event is subscribed only by the types on the allow list, so a teardown can remove it. " +
                             "Use IProcessEventSubscriptions.");
            }
            else if (use.ExternalType == ThreadType && use.MemberName == Constructor && !threadStarters.Contains(use.Source))
            {
                problems.Add($"{use.Source} starts a thread ({Describe(use.Members)}): the server runs on the engine thread and the thread pool only. " +
                             "Run the work as pool work, or add the type to the allow list with the item that removes it.");
            }
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    private static bool IsProcessWideSubscription(ExternalMemberUse use)
    {
        if (!use.MemberName.StartsWith(EventAdder, StringComparison.Ordinal))
            return false;

        return AnyEventTypes.Contains(use.ExternalType)
               || NamedEvents.Any(entry => entry.Type == use.ExternalType && entry.Adder == use.MemberName);
    }

    private static string Describe(IEnumerable<string> members) => string.Join(", ", members.OrderBy(member => member, StringComparer.Ordinal));
}
