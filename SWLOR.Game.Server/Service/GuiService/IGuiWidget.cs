using System.Collections.Generic;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWidget
    {
        string Id { get; }
        Dictionary<string, GuiEventDelegate> Events { get; }
        Json ToJson();
    }
}
