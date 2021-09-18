using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawList: GuiWidget
    {
        public GuiWidget Target { get; set; }

        public bool IsConstrainedToTargetBounds { get; set; }
        public string IsConstrainedBindName { get; set; }
        public bool IsConstrainedBound => !string.IsNullOrWhiteSpace(IsConstrainedBindName);

        public List<GuiDrawListItem> DrawItems { get; set; }

        public GuiDrawList()
        {
            DrawItems = new List<GuiDrawListItem>();
        }


        public override Json BuildElement()
        {
            var target = Target.ToJson();
            var isConstrained = IsConstrainedBound ? Nui.Bind(IsConstrainedBindName) : JsonBool(IsConstrainedToTargetBounds);
            var drawItems = JsonArray();

            foreach (var item in DrawItems)
            {
                drawItems = JsonArrayInsert(drawItems, item.ToJson());
            }

            return Nui.DrawList(target, isConstrained, drawItems);
        }
    }
}
