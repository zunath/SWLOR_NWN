using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public delegate GuiPlayerWindow CreatePlayerWindowDelegate(uint player);
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }

        public GuiConstructedWindow(GuiWindowType type, string windowId, Json window, CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            CreatePlayerWindowAction = createPlayerWindowAction;
        }
    }
}
