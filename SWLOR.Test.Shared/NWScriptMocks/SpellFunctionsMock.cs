using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        private readonly Dictionary<uint, HashSet<SpellType>> _spellEffects = new();
        private readonly Dictionary<uint, Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>> _memorizedSpells = new();
        private readonly Dictionary<uint, Dictionary<ClassType, Dictionary<int, List<SpellType>>>> _knownSpells = new();
        private readonly Dictionary<uint, Dictionary<SpellType, int>> _spellUsesLeft = new();
        private uint _spellTargetObject = OBJECT_INVALID;
        private int _metaMagicFeat = 0;
        private int _spellSaveDC = 10;
        private Location _spellTargetLocation = new Location(0);
        private uint _lastSpellCaster = OBJECT_INVALID;
        private int _lastSpell = 0;
        private int _spellId = 0;
        private bool _lastSpellHarmful = false;
        private uint _attemptedSpellTarget = OBJECT_INVALID;
        private int _spellFeatId = 0;
        private bool _spellCastSpontaneously = false;
        private int _lastSpellLevel = 0;

        private class MemorizedSpell
        {
            public SpellType SpellId = SpellType.Invalid;
            public bool IsReady = true;
            public MetaMagicType MetaMagic = MetaMagicType.None;
            public bool IsDomainSpell = false;
        }

        public bool GetHasSpellEffect(SpellType nSpell, uint oObject = OBJECT_INVALID) => _spellEffects.GetValueOrDefault(oObject, new HashSet<SpellType>()).Contains(nSpell);
        public int GetEffectSpellId(Effect eSpellEffect) => 0;
        public int GetReflexAdjustedDamage(int nDamage, uint oTarget, int nDC, int nReflexSave, int nDamageType) { return nDamage; }
        public uint GetSpellTargetObject() => _spellTargetObject;
        public int GetMetaMagicFeat() => _metaMagicFeat;
        public int GetSpellSaveDC() => _spellSaveDC;
        public Location GetSpellTargetLocation() => _spellTargetLocation;
        public void ActionCastSpellAtLocation(SpellType nSpell, Location lTargetLocation, MetaMagicType nMetaMagic = MetaMagicType.Any, bool nCheat = false, int nDomainLevel = 0, ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) { _spellTargetLocation = lTargetLocation; _lastSpell = (int)nSpell; }
        public Event EventSpellCastAt(uint oCaster, SpellType nSpell, bool bHarmful = true) { _lastSpellCaster = oCaster; _lastSpell = (int)nSpell; _lastSpellHarmful = bHarmful; return new Event(0); }
        public uint GetLastSpellCaster() => _lastSpellCaster;
        public int GetLastSpell() => _lastSpell;
        public int GetSpellId() => _spellId;
        public bool GetLastSpellHarmful() => _lastSpellHarmful;
        public void ActionCastFakeSpellAtObject(SpellType nSpell, uint oTarget, MetaMagicType nMetaMagic = MetaMagicType.Any, bool nCheat = false, int nDomainLevel = 0, ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) { _spellTargetObject = oTarget; _lastSpell = (int)nSpell; }
        public void ActionCastFakeSpellAtLocation(SpellType nSpell, Location lTarget, MetaMagicType nMetaMagic = MetaMagicType.Any, bool nCheat = false, int nDomainLevel = 0, ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) { _spellTargetLocation = lTarget; _lastSpell = (int)nSpell; }
        public void ActionCounterSpell(uint oCounterSpellTarget) { _attemptedSpellTarget = oCounterSpellTarget; }
        public uint GetAttemptedSpellTarget() => _attemptedSpellTarget;
        public int GetSpellFeatId() => _spellFeatId;
        public bool GetSpellCastSpontaneously() => _spellCastSpontaneously;
        public int GetLastSpellLevel() => _lastSpellLevel;
        public int GetMemorizedSpellCountByLevel(uint oCreature, ClassType nClassType, int nSpellLevel) { return _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()).GetValueOrDefault(nSpellLevel, new List<MemorizedSpell>()).Count; }
        public int GetMemorizedSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var spells = _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()).GetValueOrDefault(nSpellLevel, new List<MemorizedSpell>()); if (nIndex >= 0 && nIndex < spells.Count) return (int)spells[nIndex].SpellId; return 0; }
        public int GetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var spells = _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()).GetValueOrDefault(nSpellLevel, new List<MemorizedSpell>()); if (nIndex >= 0 && nIndex < spells.Count) return spells[nIndex].IsReady ? 1 : 0; return 0; }
        public int GetMemorizedSpellMetaMagic(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var spells = _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()).GetValueOrDefault(nSpellLevel, new List<MemorizedSpell>()); if (nIndex >= 0 && nIndex < spells.Count) return (int)spells[nIndex].MetaMagic; return 0; }
        public int GetMemorizedSpellIsDomainSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var spells = _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()).GetValueOrDefault(nSpellLevel, new List<MemorizedSpell>()); if (nIndex >= 0 && nIndex < spells.Count) return spells[nIndex].IsDomainSpell ? 1 : 0; return 0; }
        public void SetMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, SpellType nSpellId, MetaMagicType nMetaMagic = MetaMagicType.None, bool bDomainSpell = false) { var creatureSpells = GetOrCreateMemorizedSpells(oCreature, nClassType, nSpellLevel); while (creatureSpells.Count <= nIndex) creatureSpells.Add(new MemorizedSpell()); creatureSpells[nIndex] = new MemorizedSpell { SpellId = nSpellId, MetaMagic = nMetaMagic, IsDomainSpell = bDomainSpell }; }
        public void SetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, bool bReady) { var creatureSpells = GetOrCreateMemorizedSpells(oCreature, nClassType, nSpellLevel); if (nIndex >= 0 && nIndex < creatureSpells.Count) creatureSpells[nIndex].IsReady = bReady; }
        public void ClearMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var creatureSpells = GetOrCreateMemorizedSpells(oCreature, nClassType, nSpellLevel); if (nIndex >= 0 && nIndex < creatureSpells.Count) creatureSpells[nIndex] = new MemorizedSpell(); }
        public void ClearMemorizedSpellBySpellId(uint oCreature, ClassType nClassType, int nSpellId) { var creatureSpells = _memorizedSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<MemorizedSpell>>()); foreach (var levelSpells in creatureSpells.Values) { for (int i = 0; i < levelSpells.Count; i++) { if ((int)levelSpells[i].SpellId == nSpellId) levelSpells[i] = new MemorizedSpell(); } } }
        public int GetKnownSpellCount(uint oCreature, ClassType nClassType, int nSpellLevel) { return _knownSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<SpellType>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<SpellType>>()).GetValueOrDefault(nSpellLevel, new List<SpellType>()).Count; }
        public int GetKnownSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) { var spells = _knownSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<SpellType>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<SpellType>>()).GetValueOrDefault(nSpellLevel, new List<SpellType>()); if (nIndex >= 0 && nIndex < spells.Count) return (int)spells[nIndex]; return 0; }
        public bool GetIsInKnownSpellList(uint oCreature, ClassType nClassType, SpellType nSpellId) { var creatureSpells = _knownSpells.GetValueOrDefault(oCreature, new Dictionary<ClassType, Dictionary<int, List<SpellType>>>()).GetValueOrDefault(nClassType, new Dictionary<int, List<SpellType>>()); foreach (var levelSpells in creatureSpells.Values) { if (levelSpells.Contains(nSpellId)) return true; } return false; }
        public int GetSpellUsesLeft(uint oCreature, ClassType nClassType, int nSpellLevel, SpellType nSpellId) { return _spellUsesLeft.GetValueOrDefault(oCreature, new Dictionary<SpellType, int>()).GetValueOrDefault(nSpellId, 0); }
        public int GetSpellLevelByClass(ClassType nClassType, SpellType nSpellId) { return 1; }
        public bool SpellResistanceCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), int nCasterLevel = -1, int nSpellResistance = -1, bool bFeedback = true) { return false; }
        public bool SpellImmunityCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), bool bFeedback = true) { return false; }
        public bool SpellAbsorptionLimitedCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), SpellSchool nSpellSchool = (SpellSchool)(-1), int nSpellLevel = -1, bool bRemoveLevels = true, bool bFeedback = true) { return false; }
        public bool SpellAbsorptionUnlimitedCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), SpellSchool nSpellSchool = (SpellSchool)(-1), int nSpellLevel = -1, bool bFeedback = false) { return false; }

        private List<MemorizedSpell> GetOrCreateMemorizedSpells(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            if (!_memorizedSpells.ContainsKey(oCreature))
                _memorizedSpells[oCreature] = new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>();
            if (!_memorizedSpells[oCreature].ContainsKey(nClassType))
                _memorizedSpells[oCreature][nClassType] = new Dictionary<int, List<MemorizedSpell>>();
            if (!_memorizedSpells[oCreature][nClassType].ContainsKey(nSpellLevel))
                _memorizedSpells[oCreature][nClassType][nSpellLevel] = new List<MemorizedSpell>();
            return _memorizedSpells[oCreature][nClassType][nSpellLevel];
        }

        // Additional spell methods from INWScriptService
        public void ActionCastSpellAtLocation(SpellType nSpell, Location lTargetLocation, MetaMagicType nMetaMagic = MetaMagicType.Any,
            bool bCheat = false, ProjectilePathType nProjectilePathType = ProjectilePathType.Default,
            bool bInstantSpell = false) 
        {
            _spellTargetLocation = lTargetLocation;
            _lastSpell = (int)nSpell;
        }

        public void ActionCastFakeSpellAtObject(SpellType nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default) 
        {
            _spellTargetObject = oTarget;
            _lastSpell = (int)nSpell;
        }

        public void ActionCastFakeSpellAtLocation(SpellType nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default) 
        {
            _spellTargetLocation = lTarget;
            _lastSpell = (int)nSpell;
        }

        public void SetMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, SpellType nSpellId,
            bool bReady = true, MetaMagicType nMetaMagic = MetaMagicType.None, bool bIsDomainSpell = false) 
        {
            var spells = GetMemorizedSpells(oCreature, nClassType, nSpellLevel);
            if (nIndex >= 0 && nIndex < spells.Count)
            {
                spells[nIndex] = new MemorizedSpell
                {
                    SpellId = nSpellId,
                    IsReady = bReady,
                    MetaMagic = nMetaMagic,
                    IsDomainSpell = bIsDomainSpell
                };
            }
        }

        private List<MemorizedSpell> GetMemorizedSpells(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            if (!_memorizedSpells.ContainsKey(oCreature))
                _memorizedSpells[oCreature] = new Dictionary<ClassType, Dictionary<int, List<MemorizedSpell>>>();
            if (!_memorizedSpells[oCreature].ContainsKey(nClassType))
                _memorizedSpells[oCreature][nClassType] = new Dictionary<int, List<MemorizedSpell>>();
            if (!_memorizedSpells[oCreature][nClassType].ContainsKey(nSpellLevel))
                _memorizedSpells[oCreature][nClassType][nSpellLevel] = new List<MemorizedSpell>();
            return _memorizedSpells[oCreature][nClassType][nSpellLevel];
        }

        public int GetSpellUsesLeft(uint oCreature, ClassType nClassType, SpellType nSpellId,
            MetaMagicType nMetaMagic = MetaMagicType.None, int nDomainLevel = 0) 
        {
            var creatureSpells = _spellUsesLeft.GetValueOrDefault(oCreature, new Dictionary<SpellType, int>());
            return creatureSpells.GetValueOrDefault(nSpellId, 0);
        }
    }
}
