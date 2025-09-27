using System.Collections.Generic;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Talent type definition
        public struct Talent
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
        }
        
        // Mock data storage for talents
        private readonly Dictionary<Talent, TalentData> _talentData = new();
        private readonly Dictionary<uint, HashSet<Talent>> _creatureTalents = new();

        private class TalentData
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
            var talent = new Talent();
            var data = GetOrCreateTalentData(talent);
            data.Type = TalentType.Spell;
            data.Id = (int)nSpell;
            return talent;
        }

        public Talent TalentFeat(FeatType nFeat) 
        {
            var talent = new Talent();
            var data = GetOrCreateTalentData(talent);
            data.Type = TalentType.Feat;
            data.Id = (int)nFeat;
            return talent;
        }

        public Talent TalentSkill(NWNSkillType nSkill) 
        {
            var talent = new Talent();
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
            return new Talent();
        }

        public Talent GetCreatureTalentBest(TalentCategoryType nCategory, int nCRMax, uint oCreature = OBJECT_INVALID) 
        {
            var talents = _creatureTalents.GetValueOrDefault(oCreature, new HashSet<Talent>());
            if (talents.Count > 0)
            {
                var talentList = new List<Talent>(talents);
                return talentList[0]; // Return first talent as mock
            }
            return new Talent();
        }

        private TalentData GetOrCreateTalentData(Talent tTalent)
        {
            if (!_talentData.ContainsKey(tTalent))
                _talentData[tTalent] = new TalentData();
            return _talentData[tTalent];
        }

        // Helper methods for testing

    }
}
