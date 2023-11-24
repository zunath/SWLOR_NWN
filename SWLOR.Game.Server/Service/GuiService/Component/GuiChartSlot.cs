using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiChartSlot<T>
        where T: IGuiViewModel
    {
        private NuiChartType Type { get; set; }
        
        private string Legend { get; set; }
        private string LegendBindName { get; set; }
        private bool IsLegendBound => !string.IsNullOrWhiteSpace(LegendBindName);
        
        private GuiColor Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);
        
        private List<float> Data { get; set; }
        private string DataBindName { get; set; }
        private bool IsDataBound => !string.IsNullOrWhiteSpace(DataBindName);

        /// <summary>
        /// Sets a static value for the chart type.
        /// </summary>
        /// <param name="type">The type of chart.</param>
        public GuiChartSlot<T> SetType(NuiChartType type)
        {
            Type = type;
            return this;
        }

        /// <summary>
        /// Sets a static value for the legend.
        /// </summary>
        /// <param name="legend">The value to set for the legend.</param>
        public GuiChartSlot<T> SetLegend(string legend)
        {
            Legend = legend;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the legend.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiChartSlot<T> BindLegend<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LegendBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the color.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiChartSlot<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        /// <summary>
        /// Sets a static value for the color.
        /// </summary>
        /// <param name="red">The red value to use (between 0-255)</param>
        /// <param name="green">The green value to use (between 0-255)</param>
        /// <param name="blue">The blue value to use (between 0-255)</param>
        /// <param name="alpha">The alpha value to use (between 0-255)</param>
        public GuiChartSlot<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the color property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiChartSlot<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Adds a data point to the chart.
        /// </summary>
        /// <param name="data">The data point to add.</param>
        public GuiChartSlot<T> AddDataPoint(float data)
        {
            Data.Add(data);
            return this;
        }

        /// <summary>
        /// Binds a set of data to the chart. The data should be a BindingList of float
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiChartSlot<T> BindData<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            DataBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiChartSlot()
        {
            Data = new List<float>();
            Color = new GuiColor(0, 0, 0);
        }

        /// <summary>
        /// Builds a GuiChartSlot element.
        /// </summary>
        /// <returns>Json representing the chart slot.</returns>
        public Json ToJson()
        {
            var legend = IsLegendBound ? Nui.Bind(LegendBindName) : JsonString(Legend);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var data = JsonArray();

            if (IsDataBound)
            {
                data = Nui.Bind(DataBindName);
            }
            else
            {
                foreach (var d in Data)
                {
                    data = JsonArrayInsert(data, JsonFloat(d));
                }
            }

            return Nui.ChartSlot(Type, legend, color, data);
        }
    }
}
