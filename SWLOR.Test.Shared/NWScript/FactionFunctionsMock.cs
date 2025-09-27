using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for factions
        private readonly Dictionary<uint, FactionData> _factionData = new();
        private readonly Dictionary<string, int> _factionReputationData = new();
        private readonly Dictionary<StandardFactionType, int> _standardFactionReputation = new();
        private readonly Dictionary<uint, StandardFactionType> _creatureFactions = new();
        private readonly Dictionary<uint, List<uint>> _factionMembers = new();
        private readonly Dictionary<uint, uint> _factionLeaders = new();
        private readonly Dictionary<uint, uint> _factionIterators = new();

        private class FactionData
        {
            public int Gold { get; set; } = 0;
            public int AverageReputation { get; set; } = 50;
            public int AverageGoodEvilAlignment { get; set; } = 50;
            public int AverageLawChaosAlignment { get; set; } = 50;
            public int AverageLevel { get; set; } = 1;
            public int AverageXP { get; set; } = 0;
            public int MostFrequentClass { get; set; } = 0;
            public uint WeakestMember { get; set; } = OBJECT_INVALID;
            public uint StrongestMember { get; set; } = OBJECT_INVALID;
            public uint MostDamagedMember { get; set; } = OBJECT_INVALID;
            public uint LeastDamagedMember { get; set; } = OBJECT_INVALID;
            public uint WorstAC { get; set; } = OBJECT_INVALID;
            public uint BestAC { get; set; } = OBJECT_INVALID;
        }

        public uint GetFactionWeakestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).WeakestMember;

        public uint GetFactionStrongestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).StrongestMember;

        public uint GetFactionMostDamagedMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).MostDamagedMember;

        public uint GetFactionLeastDamagedMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).LeastDamagedMember;

        public int GetFactionGold(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).Gold;

        public int GetFactionAverageReputation(uint oSourceFactionMember, uint oTarget) => 
            _factionData.GetValueOrDefault(oSourceFactionMember, new FactionData()).AverageReputation;

        public int GetFactionAverageGoodEvilAlignment(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).AverageGoodEvilAlignment;

        public int GetFactionAverageLawChaosAlignment(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).AverageLawChaosAlignment;

        public int GetFactionAverageLevel(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).AverageLevel;

        public int GetFactionAverageXP(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).AverageXP;

        public int GetFactionMostFrequentClass(uint oFactionMember) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).MostFrequentClass;

        public uint GetFactionWorstAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).WorstAC;

        public uint GetFactionBestAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true) => 
            _factionData.GetValueOrDefault(oFactionMember, new FactionData()).BestAC;

        public int GetFactionReputation(uint oSource, uint oTarget) 
        {
            var key = $"{oSource}|{oTarget}";
            return _factionReputationData.GetValueOrDefault(key, 50); // Neutral by default
        }

        public void AdjustReputation(uint oTarget, uint oSourceFactionMember, int nAdjustment) 
        {
            var key = $"{oSourceFactionMember}|{oTarget}";
            var currentRep = _factionReputationData.GetValueOrDefault(key, 50);
            _factionReputationData[key] = Math.Max(0, Math.Min(100, currentRep + nAdjustment));
        }

        public bool GetIsEnemy(uint oTarget, uint oSource = OBJECT_INVALID) 
        {
            var rep = GetFactionReputation(oSource, oTarget);
            return rep < 20; // Enemy if reputation < 20
        }

        public bool GetIsFriend(uint oTarget, uint oSource = OBJECT_INVALID) 
        {
            var rep = GetFactionReputation(oSource, oTarget);
            return rep > 80; // Friend if reputation > 80
        }

        public bool GetIsNeutral(uint oTarget, uint oSource = OBJECT_INVALID) 
        {
            var rep = GetFactionReputation(oSource, oTarget);
            return rep >= 20 && rep <= 80; // Neutral if reputation between 20-80
        }

        public uint GetFactionLeader(uint oMemberOfFaction) => 
            _factionLeaders.GetValueOrDefault(oMemberOfFaction, OBJECT_INVALID);

        public void SetStandardFactionReputation(StandardFactionType nStandardFaction, int nNewReputation, uint oCreature = OBJECT_INVALID) 
        {
            _standardFactionReputation[nStandardFaction] = nNewReputation;
        }

        public int GetStandardFactionReputation(StandardFactionType nStandardFaction, uint oCreature = OBJECT_INVALID) => 
            _standardFactionReputation.GetValueOrDefault(nStandardFaction, 50);

        public void ChangeToStandardFaction(uint oCreatureToChange, StandardFactionType nStandardFaction) 
        {
            _creatureFactions[oCreatureToChange] = nStandardFaction;
        }

        public uint GetFirstFactionMember(uint oMemberOfFaction, bool bPCOnly = true) 
        {
            _factionIterators[oMemberOfFaction] = OBJECT_INVALID;
            if (_factionMembers.ContainsKey(oMemberOfFaction) && _factionMembers[oMemberOfFaction].Count > 0)
            {
                _factionIterators[oMemberOfFaction] = _factionMembers[oMemberOfFaction][0];
            }
            return _factionIterators[oMemberOfFaction];
        }

        public uint GetNextFactionMember(uint oMemberOfFaction, bool bPCOnly = true) 
        {
            if (_factionIterators[oMemberOfFaction] == OBJECT_INVALID || !_factionMembers.ContainsKey(oMemberOfFaction))
                return OBJECT_INVALID;
            
            var members = _factionMembers[oMemberOfFaction];
            var currentIndex = members.IndexOf(_factionIterators[oMemberOfFaction]);
            if (currentIndex >= 0 && currentIndex < members.Count - 1)
            {
                _factionIterators[oMemberOfFaction] = members[currentIndex + 1];
                return _factionIterators[oMemberOfFaction];
            }
            
            _factionIterators[oMemberOfFaction] = OBJECT_INVALID;
            return OBJECT_INVALID;
        }

        public bool GetFactionEqual(uint oFirstObject, uint oSecondObject = OBJECT_INVALID) 
        {
            var faction1 = _creatureFactions.GetValueOrDefault(oFirstObject, StandardFactionType.Hostile);
            var faction2 = _creatureFactions.GetValueOrDefault(oSecondObject, StandardFactionType.Hostile);
            return faction1 == faction2;
        }

        public void ChangeFaction(uint oObjectToChangeFaction, uint oMemberOfFactionToJoin) 
        {
            // In a real implementation, this would change the faction of the object
            // For mock purposes, we'll just track the change
        }

        // Helper methods for testing



        private FactionData GetOrCreateFactionData(uint oFactionMember)
        {
            if (!_factionData.ContainsKey(oFactionMember))
                _factionData[oFactionMember] = new FactionData();
            return _factionData[oFactionMember];
        }

    }
}
