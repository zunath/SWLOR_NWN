using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Alignment = SWLOR.NWN.API.NWScript.Enum.Alignment;
using DamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;
using SpellSchool = SWLOR.NWN.API.NWScript.Enum.SpellSchool;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Returns the string tag set for the provided effect.
        ///   - If no tag has been set, returns an empty string.
        /// </summary>
        public static string GetEffectTag(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectTag(eEffect);
        }

        /// <summary>
        ///   Tags the effect with the provided string.
        ///   - Any other tags in the link will be overwritten.
        /// </summary>
        public static Effect TagEffect(Effect eEffect, string sNewTag)
        {
            return global::NWN.Core.NWScript.TagEffect(eEffect, sNewTag);
        }

        /// <summary>
        ///   Returns the caster level of the creature who created the effect.
        ///   - If not created by a creature, returns 0.
        ///   - If created by a spell-like ability, returns 0.
        /// </summary>
        public static int GetEffectCasterLevel(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectCasterLevel(eEffect);
        }

        /// <summary>
        ///   Returns the total duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDuration(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDuration(eEffect);
        }

        /// <summary>
        ///   Returns the remaining duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDurationRemaining(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDurationRemaining(eEffect);
        }

        /// <summary>
        ///   Returns an effect that when applied will paralyze the target's legs, rendering
        ///   them unable to walk but otherwise unpenalized. This effect cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneImmobilize()
        {
            return global::NWN.Core.NWScript.EffectCutsceneImmobilize();
        }

        /// <summary>
        ///   Creates a cutscene ghost effect, this will allow creatures
        ///   to pathfind through other creatures without bumping into them
        ///   for the duration of the effect.
        /// </summary>
        public static Effect EffectCutsceneGhost()
        {
            return global::NWN.Core.NWScript.EffectCutsceneGhost();
        }

        /// <summary>
        ///   Returns TRUE if the item is cursed and cannot be dropped
        /// </summary>
        public static bool GetItemCursedFlag(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemCursedFlag(oItem) != 0;
        }

        /// <summary>
        ///   When cursed, items cannot be dropped
        /// </summary>
        public static void SetItemCursedFlag(uint oItem, bool nCursed)
        {
            global::NWN.Core.NWScript.SetItemCursedFlag(oItem, nCursed ? 1 : 0);
        }

        /// <summary>
        ///   Get the possessor of oItem
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessor(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemPossessor(oItem);
        }

        /// <summary>
        ///   Get the object possessed by oCreature with the tag sItemTag
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag)
        {
            return global::NWN.Core.NWScript.GetItemPossessedBy(oCreature, sItemTag);
        }

        /// <summary>
        ///   Create an item with the template sItemTemplate in oTarget's inventory.
        ///   - nStackSize: This is the stack size of the item to be created
        ///   - sNewTag: If this string is not empty, it will replace the default tag from the template
        ///   * Return value: The object that has been created.  On error, this returns
        ///   OBJECT_INVALID.
        ///   If the item created was merged into an existing stack of similar items,
        ///   the function will return the merged stack object. If the merged stack
        ///   overflowed, the function will return the overflowed stack that was created.
        /// </summary>
        public static uint CreateItemOnObject(string sResRef, uint oTarget = OBJECT_INVALID, int nStackSize = 1,
            string sNewTag = "")
        {
            return global::NWN.Core.NWScript.CreateItemOnObject(sResRef, oTarget, nStackSize, sNewTag);
        }

        /// <summary>
        ///   Equip oItem into nInventorySlot.
        ///   - nInventorySlot: INVENTORY_SLOT_*
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionEquipItem failed."
        ///   Note:
        ///   If the creature already has an item equipped in the slot specified, it will be
        ///   unequipped automatically by the call to ActionEquipItem.
        ///   In order for ActionEquipItem to succeed the creature must be able to equip the
        ///   item oItem normally. This means that:
        ///   1) The item is in the creature's inventory.
        ///   2) The item must already be identified (if magical).
        ///   3) The creature has the level required to equip the item (if magical and ILR is on).
        ///   4) The creature possesses the required feats to equip the item (such as weapon proficiencies).
        /// </summary>
        public static void ActionEquipItem(uint oItem, InventorySlot nInventorySlot)
        {
            global::NWN.Core.NWScript.ActionEquipItem(oItem, (int)nInventorySlot);
        }

        /// <summary>
        ///   Unequip oItem from whatever slot it is currently in.
        /// </summary>
        public static void ActionUnequipItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionUnequipItem(oItem);
        }

        /// <summary>
        ///   Pick up oItem from the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPickUpItem failed."
        /// </summary>
        public static void ActionPickUpItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionPickUpItem(oItem);
        }

        /// <summary>
        ///   Put down oItem on the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPutDownItem failed."
        /// </summary>
        public static void ActionPutDownItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionPutDownItem(oItem);
        }

        /// <summary>
        ///   Give oItem to oGiveTo
        ///   If oItem is not a valid item, or oGiveTo is not a valid object, nothing will
        ///   happen.
        /// </summary>
        public static void ActionGiveItem(uint oItem, uint oGiveTo)
        {
            global::NWN.Core.NWScript.ActionGiveItem(oItem, oGiveTo);
        }

        /// <summary>
        ///   Take oItem from oTakeFrom
        ///   If oItem is not a valid item, or oTakeFrom is not a valid object, nothing
        ///   will happen.
        /// </summary>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom)
        {
            global::NWN.Core.NWScript.ActionTakeItem(oItem, oTakeFrom);
        }

        /// <summary>
        ///   Create a Death effect
        ///   - nSpectacularDeath: if this is TRUE, the creature to which this effect is
        ///   applied will die in an extraordinary fashion
        ///   - nDisplayFeedback
        /// </summary>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true)
        {
            return global::NWN.Core.NWScript.EffectDeath(nSpectacularDeath ? 1 : 0, nDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        ///   Create a Knockdown effect
        ///   This effect knocks creatures off their feet, they will sit until the effect
        ///   is removed. This should be applied as a temporary effect with a 3 second
        ///   duration minimum (1 second to fall, 1 second sitting, 1 second to get up).
        /// </summary>
        public static Effect EffectKnockdown()
        {
            return global::NWN.Core.NWScript.EffectKnockdown();
        }

        /// <summary>
        ///   Create a Curse effect.
        ///   - nStrMod: strength modifier
        ///   - nDexMod: dexterity modifier
        ///   - nConMod: constitution modifier
        ///   - nIntMod: intelligence modifier
        ///   - nWisMod: wisdom modifier
        ///   - nChaMod: charisma modifier
        /// </summary>
        public static Effect EffectCurse(int nStrMod = 1, int nDexMod = 1, int nConMod = 1, int nIntMod = 1,
            int nWisMod = 1, int nChaMod = 1)
        {
            return global::NWN.Core.NWScript.EffectCurse(nStrMod, nDexMod, nConMod, nIntMod, nWisMod, nChaMod);
        }

        /// <summary>
        ///   Create an Entangle effect
        ///   When applied, this effect will restrict the creature's movement and apply a
        ///   (-2) to all attacks and a -4 to AC.
        /// </summary>
        public static Effect EffectEntangle()
        {
            return global::NWN.Core.NWScript.EffectEntangle();
        }

        /// <summary>
        ///   Create a Saving Throw Increase effect
        ///   - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*)
        ///   SAVING_THROW_ALL
        ///   SAVING_THROW_FORT
        ///   SAVING_THROW_REFLEX
        ///   SAVING_THROW_WILL
        ///   - nValue: size of the Saving Throw increase
        ///   - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )
        /// </summary>
        public static Effect EffectSavingThrowIncrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All)
        {
            return global::NWN.Core.NWScript.EffectSavingThrowIncrease(nSave, nValue, (int)nSaveType);
        }

        /// <summary>
        ///   Create an Attack Increase effect
        /// NOTE: On SWLOR, this is used for Accuracy.
        ///   - nBonus: size of attack bonus
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAccuracyIncrease(int nBonus, AttackBonus nModifierType = AttackBonus.Misc)
        {
            return global::NWN.Core.NWScript.EffectAttackIncrease(nBonus, (int)nModifierType);
        }

        /// <summary>
        ///   Create a Damage Reduction effect
        ///   - nAmount: amount of damage reduction
        ///   - nDamagePower: DAMAGE_POWER_*
        ///   - nLimit: How much damage the effect can absorb before disappearing.
        ///   Set to zero for infinite
        /// </summary>
        public static Effect EffectDamageReduction(int nAmount, DamagePower nDamagePower, int nLimit = 0)
        {
            return global::NWN.Core.NWScript.EffectDamageReduction(nAmount, (int)nDamagePower, nLimit);
        }

        /// <summary>
        ///   Create a Damage Increase effect
        ///   - nBonus: DAMAGE_BONUS_*
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may
        ///   result in odd behaviour.
        /// </summary>
        public static Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = DamageType.Force)
        {
            return global::NWN.Core.NWScript.EffectDamageIncrease(nBonus, (int)nDamageType);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Magical and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Magical effects are removed by resting, and by dispel magic
        /// </summary>
        public static Effect MagicalEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.MagicalEffect(eEffect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Supernatural and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Permanent supernatural effects are not removed by resting
        /// </summary>
        public static Effect SupernaturalEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.SupernaturalEffect(eEffect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Extraordinary and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Extraordinary effects are removed by resting, but not by dispel magic
        /// </summary>
        public static Effect ExtraordinaryEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.ExtraordinaryEffect(eEffect);
        }

        /// <summary>
        ///   Create an AC Increase effect
        ///   - nValue: size of AC increase
        ///   - nModifyType: AC_*_BONUS
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   * Default value for nDamageType should only ever be used in this function prototype.
        /// </summary>
        public static Effect EffectACIncrease(int nValue,
            ArmorClassModiferType nModifyType = ArmorClassModiferType.Dodge,
            AC nDamageType = AC.VsDamageTypeAll)
        {
            return global::NWN.Core.NWScript.EffectACIncrease(nValue, (int)nModifyType, (int)nDamageType);
        }

        /// <summary>
        ///   Get the first in-game effect on oCreature.
        /// </summary>
        public static Effect GetFirstEffect(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFirstEffect(oCreature);
        }

        /// <summary>
        ///   Get the next in-game effect on oCreature.
        /// </summary>
        public static Effect GetNextEffect(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetNextEffect(oCreature);
        }

        /// <summary>
        ///   Remove eEffect from oCreature.
        ///   * No return value
        /// </summary>
        public static void RemoveEffect(uint oCreature, Effect eEffect)
        {
            global::NWN.Core.NWScript.RemoveEffect(oCreature, eEffect);
        }

        /// <summary>
        ///   * Returns TRUE if eEffect is a valid effect. The effect must have been applied to
        ///   * an object or else it will return FALSE
        /// </summary>
        public static bool GetIsEffectValid(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetIsEffectValid(eEffect) == 1;
        }

        /// <summary>
        ///   Get the duration type (DURATION_TYPE_*) of eEffect.
        ///   * Return value if eEffect is not valid: -1
        /// </summary>
        public static int GetEffectDurationType(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDurationType(eEffect);
        }

        /// <summary>
        ///   Get the subtype (SUBTYPE_*) of eEffect.
        ///   * Return value on error: 0
        /// </summary>
        public static int GetEffectSubType(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectSubType(eEffect);
        }

        /// <summary>
        ///   Get the object that created eEffect.
        ///   * Returns OBJECT_INVALID if eEffect is not a valid effect.
        /// </summary>
        public static uint GetEffectCreator(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectCreator(eEffect);
        }

        /// <summary>
        ///   Create a Heal effect. This should be applied as an instantaneous effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.
        /// </summary>
        public static Effect EffectHeal(int nDamageToHeal)
        {
            return global::NWN.Core.NWScript.EffectHeal(nDamageToHeal);
        }

        /// <summary>
        ///   Create a Damage effect
        ///   - nDamageAmount: amount of damage to be dealt. This should be applied as an
        ///   instantaneous effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nDamagePower: DAMAGE_POWER_*
        /// </summary>
        public static Effect EffectDamage(int nDamageAmount, DamageType nDamageType = DamageType.Force,
            DamagePower nDamagePower = DamagePower.Normal)
        {
            return global::NWN.Core.NWScript.EffectDamage(nDamageAmount, (int)nDamageType, (int)nDamagePower);
        }

        /// <summary>
        ///   Create an Ability Increase effect
        ///   - bAbilityToIncrease: ABILITY_*
        /// </summary>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy)
        {
            return global::NWN.Core.NWScript.EffectAbilityIncrease((int)nAbilityToIncrease, nModifyBy);
        }

        /// <summary>
        ///   Create a Damage Resistance effect that removes the first nAmount points of
        ///   damage of type nDamageType, up to nLimit (or infinite if nLimit is 0)
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nAmount
        ///   - nLimit
        /// </summary>
        public static Effect EffectDamageResistance(DamageType nDamageType, int nAmount, int nLimit = 0)
        {
            return global::NWN.Core.NWScript.EffectDamageResistance((int)nDamageType, nAmount, nLimit);
        }

        /// <summary>
        ///   Create a Resurrection effect. This should be applied as an instantaneous effect.
        /// </summary>
        public static Effect EffectResurrection()
        {
            return global::NWN.Core.NWScript.EffectResurrection();
        }

        /// <summary>
        ///   Create a Summon Creature effect.  The creature is created and placed into the
        ///   caller's party/faction.
        ///   - sCreatureResref: Identifies the creature to be summoned
        ///   - nVisualEffectId: VFX_*
        ///   - fDelaySeconds: There can be delay between the visual effect being played, and the
        ///   creature being added to the area
        ///   - nUseAppearAnimation: should this creature play it's "appear" animation when it is
        ///   summoned. If zero, it will just fade in somewhere near the target.  If the value is 1
        ///   it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures)
        /// </summary>
        public static Effect EffectSummonCreature(string sCreatureResref, VisualEffect nVisualEffectId = VisualEffect.Vfx_Com_Sparks_Parry,
            float fDelaySeconds = 0.0f, bool nUseAppearAnimation = false)
        {
            return global::NWN.Core.NWScript.EffectSummonCreature(sCreatureResref, (int)nVisualEffectId, fDelaySeconds, nUseAppearAnimation ? 1 : 0);
        }

        /// <summary>
        ///   Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary
        ///   except that the observers get no saving throw
        /// </summary>
        public static Effect EffectEthereal()
        {
            return global::NWN.Core.NWScript.EffectEthereal();
        }

        /// <summary>
        ///   Creates an effect that inhibits spells
        ///   - nPercent - percentage of failure
        ///   - nSpellSchool - the school of spells affected.
        /// </summary>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            return global::NWN.Core.NWScript.EffectSpellFailure(nPercent, (int)nSpellSchool);
        }

        /// <summary>
        ///   Returns an effect that is guaranteed to dominate a creature
        ///   Like EffectDominated but cannot be resisted
        /// </summary>
        public static Effect EffectCutsceneDominated()
        {
            return global::NWN.Core.NWScript.EffectCutsceneDominated();
        }

        /// <summary>
        ///   returns an effect that will petrify the target
        ///   * currently applies EffectParalyze and the stoneskin visual effect.
        /// </summary>
        public static Effect EffectPetrify()
        {
            return global::NWN.Core.NWScript.EffectPetrify();
        }

        /// <summary>
        ///   returns an effect that is guaranteed to paralyze a creature.
        ///   this effect is identical to EffectParalyze except that it cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneParalyze()
        {
            return global::NWN.Core.NWScript.EffectCutsceneParalyze();
        }

        /// <summary>
        ///   Create a Turn Resistance Decrease effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   /  decrease
        /// </summary>
        public static Effect EffectTurnResistanceDecrease(int nHitDice)
        {
            return global::NWN.Core.NWScript.EffectTurnResistanceDecrease(nHitDice);
        }

        /// <summary>
        ///   Create a Turn Resistance Increase effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   increase
        /// </summary>
        public static Effect EffectTurnResistanceIncrease(int nHitDice)
        {
            return global::NWN.Core.NWScript.EffectTurnResistanceIncrease(nHitDice);
        }

        /// <summary>
        ///   Create a Swarm effect.
        ///   - nLooping: If this is TRUE, for the duration of the effect when one creature
        ///   created by this effect dies, the next one in the list will be created.  If
        ///   the last creature in the list dies, we loop back to the beginning and
        ///   sCreatureTemplate1 will be created, and so on...
        ///   - sCreatureTemplate1
        ///   - sCreatureTemplate2
        ///   - sCreatureTemplate3
        ///   - sCreatureTemplate4
        /// </summary>
        public static Effect EffectSwarm(int nLooping, string sCreatureTemplate1, string sCreatureTemplate2 = "",
            string sCreatureTemplate3 = "", string sCreatureTemplate4 = "")
        {
            return global::NWN.Core.NWScript.EffectSwarm(nLooping, sCreatureTemplate1, sCreatureTemplate2, sCreatureTemplate3, sCreatureTemplate4);
        }

        /// <summary>
        ///   Create a Disappear/Appear effect.
        ///   The object will "fly away" for the duration of the effect and will reappear
        ///   at lLocation.
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectDisappearAppear(Location lLocation, int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectDisappearAppear(lLocation, nAnimation);
        }

        /// <summary>
        ///   Create a Disappear effect to make the object "fly away" and then destroy
        ///   itself.
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectDisappear(int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectDisappear(nAnimation);
        }

        /// <summary>
        ///   Create an Appear effect to make the object "fly in".
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectAppear(int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectAppear(nAnimation);
        }

        /// <summary>
        ///   Create a Modify Attacks effect to add attacks.
        ///   - nAttacks: maximum is 5, even with the effect stacked
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.
        /// </summary>
        public static Effect EffectModifyAttacks(int nAttacks)
        {
            return global::NWN.Core.NWScript.EffectModifyAttacks(nAttacks);
        }

        /// <summary>
        ///   Create a Damage Shield effect which does (nDamageAmount + nRandomAmount)
        ///   damage to any melee attacker on a successful attack of damage type nDamageType.
        ///   - nDamageAmount: an integer value
        ///   - nRandomAmount: DAMAGE_BONUS_*
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may
        ///   result in odd behaviour.
        /// </summary>
        public static Effect EffectDamageShield(int nDamageAmount, DamageBonus nRandomAmount, DamageType nDamageType)
        {
            return global::NWN.Core.NWScript.EffectDamageShield(nDamageAmount, (int)nRandomAmount, (int)nDamageType);
        }

        /// <summary>
        ///   Create a Miss Chance effect.
        ///   - nPercentage: 1-100 inclusive
        ///   - nMissChanceType: MISS_CHANCE_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage
        ///   < 1 or
        ///     nPercentage>
        ///     100.
        /// </summary>
        public static Effect EffectMissChance(int nPercentage, MissChanceType nMissChanceType = MissChanceType.Normal)
        {
            return global::NWN.Core.NWScript.EffectMissChance(nPercentage, (int)nMissChanceType);
        }

        /// <summary>
        ///   Create a Spell Level Absorption effect.
        ///   - nMaxSpellLevelAbsorbed: maximum spell level that will be absorbed by the
        ///   effect
        ///   - nTotalSpellLevelsAbsorbed: maximum number of spell levels that will be
        ///   absorbed by the effect
        ///   - nSpellSchool: SPELL_SCHOOL_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if:
        ///   nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool
        ///   is invalid.
        /// </summary>
        public static Effect EffectSpellLevelAbsorption(int nMaxSpellLevelAbsorbed, int nTotalSpellLevelsAbsorbed = 0,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            return global::NWN.Core.NWScript.EffectSpellLevelAbsorption(nMaxSpellLevelAbsorbed, nTotalSpellLevelsAbsorbed, (int)nSpellSchool);
        }

        /// <summary>
        ///   Create a Dispel Magic Best effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            return global::NWN.Core.NWScript.EffectDispelMagicBest(nCasterLevel);
        }

        /// <summary>
        ///   Create an Invisibility effect.
        ///   - nInvisibilityType: INVISIBILITY_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType
        ///   is invalid.
        /// </summary>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType)
        {
            return global::NWN.Core.NWScript.EffectInvisibility((int)nInvisibilityType);
        }

        /// <summary>
        ///   Create a Concealment effect.
        ///   - nPercentage: 1-100 inclusive
        ///   - nMissChanceType: MISS_CHANCE_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage
        ///   < 1 or
        ///     nPercentage>
        ///     100.
        /// </summary>
        public static Effect EffectConcealment(int nPercentage, MissChanceType nMissType = MissChanceType.Normal)
        {
            return global::NWN.Core.NWScript.EffectConcealment(nPercentage, (int)nMissType);
        }

        /// <summary>
        ///   Create a Darkness effect.
        /// </summary>
        public static Effect EffectDarkness()
        {
            return global::NWN.Core.NWScript.EffectDarkness();
        }

        /// <summary>
        ///   Create a Dispel Magic All effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            return global::NWN.Core.NWScript.EffectDispelMagicAll(nCasterLevel);
        }

        /// <summary>
        ///   Create an Ultravision effect.
        /// </summary>
        public static Effect EffectUltravision()
        {
            return global::NWN.Core.NWScript.EffectUltravision();
        }

        /// <summary>
        ///   Create a Negative Level effect.
        ///   - nNumLevels: the number of negative levels to apply.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.
        /// </summary>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false)
        {
            return global::NWN.Core.NWScript.EffectNegativeLevel(nNumLevels, bHPBonus ? 1 : 0);
        }

        /// <summary>
        ///   Create a Polymorph effect.
        /// </summary>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false)
        {
            return global::NWN.Core.NWScript.EffectPolymorph(nPolymorphSelection, nLocked ? 1 : 0);
        }

        /// <summary>
        ///   Create a Sanctuary effect.
        ///   - nDifficultyClass: must be a non-zero, positive number
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.
        /// </summary>
        public static Effect EffectSanctuary(int nDifficultyClass)
        {
            return global::NWN.Core.NWScript.EffectSanctuary(nDifficultyClass);
        }

        /// <summary>
        ///   Create a True Seeing effect.
        /// </summary>
        public static Effect EffectTrueSeeing()
        {
            return global::NWN.Core.NWScript.EffectTrueSeeing();
        }

        /// <summary>
        ///   Create a See Invisible effect.
        /// </summary>
        public static Effect EffectSeeInvisible()
        {
            return global::NWN.Core.NWScript.EffectSeeInvisible();
        }

        /// <summary>
        ///   Create a Time Stop effect.
        /// </summary>
        public static Effect EffectTimeStop()
        {
            return global::NWN.Core.NWScript.EffectTimeStop();
        }

        /// <summary>
        ///   Create a Blindness effect.
        /// </summary>
        public static Effect EffectBlindness()
        {
            return global::NWN.Core.NWScript.EffectBlindness();
        }

        /// <summary>
        ///   Create an Ability Decrease effect.
        ///   - nAbility: ABILITY_*
        ///   - nModifyBy: This is the amount by which to decrement the ability
        /// </summary>
        public static Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy)
        {
            return global::NWN.Core.NWScript.EffectAbilityDecrease((int)nAbility, nModifyBy);
        }

        /// <summary>
        ///   Create an Attack Decrease effect.
        /// NOTE: On SWLOR, this is used for Accuracy.
        ///   - nPenalty
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAccuracyDecrease(int nPenalty, AttackBonus nModifierType = AttackBonus.Misc)
        {
            return global::NWN.Core.NWScript.EffectAttackDecrease(nPenalty, (int)nModifierType);
        }

        /// <summary>
        ///   Create a Damage Decrease effect.
        ///   - nPenalty
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Force)
        {
            return global::NWN.Core.NWScript.EffectDamageDecrease(nPenalty, (int)nDamageType);
        }

        /// <summary>
        ///   Create a Damage Immunity Decrease effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity)
        {
            return global::NWN.Core.NWScript.EffectDamageImmunityDecrease(nDamageType, nPercentImmunity);
        }

        /// <summary>
        ///   Create an AC Decrease effect.
        ///   - nValue
        ///   - nModifyType: AC_*
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   * Default value for nDamageType should only ever be used in this function prototype.
        /// </summary>
        public static Effect EffectACDecrease(int nValue,
            ArmorClassModiferType nModifyType = ArmorClassModiferType.Dodge,
            AC nDamageType = AC.VsDamageTypeAll)
        {
            return global::NWN.Core.NWScript.EffectACDecrease(nValue, (int)nModifyType, (int)nDamageType);
        }

        /// <summary>
        ///   Create a Movement Speed Decrease effect.
        ///   - nPercentChange - range 0 through 99
        ///   eg.
        ///   0 = no change in speed
        ///   50 = 50% slower
        ///   99 = almost immobile
        /// </summary>
        public static Effect EffectMovementSpeedDecrease(int nPercentChange)
        {
            return global::NWN.Core.NWScript.EffectMovementSpeedDecrease(nPercentChange);
        }

        /// <summary>
        ///   Create a Saving Throw Decrease effect.
        ///   - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*)
        ///   SAVING_THROW_ALL
        ///   SAVING_THROW_FORT
        ///   SAVING_THROW_REFLEX
        ///   SAVING_THROW_WILL
        ///   - nValue: size of the Saving Throw decrease
        ///   - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )
        /// </summary>
        public static Effect EffectSavingThrowDecrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All)
        {
            return global::NWN.Core.NWScript.EffectSavingThrowDecrease(nSave, nValue, (int)nSaveType);
        }

        /// <summary>
        ///   Create a Skill Decrease effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillDecrease(int nSkill, int nValue)
        {
            return global::NWN.Core.NWScript.EffectSkillDecrease(nSkill, nValue);
        }

        /// <summary>
        ///   Create a Spell Resistance Decrease effect.
        /// </summary>
        public static Effect EffectSpellResistanceDecrease(int nValue)
        {
            return global::NWN.Core.NWScript.EffectSpellResistanceDecrease(nValue);
        }

        /// <summary>
        ///   Activate oItem.
        /// </summary>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.EventActivateItem(oItem, lTarget, oTarget);
        }

        /// <summary>
        ///   Create a Hit Point Change When Dying effect.
        ///   - fHitPointChangePerRound: this can be positive or negative, but not zero.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.
        /// </summary>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound)
        {
            return global::NWN.Core.NWScript.EffectHitPointChangeWhenDying(fHitPointChangePerRound);
        }

        /// <summary>
        ///   Create a Turned effect.
        ///   Turned effects are supernatural by default.
        /// </summary>
        public static Effect EffectTurned()
        {
            return global::NWN.Core.NWScript.EffectTurned();
        }

        /// <summary>
        ///   Set eEffect to be versus a specific alignment.
        ///   - eEffect
        ///   - nLawChaos: ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL
        ///   - nGoodEvil: ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL
        /// </summary>
        public static Effect VersusAlignmentEffect(Effect eEffect,
            Alignment nLawChaos = Alignment.All,
            Alignment nGoodEvil = Alignment.All)
        {
            return global::NWN.Core.NWScript.VersusAlignmentEffect(eEffect, (int)nLawChaos, (int)nGoodEvil);
        }

        /// <summary>
        ///   Set eEffect to be versus nRacialType.
        ///   - eEffect
        ///   - nRacialType: RACIAL_TYPE_*
        /// </summary>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType)
        {
            return global::NWN.Core.NWScript.VersusRacialTypeEffect(eEffect, (int)nRacialType);
        }

        /// <summary>
        ///   Set eEffect to be versus traps.
        /// </summary>
        public static Effect VersusTrapEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.VersusTrapEffect(eEffect);
        }

        /// <summary>
        ///   Create a Skill Increase effect.
        ///   - nSkill: SKILL_*
        ///   - nValue
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillIncrease(NWNSkillType nSkill, int nValue)
        {
            return global::NWN.Core.NWScript.EffectSkillIncrease((int)nSkill, nValue);
        }

        /// <summary>
        ///   Create a Temporary Hitpoints effect.
        ///   - nHitPoints: a positive integer
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.
        /// </summary>
        public static Effect EffectTemporaryHitpoints(int nHitPoints)
        {
            return global::NWN.Core.NWScript.EffectTemporaryHitpoints(nHitPoints);
        }

        /// <summary>
        ///   Creates a conversation event.
        ///   Note: This only creates the event. The event wont actually trigger until SignalEvent()
        ///   is called using this created conversation event as an argument.
        ///   For example:
        ///   SignalEvent(oCreature, EventConversation());
        ///   Once the event has been signaled. The script associated with the OnConversation event will
        ///   run on the creature oCreature.
        ///   To specify the OnConversation script that should run, view the Creature Properties on
        ///   the creature and click on the Scripts Tab. Then specify a script for the OnConversation event.
        /// </summary>
        public static Event EventConversation()
        {
            return global::NWN.Core.NWScript.EventConversation();
        }

        /// <summary>
        ///   Creates a Damage Immunity Increase effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity)
        {
            return global::NWN.Core.NWScript.EffectDamageImmunityIncrease((int)nDamageType, nPercentImmunity);
        }

        /// <summary>
        ///   Create an Immunity effect.
        ///   - nImmunityType: IMMUNITY_TYPE_*
        /// </summary>
        public static Effect EffectImmunity(ImmunityType nImmunityType)
        {
            return global::NWN.Core.NWScript.EffectImmunity((int)nImmunityType);
        }

        /// <summary>
        ///   Create a Haste effect.
        /// </summary>
        public static Effect EffectHaste()
        {
            return global::NWN.Core.NWScript.EffectHaste();
        }

        /// <summary>
        ///   Create a Slow effect.
        /// </summary>
        public static Effect EffectSlow()
        {
            return global::NWN.Core.NWScript.EffectSlow();
        }

        /// <summary>
        ///   Create a Poison effect.
        ///   - nPoisonType: POISON_*
        /// </summary>
        public static Effect EffectPoison(Poison nPoisonType)
        {
            return global::NWN.Core.NWScript.EffectPoison((int)nPoisonType);
        }

        /// <summary>
        ///   Create a Disease effect.
        ///   - nDiseaseType: DISEASE_*
        /// </summary>
        public static Effect EffectDisease(Disease nDiseaseType)
        {
            return global::NWN.Core.NWScript.EffectDisease((int)nDiseaseType);
        }

        /// <summary>
        ///   Create a Silence effect.
        /// </summary>
        public static Effect EffectSilence()
        {
            return global::NWN.Core.NWScript.EffectSilence();
        }

        /// <summary>
        ///   Create a Spell Resistance Increase effect.
        ///   - nValue: size of spell resistance increase
        /// </summary>
        public static Effect EffectSpellResistanceIncrease(int nValue)
        {
            return global::NWN.Core.NWScript.EffectSpellResistanceIncrease(nValue);
        }

        /// <summary>
        ///   Create a Beam effect.
        ///   - nBeamVisualEffect: VFX_BEAM_*
        ///   - oEffector: the beam is emitted from this creature
        ///   - nBodyPart: BODY_NODE_*
        ///   - bMissEffect: If this is TRUE, the beam will fire to a random vector near or
        ///   past the target
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is
        ///   not valid.
        /// </summary>
        public static Effect EffectBeam(VisualEffect nBeamVisualEffect, uint oEffector, BodyNode nBodyPart, bool bMissEffect = false)
        {
            return global::NWN.Core.NWScript.EffectBeam((int)nBeamVisualEffect, oEffector, (int)nBodyPart, bMissEffect ? 1 : 0);
        }

        /// <summary>
        ///   Link the two supplied effects, returning eChildEffect as a child of
        ///   eParentEffect.
        ///   Note: When applying linked effects if the target is immune to all valid
        ///   effects all other effects will be removed as well. This means that if you
        ///   apply a visual effect and a silence effect (in a link) and the target is
        ///   immune to the silence effect that the visual effect will get removed as well.
        ///   Visual Effects are not considered "valid" effects for the purposes of
        ///   determining if an effect will be removed or not and as such should never be
        ///   packaged *only* with other visual effects in a link.
        /// </summary>
        public static Effect EffectLinkEffects(Effect eChildEffect, Effect eParentEffect)
        {
            return global::NWN.Core.NWScript.EffectLinkEffects(eChildEffect, eParentEffect);
        }

        /// <summary>
        ///   * Create a Visual Effect that can be applied to an object.
        ///   - nVisualEffectId
        ///   - nMissEffect: if this is TRUE, a random vector near or past the target will
        ///   be generated, on which to play the effect
        /// </summary>
        public static Effect EffectVisualEffect(VisualEffect visualEffectID, bool nMissEffect = false, float fScale = 1.0f, Vector3 vTranslate = new Vector3(), Vector3 vRotate = new Vector3())
        {
            return global::NWN.Core.NWScript.EffectVisualEffect((int)visualEffectID, nMissEffect ? 1 : 0, fScale, vTranslate, vRotate);
        }

        /// <summary>
        ///   Apply eEffect to oTarget.
        /// </summary>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f)
        {
            global::NWN.Core.NWScript.ApplyEffectToObject((int)nDurationType, eEffect, oTarget, fDuration);
        }

        /// <summary>
        ///   Get the effect type (EFFECT_TYPE_*) of eEffect.
        ///   * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT
        /// </summary>
        public static EffectTypeScript GetEffectType(Effect eEffect)
        {
            return (EffectTypeScript)global::NWN.Core.NWScript.GetEffectType(eEffect);
        }

        /// <summary>
        ///   Create an Area Of Effect effect in the area of the creature it is applied to.
        ///   If the scripts are not specified, default ones will be used.
        /// </summary>
        public static Effect EffectAreaOfEffect(AreaOfEffect nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "")
        {
            return global::NWN.Core.NWScript.EffectAreaOfEffect((int)nAreaEffect, sOnEnterScript, sHeartbeatScript, sOnExitScript);
        }

        /// <summary>
        ///   Create a Regenerate effect.
        ///   - nAmount: amount of damage to be regenerated per time interval
        ///   - fIntervalSeconds: length of interval in seconds
        /// </summary>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds)
        {
            return global::NWN.Core.NWScript.EffectRegenerate(nAmount, fIntervalSeconds);
        }

        /// <summary>
        ///   Create a Movement Speed Increase effect.
        ///   - nPercentChange - range 0 through 99
        ///   eg.
        ///   0 = no change in speed
        ///   50 = 50% faster
        ///   99 = almost twice as fast
        /// </summary>
        public static Effect EffectMovementSpeedIncrease(int nPercentChange)
        {
            return global::NWN.Core.NWScript.EffectMovementSpeedIncrease(nPercentChange);
        }

        /// <summary>
        ///   Create a Charm effect
        /// </summary>
        public static Effect EffectCharmed()
        {
            return global::NWN.Core.NWScript.EffectCharmed();
        }

        /// <summary>
        ///   Create a Confuse effect
        /// </summary>
        public static Effect EffectConfused()
        {
            return global::NWN.Core.NWScript.EffectConfused();
        }

        /// <summary>
        ///   Create a Frighten effect
        /// </summary>
        public static Effect EffectFrightened()
        {
            return global::NWN.Core.NWScript.EffectFrightened();
        }

        /// <summary>
        ///   Create a Dominate effect
        /// </summary>
        public static Effect EffectDominated()
        {
            return global::NWN.Core.NWScript.EffectDominated();
        }

        /// <summary>
        ///   Create a Daze effect
        /// </summary>
        public static Effect EffectDazed()
        {
            return global::NWN.Core.NWScript.EffectDazed();
        }

        /// <summary>
        ///   Create a Stun effect
        /// </summary>
        public static Effect EffectStunned()
        {
            return global::NWN.Core.NWScript.EffectStunned();
        }

        /// <summary>
        ///   Create a Sleep effect
        /// </summary>
        public static Effect EffectSleep()
        {
            return global::NWN.Core.NWScript.EffectSleep();
        }

        /// <summary>
        ///   Create a Paralyze effect
        /// </summary>
        public static Effect EffectParalyze()
        {
            return global::NWN.Core.NWScript.EffectParalyze();
        }

        /// <summary>
        ///   Create a Spell Immunity effect.
        ///   There is a known bug with this function. There *must* be a parameter specified
        ///   when this is called (even if the desired parameter is SPELL_ALL_SPELLS),
        ///   otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned.
        ///   - nImmunityToSpell: SPELL_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is
        ///   invalid.
        /// </summary>
        public static Effect EffectSpellImmunity(Spell nImmunityToSpell = Spell.AllSpells)
        {
            return global::NWN.Core.NWScript.EffectSpellImmunity((int)nImmunityToSpell);
        }

        /// <summary>
        ///   Create a Deaf effect
        /// </summary>
        public static Effect EffectDeaf()
        {
            return global::NWN.Core.NWScript.EffectDeaf();
        }


        /// <summary>
        ///  Get the integer parameter of eEffect at nIndex.
        ///  nIndex bounds: 0 to 7 inclusive
        ///  Some experimentation will be needed to find the right index for the value you wish to determine.
        ///  Returns: the value or 0 on error/when not set.
        /// </summary>
        public static int GetEffectInteger(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectInteger(eEffect, nIndex);
        }

        /// <summary>
        /// Get the float parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or 0.0f on error/when not set.
        /// </summary>
        public static float GetEffectFloat(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectFloat(eEffect, nIndex);
        }

        /// <summary>
        /// Get the string parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 5 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or "" on error/when not set.
        /// </summary>
        public static string GetEffectString(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectString(eEffect, nIndex);
        }
        
        /// <summary>
        /// Get the object parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or OBJECT_INVALID on error/when not set.
        /// </summary>
        public static uint GetEffectObject(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectObject(eEffect, nIndex);
        }

        /// <summary>
        /// Get the vector parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 1 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or {0.0f, 0.0f, 0.0f} on error/when not set.
        /// </summary>
        public static Vector3 GetEffectVector(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectVector(eEffect, nIndex);
        }

        /// <summary>
        /// Create a RunScript effect.
        /// Notes: When applied as instant effect, only sOnAppliedScript will fire.
        ///        In the scripts, OBJECT_SELF will be the object the effect is applied to.
        /// </summary>
        /// <param name="sOnAppliedScript">An optional script to execute when the effect is applied.</param>
        /// <param name="sOnRemovedScript">An optional script to execute when the effect is removed.</param>
        /// <param name="sOnIntervalScript">An optional script to execute every fInterval seconds.</param>
        /// <param name="fInterval">The interval in seconds, must be >0.0f if an interval script is set. Very low values may have an adverse effect on performance.</param>
        /// <param name="sData">An optional string of data saved in the effect, retrievable with GetEffectString() at index 0.</param>
        /// <returns></returns>
        public static Effect EffectRunScript(string sOnAppliedScript = "", string sOnRemovedScript = "", string sOnIntervalScript = "", float fInterval = 0.0f, string sData = "")
        {
            return global::NWN.Core.NWScript.EffectRunScript(sOnAppliedScript, sOnRemovedScript, sOnIntervalScript, fInterval, sData);
        }

        /// <summary>
        /// Get the effect that last triggered an EffectRunScript() script.
        /// Note: This can be used to get the creator or tag, among others, of the EffectRunScript() in one of its scripts.
        /// </summary>
        /// <returns>The effect that last triggered an EffectRunScript() script.</returns>
        public static Effect GetLastRunScriptEffect()
        {
            return global::NWN.Core.NWScript.GetLastRunScriptEffect();
        }

        /// <summary>
        /// Get the script type (RUNSCRIPT_EFFECT_SCRIPT_TYPE_*) of the last triggered EffectRunScript() script.
        /// * Returns 0 when called outside of an EffectRunScript() script.
        /// </summary>
        /// <returns></returns>
        public static int GetLastRunScriptEffectScriptType()
        {
            return global::NWN.Core.NWScript.GetLastRunScriptEffectScriptType();
        }

        /// <summary>
        /// Hides the effect icon of eEffect and of all effects currently linked to it.
        /// </summary>
        /// <param name="eEffect"></param>
        /// <returns></returns>
        public static Effect HideEffectIcon(Effect eEffect)
        {
            return global::NWN.Core.NWScript.HideEffectIcon(eEffect);
        }

        /// <summary>
        /// Create an Icon effect.
        /// * nIconID: The effect icon (EFFECT_ICON_*) to display.
        ///            Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases.
        /// Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255.
        /// </summary>
        public static Effect EffectIcon(EffectIconType nIconId)
        {
            return global::NWN.Core.NWScript.EffectIcon((int)nIconId);
        }

        /// <summary>
        /// Set the subtype of eEffect to Unyielding and return eEffect.
        /// (Effects default to magical if the subtype is not set)
        /// Unyielding effects are not removed by resting, death or dispel magic, only by RemoveEffect().
        /// Note: effects that modify state, Stunned/Knockdown/Deaf etc, WILL be removed on death.
        /// </summary>
        public static Effect UnyieldingEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.UnyieldingEffect(eEffect);
        }

        /// <summary>
        /// Set eEffect to ignore immunities and return eEffect.
        /// </summary>
        public static Effect IgnoreEffectImmunity(Effect eEffect)
        {
            return global::NWN.Core.NWScript.IgnoreEffectImmunity(eEffect);
        }

        /// <summary>
        /// Create a Pacified effect, making the creature unable to attack anyone
        /// </summary>
        public static Effect EffectPacified()
        {
            return global::NWN.Core.NWScript.EffectPacified();
        }

        /// <summary>
        /// Returns the given effects Link ID. There is no guarantees about this identifier other than
        /// it is unique and the same for all effects linked to it.
        /// </summary>
        public static string GetEffectLinkId(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectLinkId(eEffect);
        }

        /// <summary>
        /// Creates a bonus feat effect. These act like the Bonus Feat item property,
        /// and do not work as feat prerequisites for levelup purposes.
        /// - nFeat: FEAT_*
        /// </summary>
        public static Effect EffectBonusFeat(int nFeat)
        {
            return global::NWN.Core.NWScript.EffectBonusFeat(nFeat);
        }

        /// <summary>
        /// Provides immunity to the effects of EffectTimeStop which allows actions during other creatures time stop effects
        /// </summary>
        public static Effect EffectTimeStopImmunity()
        {
            return global::NWN.Core.NWScript.EffectTimeStopImmunity();
        }

        /// <summary>
        /// Forces the creature to always walk
        /// </summary>
        public static Effect EffectForceWalk()
        {
            return global::NWN.Core.NWScript.EffectForceWalk();
        }
    }
}