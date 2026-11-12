using Nwn.Hosting.Bootstrap;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Bootstrap;

/// <summary>
/// The host entry NWNX DotNET calls (<c>NWNX_DOTNET_ENTRYPOINT=SWLOR.Game.Host.Bootstrap.NativeBootstrap</c>,
/// <c>NWNX_DOTNET_METHOD=Initialize</c>) when <c>SWLOR_DOTNET_ENTRY</c> is <c>host</c>. It starts the package's native host
/// with SWLOR's options and mode, then loads the game build as generation 1 before returning, so the engine finds the
/// script handlers registered when it runs the boot scripts, as with the native entry (<c>ServerManager.Bootstrap</c>).
/// </summary>
public static class NativeBootstrap
{
    private static int _initialized;

    /// <summary>
    /// Starts the host and the game. NWNX calls it once; a later call does nothing. A failure is rethrown, which stops
    /// the server as a failed native bootstrap does.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
            return;

        HostSettings settings;
        try
        {
            settings = HostSettings.FromEnvironment();
        }
        catch (Exception exception)
        {
            // NativeHost writes its own lines with the prefix only after it is initialized.
            Console.WriteLine($"{HostOptions.LogPrefix} failure settings: {exception}");
            throw;
        }

        // The process's native hooks: the host's extensions find their engine functions, the game installs them through
        // the same table at its boot moments and binds its sinks to it.
        var hooks = new NativeHookTable();
        SwlorHostMode? mode = null;
        NativeHost.Initialize(HostOptions.Create(coreFunctions => mode = new SwlorHostMode(coreFunctions, settings, hooks), hooks));

        // NativeHost reports a failed start through the mode instead of throwing; LoadGame rethrows it.
        if (mode is null)
            throw new InvalidOperationException("The SWLOR host did not create its mode.");

        mode.LoadGame();
    }
}
