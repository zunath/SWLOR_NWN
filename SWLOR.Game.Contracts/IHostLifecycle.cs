namespace SWLOR.Game.Contracts;

/// <summary>
/// What the permanent host tells a game build about the process it runs in. Values only; the host owns them for the
/// whole life of the process and every generation reads the same ones.
/// </summary>
public interface IHostLifecycle
{
    /// <summary>True once the engine has loaded the module (the <c>mod_load</c> boot scripts have started).</summary>
    bool ModuleLoaded { get; }

    /// <summary>The server environment: <c>Production</c>, <c>Test</c> or <c>Development</c>.</summary>
    string Environment { get; }

    /// <summary>True when the host accepts a reload signal: development and test servers only, never production.</summary>
    bool ReloadEnabled { get; }
}
