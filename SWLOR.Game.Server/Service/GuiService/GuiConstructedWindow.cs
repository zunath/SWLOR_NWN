using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }
        public GuiRectangle InitialGeometry { get; set; }
        public Dictionary<string, Json> PartialViews { get; set; }
        public IReadOnlyList<string> LayoutFindings { get; set; }

        /// <summary>
        /// The JSON text of each partial view, keyed by partial name. Slot layouts are composed
        /// into the root from these (see <see cref="GuiLayoutComposer"/>). Empty in
        /// validation-only builds.
        /// </summary>
        public IReadOnlyDictionary<string, string> PartialViewLayouts { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type,
            string windowId,
            Json window,
            GuiRectangle initialGeometry,
            Dictionary<string, Json> partialViews,
            IReadOnlyList<string> layoutFindings,
            IReadOnlyDictionary<string, string> partialViewLayouts,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            InitialGeometry = initialGeometry;
            PartialViews = partialViews;
            LayoutFindings = layoutFindings;
            PartialViewLayouts = partialViewLayouts;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}
