using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiListTemplate<T>: GuiExpandableComponent<T>
        where T: IGuiViewModel
    {
        public override Json BuildElement()
        {
            throw new NotSupportedException();
        }

        public override Json ToJson()
        {
            var template = JsonArray();

            foreach (var element in Elements)
            {
                var json = element.ToJson();
                template = JsonArrayInsert(template, Nui.ListTemplateCell(json, 0f, true));
            }

            return template;
        }
    }
}
