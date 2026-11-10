namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// What a hot reload does with a boot handler. After a reload the module is already loaded, so the engine runs none of the
    /// boot scripts again; the new build reruns the handlers that only rebuild its own state and skips the rest.
    /// Nothing reads this at runtime yet; <see cref="BootPhaseAttribute"/> records the decision.
    /// </summary>
    public enum BootReload
    {
        /// <summary>
        /// Only fills the build's own instance state or the game assembly's own statics, reads the engine, Redis or resources,
        /// writes nothing persistent and creates nothing. A <c>Scheduler</c> timer or a registration into a static registry of
        /// the game assembly is fine. The new build runs it again.
        /// </summary>
        Rerun = 0,

        /// <summary>Changes the world, saved data or engine state that survives a reload. It never runs again after boot.</summary>
        BootOnly = 1,

        /// <summary>Mixes both. It is split into a rerun part and a boot-only part before reload can use it.</summary>
        Split = 2,

        /// <summary>Installs a native function hook. Hooks persist in the engine and are handled by the native hook work, not by rerunning.</summary>
        NativeHook = 3,

        /// <summary>Runs when the application shuts down, and also when a generation retires. It is not a boot handler.</summary>
        Retire = 4,
    }
}
