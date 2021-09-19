using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindowEvents<TDataModel>: Dictionary<string, GuiEventDelegate<TDataModel>>
        where TDataModel: IGuiViewModel
    {

    }
}
