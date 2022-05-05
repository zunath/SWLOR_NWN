using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawList<T>
        where T: IGuiViewModel
    {
        private bool IsConstrainedToTargetBounds { get; set; }
        private string IsConstrainedBindName { get; set; }
        private bool IsConstrainedBound => !string.IsNullOrWhiteSpace(IsConstrainedBindName);
        
        private List<IGuiDrawListItem> DrawItems { get; set; }

        /// <summary>
        /// Determines whether the draw list can be drawn outside the bounds of the targeted control.
        /// </summary>
        /// <param name="isConstrained">true if drawing should only be within the bounds of the targeted control.</param>
        public GuiDrawList<T> SetIsConstrainedToTargetBounds(bool isConstrained)
        {
            IsConstrainedToTargetBounds = isConstrained;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value indicating whether the draw list can be drawn outside the bounds of the targeted control.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawList<T> BindIsConstrainedToTargetBounds<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsConstrainedBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Adds an arc to the draw list.
        /// </summary>
        /// <param name="arc">The arc to draw.</param>
        public GuiDrawList<T> AddArc(Action<GuiDrawListItemArc<T>> arc)
        {
            var newArc = new GuiDrawListItemArc<T>();
            DrawItems.Add(newArc);
            arc(newArc);

            return this;
        }

        /// <summary>
        /// Adds a circle to the draw list.
        /// </summary>
        /// <param name="circle">The circle to draw.</param>
        public GuiDrawList<T> AddCircle(Action<GuiDrawListItemCircle<T>> circle)
        {
            var newCircle = new GuiDrawListItemCircle<T>();
            DrawItems.Add(newCircle);
            circle(newCircle);

            return this;
        }

        /// <summary>
        /// Adds a curve to the draw list.
        /// </summary>
        /// <param name="curve">The curve to draw.</param>
        public GuiDrawList<T> AddCurve(Action<GuiDrawListItemCurve<T>> curve)
        {
            var newCurve = new GuiDrawListItemCurve<T>();
            DrawItems.Add(newCurve);
            curve(newCurve);

            return this;
        }

        /// <summary>
        /// Adds an image to the draw list.
        /// </summary>
        /// <param name="image">The image to draw.</param>
        public GuiDrawList<T> AddImage(Action<GuiDrawListItemImage<T>> image)
        {
            var newImage = new GuiDrawListItemImage<T>();
            DrawItems.Add(newImage);
            image(newImage);

            return this;
        }

        /// <summary>
        /// Adds a poly line to the draw list.
        /// </summary>
        /// <param name="polyLine">The poly line to draw.</param>
        public GuiDrawList<T> AddPolyLine(Action<GuiDrawListItemPolyLine<T>> polyLine)
        {
            var newPolyLine = new GuiDrawListItemPolyLine<T>();
            DrawItems.Add(newPolyLine);
            polyLine(newPolyLine);

            return this;
        }

        /// <summary>
        /// Adds text to the draw list.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        public GuiDrawList<T> AddText(Action<GuiDrawListItemText<T>> text)
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

        /// <summary>
        /// Builds the GuiDrawList element.
        /// </summary>
        /// <param name="targetElement">The element which will be drawn upon.</param>
        /// <returns>Json representing the draw list element.</returns>
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
