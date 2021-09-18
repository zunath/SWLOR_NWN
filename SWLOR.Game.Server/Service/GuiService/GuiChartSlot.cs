using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiChartSlot
    {
        public NuiChartType Type { get; set; }

        public string Legend { get; set; }
        public string LegendBindName { get; set; }
        public bool IsLegendBound => !string.IsNullOrWhiteSpace(LegendBindName);

        public GuiColor Color { get; set; }
        public string ColorBindName { get; set; }
        public bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public List<float> Data { get; set; }
        public string DataBindName { get; set; }
        public bool IsDataBound => !string.IsNullOrWhiteSpace(DataBindName);

        public GuiChartSlot()
        {
            Data = new List<float>();
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
