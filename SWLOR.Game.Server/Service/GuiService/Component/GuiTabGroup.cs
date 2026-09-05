// ============================================================================
// GuiTabGroup.cs
//
// PROBLEM THIS SOLVES
// --------------------
// CharacterSheetViewModel.RestoreSelectedTabPartial() layers a SECOND,
// manual redraw workaround on top of a fix ChangePartialView already does
// automatically (GuiViewModelBase.ApplyRefreshBugFix nudges Geometry.Height
// on every ChangePartialView call). The manual layer exists specifically
// because *nested* partials (a partial swapped inside another partial's
// content area) can still get dropped mid-redraw - per the existing code
// comment: "NUI can drop nested partial layouts while its parent is being
// redrawn." The workaround is: force a full root redraw, apply the target
// partial, then reapply it again one tick later.
//
// Separately, TopTabId/BottomTabId require a hand-written re-entrancy guard
// (_isSynchronizingTabRows) purely to keep two paired toggle groups and one
// logical SelectedTabId from feeding back into each other.
//
// Both of these are generic problems, not specific to the character sheet.
// This file moves them into reusable helpers so a new window author doesn't
// need to know the nested-partial bug exists at all.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    // ------------------------------------------------------------------
    // GuiViewModelBase<TDerived, TPayload> ALREADY HAS SwapNestedPartialView
    // as of the patch applied directly to that file (see the diff added
    // right after ChangePartialView). GuiTabGroup.Select below calls it
    // directly - TViewModel must derive from a GuiViewModelBase to expose it.
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // TAB REGISTRATION - replaces GetTabPartialName + the RefreshSelectedTabData
    // switch statement + RestoreSelectedTabPartial's manual sequencing
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers a set of tabs (id -> partial name -> optional refresh action)
    /// and drives selection through the nested-partial-safe swap path, so a
    /// window author just describes "what tabs exist" instead of re-deriving
    /// the redraw sequencing per window.
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
        /// the nested partial in via the safe double-reapply path.
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
