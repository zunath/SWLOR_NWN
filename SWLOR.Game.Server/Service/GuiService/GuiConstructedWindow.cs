using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }
        public GuiRectangle Geometry { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type, 
            string windowId, 
            Json window,
            GuiRectangle geometry,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            Geometry = geometry;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}
