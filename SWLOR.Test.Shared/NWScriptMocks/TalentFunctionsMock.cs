using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Use Talent from SWLOR.NWN.API.Engine
        
        // Mock data storage for talents
        private readonly Dictionary<Talent, TalentData> _talentData = new();
        private readonly Dictionary<uint, HashSet<Talent>> _creatureTalents = new();

        public class TalentData
        {
            public TalentType Type { get; set; } = TalentType.Spell;
            public int Id { get; set; } = 0;
        }

        public bool GetIsTalentValid(Talent tTalent) => 
            _talentData.ContainsKey(tTalent);

        public TalentType GetTypeFromTalent(Talent tTalent) => 
            _talentData.GetValueOrDefault(tTalent, new TalentData()).Type;

        public int GetIdFromTalent(Talent tTalent) => 
            _talentData.GetValueOrDefault(tTalent, new TalentData()).Id;

        public Talent TalentSpell(SpellType nSpell) 
        {
            var talent = new Talent(0); // Create with handle 0 for mock
            var data = GetOrCreateTalentData(talent);
            data.Type = TalentType.Spell;
            data.Id = (int)nSpell;
            return talent;
        }

        public Talent TalentFeat(FeatType nFeat) 
        {
            var talent = new Talent(0); // Create with handle 0 for mock
            var data = GetOrCreateTalentData(talent);
            data.Type = TalentType.Feat;
            data.Id = (int)nFeat;
            return talent;
        }

        public Talent TalentSkill(NWNSkillType nSkill) 
        {
            var talent = new Talent(0); // Create with handle 0 for mock
            var data = GetOrCreateTalentData(talent);
            data.Type = TalentType.Skill;
            data.Id = (int)nSkill;
            return talent;
        }

        public bool GetCreatureHasTalent(Talent tTalent, uint oCreature = OBJECT_INVALID) => 
            _creatureTalents.GetValueOrDefault(oCreature, new HashSet<Talent>()).Contains(tTalent);

        public Talent GetCreatureTalentRandom(TalentCategoryType nCategory, uint oCreature = OBJECT_INVALID) 
        {
            var talents = _creatureTalents.GetValueOrDefault(oCreature, new HashSet<Talent>());
            if (talents.Count > 0)
            {
                var talentList = new List<Talent>(talents);
                return talentList[0]; // Return first talent as mock
            }
            return new Talent(0); // Create with handle 0 for mock
        }

        public Talent GetCreatureTalentBest(TalentCategoryType nCategory, int nCRMax, uint oCreature = OBJECT_INVALID) 
        {
            var talents = _creatureTalents.GetValueOrDefault(oCreature, new HashSet<Talent>());
            if (talents.Count > 0)
            {
                var talentList = new List<Talent>(talents);
                return talentList[0]; // Return first talent as mock
            }
            return new Talent(0); // Create with handle 0 for mock
        }

        private TalentData GetOrCreateTalentData(Talent tTalent)
        {
            if (!_talentData.ContainsKey(tTalent))
                _talentData[tTalent] = new TalentData();
            return _talentData[tTalent];
        }

        // Additional talent methods from INWScriptService
        // Note: ActionUseTalentOnObject and ActionUseTalentAtLocation are already defined in ActionFunctionsMock.cs

        // Helper methods for testing
        public Dictionary<Talent, TalentData> GetTalentData() => new Dictionary<Talent, TalentData>(_talentData);
        public void ClearTalentData() => _talentData.Clear();
        public void AddCreatureTalent(uint oCreature, Talent talent) 
        {
            if (!_creatureTalents.ContainsKey(oCreature))
                _creatureTalents[oCreature] = new HashSet<Talent>();
            _creatureTalents[oCreature].Add(talent);
        }
        public void RemoveCreatureTalent(uint oCreature, Talent talent) 
        {
            if (_creatureTalents.ContainsKey(oCreature))
                _creatureTalents[oCreature].Remove(talent);
        }
        public HashSet<Talent> GetCreatureTalents(uint oCreature) => _creatureTalents.GetValueOrDefault(oCreature, new HashSet<Talent>());

    }
}
