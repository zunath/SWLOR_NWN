namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Classifies a handler of a boot script (<c>mod_preload</c>, <c>mod_content_chg</c>, <c>mod_cache_bef</c>, <c>mod_cache_aft</c>,
    /// <c>mod_load</c>, <c>db_loaded</c>, <c>app_shutdown</c>) for hot reload. It changes no behaviour: nothing reads it at runtime yet.
    /// A unit test fails when a boot handler has none, and a snapshot test records every class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BootPhaseAttribute : Attribute
    {
        public BootReload Reload { get; }

        public BootPhaseAttribute(BootReload reload)
        {
            Reload = reload;
        }
    }
}
