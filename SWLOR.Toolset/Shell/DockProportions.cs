using System.ComponentModel;
using Dock.Model.Core;

namespace SWLOR.Toolset.Shell
{
    /// <summary>
    /// Reads and writes where a dock layout's dividers sit, keyed by dock Id.
    /// </summary>
    /// <remarks>
    /// Only the proportions are carried, not the layout tree. The panels are DI-resolved singletons the
    /// rest of the app talks to, so re-hydrating a serialized layout would replace them with fresh
    /// instances nothing else is wired to; and an Id that has since left the layout is simply never
    /// looked up, so a layout rearranged in code is not held back by what is on disk.
    /// <para>
    /// Dock writes a dragged splitter straight back into <see cref="IDockable.Proportion"/>, which is what
    /// makes reading that property enough to know where a divider was left.
    /// </para>
    /// </remarks>
    public static class DockProportions
    {
        /// <summary>
        /// Applies saved proportions to every dock in <paramref name="root"/> that has one. Docks with no
        /// saved entry keep whatever the layout gave them, which is how a new panel gets its designed size.
        /// </summary>
        public static void Apply(IDockable? root, IReadOnlyDictionary<string, double>? saved)
        {
            if (root == null || saved == null || saved.Count == 0)
                return;

            foreach (var dockable in Walk(root))
            {
                if (!string.IsNullOrEmpty(dockable.Id) &&
                    saved.TryGetValue(dockable.Id, out var proportion) &&
                    proportion > 0 && proportion < 1)
                {
                    dockable.Proportion = proportion;
                }
            }
        }

        /// <summary>
        /// Where every divider is now. Docks with no Id (Dock's own splitters) and docks that were never
        /// given a proportion are left out rather than recorded as NaN.
        /// </summary>
        public static IReadOnlyDictionary<string, double> Capture(IDockable? root)
        {
            var captured = new Dictionary<string, double>(StringComparer.Ordinal);
            if (root == null)
                return captured;

            foreach (var dockable in Walk(root))
            {
                if (!string.IsNullOrEmpty(dockable.Id) && double.IsFinite(dockable.Proportion))
                    captured[dockable.Id] = dockable.Proportion;
            }

            return captured;
        }

        /// <summary>
        /// Calls <paramref name="onChanged"/> whenever a divider in <paramref name="root"/> moves.
        /// </summary>
        public static void Watch(IDockable? root, Action onChanged)
        {
            if (root == null)
                return;

            foreach (var dockable in Walk(root))
            {
                if (string.IsNullOrEmpty(dockable.Id) || dockable is not INotifyPropertyChanged notifier)
                    continue;

                notifier.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(IDockable.Proportion))
                        onChanged();
                };
            }
        }

        /// <summary>Every dockable in the layout tree, the root included.</summary>
        public static IEnumerable<IDockable> Walk(IDockable dockable)
        {
            yield return dockable;

            if (dockable is not IDock dock || dock.VisibleDockables == null)
                yield break;

            foreach (var child in dock.VisibleDockables)
            {
                foreach (var descendant in Walk(child))
                    yield return descendant;
            }
        }
    }
}
