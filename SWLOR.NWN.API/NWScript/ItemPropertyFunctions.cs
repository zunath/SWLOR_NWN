using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Alignment = SWLOR.NWN.API.NWScript.Enum.Item.Property.Alignment;
using AlignmentGroup = SWLOR.NWN.API.NWScript.Enum.Item.Property.AlignmentGroup;
using SpellSchool = SWLOR.NWN.API.NWScript.Enum.SpellSchool;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Adds an item property to the specified item.
        /// Only temporary and permanent duration types are allowed.
        /// </summary>
        /// <param name="nDurationType">The duration type of the property</param>
        /// <param name="ipProperty">The item property to add</param>
        /// <param name="oItem">The item to add the property to</param>
        /// <param name="fDuration">The duration in seconds (defaults to 0.0f)</param>
        public static void AddItemProperty(DurationType nDurationType, ItemProperty ipProperty, uint oItem,
            float fDuration = 0.0f)
        {
            global::NWN.Core.NWScript.AddItemProperty((int)nDurationType, ipProperty, oItem, fDuration);
        }

        /// <summary>
        /// Removes an item property from the specified item.
        /// </summary>
        /// <param name="oItem">The item to remove the property from</param>
        /// <param name="ipProperty">The item property to remove</param>
        public static void RemoveItemProperty(uint oItem, ItemProperty ipProperty)
        {
            global::NWN.Core.NWScript.RemoveItemProperty(oItem, ipProperty);
        }

        /// <summary>
        /// Checks if the item property is valid.
        /// </summary>
        /// <param name="ipProperty">The item property to check</param>
        /// <returns>TRUE if the item property is valid</returns>
        public static bool GetIsItemPropertyValid(ItemProperty ipProperty)
        {
            return global::NWN.Core.NWScript.GetIsItemPropertyValid(ipProperty) != 0;
        }

        /// <summary>
        /// Gets the first item property on an item.
        /// </summary>
        /// <param name="oItem">The item to get the first property from</param>
        /// <returns>The first item property, or an invalid property if none exist</returns>
        public static ItemProperty GetFirstItemProperty(uint oItem)
        {
            return global::NWN.Core.NWScript.GetFirstItemProperty(oItem);
        }

        /// <summary>
        /// Will keep retrieving the next item property on an item.
        /// Will return an invalid item property when the list is empty.
        /// </summary>
        /// <param name="oItem">The item to get the next property from</param>
        /// <returns>The next item property, or an invalid property if none remain</returns>
        public static ItemProperty GetNextItemProperty(uint oItem)
        {
            return global::NWN.Core.NWScript.GetNextItemProperty(oItem);
        }

        /// <summary>
        /// Returns the item property type (e.g. holy avenger).
        /// </summary>
        /// <param name="ip">The item property to get the type of</param>
        /// <returns>The item property type</returns>
        public static ItemPropertyType GetItemPropertyType(ItemProperty ip)
        {
            return (ItemPropertyType)global::NWN.Core.NWScript.GetItemPropertyType(ip);
        }

        /// <summary>
        /// Returns the duration type of the item property.
        /// </summary>
        /// <param name="ip">The item property to get the duration type of</param>
        /// <returns>The duration type</returns>
        public static DurationType GetItemPropertyDurationType(ItemProperty ip)
        {
            return (DurationType)global::NWN.Core.NWScript.GetItemPropertyDurationType(ip);
        }

        /// <summary>
        /// Returns an item property ability bonus.
        /// You need to specify an ability constant (IP_CONST_ABILITY_*) and the bonus.
        /// The bonus should be a positive integer between 1 and 12.
        /// </summary>
        /// <param name="nAbility">The ability type constant</param>
        /// <param name="nBonus">The bonus amount (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAbilityBonus(AbilityType nAbility, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyAbilityBonus((int)nAbility, nBonus);
        }

        /// <summary>
        /// Returns an item property AC bonus.
        /// You need to specify the bonus.
        /// The bonus should be a positive integer between 1 and 20. The modifier
        /// type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonus(int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyACBonus(nBonus);
        }

        /// <summary>
        /// Returns an item property AC bonus vs. alignment group.
        /// An example of an alignment group is Chaotic, or Good.
        /// You need to specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier
        /// type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="ACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsAlign(AlignmentGroup nAlignGroup, int ACBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyACBonusVsAlign((int)nAlignGroup, ACBonus);
        }

        /// <summary>
        /// Returns an item property AC bonus vs. damage type (e.g. piercing).
        /// You need to specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// NOTE: Only the first 3 damage types may be used here, the 3 basic physical types.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="ACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsDmgType(ItemPropertyDamageType nDamageType, int ACBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyACBonusVsDmgType((int)nDamageType, ACBonus);
        }

        /// <summary>
        /// Returns an item property AC bonus vs. racial group.
        /// You need to specify the racial group constant (IP_CONST_RACIALTYPE_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsRace(RacialType nRace, int nACBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyACBonusVsRace((int)nRace, nACBonus);
        }

        /// <summary>
        /// Returns an item property AC bonus vs. specific alignment.
        /// You need to specify the specific alignment constant (IP_CONST_ALIGNMENT_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsSAlign(Alignment nAlign, int nACBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyACBonusVsSAlign((int)nAlign, nACBonus);
        }

        /// <summary>
        /// Returns an item property enhancement bonus.
        /// You need to specify the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nEnhancementBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonus(int nEnhancementBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyEnhancementBonus(nEnhancementBonus);
        }

        /// <summary>
        /// Returns an item property enhancement bonus vs. an alignment group.
        /// You need to specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsAlign(AlignmentGroup nAlignGroup,
            int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyEnhancementBonusVsAlign((int)nAlignGroup, nBonus);
        }

        /// <summary>
        /// Returns an item property enhancement bonus vs. racial group.
        /// You need to specify the racial group constant (IP_CONST_RACIALTYPE_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsRace(RacialType nRace, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyEnhancementBonusVsRace((int)nRace, nBonus);
        }

        /// <summary>
        /// Returns an item property enhancement bonus vs. a specific alignment.
        /// You need to specify the alignment constant (IP_CONST_ALIGNMENT_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsSAlign(Alignment nAlign,
            int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyEnhancementBonusVsSAlign((int)nAlign, nBonus);
        }

        /// <summary>
        /// Returns an item property enhancement penalty.
        /// You need to specify the enhancement penalty.
        /// The enhancement penalty should be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nPenalty">The enhancement penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementPenalty(int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyEnhancementPenalty(nPenalty);
        }

        /// <summary>
        /// Returns an item property weight reduction.
        /// You need to specify the weight reduction constant (IP_CONST_REDUCEDWEIGHT_*).
        /// </summary>
        /// <param name="nReduction">The weight reduction constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyWeightReduction(ReducedWeight nReduction)
        {
            return global::NWN.Core.NWScript.ItemPropertyWeightReduction((int)nReduction);
        }

        /// <summary>
        /// Returns an item property bonus feat.
        /// You need to specify the feat constant (IP_CONST_FEAT_*).
        /// </summary>
        /// <param name="nFeat">The feat constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusFeat(ItemPropertyFeat nFeat)
        {
            return global::NWN.Core.NWScript.ItemPropertyBonusFeat((int)nFeat);
        }

        /// <summary>
        /// Returns an item property bonus level spell (bonus spell of level).
        /// You must specify the class constant (IP_CONST_CLASS_*) of the bonus spell (MUST BE a spell casting class) and the level of the bonus spell.
        /// The level of the bonus spell should be an integer between 0 and 9.
        /// </summary>
        /// <param name="nClass">The class constant (must be a spell casting class)</param>
        /// <param name="nSpellLevel">The spell level (0-9)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusLevelSpell(Class nClass, SpellLevel nSpellLevel)
        {
            return global::NWN.Core.NWScript.ItemPropertyBonusLevelSpell((int)nClass, (int)nSpellLevel);
        }

        /// <summary>
        /// Returns an item property cast spell.
        /// You must specify the spell constant (IP_CONST_CASTSPELL_*) and the number of uses constant (IP_CONST_CASTSPELL_NUMUSES_*).
        /// NOTE: The number after the name of the spell in the constant is the level at which the spell will be cast.
        /// Sometimes there are multiple copies of the same spell but they each are cast at a different level.
        /// The higher the level, the more cost will be added to the item.
        /// NOTE: The list of spells that can be applied to an item will depend on the item type.
        /// For instance there are spells that can be applied to a wand that cannot be applied to a potion.
        /// If you try to put a cast spell effect on an item that is not allowed to have that effect it will not work.
        /// </summary>
        /// <param name="nSpell">The spell constant</param>
        /// <param name="nNumUses">The number of uses constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyCastSpell(CastSpell nSpell, CastSpellNumberUses nNumUses)
        {
            return global::NWN.Core.NWScript.ItemPropertyCastSpell((int)nSpell, (int)nNumUses);
        }

        /// <summary>
        /// Returns an item property damage bonus.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonus(ItemPropertyDamageType nDamageType,
            DamageBonus nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageBonus((int)nDamageType, (int)nDamage);
        }

        /// <summary>
        /// Returns an item property damage bonus vs. alignment groups.
        /// You must specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsAlign(AlignmentGroup nAlignGroup,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageBonusVsAlign((int)nAlignGroup, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        /// Returns an item property damage bonus vs. specific race.
        /// You must specify the racial group constant (IP_CONST_RACIALTYPE_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsRace(RacialType nRace,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageBonusVsRace((int)nRace, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        /// Returns an item property damage bonus vs. specific alignment.
        /// You must specify the specific alignment constant (IP_CONST_ALIGNMENT_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsSAlign(Alignment nAlign,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageBonusVsSAlign((int)nAlign, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        /// Returns an item property damage immunity.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) that you want to be immune to and the immune bonus percentage
        /// constant (IP_CONST_DAMAGEIMMUNITY_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nImmuneBonus">The immune bonus percentage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageImmunity(ItemPropertyDamageType nDamageType,
            DamageImmunity nImmuneBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageImmunity((int)nDamageType, (int)nImmuneBonus);
        }

        /// <summary>
        /// Returns an item property damage penalty.
        /// You must specify the damage penalty.
        /// The damage penalty should be a uint, 1-5 only.
        /// Will reduce any value less than 5 to 5.
        /// </summary>
        /// <param name="nPenalty">The damage penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamagePenalty(int nPenalty)
        {
            if (nPenalty > 5) nPenalty = 5;
            return global::NWN.Core.NWScript.ItemPropertyDamagePenalty(nPenalty);
        }

        /// <summary>
        /// Returns an item property damage reduction.
        /// You must specify the enhancement level (IP_CONST_DAMAGEREDUCTION_*) that is required to get past the damage reduction
        /// and the amount of HP of damage constant (IP_CONST_DAMAGESOAK_*) will be soaked up if your weapon is not of high enough enhancement.
        /// </summary>
        /// <param name="nEnhancement">The enhancement level constant</param>
        /// <param name="nHPSoak">The HP soak amount constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageReduction(DamageReduction nEnhancement, DamageSoak nHPSoak)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageReduction((int)nEnhancement, (int)nHPSoak);
        }

        /// <summary>
        /// Returns an item property damage resistance.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the amount of HP of damage constant
        /// (IP_CONST_DAMAGERESIST_*) that will be resisted against each round.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nHPResist">The HP resistance amount constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageResistance(ItemPropertyDamageType nDamageType,
            DamageResist nHPResist)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageResistance((int)nDamageType, (int)nHPResist);
        }

        /// <summary>
        /// Returns an item property damage vulnerability.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) that you want the user to be extra vulnerable to
        /// and the percentage vulnerability constant (IP_CONST_DAMAGEVULNERABILITY_*).
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nVulnerability">The vulnerability percentage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageVulnerability(ItemPropertyDamageType nDamageType,
            DamageVulnerability nVulnerability)
        {
            return global::NWN.Core.NWScript.ItemPropertyDamageVulnerability((int)nDamageType, (int)nVulnerability);
        }

        /// <summary>
        /// Returns an item property darkvision.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDarkvision()
        {
            return global::NWN.Core.NWScript.ItemPropertyDarkvision();
        }

        /// <summary>
        /// Returns an item property decrease ability score.
        /// You must specify the ability constant (IP_CONST_ABILITY_*) and the modifier constant.
        /// The modifier must be a POSITIVE integer between 1 and 10 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nAbility">The ability constant</param>
        /// <param name="nModifier">The modifier amount (1-10)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseAbility(Ability nAbility, int nModifier)
        {
            return global::NWN.Core.NWScript.ItemPropertyDecreaseAbility((int)nAbility, nModifier);
        }

        /// <summary>
        /// Returns an item property decrease armor class.
        /// You must specify the armor modifier type constant (IP_CONST_ACMODIFIERTYPE_*) and the armor class penalty.
        /// The penalty must be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nModifierType">The armor modifier type constant</param>
        /// <param name="nPenalty">The armor class penalty (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseAC(ArmorClassModiferType nModifierType, int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyDecreaseAC((int)nModifierType, nPenalty);
        }

        /// <summary>
        /// Returns an item property decrease skill.
        /// You must specify the constant for the skill to be decreased (SKILL_*) and the amount of the penalty.
        /// The penalty must be a POSITIVE integer between 1 and 10 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nSkill">The skill type constant</param>
        /// <param name="nPenalty">The skill penalty (1-10)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseSkill(NWNSkillType nSkill, int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyDecreaseSkill((int)nSkill, nPenalty);
        }

        /// <summary>
        /// Returns an item property container reduced weight.
        /// This is used for special containers that reduce the weight of the objects inside them.
        /// You must specify the container weight reduction type constant (IP_CONST_CONTAINERWEIGHTRED_*).
        /// </summary>
        /// <param name="nContainerType">The container weight reduction type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyContainerReducedWeight(ContainerWeight nContainerType)
        {
            return global::NWN.Core.NWScript.ItemPropertyContainerReducedWeight((int)nContainerType);
        }

        /// <summary>
        /// Returns an item property extra melee damage type.
        /// You must specify the extra melee base damage type that you want applied. It is a constant (IP_CONST_DAMAGETYPE_*).
        /// NOTE: Only the first 3 base types (piercing, slashing, & bludgeoning) are applicable here.
        /// NOTE: It is also only applicable to melee weapons.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyExtraMeleeDamageType(ItemPropertyDamageType nDamageType)
        {
            return global::NWN.Core.NWScript.ItemPropertyExtraMeleeDamageType((int)nDamageType);
        }

        /// <summary>
        /// Returns an item property extra ranged damage type.
        /// You must specify the extra ranged base damage type that you want applied. It is a constant (IP_CONST_DAMAGETYPE_*).
        /// NOTE: Only the first 3 base types (piercing, slashing, & bludgeoning) are applicable here.
        /// NOTE: It is also only applicable to ranged weapons.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyExtraRangeDamageType(ItemPropertyDamageType nDamageType)
        {
            return global::NWN.Core.NWScript.ItemPropertyExtraRangeDamageType((int)nDamageType);
        }

        /// <summary>
        /// Returns an item property haste.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHaste()
        {
            return global::NWN.Core.NWScript.ItemPropertyHaste();
        }

        /// <summary>
        /// Returns an item property holy avenger.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHolyAvenger()
        {
            return global::NWN.Core.NWScript.ItemPropertyHolyAvenger();
        }

        /// <summary>
        /// Returns an item property immunity to miscellaneous effects.
        /// You must specify the effect to which the user is immune, it is a constant (IP_CONST_IMMUNITYMISC_*).
        /// </summary>
        /// <param name="nImmunityType">The immunity type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImmunityMisc(ImmunityMisc nImmunityType)
        {
            return global::NWN.Core.NWScript.ItemPropertyImmunityMisc((int)nImmunityType);
        }

        /// <summary>
        /// Returns an item property improved evasion.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImprovedEvasion()
        {
            return global::NWN.Core.NWScript.ItemPropertyImprovedEvasion();
        }

        /// <summary>
        /// Returns an item property bonus spell resistance.
        /// You must specify the bonus spell resistance constant (IP_CONST_SPELLRESISTANCEBONUS_*).
        /// </summary>
        /// <param name="nBonus">The spell resistance bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSpellResistance(SpellResistanceBonus nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyBonusSpellResistance((int)nBonus);
        }

        /// <summary>
        /// Returns an item property saving throw bonus vs. a specific effect or damage type.
        /// You must specify the save type constant (IP_CONST_SAVEVS_*) that the bonus is applied to and the bonus that is be applied.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBonusType">The save type constant</param>
        /// <param name="nBonus">The bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSavingThrowVsX(SaveVs nBonusType, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyBonusSavingThrowVsX((int)nBonusType, nBonus);
        }

        /// <summary>
        /// Returns an item property saving throw bonus to the base type (e.g. will, reflex, fortitude).
        /// You must specify the base type constant (IP_CONST_SAVEBASETYPE_*) to which the user gets the bonus and the bonus that he/she will get.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBaseSaveType">The base save type constant</param>
        /// <param name="nBonus">The bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSavingThrow(SaveBaseType nBaseSaveType, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyBonusSavingThrow((int)nBaseSaveType, nBonus);
        }

        /// <summary>
        /// Returns an item property keen.
        /// This means a critical threat range of 19-20 on a weapon will be increased to 17-20 etc.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyKeen()
        {
            return global::NWN.Core.NWScript.ItemPropertyKeen();
        }

        /// <summary>
        /// Returns an item property light.
        /// You must specify the intensity constant of the light (IP_CONST_LIGHTBRIGHTNESS_*) and the color constant of the light (IP_CONST_LIGHTCOLOR_*).
        /// </summary>
        /// <param name="nBrightness">The light brightness constant</param>
        /// <param name="nColor">The light color constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLight(LightBrightness nBrightness, LightColor nColor)
        {
            return global::NWN.Core.NWScript.ItemPropertyLight((int)nBrightness, (int)nColor);
        }

        /// <summary>
        /// Returns an item property max range strength modification (e.g. mighty).
        /// You must specify the maximum modifier for strength that is allowed on a ranged weapon.
        /// The modifier must be a positive integer between 1 and 20.
        /// </summary>
        /// <param name="nModifier">The strength modifier (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMaxRangeStrengthMod(int nModifier)
        {
            return global::NWN.Core.NWScript.ItemPropertyMaxRangeStrengthMod(nModifier);
        }

        /// <summary>
        /// Returns an item property no damage.
        /// This means the weapon will do no damage in combat.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyNoDamage()
        {
            return global::NWN.Core.NWScript.ItemPropertyNoDamage();
        }

        /// <summary>
        /// Returns an item property on hit effect property.
        /// You must specify the on hit property constant (IP_CONST_ONHIT_*) and the save DC constant (IP_CONST_ONHIT_SAVEDC_*).
        /// Some of the item properties require a special parameter as well. If the property does not require one you may leave out the last one.
        /// The list of the ones with 3 parameters and what they are are as follows:
        /// ABILITYDRAIN: nSpecial is the ability it is to drain. constant(IP_CONST_ABILITY_*)
        /// BLINDNESS: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// CONFUSION: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DAZE: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DEAFNESS: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DISEASE: nSpecial is the type of disease that will effect the victim. constant(DISEASE_*)
        /// DOOM: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// FEAR: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// HOLD: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// ITEMPOISON: nSpecial is the type of poison that will effect the victim. constant(IP_CONST_POISON_*)
        /// SILENCE: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// SLAYRACE: nSpecial is the race that will be slain. constant(IP_CONST_RACIALTYPE_*)
        /// SLAYALIGNMENTGROUP: nSpecial is the alignment group that will be slain (e.g. chaotic). constant(IP_CONST_ALIGNMENTGROUP_*)
        /// SLAYALIGNMENT: nSpecial is the specific alignment that will be slain. constant(IP_CONST_ALIGNMENT_*)
        /// SLEEP: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// SLOW: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// STUN: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// </summary>
        /// <param name="nProperty">The on hit property constant</param>
        /// <param name="nSaveDC">The save DC constant</param>
        /// <param name="nSpecial">The special parameter (defaults to 0)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnHitProps(int nProperty, int nSaveDC, int nSpecial = 0)
        {
            return global::NWN.Core.NWScript.ItemPropertyOnHitProps(nProperty, nSaveDC, nSpecial);
        }

        /// <summary>
        /// Returns an item property reduced saving throw vs. an effect or damage type.
        /// You must specify the constant to which the penalty applies (IP_CONST_SAVEVS_*) and the penalty to be applied.
        /// The penalty must be a POSITIVE integer between 1 and 20 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nBaseSaveType">The save type constant</param>
        /// <param name="nPenalty">The penalty amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyReducedSavingThrowVsX(SaveVs nBaseSaveType, int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyReducedSavingThrowVsX((int)nBaseSaveType, nPenalty);
        }

        /// <summary>
        /// Returns an item property reduced saving to base type.
        /// You must specify the base type to which the penalty applies (e.g. will, reflex, or fortitude) and the penalty to be applied.
        /// The constant for the base type starts with (IP_CONST_SAVEBASETYPE_*).
        /// The penalty must be a POSITIVE integer between 1 and 20 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nBonusType">The base save type constant</param>
        /// <param name="nPenalty">The penalty amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyReducedSavingThrow(SaveBaseType nBonusType, int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyReducedSavingThrow((int)nBonusType, nPenalty);
        }

        /// <summary>
        /// Returns an item property regeneration.
        /// You must specify the regeneration amount.
        /// The amount must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRegenAmount">The regeneration amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyRegeneration(int nRegenAmount)
        {
            return global::NWN.Core.NWScript.ItemPropertyRegeneration(nRegenAmount);
        }

        /// <summary>
        /// Returns an item property skill bonus.
        /// You must specify the skill to which the user will get a bonus (SKILL_*) and the amount of the bonus.
        /// The bonus amount must be an integer between 1 and 50.
        /// </summary>
        /// <param name="nSkill">The skill type constant</param>
        /// <param name="nBonus">The bonus amount (1-50)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySkillBonus(NWNSkillType nSkill, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertySkillBonus((int)nSkill, nBonus);
        }

        /// <summary>
        /// Returns an item property spell immunity vs. specific spell.
        /// You must specify the spell to which the user will be immune (IP_CONST_IMMUNITYSPELL_*).
        /// </summary>
        /// <param name="nSpell">The immunity spell constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpellImmunitySpecific(ImmunitySpell nSpell)
        {
            return global::NWN.Core.NWScript.ItemPropertySpellImmunitySpecific((int)nSpell);
        }

        /// <summary>
        /// Returns an item property spell immunity vs. spell school.
        /// You must specify the school to which the user will be immune (IP_CONST_SPELLSCHOOL_*).
        /// </summary>
        /// <param name="nSchool">The spell school constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpellImmunitySchool(SpellSchool nSchool)
        {
            return global::NWN.Core.NWScript.ItemPropertySpellImmunitySchool((int)nSchool);
        }

        /// <summary>
        /// Returns an item property thieves tools.
        /// You must specify the modifier you wish the tools to have.
        /// The modifier must be an integer between 1 and 12.
        /// </summary>
        /// <param name="nModifier">The modifier amount (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyThievesTools(int nModifier)
        {
            return global::NWN.Core.NWScript.ItemPropertyThievesTools(nModifier);
        }

        /// <summary>
        /// Returns an item property attack bonus.
        /// You must specify an attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonus(int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyAttackBonus(nBonus);
        }

        /// <summary>
        /// Returns an item property attack bonus vs. alignment group.
        /// You must specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsAlign(AlignmentGroup nAlignGroup,
            int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyAttackBonusVsAlign((int)nAlignGroup, nBonus);
        }

        /// <summary>
        /// Returns an item property attack bonus vs. racial group.
        /// You must specify the racial group constant (IP_CONST_RACIALTYPE_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsRace(RacialType nRace, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyAttackBonusVsRace((int)nRace, nBonus);
        }

        /// <summary>
        /// Returns an item property attack bonus vs. a specific alignment.
        /// You must specify the alignment you want the bonus to work against (IP_CONST_ALIGNMENT_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignment">The alignment constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsSAlign(Alignment nAlignment, int nBonus)
        {
            return global::NWN.Core.NWScript.ItemPropertyAttackBonusVsSAlign((int)nAlignment, nBonus);
        }

        /// <summary>
        /// Returns an item property attack penalty.
        /// You must specify the attack penalty.
        /// The penalty must be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nPenalty">The attack penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackPenalty(int nPenalty)
        {
            return global::NWN.Core.NWScript.ItemPropertyAttackPenalty(nPenalty);
        }

        /// <summary>
        /// Returns an item property unlimited ammo.
        /// If you leave the parameter field blank it will be just a normal bolt, arrow, or bullet.
        /// However you may specify that you want the ammunition to do special damage (e.g. +1d6 Fire, or +1 enhancement bonus).
        /// For this parameter you use the constants beginning with (IP_CONST_UNLIMITEDAMMO_*).
        /// </summary>
        /// <param name="nAmmoDamage">The ammo damage type (defaults to Basic)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyUnlimitedAmmo(Unlimited nAmmoDamage = Unlimited.Basic)
        {
            return global::NWN.Core.NWScript.ItemPropertyUnlimitedAmmo((int)nAmmoDamage);
        }

        /// <summary>
        /// Returns an item property limit use by alignment group.
        /// You must specify the alignment group(s) that you want to be able to use this item (IP_CONST_ALIGNMENTGROUP_*).
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByAlign(AlignmentGroup nAlignGroup)
        {
            return global::NWN.Core.NWScript.ItemPropertyLimitUseByAlign((int)nAlignGroup);
        }

        /// <summary>
        /// Returns an item property limit use by class.
        /// You must specify the class(es) who are able to use this item (IP_CONST_CLASS_*).
        /// </summary>
        /// <param name="nClass">The class constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByClass(Class nClass)
        {
            return global::NWN.Core.NWScript.ItemPropertyLimitUseByClass((int)nClass);
        }

        /// <summary>
        /// Returns an item property limit use by race.
        /// You must specify the race(s) who are allowed to use this item (IP_CONST_RACIALTYPE_*).
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByRace(RacialType nRace)
        {
            return global::NWN.Core.NWScript.ItemPropertyLimitUseByRace((int)nRace);
        }

        /// <summary>
        /// Returns an item property limit use by specific alignment.
        /// You must specify the alignment(s) of those allowed to use the item (IP_CONST_ALIGNMENT_*).
        /// </summary>
        /// <param name="nAlignment">The alignment constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseBySAlign(Alignment nAlignment)
        {
            return global::NWN.Core.NWScript.ItemPropertyLimitUseBySAlign((int)nAlignment);
        }

        /// <summary>
        /// Replace this function it does nothing.
        /// </summary>
        /// <returns>An invalid item property</returns>
        public static ItemProperty BadBadReplaceMeThisDoesNothing()
        {
            return global::NWN.Core.NWScript.BadBadReplaceMeThisDoesNothing();
        }

        /// <summary>
        /// Returns an item property vampiric regeneration.
        /// You must specify the amount of regeneration.
        /// The regen amount must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRegenAmount">The regeneration amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyVampiricRegeneration(int nRegenAmount)
        {
            return global::NWN.Core.NWScript.ItemPropertyVampiricRegeneration(nRegenAmount);
        }

        /// <summary>
        /// Returns an item property trap.
        /// You must specify the trap level constant (IP_CONST_TRAPSTRENGTH_*) and the trap type constant (IP_CONST_TRAPTYPE_*).
        /// </summary>
        /// <param name="nTrapLevel">The trap level constant</param>
        /// <param name="nTrapType">The trap type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTrap(TrapStrength nTrapLevel, TrapType nTrapType)
        {
            return global::NWN.Core.NWScript.ItemPropertyTrap((int)nTrapLevel, (int)nTrapType);
        }

        /// <summary>
        /// Returns an item property true seeing.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTrueSeeing()
        {
            return global::NWN.Core.NWScript.ItemPropertyTrueSeeing();
        }

        /// <summary>
        /// Returns an item property monster on hit apply effect property.
        /// You must specify the property that you want applied on hit.
        /// There are some properties that require an additional special parameter to be specified.
        /// The others that don't require any additional parameter you may just put in the one.
        /// The special cases are as follows:
        /// ABILITYDRAIN: nSpecial is the ability to drain. constant(IP_CONST_ABILITY_*)
        /// DISEASE: nSpecial is the disease that you want applied. constant(DISEASE_*)
        /// LEVELDRAIN: nSpecial is the number of levels that you want drained. integer between 1 and 5.
        /// POISON: nSpecial is the type of poison that will effect the victim. constant(IP_CONST_POISON_*)
        /// WOUNDING: nSpecial is the amount of wounding. integer between 1 and 5.
        /// NOTE: Any that do not appear in the above list do not require the second parameter.
        /// NOTE: These can only be applied to monster NATURAL weapons (e.g. bite, claw, gore, and slam). IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        /// <param name="nProperty">The property constant</param>
        /// <param name="nSpecial">The special parameter (defaults to 0)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnMonsterHitProperties(int nProperty, int nSpecial = 0)
        {
            return global::NWN.Core.NWScript.ItemPropertyOnMonsterHitProperties(nProperty, nSpecial);
        }

        /// <summary>
        /// Returns an item property turn resistance.
        /// You must specify the resistance bonus.
        /// The bonus must be an integer between 1 and 50.
        /// </summary>
        /// <param name="nModifier">The resistance bonus (1-50)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTurnResistance(int nModifier)
        {
            return global::NWN.Core.NWScript.ItemPropertyTurnResistance(nModifier);
        }

        /// <summary>
        /// Returns an item property massive critical.
        /// You must specify the extra damage constant (IP_CONST_DAMAGEBONUS_*) of the criticals.
        /// </summary>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMassiveCritical(DamageBonus nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyMassiveCritical((int)nDamage);
        }

        /// <summary>
        /// Returns an item property free action.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyFreeAction()
        {
            return global::NWN.Core.NWScript.ItemPropertyFreeAction();
        }

        /// <summary>
        /// Returns an item property monster damage.
        /// You must specify the amount of damage the monster's attack will do (IP_CONST_MONSTERDAMAGE_*).
        /// NOTE: These can only be applied to monster NATURAL weapons (e.g. bite, claw, gore, and slam). IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        /// <param name="nDamage">The monster damage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMonsterDamage(MonsterDamage nDamage)
        {
            return global::NWN.Core.NWScript.ItemPropertyMonsterDamage((int)nDamage);
        }

        /// <summary>
        /// Returns an item property immunity to spell level.
        /// You must specify the level of which that and below the user will be immune.
        /// The level must be an integer between 1 and 9.
        /// By putting in a 3 it will mean the user is immune to all 3rd level and lower spells.
        /// </summary>
        /// <param name="nLevel">The spell level (1-9)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImmunityToSpellLevel(int nLevel)
        {
            return global::NWN.Core.NWScript.ItemPropertyImmunityToSpellLevel(nLevel);
        }

        /// <summary>
        /// Returns an item property special walk.
        /// If no parameters are specified it will automatically use the zombie walk.
        /// This will apply the special walk animation to the user.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpecialWalk()
        {
            return global::NWN.Core.NWScript.ItemPropertySpecialWalk(0);
        }

        /// <summary>
        /// Returns an item property healers kit.
        /// You must specify the level of the kit.
        /// The modifier must be an integer between 1 and 12.
        /// </summary>
        /// <param name="nModifier">The kit level modifier (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHealersKit(int nModifier)
        {
            return global::NWN.Core.NWScript.ItemPropertyHealersKit(nModifier);
        }

        /// <summary>
        /// Returns an item property weight increase.
        /// You must specify the weight increase constant (IP_CONST_WEIGHTINCREASE_*).
        /// </summary>
        /// <param name="nWeight">The weight increase constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyWeightIncrease(WeightIncrease nWeight)
        {
            return global::NWN.Core.NWScript.ItemPropertyWeightIncrease((int)nWeight);
        }

        /// <summary>
        /// Returns the string tag set for the provided item property.
        /// If no tag has been set, returns an empty string.
        /// </summary>
        /// <param name="nProperty">The item property to get the tag from</param>
        /// <returns>The string tag, or empty string if no tag is set</returns>
        public static string GetItemPropertyTag(ItemProperty nProperty)
        {
            return global::NWN.Core.NWScript.GetItemPropertyTag(nProperty);
        }

        /// <summary>
        /// Returns the cost table number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the cost table from</param>
        /// <returns>The cost table number</returns>
        public static int GetItemPropertyCostTable(ItemProperty iProp)
        {
            return global::NWN.Core.NWScript.GetItemPropertyCostTable(iProp);
        }

        /// <summary>
        /// Returns the cost table value (index of the cost table) of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the cost table value from</param>
        /// <returns>The cost table value</returns>
        public static int GetItemPropertyCostTableValue(ItemProperty iProp)
        {
            return global::NWN.Core.NWScript.GetItemPropertyCostTableValue(iProp);
        }

        /// <summary>
        /// Returns the param1 number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the param1 from</param>
        /// <returns>The param1 number</returns>
        public static int GetItemPropertyParam1(ItemProperty iProp)
        {
            return global::NWN.Core.NWScript.GetItemPropertyParam1(iProp);
        }

        /// <summary>
        /// Returns the param1 value of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the param1 value from</param>
        /// <returns>The param1 value</returns>
        public static int GetItemPropertyParam1Value(ItemProperty iProp)
        {
            return global::NWN.Core.NWScript.GetItemPropertyParam1Value(iProp);
        }

        /// <summary>
        /// Creates a new copy of an item, while making a single change to the appearance of the item.
        /// Helmet models and simple items ignore iIndex.
        /// iType                            iIndex                              iNewValue
        /// ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        /// ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            1-9
        /// ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        /// ITEM_APPR_TYPE_ARMOR_MODEL       ITEM_APPR_ARMOR_MODEL_*             Model #
        /// ITEM_APPR_TYPE_ARMOR_COLOR       ITEM_APPR_ARMOR_COLOR_* [0]         0-175 [1]
        /// [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is
        /// desired, the following equation can be used for nIndex to achieve that:
        /// ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        /// For example, to change the CLOTH1 channel of the torso, nIndex would be:
        /// 6 + (7 * 6) + 2 = 50
        /// [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical
        /// function 'clear colour override', which clears the per-part override for that part.
        /// </summary>
        /// <param name="oItem">The item to copy and modify</param>
        /// <param name="nType">The appearance type</param>
        /// <param name="nIndex">The appearance index</param>
        /// <param name="nNewValue">The new value</param>
        /// <param name="bCopyVars">Whether to copy variables (defaults to false)</param>
        /// <returns>The new item</returns>
        public static uint CopyItemAndModify(uint oItem, ItemAppearanceType nType, int nIndex, int nNewValue,
            bool bCopyVars = false)
        {
            return global::NWN.Core.NWScript.CopyItemAndModify(oItem, (int)nType, nIndex, nNewValue, bCopyVars ? 1 : 0);
        }

        /// <summary>
        /// Creates an item property that (when applied to a weapon item) causes a spell to be cast
        /// when a successful strike is made, or (when applied to armor) is struck by an opponent.
        /// nSpell uses the IP_CONST_ONHIT_CASTSPELL_* constants
        /// </summary>
        /// <param name="nSpellType">The spell type constant</param>
        /// <param name="nLevel">The spell level</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnHitCastSpell(OnHitCastSpellType nSpellType, int nLevel)
        {
            return global::NWN.Core.NWScript.ItemPropertyOnHitCastSpell((int)nSpellType, nLevel);
        }

        /// <summary>
        /// Returns the sub type number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProperty">The item property to get the sub type from</param>
        /// <returns>The sub type number</returns>
        public static int GetItemPropertySubType(ItemProperty iProperty)
        {
            return global::NWN.Core.NWScript.GetItemPropertySubType(iProperty);
        }

        /// <summary>
        /// Tags the item property with the provided string.
        /// Any tags currently set on the item property will be overwritten.
        /// </summary>
        /// <param name="nProperty">The item property to tag</param>
        /// <param name="sNewTag">The new tag string</param>
        /// <returns>The tagged item property</returns>
        public static ItemProperty TagItemProperty(ItemProperty nProperty, string sNewTag)
        {
            return global::NWN.Core.NWScript.TagItemProperty(nProperty, sNewTag);
        }

        /// <summary>
        /// Returns the total duration of the item property in seconds.
        /// Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        /// <param name="nProperty">The item property to get the duration from</param>
        /// <returns>The total duration in seconds</returns>
        public static int GetItemPropertyDuration(ItemProperty nProperty)
        {
            return global::NWN.Core.NWScript.GetItemPropertyDuration(nProperty);
        }

        /// <summary>
        /// Returns the remaining duration of the item property in seconds.
        /// Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        /// <param name="nProperty">The item property to get the remaining duration from</param>
        /// <returns>The remaining duration in seconds</returns>
        public static int GetItemPropertyDurationRemaining(ItemProperty nProperty)
        {
            return global::NWN.Core.NWScript.GetItemPropertyDurationRemaining(nProperty);
        }

        /// <summary>
        /// Returns an item property material.
        /// You need to specify the material type.
        /// nMaterialType: The material type should be a positive integer between 0 and 77 (see iprp_matcost.2da).
        /// Note: The material type property will only affect the cost of the item if you modify the cost in the iprp_matcost.2da.
        /// </summary>
        /// <param name="nMaterialType">The material type (0-77)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMaterial(int nMaterialType)
        {
            return global::NWN.Core.NWScript.ItemPropertyMaterial(nMaterialType);
        }

        /// <summary>
        /// Returns an item property quality.
        /// You need to specify the quality.
        /// nQuality: The quality of the item property to create (see iprp_qualcost.2da).
        /// IP_CONST_QUALITY_*
        /// Note: The quality property will only affect the cost of the item if you modify the cost in the iprp_qualcost.2da.
        /// </summary>
        /// <param name="nQuality">The quality constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyQuality(Quality nQuality)
        {
            return global::NWN.Core.NWScript.ItemPropertyQuality((int)nQuality);
        }

        /// <summary>
        /// Returns a generic additional item property.
        /// You need to specify the additional property.
        /// nProperty: The item property to create (see iprp_addcost.2da).
        /// IP_CONST_ADDITIONAL_*
        /// Note: The additional property only affects the cost of the item if you modify the cost in the iprp_addcost.2da.
        /// </summary>
        /// <param name="nAdditionalProperty">The additional property constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAdditional(Additional nAdditionalProperty)
        {
            return global::NWN.Core.NWScript.ItemPropertyAdditional((int)nAdditionalProperty);
        }

        /// <summary>
        /// Creates an item property that offsets the effect on arcane spell failure
        /// that a particular item has. Parameters come from the ITEM_PROP_ASF_* group.
        /// </summary>
        /// <param name="nModLevel">The arcane spell failure modification level</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyArcaneSpellFailure(ArcaneSpellFailure nModLevel)
        {
            return global::NWN.Core.NWScript.ItemPropertyArcaneSpellFailure((int)nModLevel);
        }

        /// <summary>
        /// Creates a visual effect (ITEM_VISUAL_*) that may be applied to
        /// melee weapons only.
        /// </summary>
        /// <param name="nEffect">The visual effect constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyVisualEffect(ItemVisual nEffect)
        {
            return global::NWN.Core.NWScript.ItemPropertyVisualEffect((int)nEffect);
        }

        /// <summary>
        /// Returns the number of uses per day remaining of the given item and item property.
        /// Will return 0 if the given item does not have the requested item property,
        /// or the item property is not uses/day.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <param name="ip">The item property to check</param>
        /// <returns>The number of uses per day remaining</returns>
        public static int GetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip)
        {
            return global::NWN.Core.NWScript.GetItemPropertyUsesPerDayRemaining(oItem, ip);
        }

        /// <summary>
        /// Sets the number of uses per day remaining of the given item and item property.
        /// Will do nothing if the given item and item property is not uses/day.
        /// Will constrain nUsesPerDay to the maximum allowed as the cost table defines.
        /// </summary>
        /// <param name="oItem">The item to set</param>
        /// <param name="ip">The item property to set</param>
        /// <param name="nUsesPerDay">The number of uses per day</param>
        public static void SetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip, int nUsesPerDay)
        {
            global::NWN.Core.NWScript.SetItemPropertyUsesPerDayRemaining(oItem, ip, nUsesPerDay);
        }

        /// <summary>
        /// Constructs a custom item property given all the parameters explicitly.
        /// This function can be used in place of all the other ItemPropertyXxx constructors
        /// Use GetItemProperty{Type,SubType,CostTableValue,Param1Value} to see the values for a given item property.
        /// </summary>
        /// <param name="nType">The item property type</param>
        /// <param name="nSubType">The sub type (defaults to -1)</param>
        /// <param name="nCostTableValue">The cost table value (defaults to -1)</param>
        /// <param name="nParam1Value">The param1 value (defaults to -1)</param>
        /// <returns>The custom item property</returns>
        public static ItemProperty ItemPropertyCustom(ItemPropertyType nType, int nSubType = -1, int nCostTableValue = -1, int nParam1Value = -1)
        {
            return global::NWN.Core.NWScript.ItemPropertyCustom((int)nType, nSubType, nCostTableValue, nParam1Value);
        }

    }
}