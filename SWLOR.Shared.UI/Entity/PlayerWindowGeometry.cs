using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Component;

namespace SWLOR.Shared.UI.Entity
{
    public class PlayerWindowGeometry: EntityBase
    {
        public PlayerWindowGeometry()
        {
            WindowGeometries = new Dictionary<GuiWindowType, GuiRectangle>();
        }

        public PlayerWindowGeometry(string playerId)
        {
            Id = playerId;
        }

        public Dictionary<GuiWindowType, GuiRectangle> WindowGeometries { get; set; }
    }
}
