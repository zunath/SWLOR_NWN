using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks.Tests.Support;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// The trampolines are process-wide and a hook installs once per process, so every test of this assembly shares one
/// installation against <see cref="RecordingNativeFunctions"/>, made the way the host makes it: every hook file resolved
/// first (the host's extensions), except <see cref="NativeHookId.LegacyItemProperties"/>, which is left for
/// <see cref="NativeHookTable.Enable"/> to resolve (the native entry); then every hook file enabled in id order.
/// </summary>
[SetUpFixture]
public class HookHarness
{
    internal static RecordingNativeFunctions Functions { get; private set; } = null!;

    internal static NativeHookTable Table { get; private set; } = null!;

    /// <summary>The symbols looked up and the hooks requested once the resolution was done, before any hook was enabled.</summary>
    internal static (int Exports, int Requests) AfterResolve { get; private set; }

    /// <summary>The symbols looked up once every hook file was enabled.</summary>
    internal static int ExportsAfterEnable { get; private set; }

    internal const NativeHookId ResolvedByEnable = NativeHookId.LegacyItemProperties;

    [OneTimeSetUp]
    public void InstallEveryHook()
    {
        Functions = new RecordingNativeFunctions();
        Table = new NativeHookTable(Functions);

        foreach (var hook in Enum.GetValues<NativeHookId>().Where(hook => hook != ResolvedByEnable))
            Table.Resolve(hook);
        AfterResolve = (Functions.Exports.Count, Functions.Requests.Count);

        foreach (var hook in Enum.GetValues<NativeHookId>())
            Table.Enable(hook);
        ExportsAfterEnable = Functions.Exports.Count;
    }
}
