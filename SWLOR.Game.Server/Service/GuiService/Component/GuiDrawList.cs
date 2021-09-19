using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawList<T>
        where T: IGuiDataModel
    {
        private bool IsConstrainedToTargetBounds { get; set; }
        private string IsConstrainedBindName { get; set; }
        private bool IsConstrainedBound => !string.IsNullOrWhiteSpace(IsConstrainedBindName);
        
        private List<IGuiDrawListItem> DrawItems { get; set; }

        public GuiDrawList<T> SetIsConstrainedToTargetBounds(bool isConstrained)
        {
            IsConstrainedToTargetBounds = isConstrained;
            return this;
        }

        public GuiDrawList<T> BindIsConstrainedToTargetBounds(string bindName)
        {
            IsConstrainedBindName = bindName;
            return this;
        }

        public GuiDrawList<T> AddArc(Action<GuiDrawListItemArc<T>> arc)
        {
            var newArc = new GuiDrawListItemArc<T>();
            DrawItems.Add(newArc);
            arc(newArc);

            return this;
        }

        public GuiDrawList<T> AddCircle(Action<GuiDrawListItemCircle<T>> circle)
        {
            var newCircle = new GuiDrawListItemCircle<T>();
            DrawItems.Add(newCircle);
            circle(newCircle);

            return this;
        }

        public GuiDrawList<T> AddCurve(Action<GuiDrawListItemCurve<T>> curve)
        {
            var newCurve = new GuiDrawListItemCurve<T>();
            DrawItems.Add(newCurve);
            curve(newCurve);

            return this;
        }

        public GuiDrawList<T> AddImage(Action<GuiDrawListItemImage<T>> image)
        {
            var newImage = new GuiDrawListItemImage<T>();
            DrawItems.Add(newImage);
            image(newImage);

            return this;
        }

        public GuiDrawList<T> AddPolyLine(Action<GuiDrawListItemPolyLine<T>> polyLine)
        {
            var newPolyLine = new GuiDrawListItemPolyLine<T>();
            DrawItems.Add(newPolyLine);
            polyLine(newPolyLine);

            return this;
        }

        public GuiDrawList<T> AddArc(Action<GuiDrawListItemText<T>> text)
        {
            var newText = new GuiDrawListItemText<T>();
            DrawItems.Add(newText);
            text(newText);

            return this;
        }


        public GuiDrawList()
        {
            DrawItems = new List<IGuiDrawListItem>();
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
