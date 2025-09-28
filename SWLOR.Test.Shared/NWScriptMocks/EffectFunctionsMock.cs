using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        private readonly Dictionary<Effect, string> _effectTags = new();
        private readonly Dictionary<Effect, int> _effectCasterLevels = new();
        private readonly Dictionary<Effect, int> _effectDurations = new();
        private readonly Dictionary<Effect, int> _effectDurationRemainings = new();
        private readonly Dictionary<Effect, int> _effectDurationTypes = new();
        private readonly Dictionary<Effect, int> _effectSubTypes = new();
        private readonly Dictionary<Effect, uint> _effectCreators = new();
        private readonly Dictionary<Effect, EffectScriptType> _effectTypes = new();
        private readonly Dictionary<Effect, int[]> _effectIntegers = new();
        private readonly Dictionary<Effect, float[]> _effectFloats = new();
        private readonly Dictionary<Effect, string[]> _effectStrings = new();
        private readonly Dictionary<Effect, uint[]> _effectObjects = new();
        private readonly Dictionary<Effect, Vector3[]> _effectVectors = new();
        private readonly Dictionary<Effect, string> _effectLinkIds = new();
        private readonly Dictionary<Effect, bool> _effectValidity = new();
        private readonly Dictionary<uint, bool> _itemCursedFlags = new();
        private readonly Dictionary<uint, uint> _itemPossessors = new();
        private readonly Dictionary<uint, Dictionary<string, uint>> _possessedItems = new();
        private readonly Dictionary<uint, List<Effect>> _objectEffects = new();
        private Effect _lastRunScriptEffect = new Effect(0);
        private int _lastRunScriptEffectScriptType = 0;
        private int _nextEffectId = 1;

        public string GetEffectTag(Effect eEffect) => _effectTags.GetValueOrDefault(eEffect, "");
        public Effect TagEffect(Effect eEffect, string sNewTag) { _effectTags[eEffect] = sNewTag; return eEffect; }
        public int GetEffectCasterLevel(Effect eEffect) => _effectCasterLevels.GetValueOrDefault(eEffect, 0);
        public int GetEffectDuration(Effect eEffect) => _effectDurations.GetValueOrDefault(eEffect, 0);
        public int GetEffectDurationRemaining(Effect eEffect) => _effectDurationRemainings.GetValueOrDefault(eEffect, 0);
        public Effect EffectCutsceneImmobilize() => CreateEffect(EffectScriptType.CutsceneImmobilize);
        public Effect EffectCutsceneGhost() => CreateEffect(EffectScriptType.CutsceneGhost);
        public bool GetItemCursedFlag(uint oItem) => _itemCursedFlags.GetValueOrDefault(oItem, false);
        public void SetItemCursedFlag(uint oItem, bool nCursed) => _itemCursedFlags[oItem] = nCursed;
        public uint GetItemPossessor(uint oItem) => _itemPossessors.GetValueOrDefault(oItem, OBJECT_INVALID);
        public uint GetItemPossessedBy(uint oCreature, string sItemTag) => _possessedItems.GetValueOrDefault(oCreature, new Dictionary<string, uint>()).GetValueOrDefault(sItemTag, OBJECT_INVALID);
        public uint CreateItemOnObject(string sResRef, uint oTarget = OBJECT_INVALID, int nStackSize = 1, string sNewTag = "") { var id = (uint)(_nextEffectId++); if (oTarget != OBJECT_INVALID) { _itemPossessors[id] = oTarget; if (!_possessedItems.ContainsKey(oTarget)) _possessedItems[oTarget] = new Dictionary<string, uint>(); _possessedItems[oTarget][sNewTag] = id; } return id; }
        public void ActionEquipItem(uint oItem, InventorySlotType nInventorySlot) { }
        public void ActionUnequipItem(uint oItem) { }
        public void ActionPickUpItem(uint oItem) { }
        public void ActionPutDownItem(uint oItem) { }
        public void ActionGiveItem(uint oItem, uint oGiveTo) { _itemPossessors[oItem] = oGiveTo; }
        public void ActionTakeItem(uint oItem, uint oTakeFrom) { _itemPossessors[oItem] = OBJECT_SELF; }
        public Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true) => CreateEffect(EffectScriptType.Petrify);
        public Effect EffectKnockdown() => CreateEffect(EffectScriptType.Stunned);
        public Effect EffectCurse(int nStrMod = 1, int nDexMod = 1, int nConMod = 1, int nIntMod = 1, int nWisMod = 1, int nChaMod = 1) => CreateEffect(EffectScriptType.Curse);
        public Effect EffectEntangle() => CreateEffect(EffectScriptType.Entangle);
        public Effect EffectSavingThrowIncrease(int nSave, int nValue, SavingThrowType nSaveType = SavingThrowType.All) => CreateEffect(EffectScriptType.SavingThrowIncrease);
        public Effect EffectAccuracyIncrease(int nBonus, AttackBonusType nModifierType = AttackBonusType.Misc) => CreateEffect(EffectScriptType.AttackIncrease);
        public Effect EffectDamageReduction(int nAmount, DamagePowerType nDamagePower, int nLimit = 0) => CreateEffect(EffectScriptType.DamageReduction);
        public Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = DamageType.Force) => CreateEffect(EffectScriptType.DamageIncrease);
        public Effect MagicalEffect(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Invalideffect; return eEffect; }
        public Effect SupernaturalEffect(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Invalideffect; return eEffect; }
        public Effect ExtraordinaryEffect(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Invalideffect; return eEffect; }
        public Effect EffectACIncrease(int nValue, ArmorClassBonus nBonusType = ArmorClassBonus.Dodge, int nModifierType = 0) => CreateEffect(EffectScriptType.ACIncrease);
        public Effect GetFirstEffect(uint oCreature) { var effects = _objectEffects.GetValueOrDefault(oCreature, new List<Effect>()); return effects.Count > 0 ? effects[0] : new Effect(0); }
        public Effect GetNextEffect(uint oCreature) => new Effect(0);
        public void RemoveEffect(uint oCreature, Effect eEffect) { if (_objectEffects.ContainsKey(oCreature)) _objectEffects[oCreature].Remove(eEffect); }
        public bool GetIsEffectValid(Effect eEffect) => _effectValidity.GetValueOrDefault(eEffect, true);
        public int GetEffectDurationType(Effect eEffect) => _effectDurationTypes.GetValueOrDefault(eEffect, 0);
        public int GetEffectSubType(Effect eEffect) => _effectSubTypes.GetValueOrDefault(eEffect, 0);
        public uint GetEffectCreator(Effect eEffect) => _effectCreators.GetValueOrDefault(eEffect, OBJECT_INVALID);
        public Effect EffectHeal(int nDamageToHeal) => CreateEffect(EffectScriptType.Regenerate);
        public Effect EffectDamage(int nDamageAmount, DamageType nDamageType = DamageType.Force, DamagePowerType nDamagePower = DamagePowerType.Normal) => CreateEffect(EffectScriptType.DamageIncrease);
        public Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy) => CreateEffect(EffectScriptType.AbilityIncrease);
        public Effect EffectDamageResistance(DamageType nDamageType, int nAmount, int nLimit = 0) => CreateEffect(EffectScriptType.DamageResistance);
        public Effect EffectResurrection() => CreateEffect(EffectScriptType.Resurrection);
        public Effect EffectSummonCreature(string sCreatureResref, VisualEffectType nVisualEffectId = VisualEffectType.Vfx_Com_Sparks_Parry, float fDelay = 0.0f) => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectEthereal() => CreateEffect(EffectScriptType.Ethereal);
        public Effect EffectSpellFailure(int nPercent = 100, SpellType nExclude = SpellType.AllSpells) => CreateEffect(EffectScriptType.SpellFailure);
        public Effect EffectCutsceneDominated() => CreateEffect(EffectScriptType.Dominated);
        public Effect EffectPetrify() => CreateEffect(EffectScriptType.Petrify);
        public Effect EffectCutsceneParalyze() => CreateEffect(EffectScriptType.CutsceneParalyze);
        public Effect EffectTurnResistanceDecrease(int nHitDice) => CreateEffect(EffectScriptType.TurnResistanceDecrease);
        public Effect EffectTurnResistanceIncrease(int nHitDice) => CreateEffect(EffectScriptType.TurnResistanceIncrease);
        public Effect EffectSwarm(int nLooping, string sCreatureTemplate1, string sCreatureTemplate2 = "", string sCreatureTemplate3 = "", string sCreatureTemplate4 = "") => CreateEffect(EffectScriptType.Swarm);
        public Effect EffectDisappearAppear(Location lLocation, int nAnimation = 1) => CreateEffect(EffectScriptType.DisappearAppear);
        public Effect EffectDisappear(int nAnimation = 1) => CreateEffect(EffectScriptType.DisappearAppear);
        public Effect EffectAppear(int nAnimation = 1) => CreateEffect(EffectScriptType.DisappearAppear);
        public Effect EffectModifyAttacks(int nAttacks) => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectDamageShield(int nDamageAmount, ItemPropertyDamageBonusType nRandomAmount, DamageType nDamageType) => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectMissChance(int nPercentage, MissChanceType nMissChanceType = MissChanceType.Normal) => CreateEffect(EffectScriptType.MissChance);
        public Effect EffectSpellLevelAbsorption(int nMaxSpellLevelAbsorbed, int nTotalSpellLevelsAbsorbed = 0, int nSpellLevel = 0) => CreateEffect(EffectScriptType.SpellLevelAbsorption);
        public Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL) => CreateEffect(EffectScriptType.DispelMagicBest);
        public Effect EffectInvisibility(InvisibilityType nInvisibilityType) => CreateEffect(EffectScriptType.Invisibility);
        public Effect EffectConcealment(int nPercentage, MissChanceType nMissType = MissChanceType.Normal) => CreateEffect(EffectScriptType.Concealment);
        public Effect EffectDarkness() => CreateEffect(EffectScriptType.Darkness);
        public Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL) => CreateEffect(EffectScriptType.DispelMagicAll);
        public Effect EffectUltravision() => CreateEffect(EffectScriptType.Ultravision);
        public Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false) => CreateEffect(EffectScriptType.NegativeLevel);
        public Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false) => CreateEffect(EffectScriptType.Polymorph);
        public Effect EffectSanctuary(int nDifficultyClass) => CreateEffect(EffectScriptType.Sanctuary);
        public Effect EffectTrueSeeing() => CreateEffect(EffectScriptType.TrueSeeing);
        public Effect EffectSeeInvisible() => CreateEffect(EffectScriptType.SeeInvisible);
        public Effect EffectTimeStop() => CreateEffect(EffectScriptType.Timestop);
        public Effect EffectBlindness() => CreateEffect(EffectScriptType.Blindness);
        public Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy) => CreateEffect(EffectScriptType.AbilityDecrease);
        public Effect EffectAccuracyDecrease(int nPenalty, AttackBonusType nModifierType = AttackBonusType.Misc) => CreateEffect(EffectScriptType.AttackDecrease);
        public Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Force) => CreateEffect(EffectScriptType.DamageDecrease);
        public Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity) => CreateEffect(EffectScriptType.DamageImmunityDecrease);
        public Effect EffectACDecrease(int nValue, ArmorClassBonus nBonusType = ArmorClassBonus.Dodge, int nModifierType = 0) => CreateEffect(EffectScriptType.ACDecrease);
        public Effect EffectMovementSpeedDecrease(int nPercentChange) => CreateEffect(EffectScriptType.MovementSpeedDecrease);
        public Effect EffectSavingThrowDecrease(int nSave, int nValue, SavingThrowType nSaveType = SavingThrowType.All) => CreateEffect(EffectScriptType.SavingThrowDecrease);
        public Effect EffectSkillDecrease(int nSkill, int nValue) => CreateEffect(EffectScriptType.SkillDecrease);
        public Effect EffectSpellResistanceDecrease(int nValue) => CreateEffect(EffectScriptType.SpellResistanceDecrease);
        public Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID) => new Event(0);
        public Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound) => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectTurned() => CreateEffect(EffectScriptType.Turned);
        public Effect VersusAlignmentEffect(Effect eEffect, AlignmentType nAlignment, bool bOpposite = false) => eEffect;
        public Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType) => eEffect;
        public Effect VersusTrapEffect(Effect eEffect) => eEffect;
        public Effect EffectSkillIncrease(NWNSkillType nSkill, int nValue) => CreateEffect(EffectScriptType.SkillIncrease);
        public Effect EffectTemporaryHitpoints(int nHitPoints) => CreateEffect(EffectScriptType.TemporaryHitpoints);
        public Event EventConversation() => new Event(0);
        public Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity) => CreateEffect(EffectScriptType.DamageImmunityIncrease);
        public Effect EffectImmunity(ImmunityType nImmunityType) => CreateEffect(EffectScriptType.Immunity);
        public Effect EffectHaste() => CreateEffect(EffectScriptType.Haste);
        public Effect EffectSlow() => CreateEffect(EffectScriptType.Slow);
        public Effect EffectPoison(PoisonType nPoisonType) => CreateEffect(EffectScriptType.Poison);
        public Effect EffectDisease(DiseaseType nDiseaseType) => CreateEffect(EffectScriptType.Disease);
        public Effect EffectSilence() => CreateEffect(EffectScriptType.Silence);
        public Effect EffectSpellResistanceIncrease(int nValue) => CreateEffect(EffectScriptType.SpellResistanceIncrease);
        public Effect EffectBeam(VisualEffectType nBeamVisualEffect, uint oEffector, BodyNodeType nBodyPart, bool bMissEffect = false) => CreateEffect(EffectScriptType.Beam);
        public Effect EffectLinkEffects(Effect eChildEffect, Effect eParentEffect) { _effectLinkIds[eChildEffect] = eParentEffect.ToString(); return eChildEffect; }
        public Effect EffectVisualEffect(VisualEffectType visualEffectID, bool nMissEffect = false, float fScale = 1.0f, Vector3 vTranslate = default, Vector3 vRotate = default) => CreateEffect(EffectScriptType.Visualeffect);
        public void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget, float fDuration = 0.0f) { if (!_objectEffects.ContainsKey(oTarget)) _objectEffects[oTarget] = new List<Effect>(); _objectEffects[oTarget].Add(eEffect); }
        public EffectScriptType GetEffectType(Effect eEffect) => _effectTypes.GetValueOrDefault(eEffect, EffectScriptType.Invalideffect);
        public Effect EffectAreaOfEffect(AreaOfEffectType nAreaEffect, string sOnEnterScript = "", string sHeartbeatScript = "", string sOnExitScript = "") => CreateEffect(EffectScriptType.AreaOfEffect);
        public Effect EffectRegenerate(int nAmount, float fIntervalSeconds) => CreateEffect(EffectScriptType.Regenerate);
        public Effect EffectMovementSpeedIncrease(int nPercentChange) => CreateEffect(EffectScriptType.MovementSpeedIncrease);
        public Effect EffectCharmed() => CreateEffect(EffectScriptType.Charmed);
        public Effect EffectConfused() => CreateEffect(EffectScriptType.Confused);
        public Effect EffectFrightened() => CreateEffect(EffectScriptType.Frightened);
        public Effect EffectDominated() => CreateEffect(EffectScriptType.Dominated);
        public Effect EffectDazed() => CreateEffect(EffectScriptType.Dazed);
        public Effect EffectStunned() => CreateEffect(EffectScriptType.Stunned);
        public Effect EffectSleep() => CreateEffect(EffectScriptType.Sleep);
        public Effect EffectParalyze() => CreateEffect(EffectScriptType.Paralyze);
        public Effect EffectSpellImmunity(SpellType nImmunityToSpell = SpellType.AllSpells) => CreateEffect(EffectScriptType.SpellImmunity);
        public Effect EffectDeaf() => CreateEffect(EffectScriptType.Deaf);
        public int GetEffectInteger(Effect eEffect, int nIndex) { var data = _effectIntegers.GetValueOrDefault(eEffect, new int[10]); return nIndex >= 0 && nIndex < data.Length ? data[nIndex] : 0; }
        public float GetEffectFloat(Effect eEffect, int nIndex) { var data = _effectFloats.GetValueOrDefault(eEffect, new float[10]); return nIndex >= 0 && nIndex < data.Length ? data[nIndex] : 0.0f; }
        public string GetEffectString(Effect eEffect, int nIndex) { var data = _effectStrings.GetValueOrDefault(eEffect, new string[10]); return nIndex >= 0 && nIndex < data.Length ? data[nIndex] : ""; }
        public uint GetEffectObject(Effect eEffect, int nIndex) { var data = _effectObjects.GetValueOrDefault(eEffect, new uint[10]); return nIndex >= 0 && nIndex < data.Length ? data[nIndex] : OBJECT_INVALID; }
        public Vector3 GetEffectVector(Effect eEffect, int nIndex) { var data = _effectVectors.GetValueOrDefault(eEffect, new Vector3[10]); return nIndex >= 0 && nIndex < data.Length ? data[nIndex] : new Vector3(0, 0, 0); }
        public Effect EffectRunScript(string sOnAppliedScript = "", string sOnRemovedScript = "", string sOnIntervalScript = "", float fInterval = 0.0f, string sData = "") { var effect = CreateEffect(EffectScriptType.RunScript); _lastRunScriptEffect = effect; return effect; }
        public Effect GetLastRunScriptEffect() => _lastRunScriptEffect;
        public int GetLastRunScriptEffectScriptType() => _lastRunScriptEffectScriptType;
        public Effect HideEffectIcon(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Icon; return eEffect; }
        public Effect EffectIcon(EffectIconType nIconId) => CreateEffect(EffectScriptType.Icon);
        public Effect UnyieldingEffect(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Invalideffect; return eEffect; }
        public Effect IgnoreEffectImmunity(Effect eEffect) { _effectTypes[eEffect] = EffectScriptType.Invalideffect; return eEffect; }
        public Effect EffectPacified() => CreateEffect(EffectScriptType.Invalideffect);
        public string GetEffectLinkId(Effect eEffect) => _effectLinkIds.GetValueOrDefault(eEffect, "");
        public Effect EffectBonusFeat(int nFeat) => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectTimeStopImmunity() => CreateEffect(EffectScriptType.Invalideffect);
        public Effect EffectForceWalk() => CreateEffect(EffectScriptType.Invalideffect);
        public Effect SetEffectCreator(Effect eEffect, uint oCreator) { _effectCreators[eEffect] = oCreator; return eEffect; }
        public Effect SetEffectCasterLevel(Effect eEffect, int nCasterLevel) { _effectCasterLevels[eEffect] = nCasterLevel; return eEffect; }
        public Effect SetEffectSpellId(Effect eEffect, SpellType nSpellId) { if (!_effectIntegers.ContainsKey(eEffect)) _effectIntegers[eEffect] = new int[10]; _effectIntegers[eEffect][0] = (int)nSpellId; return eEffect; }

        private Effect CreateEffect(EffectScriptType scriptType) { var effect = new Effect(0); _effectTypes[effect] = scriptType; _effectValidity[effect] = true; return effect; }
        
        // Additional effect methods from INWScriptService
        public Effect EffectACIncrease(int nValue,
            ItemPropertyArmorClassModiferType nModifyType = ItemPropertyArmorClassModiferType.Dodge,
            ACType nDamageType = ACType.VsDamageTypeAll) => CreateEffect(EffectScriptType.ACIncrease);

        public Effect EffectSummonCreature(string sCreatureResref,
            VisualEffectType nVisualEffectId = VisualEffectType.Vfx_Com_Sparks_Parry, float fDelaySeconds = 0,
            bool nUseAppearAnimation = false, VisualEffectType nUnsummonVisualEffectId = VisualEffectType.Vfx_Imp_Unsummon,
            uint oSummonToAdd = OBJECT_INVALID) => CreateEffect(EffectScriptType.Invalideffect);

        public Effect EffectSpellFailure(int nPercent = 100, SpellSchool nSpellSchool = SpellSchool.General) => CreateEffect(EffectScriptType.SpellFailure);

        public Effect EffectSpellLevelAbsorption(int nMaxSpellLevelAbsorbed, int nTotalSpellLevelsAbsorbed = 0,
            SpellSchool nSpellSchool = SpellSchool.General) => CreateEffect(EffectScriptType.SpellLevelAbsorption);

        public Effect EffectACDecrease(int nValue,
            ItemPropertyArmorClassModiferType nModifyType = ItemPropertyArmorClassModiferType.Dodge,
            ACType nDamageType = ACType.VsDamageTypeAll) => CreateEffect(EffectScriptType.ACDecrease);

        public Effect VersusAlignmentEffect(Effect eEffect, AlignmentType nLawChaos = AlignmentType.All,
            AlignmentType nGoodEvil = AlignmentType.All) => eEffect; // Mock implementation

        public void RemoveEffectByTag(uint creature, params string[] tags) 
        {
            // Mock implementation - remove effects by tag
        }

        public void RemoveEffect(uint creature, params EffectScriptType[] types) 
        {
            // Mock implementation - remove effects by type
        }

        public bool HasEffectByTag(uint creature, params string[] tags) 
        {
            // Mock implementation - check if creature has effects with specified tags
            return false;
        }

        public bool HasMorePowerfulEffect(uint creature, int tier, params (string, int)[] effectLevels) 
        {
            // Mock implementation - check if creature has more powerful effects
            return false;
        }

        // Effect methods
        public void RemoveEffectByTag(string sEffectTag, uint oObject) { }
    }
}
