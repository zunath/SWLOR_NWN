using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Use types from SWLOR.NWN.API.Engine
        
        // Use ItemPropertyDamageResistType from SWLOR.NWN.API.NWScript.Enum
        
        private readonly Dictionary<uint, List<ItemProperty>> _itemProperties = new();
        private readonly Dictionary<ItemProperty, ItemPropertyType> _propertyTypes = new();
        private readonly Dictionary<ItemProperty, DurationType> _propertyDurationTypes = new();
        private readonly Dictionary<ItemProperty, int> _propertySubTypes = new();
        private readonly Dictionary<ItemProperty, int> _propertyCostTables = new();
        private readonly Dictionary<ItemProperty, int> _propertyCostTableValues = new();
        private readonly Dictionary<ItemProperty, int> _propertyParam1s = new();
        private readonly Dictionary<ItemProperty, int> _propertyParam1Values = new();
        private readonly Dictionary<ItemProperty, int> _propertyDurations = new();
        private readonly Dictionary<ItemProperty, int> _propertyDurationRemainings = new();
        private readonly Dictionary<ItemProperty, string> _propertyTags = new();
        private readonly Dictionary<uint, Dictionary<ItemProperty, int>> _propertyUsesPerDay = new();
        private int _nextPropertyId = 1;

        public void AddItemProperty(DurationType nDurationType, ItemProperty ipProperty, uint oItem, float fDuration = 0.0f) { if (!_itemProperties.ContainsKey(oItem)) _itemProperties[oItem] = new List<ItemProperty>(); _itemProperties[oItem].Add(ipProperty); }
        public void RemoveItemProperty(uint oItem, ItemProperty ipProperty) { if (_itemProperties.ContainsKey(oItem)) _itemProperties[oItem].Remove(ipProperty); }
        public bool GetIsItemPropertyValid(ItemProperty ipProperty) => _propertyTypes.ContainsKey(ipProperty);
        public ItemProperty GetFirstItemProperty(uint oItem) { var properties = _itemProperties.GetValueOrDefault(oItem, new List<ItemProperty>()); return properties.Count > 0 ? properties[0] : new ItemProperty(0); }
        public ItemProperty GetNextItemProperty(uint oItem) => new ItemProperty(0);
        public ItemPropertyType GetItemPropertyType(ItemProperty ip) => _propertyTypes.GetValueOrDefault(ip, ItemPropertyType.Invalid);
        public DurationType GetItemPropertyDurationType(ItemProperty ip) => _propertyDurationTypes.GetValueOrDefault(ip, DurationType.Invalid);
        public ItemProperty ItemPropertyAbilityBonus(AbilityType nAbility, int nBonus) => CreateItemProperty(ItemPropertyType.AbilityBonus);
        public ItemProperty ItemPropertyACBonus(int nBonus) => CreateItemProperty(ItemPropertyType.ACBonus);
        public ItemProperty ItemPropertyACBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup, int ACBonus) => CreateItemProperty(ItemPropertyType.ACBonusVsAlignmentGroup);
        public ItemProperty ItemPropertyACBonusVsDmgType(ItemPropertyDamageType nDamageType, int ACBonus) => CreateItemProperty(ItemPropertyType.ACBonusVsDamageType);
        public ItemProperty ItemPropertyACBonusVsRace(RacialType nRace, int nACBonus) => CreateItemProperty(ItemPropertyType.ACBonusVsRacialGroup);
        public ItemProperty ItemPropertyACBonusVsSAlign(ItemPropertyAlignmentType nAlign, int nACBonus) => CreateItemProperty(ItemPropertyType.ACBonusVsSpecificAlignment);
        public ItemProperty ItemPropertyEnhancementBonus(int nEnhancementBonus) => CreateItemProperty(ItemPropertyType.EnhancementBonus);
        public ItemProperty ItemPropertyEnhancementBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup, int nBonus) => CreateItemProperty(ItemPropertyType.EnhancementBonusVsAlignmentGroup);
        public ItemProperty ItemPropertyEnhancementBonusVsRace(RacialType nRace, int nBonus) => CreateItemProperty(ItemPropertyType.EnhancementBonusVsRacialGroup);
        public ItemProperty ItemPropertyEnhancementBonusVsSAlign(ItemPropertyAlignmentType nAlign, int nBonus) => CreateItemProperty(ItemPropertyType.EnhancementBonusVsSpecificAlignement);
        public ItemProperty ItemPropertyEnhancementPenalty(int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedEnhancementModifier);
        public ItemProperty ItemPropertyWeightReduction(ItemPropertyReducedWeightType nReduction) => CreateItemProperty(ItemPropertyType.BaseItemWeightReduction);
        public ItemProperty ItemPropertyBonusFeat(ItemPropertyFeatType nFeat) => CreateItemProperty(ItemPropertyType.BonusFeat);
        public ItemProperty ItemPropertyBonusLevelSpell(ItemPropertyClassType nClass, ItemPropertySpellLevelType nSpellLevel) => CreateItemProperty(ItemPropertyType.BonusSpellSlotOfLevelN);
        public ItemProperty ItemPropertyCastSpell(ItemPropertyCastSpellType nSpell, ItemPropertyCastSpellNumberUsesType nNumUses) => CreateItemProperty(ItemPropertyType.CastSpell);
        public ItemProperty ItemPropertyDamageBonus(ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => CreateItemProperty(ItemPropertyType.DamageBonus);
        public ItemProperty ItemPropertyDamageBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup, ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => CreateItemProperty(ItemPropertyType.DamageBonusVsAlignmentGroup);
        public ItemProperty ItemPropertyDamageBonusVsRace(RacialType nRace, ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => CreateItemProperty(ItemPropertyType.DamageBonusVsRacialGroup);
        public ItemProperty ItemPropertyDamageBonusVsSAlign(ItemPropertyAlignmentType nAlign, ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => CreateItemProperty(ItemPropertyType.DamageBonusVsSpecificAlignment);
        public ItemProperty ItemPropertyDamageImmunity(ItemPropertyDamageType nDamageType, ItemPropertyDamageImmunityType nImmunity) => CreateItemProperty(ItemPropertyType.ImmunityDamageType);
        public ItemProperty ItemPropertyDamagePenalty(int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedDamage);
        public ItemProperty ItemPropertyDamageReduction(ItemPropertyDamageReductionType nEnhancement, ItemPropertyDamageSoakType nHPSoak) => CreateItemProperty(ItemPropertyType.DamageReduction);
        public ItemProperty ItemPropertyDamageResistance(ItemPropertyDamageType nDamageType, ItemPropertyDamageResistType nResistance) => CreateItemProperty(ItemPropertyType.DamageResistance);
        public ItemProperty ItemPropertyDamageVulnerability(ItemPropertyDamageType nDamageType, ItemPropertyDamageVulnerabilityType nVulnerability) => CreateItemProperty(ItemPropertyType.DamageVulnerability);
        public ItemProperty ItemPropertyDarkvision() => CreateItemProperty(ItemPropertyType.Darkvision);
        public ItemProperty ItemPropertyDecreaseAbility(ItemPropertyAbilityType nAbility, int nModifier) => CreateItemProperty(ItemPropertyType.DecreasedAbilityScore);
        public ItemProperty ItemPropertyDecreaseAC(ItemPropertyArmorClassModiferType nModifierType, int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedAC);
        public ItemProperty ItemPropertyDecreaseSkill(NWNSkillType nSkill, int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedSkillModifier);
        public ItemProperty ItemPropertyContainerReducedWeight(ItemPropertyContainerWeightType nContainerType) => CreateItemProperty(ItemPropertyType.EnhancedContainerReducedWeight);
        public ItemProperty ItemPropertyExtraMeleeDamageType(ItemPropertyDamageType nDamageType) => CreateItemProperty(ItemPropertyType.ExtraMeleeDamageType);
        public ItemProperty ItemPropertyExtraRangeDamageType(ItemPropertyDamageType nDamageType) => CreateItemProperty(ItemPropertyType.ExtraRangedDamageType);
        public ItemProperty ItemPropertyHaste() => CreateItemProperty(ItemPropertyType.HasteNWN);
        public ItemProperty ItemPropertyHolyAvenger() => CreateItemProperty(ItemPropertyType.HolyAvenger);
        public ItemProperty ItemPropertyImmunityMisc(ItemPropertyImmunityMiscType nImmunityType) => CreateItemProperty(ItemPropertyType.ImmunityMiscellaneous);
        public ItemProperty ItemPropertyImprovedEvasion() => CreateItemProperty(ItemPropertyType.ImprovedEvasion);
        public ItemProperty ItemPropertyBonusSpellResistance(ItemPropertySpellResistanceBonusType nBonus) => CreateItemProperty(ItemPropertyType.SpellResistance);
        public ItemProperty ItemPropertyBonusSavingThrowVsX(ItemPropertySaveVsType nBonusType, int nBonus) => CreateItemProperty(ItemPropertyType.SavingThrowBonusSpecific);
        public ItemProperty ItemPropertyBonusSavingThrow(ItemPropertySaveBaseType nBaseSaveType, int nBonus) => CreateItemProperty(ItemPropertyType.SavingThrowBonus);
        public ItemProperty ItemPropertyKeen() => CreateItemProperty(ItemPropertyType.Keen);
        public ItemProperty ItemPropertyLight(ItemPropertyLightBrightnessType nBrightness, ItemPropertyLightColorType nColor) => CreateItemProperty(ItemPropertyType.Light);
        public ItemProperty ItemPropertyMaxRangeStrengthMod(int nModifier) => CreateItemProperty(ItemPropertyType.Mighty);
        public ItemProperty ItemPropertyNoDamage() => CreateItemProperty(ItemPropertyType.NoDamage);
        public ItemProperty ItemPropertyOnHitProps(int nProperty, int nSaveDC, int nSpecial = 0) => CreateItemProperty(ItemPropertyType.OnHitProperties);
        public ItemProperty ItemPropertyReducedSavingThrowVsX(ItemPropertySaveVsType nBaseSaveType, int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedSavingThrowsSpecific);
        public ItemProperty ItemPropertyReducedSavingThrow(ItemPropertySaveBaseType nBonusType, int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedSavingThrows);
        public ItemProperty ItemPropertyRegeneration(int nRegenAmount) => CreateItemProperty(ItemPropertyType.Regeneration);
        public ItemProperty ItemPropertySkillBonus(NWNSkillType nSkill, int nBonus) => CreateItemProperty(ItemPropertyType.SkillBonus);
        public ItemProperty ItemPropertySpellImmunitySpecific(ItemPropertyImmunitySpellType nSpell) => CreateItemProperty(ItemPropertyType.ImmunitySpecificSpell);
        public ItemProperty ItemPropertySpellImmunitySchool(SpellSchool nSchool) => CreateItemProperty(ItemPropertyType.ImmunitySpellSchool);
        public ItemProperty ItemPropertyThievesTools(int nModifier) => CreateItemProperty(ItemPropertyType.ThievesTools);
        public ItemProperty ItemPropertyAttackBonus(int nBonus) => CreateItemProperty(ItemPropertyType.AccuracyBonus);
        public ItemProperty ItemPropertyAttackBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup, int nBonus) => CreateItemProperty(ItemPropertyType.AttackBonusVsAlignmentGroup);
        public ItemProperty ItemPropertyAttackBonusVsRace(RacialType nRace, int nBonus) => CreateItemProperty(ItemPropertyType.AttackBonusVsRacialGroup);
        public ItemProperty ItemPropertyAttackBonusVsSAlign(ItemPropertyAlignmentType nAlignment, int nBonus) => CreateItemProperty(ItemPropertyType.AttackBonusVsSpecificAlignment);
        public ItemProperty ItemPropertyAttackPenalty(int nPenalty) => CreateItemProperty(ItemPropertyType.DecreasedAttackModifier);
        public ItemProperty ItemPropertyUnlimitedAmmo(ItemPropertyUnlimitedType nAmmoDamage = ItemPropertyUnlimitedType.Basic) => CreateItemProperty(ItemPropertyType.UnlimitedAmmunition);
        public ItemProperty ItemPropertyLimitUseByAlign(ItemPropertyAlignmentGroupType nAlignGroup) => CreateItemProperty(ItemPropertyType.UseLimitationAlignmentGroup);
        public ItemProperty ItemPropertyLimitUseByClass(ItemPropertyClassType nClass) => CreateItemProperty(ItemPropertyType.UseLimitationClass);
        public ItemProperty ItemPropertyLimitUseByRace(RacialType nRace) => CreateItemProperty(ItemPropertyType.UseLimitationRacialType);
        public ItemProperty ItemPropertyLimitUseBySAlign(ItemPropertyAlignmentType nAlignment) => CreateItemProperty(ItemPropertyType.UseLimitationSpecificAlignment);
        public ItemProperty BadBadReplaceMeThisDoesNothing() => CreateItemProperty(ItemPropertyType.Invalid);
        public ItemProperty ItemPropertyVampiricRegeneration(int nRegenAmount) => CreateItemProperty(ItemPropertyType.RegenerationVampiric);
        public ItemProperty ItemPropertyTrap(ItemPropertyTrapStrengthType nTrapLevel, ItemPropertyTrapType nTrapType) => CreateItemProperty(ItemPropertyType.Trap);
        public ItemProperty ItemPropertyTrueSeeing() => CreateItemProperty(ItemPropertyType.TrueSeeing);
        public ItemProperty ItemPropertyOnMonsterHitProperties(int nProperty, int nSpecial = 0) => CreateItemProperty(ItemPropertyType.OnMonsterHit);
        public ItemProperty ItemPropertyTurnResistance(int nModifier) => CreateItemProperty(ItemPropertyType.TurnResistance);
        public ItemProperty ItemPropertyMassiveCritical(ItemPropertyDamageBonusType nDamage) => CreateItemProperty(ItemPropertyType.MassiveCriticals);
        public ItemProperty ItemPropertyFreeAction() => CreateItemProperty(ItemPropertyType.FreedomOfMovement);
        public ItemProperty ItemPropertyMonsterDamage(ItemPropertyMonsterDamageType nDamage) => CreateItemProperty(ItemPropertyType.MonsterDamage);
        public ItemProperty ItemPropertyImmunityToSpellLevel(int nLevel) => CreateItemProperty(ItemPropertyType.ImmunitySpellsByLevel);
        public ItemProperty ItemPropertySpecialWalk() => CreateItemProperty(ItemPropertyType.SpecialWalk);
        public ItemProperty ItemPropertyHealersKit(int nModifier) => CreateItemProperty(ItemPropertyType.HealersKit);
        public ItemProperty ItemPropertyWeightIncrease(ItemPropertyWeightIncreaseType nWeight) => CreateItemProperty(ItemPropertyType.WeightIncrease);
        public string GetItemPropertyTag(ItemProperty nProperty) => _propertyTags.GetValueOrDefault(nProperty, "");
        public int GetItemPropertyCostTable(ItemProperty iProp) => _propertyCostTables.GetValueOrDefault(iProp, 0);
        public int GetItemPropertyCostTableValue(ItemProperty iProp) => _propertyCostTableValues.GetValueOrDefault(iProp, 0);
        public int GetItemPropertyParam1(ItemProperty iProp) => _propertyParam1s.GetValueOrDefault(iProp, 0);
        public int GetItemPropertyParam1Value(ItemProperty iProp) => _propertyParam1Values.GetValueOrDefault(iProp, 0);
        public uint CopyItemAndModify(uint oItem, ItemModelColorType nType, int nIndex, int nNewValue, bool bCopyVars = false) => (uint)(_nextPropertyId++);
        public ItemProperty ItemPropertyOnHitCastSpell(ItemPropertyOnHitCastSpellType nSpellType, int nLevel) => CreateItemProperty(ItemPropertyType.OnHitCastSpell);
        public int GetItemPropertySubType(ItemProperty iProperty) => _propertySubTypes.GetValueOrDefault(iProperty, 0);
        public ItemProperty TagItemProperty(ItemProperty nProperty, string sNewTag) { _propertyTags[nProperty] = sNewTag; return nProperty; }
        public int GetItemPropertyDuration(ItemProperty nProperty) => _propertyDurations.GetValueOrDefault(nProperty, 0);
        public int GetItemPropertyDurationRemaining(ItemProperty nProperty) => _propertyDurationRemainings.GetValueOrDefault(nProperty, 0);
        public ItemProperty ItemPropertyMaterial(int nMaterialType) => CreateItemProperty(ItemPropertyType.Material);
        public ItemProperty ItemPropertyQuality(ItemPropertyQualityType nQuality) => CreateItemProperty(ItemPropertyType.Quality);
        public ItemProperty ItemPropertyAdditional(ItemPropertyAdditionalType nAdditionalProperty) => CreateItemProperty(ItemPropertyType.Additional);
        public ItemProperty ItemPropertyArcaneSpellFailure(ItemPropertyArcaneSpellFailureType nModLevel) => CreateItemProperty(ItemPropertyType.ArcaneSpellFailure);
        public ItemProperty ItemPropertyVisualEffect(ItemVisualType nEffect) => CreateItemProperty(ItemPropertyType.Visualeffect);
        public int GetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip) => _propertyUsesPerDay.GetValueOrDefault(oItem, new Dictionary<ItemProperty, int>()).GetValueOrDefault(new ItemProperty(0), 0);
        public void SetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip, int nUsesPerDay) { if (!_propertyUsesPerDay.ContainsKey(oItem)) _propertyUsesPerDay[oItem] = new Dictionary<ItemProperty, int>(); _propertyUsesPerDay[oItem][new ItemProperty(0)] = nUsesPerDay; }
        public ItemProperty ItemPropertyCustom(ItemPropertyType nType, int nSubType = -1, int nCostTableValue = -1, int nParam1Value = -1) { var property = new ItemProperty(0); _propertyTypes[property] = nType; _propertySubTypes[property] = nSubType; _propertyCostTableValues[property] = nCostTableValue; _propertyParam1Values[property] = nParam1Value; return property; }

        // Additional item property methods from INWScriptService
        // Note: Most methods are already defined above, these are additional implementations

        private ItemProperty CreateItemProperty(ItemPropertyType type) 
        { 
            var property = new ItemProperty(0); // Create with handle 0 for mock
            _propertyTypes[property] = type; 
            return property; 
        }
    }
}
