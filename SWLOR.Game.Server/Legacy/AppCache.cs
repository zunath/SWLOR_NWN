using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Legacy.ValueObject.Skill;

namespace SWLOR.Game.Server.Legacy
{
    public static class AppCache
    {
        public static Dictionary<Guid, PlayerDialog> PlayerDialogs { get; }
        public static Dictionary<int, bool> DialogFilesInUse { get; }
        public static Dictionary<Guid, CreatureSkillRegistration> CreatureSkillRegistrations;
        public static Dictionary<Guid, CustomData> CustomObjectData { get; set; } 
        public static Dictionary<Guid, Dictionary<int, int>> PlayerEffectivePerkLevels { get; set; }
    }
}
