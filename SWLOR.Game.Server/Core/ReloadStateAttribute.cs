namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Marks a field of a service that the reload digests (<see cref="ReloadDigests"/>) must not treat as cache: adopted state,
    /// which is compared with the old build's at quiescence, or runtime state, which is not compared. A field without it, in a
    /// class that owns a rerun boot handler, is cache: after a reload it must hash as it did at boot. Nothing reads it unless
    /// hot reload is on.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ReloadStateAttribute : Attribute
    {
        public ReloadStateKind Kind { get; }

        public ReloadStateAttribute(ReloadStateKind kind)
        {
            Kind = kind;
        }
    }
}
