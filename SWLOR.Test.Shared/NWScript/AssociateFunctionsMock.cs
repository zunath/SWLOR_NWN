using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for associates
        private readonly Dictionary<uint, AssociateData> _associateData = new();
        private readonly Dictionary<uint, List<uint>> _henchmen = new();
        private readonly Dictionary<uint, uint> _masters = new();
        private readonly Dictionary<uint, int> _lastAssociateCommands = new();
        private int _maxHenchmen = 0;

        private class AssociateData
        {
            public AssociateType Type { get; set; } = AssociateType.None;
            public int FamiliarCreatureType { get; set; } = 0;
            public int AnimalCompanionCreatureType { get; set; } = 0;
            public string FamiliarName { get; set; } = "";
            public string AnimalCompanionName { get; set; } = "";
        }

        public void SetMaxHenchmen(int nNumHenchmen) => _maxHenchmen = nNumHenchmen;
        public int GetMaxHenchmen() => _maxHenchmen;

        public AssociateType GetAssociateType(uint oAssociate) => 
            _associateData.GetValueOrDefault(oAssociate, new AssociateData()).Type;

        public int LevelUpHenchman(uint oHenchman, string sClass = "", int nLevel = 1, bool bReadyAllSpells = true) 
        {
            if (!_associateData.ContainsKey(oHenchman))
                _associateData[oHenchman] = new AssociateData();
            
            _associateData[oHenchman].Type = AssociateType.Henchman;
            return 1; // Success
        }

        public void SetAssociateListenPatterns(uint oTarget = OBJECT_INVALID) { }

        public void RemoveSummonedAssociate(uint oMaster, uint oAssociate = OBJECT_INVALID) 
        {
            if (oAssociate != OBJECT_INVALID)
            {
                _associateData.Remove(oAssociate);
                _masters.Remove(oAssociate);
            }
        }

        public int GetFamiliarCreatureType(uint oCreature) => 
            _associateData.GetValueOrDefault(oCreature, new AssociateData()).FamiliarCreatureType;

        public int GetAnimalCompanionCreatureType(uint oCreature) => 
            _associateData.GetValueOrDefault(oCreature, new AssociateData()).AnimalCompanionCreatureType;

        public string GetFamiliarName(uint oCreature) => 
            _associateData.GetValueOrDefault(oCreature, new AssociateData()).FamiliarName;

        public string GetAnimalCompanionName(uint oTarget) => 
            _associateData.GetValueOrDefault(oTarget, new AssociateData()).AnimalCompanionName;

        public uint GetAssociate(AssociateType nAssociateType, uint oMaster = OBJECT_INVALID, int nTh = 1) 
        {
            if (nAssociateType == AssociateType.Henchman && _henchmen.ContainsKey(oMaster))
            {
                var henchmen = _henchmen[oMaster];
                if (nTh > 0 && nTh <= henchmen.Count)
                    return henchmen[nTh - 1];
            }
            return OBJECT_INVALID;
        }

        public void AddHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID) 
        {
            if (!_henchmen.ContainsKey(oMaster))
                _henchmen[oMaster] = new List<uint>();
            
            if (oHenchman != OBJECT_INVALID && !_henchmen[oMaster].Contains(oHenchman))
            {
                _henchmen[oMaster].Add(oHenchman);
                _masters[oHenchman] = oMaster;
                _associateData[oHenchman] = new AssociateData { Type = AssociateType.Henchman };
            }
        }

        public void RemoveHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID) 
        {
            if (_henchmen.ContainsKey(oMaster) && oHenchman != OBJECT_INVALID)
            {
                _henchmen[oMaster].Remove(oHenchman);
                _masters.Remove(oHenchman);
                _associateData.Remove(oHenchman);
            }
        }

        public uint GetHenchman(uint oMaster = OBJECT_INVALID, int nNth = 1) 
        {
            if (_henchmen.ContainsKey(oMaster) && nNth > 0 && nNth <= _henchmen[oMaster].Count)
                return _henchmen[oMaster][nNth - 1];
            return OBJECT_INVALID;
        }

        public void SummonAnimalCompanion(uint oMaster = OBJECT_INVALID) 
        {
            if (oMaster != OBJECT_INVALID)
            {
                var companion = (uint)(_associateData.Count + 2000);
                _associateData[companion] = new AssociateData 
                { 
                    Type = AssociateType.AnimalCompanion,
                    AnimalCompanionCreatureType = 1,
                    AnimalCompanionName = "Animal Companion"
                };
                _masters[companion] = oMaster;
            }
        }

        public void SummonFamiliar(uint oMaster = OBJECT_INVALID) 
        {
            if (oMaster != OBJECT_INVALID)
            {
                var familiar = (uint)(_associateData.Count + 3000);
                _associateData[familiar] = new AssociateData 
                { 
                    Type = AssociateType.Familiar,
                    FamiliarCreatureType = 1,
                    FamiliarName = "Familiar"
                };
                _masters[familiar] = oMaster;
            }
        }

        public int GetLastAssociateCommand(uint oAssociate = OBJECT_INVALID) => 
            _lastAssociateCommands.GetValueOrDefault(oAssociate, 0);

        public uint GetMaster(uint oAssociate = OBJECT_INVALID) => 
            _masters.GetValueOrDefault(oAssociate, OBJECT_INVALID);

        // Helper methods for testing



    }
}
