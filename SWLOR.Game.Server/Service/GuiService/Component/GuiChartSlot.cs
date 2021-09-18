using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiChartSlot
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

        public GuiChartSlot SetType(NuiChartType type)
        {
            Type = type;
            return this;
        }

        public GuiChartSlot SetLegend(string legend)
        {
            Legend = legend;
            return this;
        }

        public GuiChartSlot BindLegend(string bindName)
        {
            LegendBindName = bindName;
            return this;
        }

        public GuiChartSlot SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiChartSlot SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiChartSlot BindColor(string bindName)
        {
            ColorBindName = bindName;
            return this;
        }

        public GuiChartSlot AddDataPoint(float data)
        {
            Data.Add(data);
            return this;
        }

        public GuiChartSlot BindData(string bindName)
        {
            DataBindName = bindName;
            return this;
        }

        public GuiChartSlot()
        {
            Data = new List<float>();
            Color = new GuiColor(0, 0, 0);
        }

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
