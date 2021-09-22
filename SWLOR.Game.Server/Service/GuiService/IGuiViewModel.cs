using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiViewModel
    {
        public GuiRectangle Geometry { get; set; }
        void Bind(uint player, int windowToken);
        void UpdatePropertyFromClient(string propertyName);
    }
}
