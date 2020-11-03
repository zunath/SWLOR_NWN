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
        public static Dictionary<CasterSpellVO, int> NPCEffects { get; }
        public static Queue<string> UnregisterProcessingEvents { get; set; }
        public static Dictionary<Guid, EnmityTable> NPCEnmityTables { get; set; }
        public static Dictionary<Guid, CustomData> CustomObjectData { get; set; } 
        public static List<Guid> PCEffectsForRemoval { get; set; }
        public static List<NWObject> ConnectedDMs { get; set; }
        public static Dictionary<Guid, Dictionary<int, int>> PlayerEffectivePerkLevels { get; set; }

        static AppCache()
        {
            PlayerDialogs = new Dictionary<Guid, PlayerDialog>();
            DialogFilesInUse = new Dictionary<int, bool>();
            CreatureSkillRegistrations = new Dictionary<Guid, CreatureSkillRegistration>();
            NPCEffects = new Dictionary<CasterSpellVO, int>();
            UnregisterProcessingEvents = new Queue<string>();
            NPCEnmityTables = new Dictionary<Guid, EnmityTable>();
            CustomObjectData = new Dictionary<Guid, CustomData>();
            PCEffectsForRemoval = new List<Guid>();
            ConnectedDMs = new List<NWObject>();
            PlayerEffectivePerkLevels = new Dictionary<Guid, Dictionary<int, int>>();

            for (var x = 1; x <= DialogService.NumberOfDialogs; x++)
            {
                DialogFilesInUse.Add(x, false);
            }
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => Clean());
            MessageHub.Instance.Subscribe<OnRequestCacheStats>(message => OnRequestCacheStats(message.Player));
        }

        private static void OnRequestCacheStats(NWPlayer player)
        {
            player.SendMessage("PlayerDialogs: " + PlayerDialogs.Count);
            player.SendMessage("DialogFilesInUse: " + DialogFilesInUse.Count);
            player.SendMessage("CreatureSkillRegistrations: " + CreatureSkillRegistrations.Count);
            player.SendMessage("NPCEffects: " + NPCEffects.Count);
            player.SendMessage("UnregisterProcessingEvents: " + UnregisterProcessingEvents.Count);
            player.SendMessage("NPCEnmityTables: " + NPCEnmityTables.Count);
            player.SendMessage("CustomObjectData: " + CustomObjectData.Count);
            player.SendMessage("PCEffectsForRemoval: " + PCEffectsForRemoval.Count);
            player.SendMessage("ConnectedDMs: " + ConnectedDMs.Count);
            player.SendMessage("PlayerEffectivePerkLevels: " + PlayerEffectivePerkLevels.Count);
        }

        private static void Clean()
        {
            for(var index = NPCEnmityTables.Count-1; index >= 0; index--)
            {
                var npcTable = NPCEnmityTables.ElementAt(index);
                if (!npcTable.Value.NPCObject.IsValid)
                {
                    NPCEnmityTables.Remove(npcTable.Key);
                }
            }
            
            for(var index = CustomObjectData.Count-1; index >= 0; index--)
            {
                var customData = CustomObjectData.ElementAt(index);
                var owner = customData.Value.Owner;
                if (!owner.IsValid)
                {
                    CustomObjectData.Remove(customData.Key);
                }
            }

        }

    }
}
