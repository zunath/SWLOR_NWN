using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWindowListDefinition
    {
        Dictionary<GuiWindowType, GuiWindow> BuildWindows();
    }
}
