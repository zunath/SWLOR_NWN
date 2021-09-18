using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiColumn: GuiWidget
    {
        public List<GuiRow> Rows { get; set; }

        public GuiColumn()
        {
            Rows = new List<GuiRow>();
        }

        public override Json BuildElement()
        {
            var column = JsonArray();

            foreach (var row in Rows)
            {
                column = JsonArrayInsert(column, row.ToJson());
            }

            return Nui.Column(column);
        }
    }
}
