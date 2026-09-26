using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Decides how <see cref="GuiViewModelBase{TDerived,TPayload}.ChangePartialView"/> applies a
    /// partial. NUI can drop a plain nested layout while the window redraws, leaving the content
    /// area blank, so groups that sit directly in the main view are swapped through the
    /// root-redraw + next-tick reapply path.
    /// </summary>
    public static class GuiPartialViewRouting
    {
        public const string WindowElementId = "_window_";
        public const string MainViewPartial = "%%WINDOW_MAIN%%";

        /// <summary>
        /// True when <paramref name="elementId"/> is a group declared directly in the main view and
        /// the main view is currently showing. The root itself, slots nested inside another partial
        /// (a root redraw would wipe their parent), and swaps while a modal replaces the main view
        /// are applied directly.
        /// </summary>
        public static bool RequiresRedrawSafeSwap(
            string elementId,
            string rootPartial,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> partialElementIds)
        {
            if (elementId == WindowElementId || rootPartial != MainViewPartial || partialElementIds == null)
                return false;

            return partialElementIds.TryGetValue(MainViewPartial, out var mainViewElementIds) &&
                   mainViewElementIds.Contains(elementId);
        }
    }
}
