using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiViewModel
    {
        void Bind(uint player, int windowToken);
        void UpdatePropertyFromClient(string propertyName);
    }
}
