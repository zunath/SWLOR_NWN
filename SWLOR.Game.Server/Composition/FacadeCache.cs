using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The per-service cache behind <see cref="FacadeAccessor"/>: one static slot per service type, holding the instance
    /// together with the container it was resolved from. A facade call reads the slot, compares the container with the
    /// one <see cref="CompositionRoot.Current"/> names, and returns the instance: no dictionary lookup and no allocation.
    /// </summary>
    /// <remarks>
    /// The slot is valid only for the container it was resolved from, so it never needs clearing: when the container
    /// is built, replaced, rebuilt (hot reload) or a test installs its own with <see cref="CompositionRoot.InstallForTests"/>,
    /// <c>Current</c> names a different container, the comparison fails and the next call resolves again. Disposing a
    /// container does not clear a slot, but nothing can name a disposed container as current once its test scope ends.
    /// The slot holds one container at a time; flows that alternate between two containers (parallel tests) refill it
    /// each time and stay correct.
    /// Thread safety: the slot is an immutable <see cref="Entry"/> published with a volatile write, so a reader sees
    /// either the old entry or the complete new one. Two threads that miss together both resolve and both write; the
    /// services are singletons, so they resolve the same instance and the race is benign.
    /// </remarks>
    internal static class FacadeCache<TService>
        where TService : class
    {
        private static Entry _entry;

        internal static TService Get()
        {
            var provider = CompositionRoot.Current;
            var entry = Volatile.Read(ref _entry);
            if (entry != null && ReferenceEquals(entry.Provider, provider))
                return entry.Instance;

            var instance = provider.GetRequiredService<TService>();
            Volatile.Write(ref _entry, new Entry(provider, instance));
            return instance;
        }

        private sealed class Entry
        {
            public Entry(IServiceProvider provider, TService instance)
            {
                Provider = provider;
                Instance = instance;
            }

            public IServiceProvider Provider { get; }

            public TService Instance { get; }
        }
    }
}
