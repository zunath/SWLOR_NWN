using System.Threading;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The cached delegate of one <see cref="ContentFactory"/> signature: one static slot per delegate type (<c>Func&lt;A1, ..., T&gt;</c>),
    /// holding the factory that compiled it and the delegate. A call checks the factory by reference and invokes the delegate: no
    /// dictionary lookup and no allocation, as <see cref="FacadeCache{TService}"/> reads a service.
    /// </summary>
    /// <remarks>
    /// The slot belongs to one factory at a time. Another factory (a test's, the reload validation's) misses, takes its own compiled
    /// delegate from its dictionary and takes the slot over, so a slot never serves a factory other than its owner. The slots are
    /// statics of the game build, so they unload with it. Thread safety: the entry is immutable and published with a volatile write;
    /// two factories that miss together both write, and each later call refills for the one that lost.
    /// </remarks>
    internal static class ContentSlot<TDelegate>
        where TDelegate : Delegate
    {
        private static Entry _entry;

        /// <summary>The delegate <paramref name="owner"/> compiled for this signature, or null when the slot holds another factory's.</summary>
        public static TDelegate Get(ContentFactory owner)
        {
            var entry = Volatile.Read(ref _entry);
            return entry != null && ReferenceEquals(entry.Owner, owner) ? entry.Create : null;
        }

        /// <summary>Makes <paramref name="create"/>, compiled by <paramref name="owner"/>, the slot's delegate.</summary>
        public static void Set(ContentFactory owner, TDelegate create)
        {
            Volatile.Write(ref _entry, new Entry(owner, create));
        }

        private sealed class Entry
        {
            public Entry(ContentFactory owner, TDelegate create)
            {
                Owner = owner;
                Create = create;
            }

            public ContentFactory Owner { get; }

            public TDelegate Create { get; }
        }
    }
}
