namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Classifies a handler of a boot script (<c>mod_preload</c>, <c>mod_content_chg</c>, <c>mod_cache_bef</c>, <c>mod_cache_aft</c>,
    /// <c>mod_load</c>, <c>db_loaded</c>, <c>app_shutdown</c>) for hot reload. The boot never reads it; the reload bootstrap
    /// (<see cref="ReloadBootstrap"/>) does, and only when a generation activates after the module has loaded. A unit test fails
    /// when a boot handler has none, and snapshot tests record every class and every reload decision.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BootPhaseAttribute : Attribute
    {
        public BootReload Reload { get; }

        public BootPhaseAttribute(BootReload reload)
        {
            Reload = reload;
        }

        /// <summary>
        /// For a <see cref="BootReload.BootOnly"/> handler whose work a reload must stand in for: the name of a public
        /// parameterless method of the same class (static when the handler is static) that the reload bootstrap runs after
        /// the rerun handlers, in the order of the boot-only handlers it stands in for. It reads live engine objects, Redis
        /// or what <see cref="CarriedBy"/> left on the module, and writes nothing persistent.
        /// </summary>
        public string AdoptedBy { get; set; }

        /// <summary>
        /// For a handler with <see cref="AdoptedBy"/> whose state cannot be read back from the world: the name of a public
        /// parameterless method of the same class that the running build calls when a reload quiesces it, which leaves that
        /// state on the module (<see cref="ReloadCarry"/>) for the new build's adoption step.
        /// </summary>
        public string CarriedBy { get; set; }

        /// <summary>
        /// For a <see cref="BootReload.BootOnly"/> handler a reload needs nothing for: why (what it did stays in the engine or in
        /// Redis, or the schema gate refuses a build that would need it).
        /// </summary>
        public string NothingToAdopt { get; set; }
    }
}
