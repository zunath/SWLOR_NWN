using SWLOR.Shared.Abstractions;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Enums;

namespace SWLOR.Shared.UI.Entity
{
    public class PlayerWindowGeometry: EntityBase
    {
        public PlayerWindowGeometry()
        {
        }

        public PlayerWindowGeometry(string playerId)
        {
            Id = playerId;
        }

        public Dictionary<GuiWindowType, GuiRectangle> WindowGeometries { get; set; } = new();
    }
}
