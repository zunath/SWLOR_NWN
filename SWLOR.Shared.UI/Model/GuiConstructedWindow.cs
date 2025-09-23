using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Delegates;

namespace SWLOR.Shared.UI.Model
{
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }
        public GuiRectangle InitialGeometry { get; set; }
        public Dictionary<string, Json> PartialViews { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type, 
            string windowId, 
            Json window,
            GuiRectangle initialGeometry,
            Dictionary<string, Json> partialViews,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            InitialGeometry = initialGeometry;
            PartialViews = partialViews;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}
