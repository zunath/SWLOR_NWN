using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemPolyLine<T>: GuiDrawListItem<T, GuiDrawListItemPolyLine<T>>, IGuiDrawListItem
        where T: IGuiViewModel
    {
        private GuiColor Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);
        
        private bool IsFilled { get; set; }
        private string IsFilledBindName { get; set; }
        private bool IsFilledBound => !string.IsNullOrWhiteSpace(IsFilledBindName);
        
        private float LineThickness { get; set; }
        private string LineThicknessBindName { get; set; }
        private bool IsLineThicknessBound => !string.IsNullOrWhiteSpace(LineThicknessBindName);
        
        private List<GuiVector2> Points { get; set; }
        private string PointsBindName { get; set; }
        private bool IsPointsBound => !string.IsNullOrWhiteSpace(PointsBindName);

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiDrawListItemPolyLine<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }


        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="red">The amount of red to use. 0-255</param>
        /// <param name="green">The amount of green to use. 0-255</param>
        /// <param name="blue">The amount of blue to use. 0-255</param>
        /// <param name="alpha">The amount of alpha to use. 0-255</param>
        public GuiDrawListItemPolyLine<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemPolyLine<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Filled property.
        /// </summary>
        /// <param name="isFilled">true if filled, false otherwise</param>
        public GuiDrawListItemPolyLine<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the Filled property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemPolyLine<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the line thickness.
        /// </summary>
        /// <param name="lineThickness">The thickness to use.</param>
        public GuiDrawListItemPolyLine<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the line thickness.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemPolyLine<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Adds a point to the poly line.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public GuiDrawListItemPolyLine<T> AddPoint(float x, float y)
        {
            Points.Add(new GuiVector2(x, y));
            return this;
        }

        /// <summary>
        /// Adds a point to the poly line.
        /// </summary>
        /// <param name="point">The X and Y coordinates</param>
        public GuiDrawListItemPolyLine<T> AddPoint(GuiVector2 point)
        {
            Points.Add(point);
            return this;
        }

        /// <summary>
        /// Binds a list of points to the poly line.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemPolyLine<T> BindPoints<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PointsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemPolyLine()
        {
            Points = new List<GuiVector2>();
            Color = new GuiColor(0, 0, 0);
        }

        /// <summary>
        /// Builds a GuiDrawListItemPolyLine element.
        /// </summary>
        /// <returns>Json representing the poly line draw list item.</returns>
        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var isFilled = IsFilledBound ? Nui.Bind(IsFilledBindName) : JsonBool(IsFilled);
            var lineThickness = IsLineThicknessBound ? Nui.Bind(LineThicknessBindName) : JsonFloat(LineThickness);
            var points = JsonArray();

            if (IsPointsBound)
            {
                points = Nui.Bind(PointsBindName);
            }
            else
            {
                foreach (var point in Points)
                {
                    points = JsonArrayInsert(points, point.ToJson());
                }
            }

            return Nui.DrawListPolyLine(isEnabled, color, isFilled, lineThickness, points);
        }
    }
}
