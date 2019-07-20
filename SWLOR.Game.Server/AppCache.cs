using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Skill;

namespace SWLOR.Game.Server
{
    public static class AppCache
    {
        public static Dictionary<Guid, PlayerDialog> PlayerDialogs { get; }
        public static Dictionary<int, bool> DialogFilesInUse { get; }
        public static Dictionary<string, int> EffectTicks { get; }
        public static Dictionary<Guid, CreatureSkillRegistration> CreatureSkillRegistrations;
        public static Dictionary<CasterSpellVO, int> NPCEffects { get; }
        public static Queue<string> UnregisterProcessingEvents { get; set; }
        public static Dictionary<Guid, EnmityTable> NPCEnmityTables { get; set; }
        public static Dictionary<Guid, CustomData> CustomObjectData { get; set; } 
        public static Dictionary<string, NWObject> VisibilityObjects { get; set; }
        public static List<Guid> PCEffectsForRemoval { get; set; }
        public static List<NWObject> ConnectedDMs { get; set; }
        public static Dictionary<Guid, Dictionary<int, int>> PlayerEffectivePerkLevels { get; set; }

        static AppCache()
        {
            PlayerDialogs = new Dictionary<Guid, PlayerDialog>();
            DialogFilesInUse = new Dictionary<int, bool>();
            EffectTicks = new Dictionary<string, int>();
            CreatureSkillRegistrations = new Dictionary<Guid, CreatureSkillRegistration>();
            NPCEffects = new Dictionary<CasterSpellVO, int>();
            UnregisterProcessingEvents = new Queue<string>();
            NPCEnmityTables = new Dictionary<Guid, EnmityTable>();
            CustomObjectData = new Dictionary<Guid, CustomData>();
            VisibilityObjects = new Dictionary<string, NWObject>();
            PCEffectsForRemoval = new List<Guid>();
            ConnectedDMs = new List<NWObject>();
            PlayerEffectivePerkLevels = new Dictionary<Guid, Dictionary<int, int>>();

            for (int x = 1; x <= DialogService.NumberOfDialogs; x++)
            {
                DialogFilesInUse.Add(x, false);
            }
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => Clean());
        }

        private static void Clean()
        {
            foreach (var npcTable in NPCEnmityTables.ToArray())
            {
                if (!npcTable.Value.NPCObject.IsValid)
                {
                    NPCEnmityTables.Remove(npcTable.Key);
                }
            }
            foreach (var customData in CustomObjectData.ToArray())
            {
                NWObject owner = customData.Value.Owner;
                if (!owner.IsValid)
                {
                    CustomObjectData.Remove(customData.Key);
                }
            }

        }

    }
}
