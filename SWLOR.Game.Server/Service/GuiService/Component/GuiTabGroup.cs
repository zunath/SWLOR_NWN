// ============================================================================
// GuiTabGroup.cs
//
// PROBLEM THIS SOLVES
// --------------------
// Tabbed windows each hand-rolled tab registration (a partial name and a
// refresh action per tab) and a re-entrancy guard (_isSynchronizingTabRows)
// to keep two paired toggle groups and one logical SelectedTabId from
// feeding back into each other. Both are generic, so they live here. Tab
// content goes through SwapNestedPartialView, which composes it into the
// window's root layout (see GuiLayoutComposer).
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    // ------------------------------------------------------------------
    // TAB REGISTRATION - replaces GetTabPartialName + the RefreshSelectedTabData
    // switch statement
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers a set of tabs (id -> partial name -> optional refresh action)
    /// and drives selection through SwapNestedPartialView, so a window author
    /// just describes "what tabs exist".
    /// </summary>
    public class GuiTabGroup<TViewModel, TPayload>
        where TViewModel : GuiViewModelBase<TViewModel, TPayload>
        where TPayload : GuiPayloadBase
    {
        private readonly Dictionary<int, (string PartialId, Action<TViewModel> OnSelected)> _tabs = new();

        public GuiTabGroup<TViewModel, TPayload> AddTab(int tabId, string partialId, Action<TViewModel> onSelected = null)
        {
            _tabs[tabId] = (partialId, onSelected);
            return this;
        }

        public string GetPartialName(int tabId) => _tabs[tabId].PartialId;

        /// <summary>
        /// Applies the given tab: runs its refresh action (if any) then swaps
        /// its partial into the content element.
        /// </summary>
        public void Select(TViewModel model, string contentElementId, int tabId, Action onAfterApply = null)
        {
            if (!_tabs.TryGetValue(tabId, out var tab))
                throw new KeyNotFoundException($"Tab id '{tabId}' was not registered in this GuiTabGroup.");

            model.SwapNestedPartialView(contentElementId, tab.PartialId, () => tab.OnSelected?.Invoke(model), onAfterApply);
        }
    }

    // ------------------------------------------------------------------
    // PAIRED TOGGLE SYNC - replaces TopTabId/BottomTabId/_isSynchronizingTabRows
    // ------------------------------------------------------------------

    /// <summary>
    /// Synchronizes N independent toggle-pair properties (e.g. TopTabId,
    /// BottomTabId) against one logical selected-tab id, without a hand-written
    /// re-entrancy flag per window. Each toggle group maps its local index
    /// (0, 1, ...) to a shared tab id; selecting a tab drives all groups to
    /// the right local index (or -1 if the tab isn't in that group), and a
    /// toggle change routes back to the shared SelectTab call.
    /// </summary>
    public class GuiToggleGroupSync
    {
        private readonly List<int> _tabIdsInOrder;
        private bool _isSyncing;

        public GuiToggleGroupSync(params int[] tabIdsInOrder)
        {
            _tabIdsInOrder = tabIdsInOrder.ToList();
        }

        /// <summary>Local toggle index for a given tab id, or -1 if this group doesn't contain it.</summary>
        public int LocalIndexFor(int tabId) => _tabIdsInOrder.IndexOf(tabId);

        /// <summary>Tab id for a given local toggle index.</summary>
        public int TabIdFor(int localIndex) => _tabIdsInOrder[localIndex];

        /// <summary>
        /// Wraps a toggle-group's setter so it ignores changes caused by
        /// programmatic sync (avoiding the feedback loop _isSynchronizingTabRows
        /// exists to prevent) and only forwards genuine user clicks.
        /// </summary>
        public void HandleClientChange(int localIndex, Action<int> onUserSelectedTab)
        {
            if (_isSyncing || localIndex < 0 || localIndex >= _tabIdsInOrder.Count)
                return;

            onUserSelectedTab(TabIdFor(localIndex));
        }

        /// <summary>Call when driving the toggle group's value programmatically (not from user input).</summary>
        public void SyncTo(int tabId, Action<int> setLocalIndex)
        {
            _isSyncing = true;
            try
            {
                setLocalIndex(LocalIndexFor(tabId));
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
