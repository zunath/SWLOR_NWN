using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;

namespace SWLOR.Game.Server.Legacy
{
    public static class AppCache
    {
        public static Dictionary<Guid, PlayerDialog> PlayerDialogs { get; }
        public static Dictionary<int, bool> DialogFilesInUse { get; }
        public static Dictionary<Guid, CustomData> CustomObjectData { get; set; } 
    }
}
