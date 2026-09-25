using System;
using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Tracks which layout is applied to a window's root and to each nested group element, so
    /// the whole layout tree can be re-applied after NUI drops part of it. NUI discards every
    /// nested layout whenever an ancestor is redrawn (a root swap, a modal closing, a parent
    /// partial being replaced) and can also drop a freshly applied nested layout mid-redraw,
    /// leaving the content area blank.
    /// </summary>
    public sealed class GuiPartialViewState
    {
        public const string WindowElementId = "_window_";
        public const string MainViewPartial = "%%WINDOW_MAIN%%";

        private readonly List<GuiNestedLayout> _nestedLayouts = new();
        private readonly Func<string, IReadOnlyCollection<string>> _getPartialElementIds;

        /// <param name="getPartialElementIds">
        /// Returns the element ids declared inside a registered partial view, or null when unknown.
        /// </param>
        public GuiPartialViewState(Func<string, IReadOnlyCollection<string>> getPartialElementIds)
        {
            _getPartialElementIds = getPartialElementIds;
        }

        /// <summary>
        /// The partial currently applied to the window root, or null before the window is bound.
        /// </summary>
        public string RootPartial { get; private set; }

        /// <summary>
        /// True when the root shows the window's main view rather than a modal or other root partial.
        /// Nested layouts only exist on screen while this is true.
        /// </summary>
        public bool IsMainViewShown => RootPartial == MainViewPartial;

        /// <summary>
        /// Nested layouts in the order they must be applied: every parent precedes the
        /// layouts nested inside it.
        /// </summary>
        public IReadOnlyList<GuiNestedLayout> NestedLayouts => _nestedLayouts;

        public bool IsTracked(string elementId)
        {
            return _nestedLayouts.Exists(layout => layout.ElementId == elementId);
        }

        public void Reset()
        {
            RootPartial = null;
            _nestedLayouts.Clear();
        }

        public void SetRootPartial(string partialName)
        {
            RootPartial = partialName;
        }

        public void SetNestedPartial(string elementId, string partialName)
        {
            SetNestedLayout(new GuiNestedLayout(elementId, partialName, null));
        }

        public void SetNestedLayout(string elementId, Json layout)
        {
            SetNestedLayout(new GuiNestedLayout(elementId, null, layout));
        }

        private void SetNestedLayout(GuiNestedLayout layout)
        {
            var index = _nestedLayouts.FindIndex(existing => existing.ElementId == layout.ElementId);
            if (index >= 0)
            {
                var previous = _nestedLayouts[index];
                _nestedLayouts[index] = layout;

                // A replaced layout starts with empty child slots, so forget what was nested
                // inside the old one. Re-applying the same partial keeps its children.
                if (previous.PartialName == null || previous.PartialName != layout.PartialName)
                    RemoveLayoutsNestedIn(previous, index);
            }
            else
            {
                _nestedLayouts.Add(layout);
            }

            RemoveUnreachableLayouts();
        }

        private void RemoveLayoutsNestedIn(GuiNestedLayout parent, int parentIndex)
        {
            if (parent.PartialName == null)
                return;

            var nestedIds = new HashSet<string>(_getPartialElementIds(parent.PartialName) ?? Array.Empty<string>());
            for (var index = parentIndex + 1; index < _nestedLayouts.Count;)
            {
                var layout = _nestedLayouts[index];
                if (!nestedIds.Contains(layout.ElementId))
                {
                    index++;
                    continue;
                }

                if (layout.PartialName != null)
                    nestedIds.UnionWith(_getPartialElementIds(layout.PartialName) ?? Array.Empty<string>());

                _nestedLayouts.RemoveAt(index);
            }
        }

        /// <summary>
        /// Drops layouts whose element no longer exists, e.g. a child slot of a parent partial
        /// that has since been replaced. Re-applying those would target a missing element id.
        /// </summary>
        private void RemoveUnreachableLayouts()
        {
            var reachable = new HashSet<string>(_getPartialElementIds(MainViewPartial) ?? Array.Empty<string>());
            var kept = new List<GuiNestedLayout>();

            foreach (var layout in _nestedLayouts)
            {
                if (!reachable.Contains(layout.ElementId))
                    continue;

                kept.Add(layout);

                // Runtime-generated layouts are opaque, so nothing nested inside them is tracked.
                if (layout.PartialName != null)
                    reachable.UnionWith(_getPartialElementIds(layout.PartialName) ?? Array.Empty<string>());
            }

            _nestedLayouts.Clear();
            _nestedLayouts.AddRange(kept);
        }
    }
}
