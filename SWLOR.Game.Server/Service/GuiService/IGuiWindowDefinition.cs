using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWindowDefinition<T>
        where T: IGuiDataModel
    {
        (GuiWindowType, GuiWindow<T>) BuildWindow();
    }
}
