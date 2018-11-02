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
        public Dictionary<string, PlayerDialog> PlayerDialogs { get; }
        public Dictionary<int, bool> DialogFilesInUse { get; }
        public Dictionary<string, int> EffectTicks { get; }
        public Dictionary<string, CreatureSkillRegistration> CreatureSkillRegistrations;
        public Dictionary<CasterSpellVO, int> NPCEffects { get; }
        public Dictionary<string, ProcessingEvent> ProcessingEvents { get; set; }
        public Queue<string> UnregisterProcessingEvents { get; set; }
        public Dictionary<string, EnmityTable> NPCEnmityTables { get; set; }
        public Dictionary<string, CustomData> CustomObjectData { get; set; } 
        public Dictionary<string, NWCreature> NPCBehaviours { get; set; }
        public Dictionary<NWArea, AreaSpawn> AreaSpawns { get; set; }
        public Dictionary<string, NWObject> VisibilityObjects { get; set; }
        public List<long> PCEffectsForRemoval { get; set; }
        public List<NWObject> ConnectedDMs { get; set; }
        public HashSet<CachedSkillCategory> SkillCategories { get; set; }
        public Dictionary<SkillType, CachedSkill> Skills { get; set; }
        public Dictionary<string, CachedPCSkills> PCSkills { get; set; }

        public AppCache()
        {
            PlayerDialogs = new Dictionary<string, PlayerDialog>();
            DialogFilesInUse = new Dictionary<int, bool>();
            EffectTicks = new Dictionary<string, int>();
            CreatureSkillRegistrations = new Dictionary<string, CreatureSkillRegistration>();
            NPCEffects = new Dictionary<CasterSpellVO, int>();
            ProcessingEvents = new Dictionary<string, ProcessingEvent>();
            UnregisterProcessingEvents = new Queue<string>();
            NPCEnmityTables = new Dictionary<string, EnmityTable>();
            CustomObjectData = new Dictionary<string, CustomData>();
            NPCBehaviours = new Dictionary<string, NWCreature>();
            AreaSpawns = new Dictionary<NWArea, AreaSpawn>();
            VisibilityObjects = new Dictionary<string, NWObject>();
            PCEffectsForRemoval = new List<long>();
            ConnectedDMs = new List<NWObject>();
            SkillCategories = new HashSet<CachedSkillCategory>();
            Skills = new Dictionary<SkillType, CachedSkill>();
            PCSkills = new Dictionary<string, CachedPCSkills>();
            

            for (int x = 1; x <= Constants.NumberOfDialogs; x++)
            {
                DialogFilesInUse.Add(x, false);
            }
        }

    }
}
