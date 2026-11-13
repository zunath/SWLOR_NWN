namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Marks a public method <c>void Name(IReadOnlyList&lt;uint&gt; players)</c> that a hot reload runs (P4-15,
    /// <see cref="ReloadHooks"/>): a closure of the old build is dropped by the reload, so what it would have released stays set in
    /// the engine and what it kept going stops. A <see cref="ReloadHookPhase.Quiesce"/> hook ends that work in the old build; a
    /// <see cref="ReloadHookPhase.Resume"/> hook restarts it in the new one. A static method runs as it is; an instance method runs
    /// on the container's singleton of its class. Hooks run by <see cref="Order"/>, then by class and method name. A normal boot,
    /// login or logout never runs one. The audit of every closure and its decision is in MaintainabilityRefactorHotReload.md section 3.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ReloadHookAttribute : Attribute
    {
        public ReloadHookPhase Phase { get; }

        public ReloadHookAttribute(ReloadHookPhase phase)
        {
            Phase = phase;
        }

        /// <summary>Hooks of a phase run from the lowest order up (default 0); a hook that relies on another's work runs later.</summary>
        public int Order { get; set; }
    }
}
