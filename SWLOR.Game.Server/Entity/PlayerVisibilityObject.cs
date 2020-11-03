using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerVisibilityObject: EntityBase
    {
        public PlayerVisibilityObject()
        {
            ObjectVisibilities = new Dictionary<string, VisibilityType>();
        }
        public override string KeyPrefix => "PlayerVisibilityObject";
        public Dictionary<string, VisibilityType> ObjectVisibilities { get; set; }
    }
}
