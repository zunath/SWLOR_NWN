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
        /// The element ids declared inside each partial view, keyed by partial name. Used to
        /// work out which nested layouts are still on screen after a parent layout changes.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> PartialElementIds { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type,
            string windowId,
            Json window,
            GuiRectangle initialGeometry,
            Dictionary<string, Json> partialViews,
            IReadOnlyList<string> layoutFindings,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> partialElementIds,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            InitialGeometry = initialGeometry;
            PartialViews = partialViews;
            LayoutFindings = layoutFindings;
            PartialElementIds = partialElementIds;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}
