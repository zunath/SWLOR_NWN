using System.Collections.Generic;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiConstructedWindow
    {
        public GuiWindowType Type { get; set; }
        public string WindowId { get; set; }
        public Json Window { get; set; }
        public CreatePlayerWindowDelegate CreatePlayerWindowAction { get; set; }
        public Dictionary<string, Dictionary<string, GuiEventDelegate>> Events { get; set; }

        public GuiConstructedWindow(
            GuiWindowType type, 
            string windowId, 
            Json window,
            Dictionary<string, Dictionary<string, GuiEventDelegate>> events,
            CreatePlayerWindowDelegate createPlayerWindowAction)
        {
            Type = type;
            WindowId = windowId;
            Window = window;
            CreatePlayerWindowAction = createPlayerWindowAction;
            Events = events;
        }
    }
}
