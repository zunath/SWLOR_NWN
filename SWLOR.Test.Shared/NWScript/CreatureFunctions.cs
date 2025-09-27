using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for creature properties
        private readonly Dictionary<uint, CreatureData> _creatureData = new();
        private readonly Dictionary<uint, int> _creatureGold = new();
        private readonly Dictionary<uint, int> _creatureXP = new();
        private readonly Dictionary<uint, HashSet<FeatType>> _creatureFeats = new();
        private readonly Dictionary<uint, HashSet<SpellType>> _creatureSpells = new();
        private readonly Dictionary<uint, Dictionary<NWNSkillType, int>> _creatureSkills = new();
        private readonly Dictionary<uint, Dictionary<AbilityType, int>> _creatureAbilities = new();
        private readonly Dictionary<uint, Dictionary<ClassType, int>> _creatureLevels = new();
        private readonly Dictionary<uint, Dictionary<ActionModeType, bool>> _creatureActionModes = new();
        private readonly Dictionary<uint, Dictionary<ImmunityType, bool>> _creatureImmunities = new();
        private uint _lastSpellCastClass = (uint)ClassType.Invalid;
        private uint _lastKiller = OBJECT_INVALID;
        private uint _lastRespawnButtonPresser = OBJECT_INVALID;
        private uint _lastOpenedBy = OBJECT_INVALID;
        private uint _lastHostileActor = OBJECT_INVALID;
        private uint _goingToBeAttackedBy = OBJECT_INVALID;
        private DisturbType _lastInventoryDisturbType = DisturbType.Added;
        private uint _lastInventoryDisturbItem = OBJECT_INVALID;

        private class CreatureData
        {
            public FootstepType FootstepType { get; set; } = FootstepType.Normal;
            public CreatureWingType WingType { get; set; } = CreatureWingType.None;
            public CreatureTailType TailType { get; set; } = CreatureTailType.None;
            public PhenoType PhenoType { get; set; } = PhenoType.Normal;
            public AppearanceType AppearanceType { get; set; } = AppearanceType.Invalid;
            public AILevelType AILevel { get; set; } = AILevelType.Default;
            public GenderType Gender { get; set; } = GenderType.Male;
            public RacialType RacialType { get; set; } = RacialType.Human;
            public CreatureSizeType Size { get; set; } = CreatureSizeType.Medium;
            public int BaseAttackBonus { get; set; } = 0;
            public int SpellResistance { get; set; } = 0;
            public int ArcaneSpellFailure { get; set; } = 0;
            public int TurnResistanceHD { get; set; } = 0;
            public int Soundset { get; set; } = 0;
            public int ObjectUiDiscoveryMask { get; set; } = 0;
            public string SubRace { get; set; } = "";
            public string Deity { get; set; } = "";
            public bool IsDisarmable { get; set; } = false;
            public bool IsLootable { get; set; } = false;
            public bool IsDMPossessed { get; set; } = false;
            public bool IsPossessedFamiliar { get; set; } = false;
            public bool IsImmortal { get; set; } = false;
            public bool IsDead { get; set; } = false;
            public bool IsPC { get; set; } = false;
            public bool IsDM { get; set; } = false;
            public bool IsPlayableRacialType { get; set; } = false;
            public bool IsInCombat { get; set; } = false;
            public StealthModeType StealthMode { get; set; } = StealthModeType.Passive;
            public DetectModeType DetectMode { get; set; } = DetectModeType.Passive;
            public CastingModeType DefensiveCastingMode { get; set; } = CastingModeType.Disabled;
            public SpecialAttackType LastAttackType { get; set; } = SpecialAttackType.Invalid;
            public uint AttackTarget { get; set; } = OBJECT_INVALID;
            public uint CommandingPlayer { get; set; } = OBJECT_INVALID;
            public Dictionary<CreaturePartType, int> BodyParts { get; set; } = new();
            public Dictionary<FeatType, int> FeatUses { get; set; } = new();
        }

        private CreatureData GetOrCreateCreatureData(uint oCreature)
        {
            if (!_creatureData.ContainsKey(oCreature))
                _creatureData[oCreature] = new CreatureData();
            return _creatureData[oCreature];
        }

        public FootstepType GetFootstepType(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).FootstepType;
        public void SetFootstepType(FootstepType nFootstepType, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).FootstepType = nFootstepType;
        public CreatureWingType GetCreatureWingType(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).WingType;
        public void SetCreatureWingType(CreatureWingType nWingType, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).WingType = nWingType;
        public int GetCreatureBodyPart(CreaturePartType nPart, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).BodyParts.GetValueOrDefault(nPart, 0);
        public void SetCreatureBodyPart(CreaturePartType nPart, int nModelNumber, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).BodyParts[nPart] = nModelNumber;
        public CreatureTailType GetCreatureTailType(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).TailType;
        public void SetCreatureTailType(CreatureTailType nTailType, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).TailType = nTailType;
        public PhenoType GetPhenoType(uint oCreature) => GetOrCreateCreatureData(oCreature).PhenoType;
        public void SetPhenoType(PhenoType nPhenoType, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).PhenoType = nPhenoType;
        public bool GetIsCreatureDisarmable(uint oCreature) => GetOrCreateCreatureData(oCreature).IsDisarmable;
        public ClassType GetLastSpellCastClass() => (ClassType)_lastSpellCastClass;
        public void SetBaseAttackBonus(int nBaseAttackBonus, uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).BaseAttackBonus = nBaseAttackBonus;
        public void RestoreBaseAttackBonus(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).BaseAttackBonus = 0;
        public void SetCreatureAppearanceType(uint oCreature, AppearanceType nAppearanceType) => GetOrCreateCreatureData(oCreature).AppearanceType = nAppearanceType;
        public int GetCreatureStartingPackage(uint oCreature) => 0;
        public int GetSpellResistance(uint oCreature) => GetOrCreateCreatureData(oCreature).SpellResistance;
        public void SetLootable(uint oCreature, bool bLootable) => GetOrCreateCreatureData(oCreature).IsLootable = bLootable;
        public bool GetLootable(uint oCreature) => GetOrCreateCreatureData(oCreature).IsLootable;
        public bool GetActionMode(uint oCreature, ActionModeType nMode) => _creatureActionModes.GetValueOrDefault(oCreature, new Dictionary<ActionModeType, bool>()).GetValueOrDefault(nMode, false);
        public void SetActionMode(uint oCreature, ActionModeType nMode, bool nStatus) 
        {
            if (!_creatureActionModes.ContainsKey(oCreature))
                _creatureActionModes[oCreature] = new Dictionary<ActionModeType, bool>();
            _creatureActionModes[oCreature][nMode] = nStatus;
        }
        public int GetArcaneSpellFailure(uint oCreature) => GetOrCreateCreatureData(oCreature).ArcaneSpellFailure;
        public void SetSubRace(uint oCreature, string sSubRace) => GetOrCreateCreatureData(oCreature).SubRace = sSubRace;
        public void SetDeity(uint oCreature, string sDeity) => GetOrCreateCreatureData(oCreature).Deity = sDeity;
        public bool GetIsDMPossessed(uint oCreature) => GetOrCreateCreatureData(oCreature).IsDMPossessed;
        public void IncrementRemainingFeatUses(uint oCreature, FeatType nFeat) 
        {
            var data = GetOrCreateCreatureData(oCreature);
            data.FeatUses[nFeat] = data.FeatUses.GetValueOrDefault(nFeat, 0) + 1;
        }
        public AILevelType GetAILevel(uint oTarget = OBJECT_INVALID) => GetOrCreateCreatureData(oTarget).AILevel;
        public void SetAILevel(uint oTarget, AILevelType nAILevel) => GetOrCreateCreatureData(oTarget).AILevel = nAILevel;
        public bool GetIsPossessedFamiliar(uint oCreature) => GetOrCreateCreatureData(oCreature).IsPossessedFamiliar;
        public void UnpossessFamiliar(uint oCreature) => GetOrCreateCreatureData(oCreature).IsPossessedFamiliar = false;
        public bool GetImmortal(uint oTarget = OBJECT_INVALID) => GetOrCreateCreatureData(oTarget).IsImmortal;
        public void DoWhirlwindAttack(bool bDisplayFeedback = true, bool bImproved = false) { }
        public int GetBaseAttackBonus(uint oCreature) => GetOrCreateCreatureData(oCreature).BaseAttackBonus;
        public void SetImmortal(uint oCreature, bool bImmortal) => GetOrCreateCreatureData(oCreature).IsImmortal = bImmortal;
        public bool GetIsSkillSuccessful(uint oTarget, NWNSkillType nSkill, int nDifficulty) => false;
        public void DecrementRemainingFeatUses(uint oCreature, int nFeat) 
        {
            var data = GetOrCreateCreatureData(oCreature);
            var feat = (FeatType)nFeat;
            if (data.FeatUses.ContainsKey(feat) && data.FeatUses[feat] > 0)
                data.FeatUses[feat]--;
        }
        public void DecrementRemainingSpellUses(uint oCreature, int nSpell) { }
        public StealthModeType GetStealthMode(uint oCreature) => GetOrCreateCreatureData(oCreature).StealthMode;
        public DetectModeType GetDetectMode(uint oCreature) => GetOrCreateCreatureData(oCreature).DetectMode;
        public CastingModeType GetDefensiveCastingMode(uint oCreature) => GetOrCreateCreatureData(oCreature).DefensiveCastingMode;
        public AppearanceType GetAppearanceType(uint oCreature) => GetOrCreateCreatureData(oCreature).AppearanceType;
        public uint GetLastHostileActor(uint oVictim = OBJECT_INVALID) => _lastHostileActor;
        public int GetTurnResistanceHD(uint oUndead = OBJECT_INVALID) => GetOrCreateCreatureData(oUndead).TurnResistanceHD;
        public CreatureSizeType GetCreatureSize(uint oCreature) => GetOrCreateCreatureData(oCreature).Size;
        public void SurrenderToEnemies() { }
        public int GetIsReactionTypeFriendly(uint oTarget, uint oSource = OBJECT_INVALID) => 0;
        public int GetIsReactionTypeNeutral(uint oTarget, uint oSource = OBJECT_INVALID) => 0;
        public bool GetIsReactionTypeHostile(uint oTarget, uint oSource = OBJECT_INVALID) => false;
        public void TakeGoldFromCreature(int nAmount, uint oCreatureToTakeFrom, bool bDestroy = false) 
        {
            if (_creatureGold.ContainsKey(oCreatureToTakeFrom))
                _creatureGold[oCreatureToTakeFrom] = Math.Max(0, _creatureGold[oCreatureToTakeFrom] - nAmount);
        }
        public uint GetLastKiller() => _lastKiller;
        public bool GetIsDM(uint oCreature) => GetOrCreateCreatureData(oCreature).IsDM;
        public uint GetLastRespawnButtonPresser() => _lastRespawnButtonPresser;
        public void ActionEquipMostEffectiveArmor() { }
        public int GetIsEncounterCreature(uint oCreature = OBJECT_INVALID) => 0;
        public void ActionEquipMostDamagingMelee(uint oVersus = OBJECT_INVALID, bool bOffHand = false) { }
        public void ActionEquipMostDamagingRanged(uint oVersus = OBJECT_INVALID) { }
        public void GiveXPToCreature(uint oCreature, int nXpAmount) 
        {
            _creatureXP[oCreature] = _creatureXP.GetValueOrDefault(oCreature, 0) + nXpAmount;
        }
        public void SetXP(uint oCreature, int nXpAmount) => _creatureXP[oCreature] = nXpAmount;
        public int GetXP(uint oCreature) => _creatureXP.GetValueOrDefault(oCreature, 0);
        public void ActionForceMoveToLocation(Location lDestination, bool bRun = false, float fTimeout = 30.0f) { }
        public void ActionForceMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f, float fTimeout = 30.0f) { }
        public uint GetLastOpenedBy(uint oObject = OBJECT_INVALID) => _lastOpenedBy;
        public int GetHasSpell(SpellType nSpell, uint oCreature = OBJECT_INVALID) => _creatureSpells.GetValueOrDefault(oCreature, new HashSet<SpellType>()).Contains(nSpell) ? 1 : 0;
        public GenderType GetGender(uint oCreature) => GetOrCreateCreatureData(oCreature).Gender;
        public DisturbType GetInventoryDisturbType(uint oObject = OBJECT_INVALID) => _lastInventoryDisturbType;
        public uint GetInventoryDisturbItem(uint oObject = OBJECT_INVALID) => _lastInventoryDisturbItem;
        public ClassType GetClassByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID) => ClassType.Invalid;
        public int GetLevelByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID) => 0;
        public int GetLevelByClass(ClassType nClassType, uint oCreature = OBJECT_INVALID) => _creatureLevels.GetValueOrDefault(oCreature, new Dictionary<ClassType, int>()).GetValueOrDefault(nClassType, 0);
        public int GetAbilityModifier(AbilityType nAbility, uint oCreature = OBJECT_INVALID) => 0;
        public bool GetIsInCombat(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).IsInCombat;
        public void GiveGoldToCreature(uint oCreature, int nGP) 
        {
            _creatureGold[oCreature] = _creatureGold.GetValueOrDefault(oCreature, 0) + nGP;
        }
        public uint GetNearestCreatureToLocation(CreatureType nFirstCriteriaType, bool nFirstCriteriaValue, Location lLocation, int nNth = 1) => OBJECT_INVALID;
        public int GetCasterLevel(uint oCreature) => 0;
        public RacialType GetRacialType(uint oCreature) => GetOrCreateCreatureData(oCreature).RacialType;
        public uint GetNearestCreature(CreatureType nFirstCriteriaType, int nFirstCriteriaValue, uint oTarget = OBJECT_INVALID, int nNth = 1) => OBJECT_INVALID;
        public int GetAbilityScore(uint oCreature, AbilityType nAbilityType, bool nBaseAbilityScore = false) => _creatureAbilities.GetValueOrDefault(oCreature, new Dictionary<AbilityType, int>()).GetValueOrDefault(nAbilityType, 10);
        public bool GetIsDead(uint oCreature) => GetOrCreateCreatureData(oCreature).IsDead;
        public int GetHitDice(uint oCreature) => 0;
        public uint GetGoingToBeAttackedBy(uint oTarget) => _goingToBeAttackedBy;
        public bool GetIsPC(uint oCreature) => GetOrCreateCreatureData(oCreature).IsPC;
        public bool GetIsImmune(uint oCreature, ImmunityType nImmunityType, uint oVersus = OBJECT_INVALID) => _creatureImmunities.GetValueOrDefault(oCreature, new Dictionary<ImmunityType, bool>()).GetValueOrDefault(nImmunityType, false);
        public bool GetHasFeat(FeatType nFeat, uint oCreature = OBJECT_INVALID) => _creatureFeats.GetValueOrDefault(oCreature, new HashSet<FeatType>()).Contains(nFeat);
        public bool GetHasSkill(NWNSkillType nSkill, uint oCreature = OBJECT_INVALID) => _creatureSkills.GetValueOrDefault(oCreature, new Dictionary<NWNSkillType, int>()).ContainsKey(nSkill);
        public bool GetObjectSeen(uint oTarget, uint oSource = OBJECT_INVALID) => false;
        public bool GetObjectHeard(uint oTarget, uint oSource = OBJECT_INVALID) => false;
        public bool GetIsPlayableRacialType(uint oCreature) => GetOrCreateCreatureData(oCreature).IsPlayableRacialType;
        public int GetSkillRank(NWNSkillType nSkill, uint oTarget = OBJECT_INVALID, bool nBaseSkillRank = false) => _creatureSkills.GetValueOrDefault(oTarget, new Dictionary<NWNSkillType, int>()).GetValueOrDefault(nSkill, 0);
        public uint GetAttackTarget(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).AttackTarget;
        public SpecialAttackType GetLastAttackType(uint oCreature = OBJECT_INVALID) => GetOrCreateCreatureData(oCreature).LastAttackType;
        public void SetGender(uint oCreature, GenderType nGender) => GetOrCreateCreatureData(oCreature).Gender = nGender;
        public int GetSoundset(uint oCreature) => GetOrCreateCreatureData(oCreature).Soundset;
        public void SetSoundset(uint oCreature, int nSoundset) => GetOrCreateCreatureData(oCreature).Soundset = nSoundset;
        public void ReadySpellLevel(uint oCreature, int nSpellLevel, ClassType nClassType = ClassType.Invalid) { }
        public void SetCommandingPlayer(uint oCreature, uint oPlayer) => GetOrCreateCreatureData(oCreature).CommandingPlayer = oPlayer;
        public int GetObjectUiDiscoveryMask(uint oObject) => GetOrCreateCreatureData(oObject).ObjectUiDiscoveryMask;
        public void SetObjectUiDiscoveryMask(uint oObject, ObjectUIDiscoveryType nMask = ObjectUIDiscoveryType.Default) => GetOrCreateCreatureData(oObject).ObjectUiDiscoveryMask = (int)nMask;
        public void SetObjectTextBubbleOverride(uint oObject, ObjectUITextBubbleOverrideType nMode, string sText) { }

        // Helper methods for testing

    }
}
