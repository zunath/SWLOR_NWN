using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using Alignment = SWLOR.Game.Server.Core.NWScript.Enum.Item.Property.Alignment;
using AlignmentGroup = SWLOR.Game.Server.Core.NWScript.Enum.Item.Property.AlignmentGroup;
using SpellSchool = SWLOR.Game.Server.Core.NWScript.Enum.SpellSchool;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   adds an item property to the specified item
        ///   Only temporary and permanent duration types are allowed.
        /// </summary>
        public static void AddItemProperty(DurationType nDurationType, ItemProperty ipProperty, uint oItem,
            float fDuration = 0.0f)
        {
            NWN.Core.NWScript.AddItemProperty((int)nDurationType, ipProperty, oItem, fDuration);
        }

        /// <summary>
        ///   removes an item property from the specified item
        /// </summary>
        public static void RemoveItemProperty(uint oItem, ItemProperty ipProperty)
        {
            NWN.Core.NWScript.RemoveItemProperty(oItem, ipProperty);
        }

        /// <summary>
        ///   if the item property is valid this will return true
        /// </summary>
        public static bool GetIsItemPropertyValid(ItemProperty ipProperty)
        {
            return NWN.Core.NWScript.GetIsItemPropertyValid(ipProperty) != 0;
        }

        /// <summary>
        ///   Gets the first item property on an item
        /// </summary>
        public static ItemProperty GetFirstItemProperty(uint oItem)
        {
            return NWN.Core.NWScript.GetFirstItemProperty(oItem);
        }

        /// <summary>
        ///   Will keep retrieving the next and the next item property on an Item,
        ///   will return an invalid item property when the list is empty.
        /// </summary>
        public static ItemProperty GetNextItemProperty(uint oItem)
        {
            return NWN.Core.NWScript.GetNextItemProperty(oItem);
        }

        /// <summary>
        ///   will return the item property type (ie. holy avenger)
        /// </summary>
        public static ItemPropertyType GetItemPropertyType(ItemProperty ip)
        {
            return (ItemPropertyType)NWN.Core.NWScript.GetItemPropertyType(ip);
        }

        /// <summary>
        ///   will return the duration type of the item property
        /// </summary>
        public static DurationType GetItemPropertyDurationType(ItemProperty ip)
        {
            return (DurationType)NWN.Core.NWScript.GetItemPropertyDurationType(ip);
        }

        /// <summary>
        ///   Returns Item property ability bonus.  You need to specify an
        ///   ability constant(IP_CONST_ABILITY_*) and the bonus.  The bonus should
        ///   be a positive integer between 1 and 12.
        /// </summary>
        public static ItemProperty ItemPropertyAbilityBonus(AbilityType nAbility, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyAbilityBonus((int)nAbility, nBonus);
        }

        /// <summary>
        ///   Returns Item property AC bonus.  You need to specify the bonus.
        ///   The bonus should be a positive integer between 1 and 20. The modifier
        ///   type depends on the item it is being applied to.
        /// </summary>
        public static ItemProperty ItemPropertyACBonus(int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyACBonus(nBonus);
        }

        /// <summary>
        ///   Returns Item property AC bonus vs. alignment group.  An example of
        ///   an alignment group is Chaotic, or Good.  You need to specify the
        ///   alignment group constant(IP_CONST_ALIGNMENTGROUP_*) and the AC bonus.
        ///   The AC bonus should be an integer between 1 and 20.  The modifier
        ///   type depends on the item it is being applied to.
        /// </summary>
        public static ItemProperty ItemPropertyACBonusVsAlign(AlignmentGroup nAlignGroup, int ACBonus)
        {
            return NWN.Core.NWScript.ItemPropertyACBonusVsAlign((int)nAlignGroup, ACBonus);
        }

        /// <summary>
        ///   Returns Item property AC bonus vs. Damage type (ie. piercing).  You
        ///   need to specify the damage type constant(IP_CONST_DAMAGETYPE_*) and the
        ///   AC bonus.  The AC bonus should be an integer between 1 and 20.  The
        ///   modifier type depends on the item it is being applied to.
        ///   NOTE: Only the first 3 damage types may be used here, the 3 basic
        ///   physical types.
        /// </summary>
        public static ItemProperty ItemPropertyACBonusVsDmgType(ItemPropertyDamageType nDamageType, int ACBonus)
        {
            return NWN.Core.NWScript.ItemPropertyACBonusVsDmgType((int)nDamageType, ACBonus);
        }

        /// <summary>
        ///   Returns Item property AC bonus vs. Racial group.  You need to specify
        ///   the racial group constant(IP_CONST_RACIALTYPE_*) and the AC bonus.  The AC
        ///   bonus should be an integer between 1 and 20.  The modifier type depends
        ///   on the item it is being applied to.
        /// </summary>
        public static ItemProperty ItemPropertyACBonusVsRace(RacialType nRace, int nACBonus)
        {
            return NWN.Core.NWScript.ItemPropertyACBonusVsRace((int)nRace, nACBonus);
        }

        /// <summary>
        ///   Returns Item property AC bonus vs. Specific alignment.  You need to
        ///   specify the specific alignment constant(IP_CONST_ALIGNMENT_*) and the AC
        ///   bonus.  The AC bonus should be an integer between 1 and 20.  The
        ///   modifier type depends on the item it is being applied to.
        /// </summary>
        public static ItemProperty ItemPropertyACBonusVsSAlign(Alignment nAlign, int nACBonus)
        {
            return NWN.Core.NWScript.ItemPropertyACBonusVsSAlign((int)nAlign, nACBonus);
        }

        /// <summary>
        ///   Returns Item property Enhancement bonus.  You need to specify the
        ///   enhancement bonus.  The Enhancement bonus should be an integer between
        ///   1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyEnhancementBonus(int nEnhancementBonus)
        {
            return NWN.Core.NWScript.ItemPropertyEnhancementBonus(nEnhancementBonus);
        }

        /// <summary>
        ///   Returns Item property Enhancement bonus vs. an Alignment group.  You
        ///   need to specify the alignment group constant(IP_CONST_ALIGNMENTGROUP_*)
        ///   and the enhancement bonus.  The Enhancement bonus should be an integer
        ///   between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyEnhancementBonusVsAlign(AlignmentGroup nAlignGroup,
            int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyEnhancementBonusVsAlign((int)nAlignGroup, nBonus);
        }

        /// <summary>
        ///   Returns Item property Enhancement bonus vs. Racial group.  You need
        ///   to specify the racial group constant(IP_CONST_RACIALTYPE_*) and the
        ///   enhancement bonus.  The enhancement bonus should be an integer between
        ///   1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyEnhancementBonusVsRace(RacialType nRace, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyEnhancementBonusVsRace((int)nRace, nBonus);
        }

        /// <summary>
        ///   Returns Item property Enhancement bonus vs. a specific alignment.  You
        ///   need to specify the alignment constant(IP_CONST_ALIGNMENT_*) and the
        ///   enhancement bonus.  The enhancement bonus should be an integer between
        ///   1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyEnhancementBonusVsSAlign(Alignment nAlign,
            int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyEnhancementBonusVsSAlign((int)nAlign, nBonus);
        }

        /// <summary>
        ///   Returns Item property Enhancment penalty.  You need to specify the
        ///   enhancement penalty.  The enhancement penalty should be a POSITIVE
        ///   integer between 1 and 5 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyEnhancementPenalty(int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyEnhancementPenalty(nPenalty);
        }

        /// <summary>
        ///   Returns Item property weight reduction.  You need to specify the weight
        ///   reduction constant(IP_CONST_REDUCEDWEIGHT_*).
        /// </summary>
        public static ItemProperty ItemPropertyWeightReduction(ReducedWeight nReduction)
        {
            return NWN.Core.NWScript.ItemPropertyWeightReduction((int)nReduction);
        }

        /// <summary>
        ///   Returns Item property Bonus Feat.  You need to specify the the feat
        ///   constant(IP_CONST_FEAT_*).
        /// </summary>
        public static ItemProperty ItemPropertyBonusFeat(ItemPropertyFeat nFeat)
        {
            return NWN.Core.NWScript.ItemPropertyBonusFeat((int)nFeat);
        }

        /// <summary>
        ///   Returns Item property Bonus level spell (Bonus spell of level).  You must
        ///   specify the class constant(IP_CONST_CLASS_*) of the bonus spell(MUST BE a
        ///   spell casting class) and the level of the bonus spell.  The level of the
        ///   bonus spell should be an integer between 0 and 9.
        /// </summary>
        public static ItemProperty ItemPropertyBonusLevelSpell(Class nClass, SpellLevel nSpellLevel)
        {
            return NWN.Core.NWScript.ItemPropertyBonusLevelSpell((int)nClass, (int)nSpellLevel);
        }

        /// <summary>
        ///   Returns Item property Cast spell.  You must specify the spell constant
        ///   (IP_CONST_CASTSPELL_*) and the number of uses constant(IP_CONST_CASTSPELL_NUMUSES_*).
        ///   NOTE: The number after the name of the spell in the constant is the level
        ///   at which the spell will be cast.  Sometimes there are multiple copies
        ///   of the same spell but they each are cast at a different level.  The higher
        ///   the level, the more cost will be added to the item.
        ///   NOTE: The list of spells that can be applied to an item will depend on the
        ///   item type.  For instance there are spells that can be applied to a wand
        ///   that cannot be applied to a potion.  Below is a list of the types and the
        ///   spells that are allowed to be placed on them.  If you try to put a cast
        ///   spell effect on an item that is not allowed to have that effect it will
        ///   not work.
        /// </summary>
        public static ItemProperty ItemPropertyCastSpell(CastSpell nSpell, CastSpellNumberUses nNumUses)
        {
            return NWN.Core.NWScript.ItemPropertyCastSpell((int)nSpell, (int)nNumUses);
        }

        /// <summary>
        ///   Returns Item property damage bonus.  You must specify the damage type constant
        ///   (IP_CONST_DAMAGETYPE_*) and the amount of damage constant(IP_CONST_DAMAGEBONUS_*).
        ///   NOTE: not all the damage types will work, use only the following: Acid, Bludgeoning,
        ///   Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        public static ItemProperty ItemPropertyDamageBonus(ItemPropertyDamageType nDamageType,
            DamageBonus nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyDamageBonus((int)nDamageType, (int)nDamage);
        }

        /// <summary>
        ///   Returns Item property damage bonus vs. Alignment groups.  You must specify the
        ///   alignment group constant(IP_CONST_ALIGNMENTGROUP_*) and the damage type constant
        ///   (IP_CONST_DAMAGETYPE_*) and the amount of damage constant(IP_CONST_DAMAGEBONUS_*).
        ///   NOTE: not all the damage types will work, use only the following: Acid, Bludgeoning,
        ///   Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        public static ItemProperty ItemPropertyDamageBonusVsAlign(AlignmentGroup nAlignGroup,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyDamageBonusVsAlign((int)nAlignGroup, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        ///   Returns Item property damage bonus vs. specific race.  You must specify the
        ///   racial group constant(IP_CONST_RACIALTYPE_*) and the damage type constant
        ///   (IP_CONST_DAMAGETYPE_*) and the amount of damage constant(IP_CONST_DAMAGEBONUS_*).
        ///   NOTE: not all the damage types will work, use only the following: Acid, Bludgeoning,
        ///   Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        public static ItemProperty ItemPropertyDamageBonusVsRace(RacialType nRace,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyDamageBonusVsRace((int)nRace, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        ///   Returns Item property damage bonus vs. specific alignment.  You must specify the
        ///   specific alignment constant(IP_CONST_ALIGNMENT_*) and the damage type constant
        ///   (IP_CONST_DAMAGETYPE_*) and the amount of damage constant(IP_CONST_DAMAGEBONUS_*).
        ///   NOTE: not all the damage types will work, use only the following: Acid, Bludgeoning,
        ///   Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        public static ItemProperty ItemPropertyDamageBonusVsSAlign(Alignment nAlign,
            ItemPropertyDamageType nDamageType, DamageBonus nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyDamageBonusVsSAlign((int)nAlign, (int)nDamageType, (int)nDamage);
        }

        /// <summary>
        ///   Returns Item property damage immunity.  You must specify the damage type constant
        ///   (IP_CONST_DAMAGETYPE_*) that you want to be immune to and the immune bonus percentage
        ///   constant(IP_CONST_DAMAGEIMMUNITY_*).
        ///   NOTE: not all the damage types will work, use only the following: Acid, Bludgeoning,
        ///   Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        public static ItemProperty ItemPropertyDamageImmunity(ItemPropertyDamageType nDamageType,
            DamageImmunity nImmuneBonus)
        {
            return NWN.Core.NWScript.ItemPropertyDamageImmunity((int)nDamageType, (int)nImmuneBonus);
        }

        /// <summary>
        ///   Returns Item property damage penalty.  You must specify the damage penalty.
        ///   The damage penalty should be a uint, 1 - 5 only.
        ///   will reduce any value < 5 to 5.
        /// </summary>
        public static ItemProperty ItemPropertyDamagePenalty(int nPenalty)
        {
            if (nPenalty > 5) nPenalty = 5;
            return NWN.Core.NWScript.ItemPropertyDamagePenalty(nPenalty);
        }

        /// <summary>
        ///   Returns Item property damage reduction.  You must specify the enhancment level
        ///   (IP_CONST_DAMAGEREDUCTION_*) that is required to get past the damage reduction
        ///   and the amount of HP of damage constant(IP_CONST_DAMAGESOAK_*) will be soaked
        ///   up if your weapon is not of high enough enhancement.
        /// </summary>
        public static ItemProperty ItemPropertyDamageReduction(DamageReduction nEnhancement, DamageSoak nHPSoak)
        {
            return NWN.Core.NWScript.ItemPropertyDamageReduction((int)nEnhancement, (int)nHPSoak);
        }

        /// <summary>
        ///   Returns Item property damage resistance.  You must specify the damage type
        ///   constant(IP_CONST_DAMAGETYPE_*) and the amount of HP of damage constant
        ///   (IP_CONST_DAMAGERESIST_*) that will be resisted against each round.
        /// </summary>
        public static ItemProperty ItemPropertyDamageResistance(ItemPropertyDamageType nDamageType,
            DamageResist nHPResist)
        {
            return NWN.Core.NWScript.ItemPropertyDamageResistance((int)nDamageType, (int)nHPResist);
        }

        /// <summary>
        ///   Returns Item property damage vulnerability.  You must specify the damage type
        ///   constant(IP_CONST_DAMAGETYPE_*) that you want the user to be extra vulnerable to
        ///   and the percentage vulnerability constant(IP_CONST_DAMAGEVULNERABILITY_*).
        /// </summary>
        public static ItemProperty ItemPropertyDamageVulnerability(ItemPropertyDamageType nDamageType,
            DamageVulnerability nVulnerability)
        {
            return NWN.Core.NWScript.ItemPropertyDamageVulnerability((int)nDamageType, (int)nVulnerability);
        }

        /// <summary>
        ///   Return Item property Darkvision.
        /// </summary>
        public static ItemProperty ItemPropertyDarkvision()
        {
            return NWN.Core.NWScript.ItemPropertyDarkvision();
        }

        /// <summary>
        ///   Return Item property decrease ability score.  You must specify the ability
        ///   constant(IP_CONST_ABILITY_*) and the modifier constant.  The modifier must be
        ///   a POSITIVE integer between 1 and 10 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyDecreaseAbility(Ability nAbility, int nModifier)
        {
            return NWN.Core.NWScript.ItemPropertyDecreaseAbility((int)nAbility, nModifier);
        }

        /// <summary>
        ///   Returns Item property decrease Armor Class.  You must specify the armor
        ///   modifier type constant(IP_CONST_ACMODIFIERTYPE_*) and the armor class penalty.
        ///   The penalty must be a POSITIVE integer between 1 and 5 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyDecreaseAC(ArmorClassModiferType nModifierType, int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyDecreaseAC((int)nModifierType, nPenalty);
        }

        /// <summary>
        ///   Returns Item property decrease skill.  You must specify the constant for the
        ///   skill to be decreased(SKILL_*) and the amount of the penalty.  The penalty
        ///   must be a POSITIVE integer between 1 and 10 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyDecreaseSkill(NWNSkillType nSkill, int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyDecreaseSkill((int)nSkill, nPenalty);
        }

        /// <summary>
        ///   Returns Item property container reduced weight.  This is used for special
        ///   containers that reduce the weight of the objects inside them.  You must
        ///   specify the container weight reduction type constant(IP_CONST_CONTAINERWEIGHTRED_*).
        /// </summary>
        public static ItemProperty ItemPropertyContainerReducedWeight(ContainerWeight nContainerType)
        {
            return NWN.Core.NWScript.ItemPropertyContainerReducedWeight((int)nContainerType);
        }

        /// <summary>
        ///   Returns Item property extra melee damage type.  You must specify the extra
        ///   melee base damage type that you want applied.  It is a constant(IP_CONST_DAMAGETYPE_*).
        ///   NOTE: only the first 3 base types (piercing, slashing, & bludgeoning are applicable
        ///   here.
        ///   NOTE: It is also only applicable to melee weapons.
        /// </summary>
        public static ItemProperty ItemPropertyExtraMeleeDamageType(ItemPropertyDamageType nDamageType)
        {
            return NWN.Core.NWScript.ItemPropertyExtraMeleeDamageType((int)nDamageType);
        }

        /// <summary>
        ///   Returns Item property extra ranged damage type.  You must specify the extra
        ///   melee base damage type that you want applied.  It is a constant(IP_CONST_DAMAGETYPE_*).
        ///   NOTE: only the first 3 base types (piercing, slashing, & bludgeoning are applicable
        ///   here.
        ///   NOTE: It is also only applicable to ranged weapons.
        /// </summary>
        public static ItemProperty ItemPropertyExtraRangeDamageType(ItemPropertyDamageType nDamageType)
        {
            return NWN.Core.NWScript.ItemPropertyExtraRangeDamageType((int)nDamageType);
        }

        /// <summary>
        ///   Returns Item property haste.
        /// </summary>
        public static ItemProperty ItemPropertyHaste()
        {
            return NWN.Core.NWScript.ItemPropertyHaste();
        }

        /// <summary>
        ///   Returns Item property Holy Avenger.
        /// </summary>
        public static ItemProperty ItemPropertyHolyAvenger()
        {
            return NWN.Core.NWScript.ItemPropertyHolyAvenger();
        }

        /// <summary>
        ///   Returns Item property immunity to miscellaneous effects.  You must specify the
        ///   effect to which the user is immune, it is a constant(IP_CONST_IMMUNITYMISC_*).
        /// </summary>
        public static ItemProperty ItemPropertyImmunityMisc(ImmunityMisc nImmunityType)
        {
            return NWN.Core.NWScript.ItemPropertyImmunityMisc((int)nImmunityType);
        }

        /// <summary>
        ///   Returns Item property improved evasion.
        /// </summary>
        public static ItemProperty ItemPropertyImprovedEvasion()
        {
            return NWN.Core.NWScript.ItemPropertyImprovedEvasion();
        }

        /// <summary>
        ///   Returns Item property bonus spell resistance.  You must specify the bonus spell
        ///   resistance constant(IP_CONST_SPELLRESISTANCEBONUS_*).
        /// </summary>
        public static ItemProperty ItemPropertyBonusSpellResistance(SpellResistanceBonus nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyBonusSpellResistance((int)nBonus);
        }

        /// <summary>
        ///   Returns Item property saving throw bonus vs. a specific effect or damage type.
        ///   You must specify the save type constant(IP_CONST_SAVEVS_*) that the bonus is
        ///   applied to and the bonus that is be applied.  The bonus must be an integer
        ///   between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyBonusSavingThrowVsX(SaveVs nBonusType, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyBonusSavingThrowVsX((int)nBonusType, nBonus);
        }

        /// <summary>
        ///   Returns Item property saving throw bonus to the base type (ie. will, reflex,
        ///   fortitude).  You must specify the base type constant(IP_CONST_SAVEBASETYPE_*)
        ///   to which the user gets the bonus and the bonus that he/she will get.  The
        ///   bonus must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyBonusSavingThrow(SaveBaseType nBaseSaveType, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyBonusSavingThrow((int)nBaseSaveType, nBonus);
        }

        /// <summary>
        ///   Returns Item property keen.  This means a critical threat range of 19-20 on a
        ///   weapon will be increased to 17-20 etc.
        /// </summary>
        public static ItemProperty ItemPropertyKeen()
        {
            return NWN.Core.NWScript.ItemPropertyKeen();
        }

        /// <summary>
        ///   Returns Item property light.  You must specify the intesity constant of the
        ///   light(IP_CONST_LIGHTBRIGHTNESS_*) and the color constant of the light
        ///   (IP_CONST_LIGHTCOLOR_*).
        /// </summary>
        public static ItemProperty ItemPropertyLight(LightBrightness nBrightness, LightColor nColor)
        {
            return NWN.Core.NWScript.ItemPropertyLight((int)nBrightness, (int)nColor);
        }

        /// <summary>
        ///   Returns Item property Max range strength modification (ie. mighty).  You must
        ///   specify the maximum modifier for strength that is allowed on a ranged weapon.
        ///   The modifier must be a positive integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyMaxRangeStrengthMod(int nModifier)
        {
            return NWN.Core.NWScript.ItemPropertyMaxRangeStrengthMod(nModifier);
        }

        /// <summary>
        ///   Returns Item property no damage.  This means the weapon will do no damage in
        ///   combat.
        /// </summary>
        public static ItemProperty ItemPropertyNoDamage()
        {
            return NWN.Core.NWScript.ItemPropertyNoDamage();
        }

        /// <summary>
        ///   Returns Item property on hit -> do effect property.  You must specify the on
        ///   hit property constant(IP_CONST_ONHIT_*) and the save DC constant(IP_CONST_ONHIT_SAVEDC_*).
        ///   Some of the item properties require a special parameter as well.  If the
        ///   property does not require one you may leave out the last one.  The list of
        ///   the ones with 3 parameters and what they are are as follows:
        ///   ABILITYDRAIN      :nSpecial is the ability it is to drain.
        ///   constant(IP_CONST_ABILITY_*)
        ///   BLINDNESS         :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   CONFUSION         :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   DAZE              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   DEAFNESS          :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   DISEASE           :nSpecial is the type of desease that will effect the victim.
        ///   constant(DISEASE_*)
        ///   DOOM              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   FEAR              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   HOLD              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   ITEMPOISON        :nSpecial is the type of poison that will effect the victim.
        ///   constant(IP_CONST_POISON_*)
        ///   SILENCE           :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   SLAYRACE          :nSpecial is the race that will be slain.
        ///   constant(IP_CONST_RACIALTYPE_*)
        ///   SLAYALIGNMENTGROUP:nSpecial is the alignment group that will be slain(ie. chaotic).
        ///   constant(IP_CONST_ALIGNMENTGROUP_*)
        ///   SLAYALIGNMENT     :nSpecial is the specific alignment that will be slain.
        ///   constant(IP_CONST_ALIGNMENT_*)
        ///   SLEEP             :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   SLOW              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        ///   STUN              :nSpecial is the duration/percentage of effecting victim.
        ///   constant(IP_CONST_ONHIT_DURATION_*)
        /// </summary>
        public static ItemProperty ItemPropertyOnHitProps(int nProperty, int nSaveDC, int nSpecial = 0)
        {
            return NWN.Core.NWScript.ItemPropertyOnHitProps(nProperty, nSaveDC, nSpecial);
        }

        /// <summary>
        ///   Returns Item property reduced saving throw vs. an effect or damage type.  You must
        ///   specify the constant to which the penalty applies(IP_CONST_SAVEVS_*) and the
        ///   penalty to be applied.  The penalty must be a POSITIVE integer between 1 and 20
        ///   (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyReducedSavingThrowVsX(SaveVs nBaseSaveType, int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyReducedSavingThrowVsX((int)nBaseSaveType, nPenalty);
        }

        /// <summary>
        ///   Returns Item property reduced saving to base type.  You must specify the base
        ///   type to which the penalty applies (ie. will, reflex, or fortitude) and the penalty
        ///   to be applied.  The constant for the base type starts with (IP_CONST_SAVEBASETYPE_*).
        ///   The penalty must be a POSITIVE integer between 1 and 20 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyReducedSavingThrow(SaveBaseType nBonusType, int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyReducedSavingThrow((int)nBonusType, nPenalty);
        }

        /// <summary>
        ///   Returns Item property regeneration.  You must specify the regeneration amount.
        ///   The amount must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyRegeneration(int nRegenAmount)
        {
            return NWN.Core.NWScript.ItemPropertyRegeneration(nRegenAmount);
        }

        /// <summary>
        ///   Returns Item property skill bonus.  You must specify the skill to which the user
        ///   will get a bonus(SKILL_*) and the amount of the bonus.  The bonus amount must
        ///   be an integer between 1 and 50.
        /// </summary>
        public static ItemProperty ItemPropertySkillBonus(NWNSkillType nSkill, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertySkillBonus((int)nSkill, nBonus);
        }

        /// <summary>
        ///   Returns Item property spell immunity vs. specific spell.  You must specify the
        ///   spell to which the user will be immune(IP_CONST_IMMUNITYSPELL_*).
        /// </summary>
        public static ItemProperty ItemPropertySpellImmunitySpecific(ImmunitySpell nSpell)
        {
            return NWN.Core.NWScript.ItemPropertySpellImmunitySpecific((int)nSpell);
        }

        /// <summary>
        ///   Returns Item property spell immunity vs. spell school.  You must specify the
        ///   school to which the user will be immune(IP_CONST_SPELLSCHOOL_*).
        /// </summary>
        public static ItemProperty ItemPropertySpellImmunitySchool(SpellSchool nSchool)
        {
            return NWN.Core.NWScript.ItemPropertySpellImmunitySchool((int)nSchool);
        }

        /// <summary>
        ///   Returns Item property Thieves tools.  You must specify the modifier you wish
        ///   the tools to have.  The modifier must be an integer between 1 and 12.
        /// </summary>
        public static ItemProperty ItemPropertyThievesTools(int nModifier)
        {
            return NWN.Core.NWScript.ItemPropertyThievesTools(nModifier);
        }

        /// <summary>
        ///   Returns Item property Attack bonus.  You must specify an attack bonus.  The bonus
        ///   must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyAttackBonus(int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyAttackBonus(nBonus);
        }

        /// <summary>
        ///   Returns Item property Attack bonus vs. alignment group.  You must specify the
        ///   alignment group constant(IP_CONST_ALIGNMENTGROUP_*) and the attack bonus.  The
        ///   bonus must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyAttackBonusVsAlign(AlignmentGroup nAlignGroup,
            int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyAttackBonusVsAlign((int)nAlignGroup, nBonus);
        }

        /// <summary>
        ///   Returns Item property attack bonus vs. racial group.  You must specify the
        ///   racial group constant(IP_CONST_RACIALTYPE_*) and the attack bonus.  The bonus
        ///   must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyAttackBonusVsRace(RacialType nRace, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyAttackBonusVsRace((int)nRace, nBonus);
        }

        /// <summary>
        ///   Returns Item property attack bonus vs. a specific alignment.  You must specify
        ///   the alignment you want the bonus to work against(IP_CONST_ALIGNMENT_*) and the
        ///   attack bonus.  The bonus must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyAttackBonusVsSAlign(Alignment nAlignment, int nBonus)
        {
            return NWN.Core.NWScript.ItemPropertyAttackBonusVsSAlign((int)nAlignment, nBonus);
        }

        /// <summary>
        ///   Returns Item property attack penalty.  You must specify the attack penalty.
        ///   The penalty must be a POSITIVE integer between 1 and 5 (ie. 1 = -1).
        /// </summary>
        public static ItemProperty ItemPropertyAttackPenalty(int nPenalty)
        {
            return NWN.Core.NWScript.ItemPropertyAttackPenalty(nPenalty);
        }

        /// <summary>
        ///   Returns Item property unlimited ammo.  If you leave the parameter field blank
        ///   it will be just a normal bolt, arrow, or bullet.  However you may specify that
        ///   you want the ammunition to do special damage (ie. +1d6 Fire, or +1 enhancement
        ///   bonus).  For this parmeter you use the constants beginning with:
        ///   (IP_CONST_UNLIMITEDAMMO_*).
        /// </summary>
        public static ItemProperty ItemPropertyUnlimitedAmmo(Unlimited nAmmoDamage = Unlimited.Basic)
        {
            return NWN.Core.NWScript.ItemPropertyUnlimitedAmmo((int)nAmmoDamage);
        }

        /// <summary>
        ///   Returns Item property limit use by alignment group.  You must specify the
        ///   alignment group(s) that you want to be able to use this item(IP_CONST_ALIGNMENTGROUP_*).
        /// </summary>
        public static ItemProperty ItemPropertyLimitUseByAlign(AlignmentGroup nAlignGroup)
        {
            return NWN.Core.NWScript.ItemPropertyLimitUseByAlign((int)nAlignGroup);
        }

        /// <summary>
        ///   Returns Item property limit use by class.  You must specify the class(es) who
        ///   are able to use this item(IP_CONST_CLASS_*).
        /// </summary>
        public static ItemProperty ItemPropertyLimitUseByClass(Class nClass)
        {
            return NWN.Core.NWScript.ItemPropertyLimitUseByClass((int)nClass);
        }

        /// <summary>
        ///   Returns Item property limit use by race.  You must specify the race(s) who are
        ///   allowed to use this item(IP_CONST_RACIALTYPE_*).
        /// </summary>
        public static ItemProperty ItemPropertyLimitUseByRace(RacialType nRace)
        {
            return NWN.Core.NWScript.ItemPropertyLimitUseByRace((int)nRace);
        }

        /// <summary>
        ///   Returns Item property limit use by specific alignment.  You must specify the
        ///   alignment(s) of those allowed to use the item(IP_CONST_ALIGNMENT_*).
        /// </summary>
        public static ItemProperty ItemPropertyLimitUseBySAlign(Alignment nAlignment)
        {
            return NWN.Core.NWScript.ItemPropertyLimitUseBySAlign((int)nAlignment);
        }

        /// <summary>
        ///   replace this function it does nothing.
        /// </summary>
        public static ItemProperty BadBadReplaceMeThisDoesNothing()
        {
            return NWN.Core.NWScript.BadBadReplaceMeThisDoesNothing();
        }

        /// <summary>
        ///   Returns Item property vampiric regeneration.  You must specify the amount of
        ///   regeneration.  The regen amount must be an integer between 1 and 20.
        /// </summary>
        public static ItemProperty ItemPropertyVampiricRegeneration(int nRegenAmount)
        {
            return NWN.Core.NWScript.ItemPropertyVampiricRegeneration(nRegenAmount);
        }

        /// <summary>
        ///   Returns Item property Trap.  You must specify the trap level constant
        ///   (IP_CONST_TRAPSTRENGTH_*) and the trap type constant(IP_CONST_TRAPTYPE_*).
        /// </summary>
        public static ItemProperty ItemPropertyTrap(TrapStrength nTrapLevel, TrapType nTrapType)
        {
            return NWN.Core.NWScript.ItemPropertyTrap((int)nTrapLevel, (int)nTrapType);
        }

        /// <summary>
        ///   Returns Item property true seeing.
        /// </summary>
        public static ItemProperty ItemPropertyTrueSeeing()
        {
            return NWN.Core.NWScript.ItemPropertyTrueSeeing();
        }

        /// <summary>
        ///   Returns Item property Monster on hit apply effect property.  You must specify
        ///   the property that you want applied on hit.  There are some properties that
        ///   require an additional special parameter to be specified.  The others that
        ///   don't require any additional parameter you may just put in the one.  The
        ///   special cases are as follows:
        ///   ABILITYDRAIN:nSpecial is the ability to drain.
        ///   constant(IP_CONST_ABILITY_*)
        ///   DISEASE     :nSpecial is the disease that you want applied.
        ///   constant(DISEASE_*)
        ///   LEVELDRAIN  :nSpecial is the number of levels that you want drained.
        ///   integer between 1 and 5.
        ///   POISON      :nSpecial is the type of poison that will effect the victim.
        ///   constant(IP_CONST_POISON_*)
        ///   WOUNDING    :nSpecial is the amount of wounding.
        ///   integer between 1 and 5.
        ///   NOTE: Any that do not appear in the above list do not require the second
        ///   parameter.
        ///   NOTE: These can only be applied to monster NATURAL weapons (ie. bite, claw,
        ///   gore, and slam).  IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        public static ItemProperty ItemPropertyOnMonsterHitProperties(int nProperty, int nSpecial = 0)
        {
            return NWN.Core.NWScript.ItemPropertyOnMonsterHitProperties(nProperty, nSpecial);
        }

        /// <summary>
        ///   Returns Item property turn resistance.  You must specify the resistance bonus.
        ///   The bonus must be an integer between 1 and 50.
        /// </summary>
        public static ItemProperty ItemPropertyTurnResistance(int nModifier)
        {
            return NWN.Core.NWScript.ItemPropertyTurnResistance(nModifier);
        }

        /// <summary>
        ///   Returns Item property Massive Critical.  You must specify the extra damage
        ///   constant(IP_CONST_DAMAGEBONUS_*) of the criticals.
        /// </summary>
        public static ItemProperty ItemPropertyMassiveCritical(DamageBonus nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyMassiveCritical((int)nDamage);
        }

        /// <summary>
        ///   Returns Item property free action.
        /// </summary>
        public static ItemProperty ItemPropertyFreeAction()
        {
            return NWN.Core.NWScript.ItemPropertyFreeAction();
        }

        /// <summary>
        ///   Returns Item property monster damage.  You must specify the amount of damage
        ///   the monster's attack will do(IP_CONST_MONSTERDAMAGE_*).
        ///   NOTE: These can only be applied to monster NATURAL weapons (ie. bite, claw,
        ///   gore, and slam).  IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        public static ItemProperty ItemPropertyMonsterDamage(MonsterDamage nDamage)
        {
            return NWN.Core.NWScript.ItemPropertyMonsterDamage((int)nDamage);
        }

        /// <summary>
        ///   Returns Item property immunity to spell level.  You must specify the level of
        ///   which that and below the user will be immune.  The level must be an integer
        ///   between 1 and 9.  By putting in a 3 it will mean the user is immune to all
        ///   3rd level and lower spells.
        /// </summary>
        public static ItemProperty ItemPropertyImmunityToSpellLevel(int nLevel)
        {
            return NWN.Core.NWScript.ItemPropertyImmunityToSpellLevel(nLevel);
        }

        /// <summary>
        ///   Returns Item property special walk.  If no parameters are specified it will
        ///   automatically use the zombie walk.  This will apply the special walk animation
        ///   to the user.
        /// </summary>
        public static ItemProperty ItemPropertySpecialWalk()
        {
            return NWN.Core.NWScript.ItemPropertySpecialWalk(0);
        }

        /// <summary>
        ///   Returns Item property healers kit.  You must specify the level of the kit.
        ///   The modifier must be an integer between 1 and 12.
        /// </summary>
        public static ItemProperty ItemPropertyHealersKit(int nModifier)
        {
            return NWN.Core.NWScript.ItemPropertyHealersKit(nModifier);
        }

        /// <summary>
        ///   Returns Item property weight increase.  You must specify the weight increase
        ///   constant(IP_CONST_WEIGHTINCREASE_*).
        /// </summary>
        public static ItemProperty ItemPropertyWeightIncrease(WeightIncrease nWeight)
        {
            return NWN.Core.NWScript.ItemPropertyWeightIncrease((int)nWeight);
        }

        /// <summary>
        ///   Returns the string tag set for the provided item property.
        ///   - If no tag has been set, returns an empty string.
        /// </summary>
        public static string GetItemPropertyTag(ItemProperty nProperty)
        {
            return NWN.Core.NWScript.GetItemPropertyTag(nProperty);
        }

        /// <summary>
        ///   Returns the Cost Table number of the item property. See the 2DA files for value definitions.
        /// </summary>
        public static int GetItemPropertyCostTable(ItemProperty iProp)
        {
            return NWN.Core.NWScript.GetItemPropertyCostTable(iProp);
        }

        /// <summary>
        ///   Returns the Cost Table value (index of the cost table) of the item property.
        ///   See the 2DA files for value definitions.
        /// </summary>
        public static int GetItemPropertyCostTableValue(ItemProperty iProp)
        {
            return NWN.Core.NWScript.GetItemPropertyCostTableValue(iProp);
        }

        /// <summary>
        ///   Returns the Param1 number of the item property. See the 2DA files for value definitions.
        /// </summary>
        public static int GetItemPropertyParam1(ItemProperty iProp)
        {
            return NWN.Core.NWScript.GetItemPropertyParam1(iProp);
        }

        /// <summary>
        ///   Returns the Param1 value of the item property. See the 2DA files for value definitions.
        /// </summary>
        public static int GetItemPropertyParam1Value(ItemProperty iProp)
        {
            return NWN.Core.NWScript.GetItemPropertyParam1Value(iProp);
        }

        /// <summary>
        ///   Creates a new copy of an item, while making a single change to the appearance of the item.
        ///   Helmet models and simple items ignore iIndex.
        ///   iType                            iIndex                              iNewValue
        ///   ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        ///   ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            1-9
        ///   ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        ///   ITEM_APPR_TYPE_ARMOR_MODEL       ITEM_APPR_ARMOR_MODEL_*             Model #
        ///   ITEM_APPR_TYPE_ARMOR_COLOR       ITEM_APPR_ARMOR_COLOR_* [0]         0-175 [1]
        ///   [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is
        ///   desired, the following equation can be used for nIndex to achieve that:
        ///   ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        ///   For example, to change the CLOTH1 channel of the torso, nIndex would be:
        ///   6 + (7 * 6) + 2 = 50
        ///   [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical
        ///   function 'clear colour override', which clears the per-part override for that part.
        /// </summary>
        public static uint CopyItemAndModify(uint oItem, ItemAppearanceType nType, int nIndex, int nNewValue,
            bool bCopyVars = false)
        {
            return NWN.Core.NWScript.CopyItemAndModify(oItem, (int)nType, nIndex, nNewValue, bCopyVars ? 1 : 0);
        }

        /// <summary>
        ///   Creates an item property that (when applied to a weapon item) causes a spell to be cast
        ///   when a successful strike is made, or (when applied to armor) is struck by an opponent.
        ///   - nSpell uses the IP_CONST_ONHIT_CASTSPELL_* constants
        /// </summary>
        public static ItemProperty ItemPropertyOnHitCastSpell(OnHitCastSpellType nSpellType, int nLevel)
        {
            return NWN.Core.NWScript.ItemPropertyOnHitCastSpell((int)nSpellType, nLevel);
        }

        /// <summary>
        ///   Returns the SubType number of the item property. See the 2DA files for value definitions.
        /// </summary>
        public static int GetItemPropertySubType(ItemProperty iProperty)
        {
            return NWN.Core.NWScript.GetItemPropertySubType(iProperty);
        }

        /// <summary>
        ///   Tags the item property with the provided string.
        ///   - Any tags currently set on the item property will be overwritten.
        /// </summary>
        public static ItemProperty TagItemProperty(ItemProperty nProperty, string sNewTag)
        {
            return NWN.Core.NWScript.TagItemProperty(nProperty, sNewTag);
        }

        /// <summary>
        ///   Returns the total duration of the item property in seconds.
        ///   - Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetItemPropertyDuration(ItemProperty nProperty)
        {
            return NWN.Core.NWScript.GetItemPropertyDuration(nProperty);
        }

        /// <summary>
        ///   Returns the remaining duration of the item property in seconds.
        ///   - Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetItemPropertyDurationRemaining(ItemProperty nProperty)
        {
            return NWN.Core.NWScript.GetItemPropertyDurationRemaining(nProperty);
        }

        /// <summary>
        ///   Returns Item property Material.  You need to specify the Material Type.
        ///   - nMasterialType: The Material Type should be a positive integer between 0 and 77 (see iprp_matcost.2da).
        ///   Note: The Material Type property will only affect the cost of the item if you modify the cost in the
        ///   iprp_matcost.2da.
        /// </summary>
        public static ItemProperty ItemPropertyMaterial(int nMaterialType)
        {
            return NWN.Core.NWScript.ItemPropertyMaterial(nMaterialType);
        }

        /// <summary>
        ///   Returns Item property Quality. You need to specify the Quality.
        ///   - nQuality:  The Quality of the item property to create (see iprp_qualcost.2da).
        ///   IP_CONST_QUALITY_*
        ///   Note: The quality property will only affect the cost of the item if you modify the cost in the iprp_qualcost.2da.
        /// </summary>
        public static ItemProperty ItemPropertyQuality(Quality nQuality)
        {
            return NWN.Core.NWScript.ItemPropertyQuality((int)nQuality);
        }

        /// <summary>
        ///   Returns a generic Additional Item property. You need to specify the Additional property.
        ///   - nProperty: The item property to create (see iprp_addcost.2da).
        ///   IP_CONST_ADDITIONAL_*
        ///   Note: The additional property only affects the cost of the item if you modify the cost in the iprp_addcost.2da.
        /// </summary>
        public static ItemProperty ItemPropertyAdditional(Additional nAdditionalProperty)
        {
            return NWN.Core.NWScript.ItemPropertyAdditional((int)nAdditionalProperty);
        }

        /// <summary>
        ///   Creates an item property that offsets the effect on arcane spell failure
        ///   that a particular item has. Parameters come from the ITEM_PROP_ASF_* group.
        /// </summary>
        public static ItemProperty ItemPropertyArcaneSpellFailure(ArcaneSpellFailure nModLevel)
        {
            return NWN.Core.NWScript.ItemPropertyArcaneSpellFailure((int)nModLevel);
        }

        /// <summary>
        ///   Creates a visual effect (ITEM_VISUAL_*) that may be applied to
        ///   melee weapons only.
        /// </summary>
        public static ItemProperty ItemPropertyVisualEffect(ItemVisual nEffect)
        {
            return NWN.Core.NWScript.ItemPropertyVisualEffect((int)nEffect);
        }

        /// <summary>
        /// Returns the number of uses per day remaining of the given item and item property.
        /// * Will return 0 if the given item does not have the requested item property,
        ///   or the item property is not uses/day.
        /// </summary>
        public static int GetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip)
        {
            return NWN.Core.NWScript.GetItemPropertyUsesPerDayRemaining(oItem, ip);
        }

        /// <summary>
        /// Sets the number of uses per day remaining of the given item and item property.
        /// * Will do nothing if the given item and item property is not uses/day.
        /// * Will constrain nUsesPerDay to the maximum allowed as the cost table defines.
        /// </summary>
        public static void SetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip, int nUsesPerDay)
        {
            NWN.Core.NWScript.SetItemPropertyUsesPerDayRemaining(oItem, ip, nUsesPerDay);
        }

        /// <summary>
        /// Constructs a custom itemproperty given all the parameters explicitly.
        /// This function can be used in place of all the other ItemPropertyXxx constructors
        /// Use GetItemProperty{Type,SubType,CostTableValue,Param1Value} to see the values for a given itemproperty.
        /// </summary>
        public static ItemProperty ItemPropertyCustom(ItemPropertyType nType, int nSubType = -1, int nCostTableValue = -1, int nParam1Value = -1)
        {
            return NWN.Core.NWScript.ItemPropertyCustom((int)nType, nSubType, nCostTableValue, nParam1Value);
        }

    }
}