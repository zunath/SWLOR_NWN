using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Lazily builds the expensive module-wide script-usage index and drops that generation whenever
    /// a resource that can carry a script slot changes. Callers already holding the old task may
    /// finish their read; every subsequent request receives the new generation.
    /// </summary>
    public sealed class ScriptUsageIndexCache
    {
        private readonly object _gate = new();
        private readonly Func<ScriptUsageIndex?> _build;
        private Lazy<Task<ScriptUsageIndex?>> _current;

        public ScriptUsageIndexCache(Func<ScriptUsageIndex?> build)
        {
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _current = CreateGeneration();
        }

        public Task<ScriptUsageIndex?> GetAsync()
        {
            lock (_gate)
                return _current.Value;
        }

        public void Invalidate()
        {
            lock (_gate)
                _current = CreateGeneration();
        }

        private Lazy<Task<ScriptUsageIndex?>> CreateGeneration() =>
            new(() => Task.Run(_build), LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
