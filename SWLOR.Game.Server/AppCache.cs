using System;
using System.Collections.Generic;
using Autofac.Util;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Skill;

namespace SWLOR.Game.Server
{
    public class AppCache
    {
        public Dictionary<Guid, PlayerDialog> PlayerDialogs { get; }
        public Dictionary<int, bool> DialogFilesInUse { get; }
        public Dictionary<string, int> EffectTicks { get; }
        public Dictionary<Guid, CreatureSkillRegistration> CreatureSkillRegistrations;
        public Dictionary<CasterSpellVO, int> NPCEffects { get; }
        public Dictionary<string, ProcessingEvent> ProcessingEvents { get; set; }
        public Queue<string> UnregisterProcessingEvents { get; set; }
        public Dictionary<Guid, EnmityTable> NPCEnmityTables { get; set; }
        public Dictionary<Guid, CustomData> CustomObjectData { get; set; } 
        public Dictionary<string, NWCreature> NPCBehaviours { get; set; }
        public Dictionary<NWArea, AreaSpawn> AreaSpawns { get; set; }
        public Dictionary<string, NWObject> VisibilityObjects { get; set; }
        public List<Guid> PCEffectsForRemoval { get; set; }
        public List<NWObject> ConnectedDMs { get; set; }

        public AppCache()
        {
            PlayerDialogs = new Dictionary<Guid, PlayerDialog>();
            DialogFilesInUse = new Dictionary<int, bool>();
            EffectTicks = new Dictionary<string, int>();
            CreatureSkillRegistrations = new Dictionary<Guid, CreatureSkillRegistration>();
            NPCEffects = new Dictionary<CasterSpellVO, int>();
            ProcessingEvents = new Dictionary<string, ProcessingEvent>();
            UnregisterProcessingEvents = new Queue<string>();
            NPCEnmityTables = new Dictionary<Guid, EnmityTable>();
            CustomObjectData = new Dictionary<Guid, CustomData>();
            NPCBehaviours = new Dictionary<string, NWCreature>();
            AreaSpawns = new Dictionary<NWArea, AreaSpawn>();
            VisibilityObjects = new Dictionary<string, NWObject>();
            PCEffectsForRemoval = new List<Guid>();
            ConnectedDMs = new List<NWObject>();
            

            for (int x = 1; x <= Constants.NumberOfDialogs; x++)
            {
                DialogFilesInUse.Add(x, false);
            }
        }

    }
}
