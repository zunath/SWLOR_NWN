using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiListTemplate: List<GuiListTemplateCell>
    {
        public Json ToJson()
        {
            var template = JsonArray();

            foreach (var cell in this)
            {
                template = JsonArrayInsert(template, cell.ToJson());
            }

            return template;
        }
    }
}
