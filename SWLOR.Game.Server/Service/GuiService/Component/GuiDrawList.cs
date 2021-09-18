using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawList
    {
        private bool IsConstrainedToTargetBounds { get; set; }
        private string IsConstrainedBindName { get; set; }
        private bool IsConstrainedBound => !string.IsNullOrWhiteSpace(IsConstrainedBindName);
        
        private List<GuiDrawListItem> DrawItems { get; set; }

        public GuiDrawList SetIsConstrainedToTargetBounds(bool isConstrained)
        {
            IsConstrainedToTargetBounds = isConstrained;
            return this;
        }

        public GuiDrawList BindIsConstrainedToTargetBounds(string bindName)
        {
            IsConstrainedBindName = bindName;
            return this;
        }

        public GuiDrawList AddArc(Action<GuiDrawListItemArc> arc)
        {
            var newArc = new GuiDrawListItemArc();
            DrawItems.Add(newArc);
            arc(newArc);

            return this;
        }

        public GuiDrawList AddCircle(Action<GuiDrawListItemCircle> circle)
        {
            var newCircle = new GuiDrawListItemCircle();
            DrawItems.Add(newCircle);
            circle(newCircle);

            return this;
        }

        public GuiDrawList AddCurve(Action<GuiDrawListItemCurve> curve)
        {
            var newCurve = new GuiDrawListItemCurve();
            DrawItems.Add(newCurve);
            curve(newCurve);

            return this;
        }

        public GuiDrawList AddImage(Action<GuiDrawListItemImage> image)
        {
            var newImage = new GuiDrawListItemImage();
            DrawItems.Add(newImage);
            image(newImage);

            return this;
        }

        public GuiDrawList AddPolyLine(Action<GuiDrawListItemPolyLine> polyLine)
        {
            var newPolyLine = new GuiDrawListItemPolyLine();
            DrawItems.Add(newPolyLine);
            polyLine(newPolyLine);

            return this;
        }

        public GuiDrawList AddArc(Action<GuiDrawListItemText> text)
        {
            var newText = new GuiDrawListItemText();
            DrawItems.Add(newText);
            text(newText);

            return this;
        }


        public GuiDrawList()
        {
            DrawItems = new List<GuiDrawListItem>();
        }

        public Json ToJson(Json targetElement)
        {
            var isConstrained = IsConstrainedBound ? Nui.Bind(IsConstrainedBindName) : JsonBool(IsConstrainedToTargetBounds);
            var drawList = JsonArray();

            foreach (var item in DrawItems)
            {
                drawList = JsonArrayInsert(drawList, item.ToJson());
            }

            return Nui.DrawList(targetElement, isConstrained, drawList);
        }
    }
}
