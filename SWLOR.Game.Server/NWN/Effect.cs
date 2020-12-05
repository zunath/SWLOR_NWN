using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item.Property;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using static SWLOR.Game.Server.NWN.Internal;
using Alignment = SWLOR.Game.Server.NWN.Enum.Alignment;
using DamageType = SWLOR.Game.Server.NWN.Enum.DamageType;
using RacialType = SWLOR.Game.Server.NWN.Enum.RacialType;
using SpellSchool = SWLOR.Game.Server.NWN.Enum.SpellSchool;

namespace SWLOR.Game.Server.NWN
{
    public partial class _
    {
        /// <summary>
        ///   Returns the string tag set for the provided effect.
        ///   - If no tag has been set, returns an empty string.
        /// </summary>
        public static string GetEffectTag(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(849);
            return NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Tags the effect with the provided string.
        ///   - Any other tags in the link will be overwritten.
        /// </summary>
        public static Effect TagEffect(Effect eEffect, string sNewTag)
        {
            NativeFunctions.StackPushStringUTF8(sNewTag);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(850);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns the caster level of the creature who created the effect.
        ///   - If not created by a creature, returns 0.
        ///   - If created by a spell-like ability, returns 0.
        /// </summary>
        public static int GetEffectCasterLevel(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(851);
            return NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns the total duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDuration(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(852);
            return NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns the remaining duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDurationRemaining(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(853);
            return NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns an effect that when applied will paralyze the target's legs, rendering
        ///   them unable to walk but otherwise unpenalized. This effect cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneImmobilize()
        {
            NativeFunctions.CallBuiltIn(767);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates a cutscene ghost effect, this will allow creatures
        ///   to pathfind through other creatures without bumping into them
        ///   for the duration of the effect.
        /// </summary>
        public static Effect EffectCutsceneGhost()
        {
            NativeFunctions.CallBuiltIn(757);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns TRUE if the item is cursed and cannot be dropped
        /// </summary>
        public static bool GetItemCursedFlag(uint oItem)
        {
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(744);
            return NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   When cursed, items cannot be dropped
        /// </summary>
        public static void SetItemCursedFlag(uint oItem, bool nCursed)
        {
            NativeFunctions.StackPushInteger(nCursed ? 1 : 0);
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(745);
        }

        /// <summary>
        ///   Get the possessor of oItem
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessor(uint oItem)
        {
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(29);
            return NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the object possessed by oCreature with the tag sItemTag
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag)
        {
            NativeFunctions.StackPushStringUTF8(sItemTag);
            NativeFunctions.StackPushObject(oCreature);
            NativeFunctions.CallBuiltIn(30);
            return NativeFunctions.StackPopObject();
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
        public static uint CreateItemOnObject(string sTag, uint oTarget = OBJECT_INVALID, int nStackSize = 1,
            string sNewTag = "")
        {
            NativeFunctions.StackPushStringUTF8(sNewTag);
            NativeFunctions.StackPushInteger(nStackSize);
            NativeFunctions.StackPushObject(oTarget);
            NativeFunctions.StackPushStringUTF8(sTag);
            NativeFunctions.CallBuiltIn(31);
            return NativeFunctions.StackPopObject();
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
            NativeFunctions.StackPushInteger((int)nInventorySlot);
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(32);
        }

        /// <summary>
        ///   Unequip oItem from whatever slot it is currently in.
        /// </summary>
        public static void ActionUnequipItem(uint oItem)
        {
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(33);
        }

        /// <summary>
        ///   Pick up oItem from the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPickUpItem failed."
        /// </summary>
        public static void ActionPickUpItem(uint oItem)
        {
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(34);
        }

        /// <summary>
        ///   Put down oItem on the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPutDownItem failed."
        /// </summary>
        public static void ActionPutDownItem(uint oItem)
        {
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(35);
        }

        /// <summary>
        ///   Give oItem to oGiveTo
        ///   If oItem is not a valid item, or oGiveTo is not a valid object, nothing will
        ///   happen.
        /// </summary>
        public static void ActionGiveItem(uint oItem, uint oGiveTo)
        {
            NativeFunctions.StackPushObject(oGiveTo);
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(135);
        }

        /// <summary>
        ///   Take oItem from oTakeFrom
        ///   If oItem is not a valid item, or oTakeFrom is not a valid object, nothing
        ///   will happen.
        /// </summary>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom)
        {
            NativeFunctions.StackPushObject(oTakeFrom);
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(136);
        }

        /// <summary>
        ///   Create a Death effect
        ///   - nSpectacularDeath: if this is TRUE, the creature to which this effect is
        ///   applied will die in an extraordinary fashion
        ///   - nDisplayFeedback
        /// </summary>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true)
        {
            NativeFunctions.StackPushInteger(nDisplayFeedback ? 1 : 0);
            NativeFunctions.StackPushInteger(nSpectacularDeath ? 1 : 0);
            NativeFunctions.CallBuiltIn(133);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Knockdown effect
        ///   This effect knocks creatures off their feet, they will sit until the effect
        ///   is removed. This should be applied as a temporary effect with a 3 second
        ///   duration minimum (1 second to fall, 1 second sitting, 1 second to get up).
        /// </summary>
        public static Effect EffectKnockdown()
        {
            NativeFunctions.CallBuiltIn(134);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nChaMod);
            NativeFunctions.StackPushInteger(nWisMod);
            NativeFunctions.StackPushInteger(nIntMod);
            NativeFunctions.StackPushInteger(nConMod);
            NativeFunctions.StackPushInteger(nDexMod);
            NativeFunctions.StackPushInteger(nStrMod);
            NativeFunctions.CallBuiltIn(138);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Entangle effect
        ///   When applied, this effect will restrict the creature's movement and apply a
        ///   (-2) to all attacks and a -4 to AC.
        /// </summary>
        public static Effect EffectEntangle()
        {
            NativeFunctions.CallBuiltIn(130);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nSaveType);
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.StackPushInteger(nSave);
            NativeFunctions.CallBuiltIn(117);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Increase effect
        ///   - nBonus: size of attack bonus
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackIncrease(int nBonus, AttackBonus nModifierType = AttackBonus.Misc)
        {
            NativeFunctions.StackPushInteger((int)nModifierType);
            NativeFunctions.StackPushInteger(nBonus);
            NativeFunctions.CallBuiltIn(118);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nLimit);
            NativeFunctions.StackPushInteger((int)nDamagePower);
            NativeFunctions.StackPushInteger(nAmount);
            NativeFunctions.CallBuiltIn(119);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Increase effect
        ///   - nBonus: DAMAGE_BONUS_*
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may
        ///   result in odd behaviour.
        /// </summary>
        public static Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = Enum.DamageType.Magical)
        {
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger(nBonus);
            NativeFunctions.CallBuiltIn(120);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Magical and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Magical effects are removed by resting, and by dispel magic
        /// </summary>
        public static Effect MagicalEffect(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(112);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Supernatural and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Permanent supernatural effects are not removed by resting
        /// </summary>
        public static Effect SupernaturalEffect(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(113);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Extraordinary and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Extraordinary effects are removed by resting, but not by dispel magic
        /// </summary>
        public static Effect ExtraordinaryEffect(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(114);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger((int)nModifyType);
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.CallBuiltIn(115);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the first in-game effect on oCreature.
        /// </summary>
        public static Effect GetFirstEffect(uint oCreature)
        {
            NativeFunctions.StackPushObject(oCreature);
            NativeFunctions.CallBuiltIn(85);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the next in-game effect on oCreature.
        /// </summary>
        public static Effect GetNextEffect(uint oCreature)
        {
            NativeFunctions.StackPushObject(oCreature);
            NativeFunctions.CallBuiltIn(86);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Remove eEffect from oCreature.
        ///   * No return value
        /// </summary>
        public static void RemoveEffect(uint oCreature, Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.StackPushObject(oCreature);
            NativeFunctions.CallBuiltIn(87);
        }

        /// <summary>
        ///   * Returns TRUE if eEffect is a valid effect. The effect must have been applied to
        ///   * an object or else it will return FALSE
        /// </summary>
        public static bool GetIsEffectValid(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(88);
            return NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Get the duration type (DURATION_TYPE_*) of eEffect.
        ///   * Return value if eEffect is not valid: -1
        /// </summary>
        public static int GetEffectDurationType(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(89);
            return NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the subtype (SUBTYPE_*) of eEffect.
        ///   * Return value on error: 0
        /// </summary>
        public static int GetEffectSubType(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(90);
            return NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the object that created eEffect.
        ///   * Returns OBJECT_INVALID if eEffect is not a valid effect.
        /// </summary>
        public static uint GetEffectCreator(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(91);
            return NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Create a Heal effect. This should be applied as an instantaneous effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.
        /// </summary>
        public static Effect EffectHeal(int nDamageToHeal)
        {
            NativeFunctions.StackPushInteger(nDamageToHeal);
            NativeFunctions.CallBuiltIn(78);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage effect
        ///   - nDamageAmount: amount of damage to be dealt. This should be applied as an
        ///   instantaneous effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nDamagePower: DAMAGE_POWER_*
        /// </summary>
        public static Effect EffectDamage(int nDamageAmount, DamageType nDamageType = Enum.DamageType.Magical,
            DamagePower nDamagePower = DamagePower.Normal)
        {
            NativeFunctions.StackPushInteger((int)nDamagePower);
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger(nDamageAmount);
            NativeFunctions.CallBuiltIn(79);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Increase effect
        ///   - bAbilityToIncrease: ABILITY_*
        /// </summary>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy)
        {
            NativeFunctions.StackPushInteger(nModifyBy);
            NativeFunctions.StackPushInteger((int)nAbilityToIncrease);
            NativeFunctions.CallBuiltIn(80);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nLimit);
            NativeFunctions.StackPushInteger(nAmount);
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.CallBuiltIn(81);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Resurrection effect. This should be applied as an instantaneous effect.
        /// </summary>
        public static Effect EffectResurrection()
        {
            NativeFunctions.CallBuiltIn(82);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nUseAppearAnimation ? 1 : 0);
            NativeFunctions.StackPushFloat(fDelaySeconds);
            NativeFunctions.StackPushInteger((int)nVisualEffectId);
            NativeFunctions.StackPushStringUTF8(sCreatureResref);
            NativeFunctions.CallBuiltIn(83);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary
        ///   except that the observers get no saving throw
        /// </summary>
        public static Effect EffectEthereal()
        {
            NativeFunctions.CallBuiltIn(711);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates an effect that inhibits spells
        ///   - nPercent - percentage of failure
        ///   - nSpellSchool - the school of spells affected.
        /// </summary>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = Enum.SpellSchool.General)
        {
            NativeFunctions.StackPushInteger((int)nSpellSchool);
            NativeFunctions.StackPushInteger(nPercent);
            NativeFunctions.CallBuiltIn(690);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect that is guaranteed to dominate a creature
        ///   Like EffectDominated but cannot be resisted
        /// </summary>
        public static Effect EffectCutsceneDominated()
        {
            NativeFunctions.CallBuiltIn(604);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that will petrify the target
        ///   * currently applies EffectParalyze and the stoneskin visual effect.
        /// </summary>
        public static Effect EffectPetrify()
        {
            NativeFunctions.CallBuiltIn(583);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that is guaranteed to paralyze a creature.
        ///   this effect is identical to EffectParalyze except that it cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneParalyze()
        {
            NativeFunctions.CallBuiltIn(585);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Decrease effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   /  decrease
        /// </summary>
        public static Effect EffectTurnResistanceDecrease(int nHitDice)
        {
            NativeFunctions.StackPushInteger(nHitDice);
            NativeFunctions.CallBuiltIn(552);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Increase effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   increase
        /// </summary>
        public static Effect EffectTurnResistanceIncrease(int nHitDice)
        {
            NativeFunctions.StackPushInteger(nHitDice);
            NativeFunctions.CallBuiltIn(553);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushStringUTF8(sCreatureTemplate4);
            NativeFunctions.StackPushStringUTF8(sCreatureTemplate3);
            NativeFunctions.StackPushStringUTF8(sCreatureTemplate2);
            NativeFunctions.StackPushStringUTF8(sCreatureTemplate1);
            NativeFunctions.StackPushInteger(nLooping);
            NativeFunctions.CallBuiltIn(510);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nAnimation);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocation);
            NativeFunctions.CallBuiltIn(480);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disappear effect to make the object "fly away" and then destroy
        ///   itself.
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectDisappear(int nAnimation = 1)
        {
            NativeFunctions.StackPushInteger(nAnimation);
            NativeFunctions.CallBuiltIn(481);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Appear effect to make the object "fly in".
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectAppear(int nAnimation = 1)
        {
            NativeFunctions.StackPushInteger(nAnimation);
            NativeFunctions.CallBuiltIn(482);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Modify Attacks effect to add attacks.
        ///   - nAttacks: maximum is 5, even with the effect stacked
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.
        /// </summary>
        public static Effect EffectModifyAttacks(int nAttacks)
        {
            NativeFunctions.StackPushInteger(nAttacks);
            NativeFunctions.CallBuiltIn(485);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger((int)nRandomAmount);
            NativeFunctions.StackPushInteger(nDamageAmount);
            NativeFunctions.CallBuiltIn(487);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nMissChanceType);
            NativeFunctions.StackPushInteger(nPercentage);
            NativeFunctions.CallBuiltIn(477);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            SpellSchool nSpellSchool = Enum.SpellSchool.General)
        {
            NativeFunctions.StackPushInteger((int)nSpellSchool);
            NativeFunctions.StackPushInteger(nTotalSpellLevelsAbsorbed);
            NativeFunctions.StackPushInteger(nMaxSpellLevelAbsorbed);
            NativeFunctions.CallBuiltIn(472);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic Best effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            NativeFunctions.StackPushInteger(nCasterLevel);
            NativeFunctions.CallBuiltIn(473);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Invisibility effect.
        ///   - nInvisibilityType: INVISIBILITY_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType
        ///   is invalid.
        /// </summary>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType)
        {
            NativeFunctions.StackPushInteger((int)nInvisibilityType);
            NativeFunctions.CallBuiltIn(457);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nMissType);
            NativeFunctions.StackPushInteger(nPercentage);
            NativeFunctions.CallBuiltIn(458);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Darkness effect.
        /// </summary>
        public static Effect EffectDarkness()
        {
            NativeFunctions.CallBuiltIn(459);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic All effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            NativeFunctions.StackPushInteger(nCasterLevel);
            NativeFunctions.CallBuiltIn(460);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ultravision effect.
        /// </summary>
        public static Effect EffectUltravision()
        {
            NativeFunctions.CallBuiltIn(461);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Negative Level effect.
        ///   - nNumLevels: the number of negative levels to apply.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.
        /// </summary>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false)
        {
            NativeFunctions.StackPushInteger(bHPBonus ? 1 : 0);
            NativeFunctions.StackPushInteger(nNumLevels);
            NativeFunctions.CallBuiltIn(462);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Polymorph effect.
        /// </summary>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false)
        {
            NativeFunctions.StackPushInteger(nLocked ? 1 : 0);
            NativeFunctions.StackPushInteger(nPolymorphSelection);
            NativeFunctions.CallBuiltIn(463);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sanctuary effect.
        ///   - nDifficultyClass: must be a non-zero, positive number
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.
        /// </summary>
        public static Effect EffectSanctuary(int nDifficultyClass)
        {
            NativeFunctions.StackPushInteger(nDifficultyClass);
            NativeFunctions.CallBuiltIn(464);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a True Seeing effect.
        /// </summary>
        public static Effect EffectTrueSeeing()
        {
            NativeFunctions.CallBuiltIn(465);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a See Invisible effect.
        /// </summary>
        public static Effect EffectSeeInvisible()
        {
            NativeFunctions.CallBuiltIn(466);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Time Stop effect.
        /// </summary>
        public static Effect EffectTimeStop()
        {
            NativeFunctions.CallBuiltIn(467);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Blindness effect.
        /// </summary>
        public static Effect EffectBlindness()
        {
            NativeFunctions.CallBuiltIn(468);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Decrease effect.
        ///   - nAbility: ABILITY_*
        ///   - nModifyBy: This is the amount by which to decrement the ability
        /// </summary>
        public static Effect EffectAbilityDecrease(int nAbility, int nModifyBy)
        {
            NativeFunctions.StackPushInteger(nModifyBy);
            NativeFunctions.StackPushInteger(nAbility);
            NativeFunctions.CallBuiltIn(446);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Decrease effect.
        ///   - nPenalty
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackDecrease(int nPenalty, AttackBonus nModifierType = AttackBonus.Misc)
        {
            NativeFunctions.StackPushInteger((int)nModifierType);
            NativeFunctions.StackPushInteger(nPenalty);
            NativeFunctions.CallBuiltIn(447);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Decrease effect.
        ///   - nPenalty
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = Enum.DamageType.Magical)
        {
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger(nPenalty);
            NativeFunctions.CallBuiltIn(448);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Immunity Decrease effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity)
        {
            NativeFunctions.StackPushInteger(nPercentImmunity);
            NativeFunctions.StackPushInteger(nDamageType);
            NativeFunctions.CallBuiltIn(449);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.StackPushInteger((int)nModifyType);
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.CallBuiltIn(450);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nPercentChange);
            NativeFunctions.CallBuiltIn(451);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nSaveType);
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.StackPushInteger(nSave);
            NativeFunctions.CallBuiltIn(452);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Decrease effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillDecrease(int nSkill, int nValue)
        {
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.StackPushInteger(nSkill);
            NativeFunctions.CallBuiltIn(453);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Decrease effect.
        /// </summary>
        public static Effect EffectSpellResistanceDecrease(int nValue)
        {
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.CallBuiltIn(454);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Activate oItem.
        /// </summary>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID)
        {
            NativeFunctions.StackPushObject(oTarget);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            NativeFunctions.StackPushObject(oItem);
            NativeFunctions.CallBuiltIn(424);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Create a Hit Point Change When Dying effect.
        ///   - fHitPointChangePerRound: this can be positive or negative, but not zero.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.
        /// </summary>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound)
        {
            NativeFunctions.StackPushFloat(fHitPointChangePerRound);
            NativeFunctions.CallBuiltIn(387);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turned effect.
        ///   Turned effects are supernatural by default.
        /// </summary>
        public static Effect EffectTurned()
        {
            NativeFunctions.CallBuiltIn(379);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus a specific alignment.
        ///   - eEffect
        ///   - nLawChaos: ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL
        ///   - nGoodEvil: ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL
        /// </summary>
        public static Effect VersusAlignmentEffect(Effect eEffect,
            Alignment nLawChaos = Enum.Alignment.All,
            Alignment nGoodEvil = Enum.Alignment.All)
        {
            NativeFunctions.StackPushInteger((int)nGoodEvil);
            NativeFunctions.StackPushInteger((int)nLawChaos);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(355);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus nRacialType.
        ///   - eEffect
        ///   - nRacialType: RACIAL_TYPE_*
        /// </summary>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType)
        {
            NativeFunctions.StackPushInteger((int)nRacialType);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(356);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus traps.
        /// </summary>
        public static Effect VersusTrapEffect(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(357);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Increase effect.
        ///   - nSkill: SKILL_*
        ///   - nValue
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillIncrease(Skill nSkill, int nValue)
        {
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.StackPushInteger((int)nSkill);
            NativeFunctions.CallBuiltIn(351);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Temporary Hitpoints effect.
        ///   - nHitPoints: a positive integer
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.
        /// </summary>
        public static Effect EffectTemporaryHitpoints(int nHitPoints)
        {
            NativeFunctions.StackPushInteger(nHitPoints);
            NativeFunctions.CallBuiltIn(314);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.CallBuiltIn(295);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Creates a Damage Immunity Increase effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity)
        {
            NativeFunctions.StackPushInteger(nPercentImmunity);
            NativeFunctions.StackPushInteger((int)nDamageType);
            NativeFunctions.CallBuiltIn(275);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Immunity effect.
        ///   - nImmunityType: IMMUNITY_TYPE_*
        /// </summary>
        public static Effect EffectImmunity(ImmunityType nImmunityType)
        {
            NativeFunctions.StackPushInteger((int)nImmunityType);
            NativeFunctions.CallBuiltIn(273);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Haste effect.
        /// </summary>
        public static Effect EffectHaste()
        {
            NativeFunctions.CallBuiltIn(270);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Slow effect.
        /// </summary>
        public static Effect EffectSlow()
        {
            NativeFunctions.CallBuiltIn(271);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Poison effect.
        ///   - nPoisonType: POISON_*
        /// </summary>
        public static Effect EffectPoison(Poison nPoisonType)
        {
            NativeFunctions.StackPushInteger((int)nPoisonType);
            NativeFunctions.CallBuiltIn(250);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disease effect.
        ///   - nDiseaseType: DISEASE_*
        /// </summary>
        public static Effect EffectDisease(Disease nDiseaseType)
        {
            NativeFunctions.StackPushInteger((int)nDiseaseType);
            NativeFunctions.CallBuiltIn(251);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Silence effect.
        /// </summary>
        public static Effect EffectSilence()
        {
            NativeFunctions.CallBuiltIn(252);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Increase effect.
        ///   - nValue: size of spell resistance increase
        /// </summary>
        public static Effect EffectSpellResistanceIncrease(int nValue)
        {
            NativeFunctions.StackPushInteger(nValue);
            NativeFunctions.CallBuiltIn(212);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(bMissEffect ? 1 : 0);
            NativeFunctions.StackPushInteger((int)nBodyPart);
            NativeFunctions.StackPushObject(oEffector);
            NativeFunctions.StackPushInteger((int)nBeamVisualEffect);
            NativeFunctions.CallBuiltIn(207);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eParentEffect);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eChildEffect);
            NativeFunctions.CallBuiltIn(199);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   * Create a Visual Effect that can be applied to an object.
        ///   - nVisualEffectId
        ///   - nMissEffect: if this is TRUE, a random vector near or past the target will
        ///   be generated, on which to play the effect
        /// </summary>
        public static Effect EffectVisualEffect(VisualEffect visualEffectID, bool nMissEffect = false)
        {
            NativeFunctions.StackPushInteger(nMissEffect ? 1 : 0);
            NativeFunctions.StackPushInteger((int)visualEffectID);
            NativeFunctions.CallBuiltIn(180);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Apply eEffect to oTarget.
        /// </summary>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f)
        {
            NativeFunctions.StackPushFloat(fDuration);
            NativeFunctions.StackPushObject(oTarget);
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.StackPushInteger((int)nDurationType);
            NativeFunctions.CallBuiltIn(220);
        }

        /// <summary>
        ///   Get the effect type (EFFECT_TYPE_*) of eEffect.
        ///   * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT
        /// </summary>
        public static EffectTypeScript GetEffectType(Effect eEffect)
        {
            NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            NativeFunctions.CallBuiltIn(170);
            return (EffectTypeScript)NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Create an Area Of Effect effect in the area of the creature it is applied to.
        ///   If the scripts are not specified, default ones will be used.
        /// </summary>
        public static Effect EffectAreaOfEffect(AreaOfEffect nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "")
        {
            NativeFunctions.StackPushStringUTF8(sOnExitScript);
            NativeFunctions.StackPushStringUTF8(sHeartbeatScript);
            NativeFunctions.StackPushStringUTF8(sOnEnterScript);
            NativeFunctions.StackPushInteger((int)nAreaEffect);
            NativeFunctions.CallBuiltIn(171);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Regenerate effect.
        ///   - nAmount: amount of damage to be regenerated per time interval
        ///   - fIntervalSeconds: length of interval in seconds
        /// </summary>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds)
        {
            NativeFunctions.StackPushFloat(fIntervalSeconds);
            NativeFunctions.StackPushInteger(nAmount);
            NativeFunctions.CallBuiltIn(164);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger(nPercentChange);
            NativeFunctions.CallBuiltIn(165);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Charm effect
        /// </summary>
        public static Effect EffectCharmed()
        {
            NativeFunctions.CallBuiltIn(156);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Confuse effect
        /// </summary>
        public static Effect EffectConfused()
        {
            NativeFunctions.CallBuiltIn(157);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Frighten effect
        /// </summary>
        public static Effect EffectFrightened()
        {
            NativeFunctions.CallBuiltIn(158);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dominate effect
        /// </summary>
        public static Effect EffectDominated()
        {
            NativeFunctions.CallBuiltIn(159);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Daze effect
        /// </summary>
        public static Effect EffectDazed()
        {
            NativeFunctions.CallBuiltIn(160);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Stun effect
        /// </summary>
        public static Effect EffectStunned()
        {
            NativeFunctions.CallBuiltIn(161);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sleep effect
        /// </summary>
        public static Effect EffectSleep()
        {
            NativeFunctions.CallBuiltIn(154);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Paralyze effect
        /// </summary>
        public static Effect EffectParalyze()
        {
            NativeFunctions.CallBuiltIn(148);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            NativeFunctions.StackPushInteger((int)nImmunityToSpell);
            NativeFunctions.CallBuiltIn(149);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Deaf effect
        /// </summary>
        public static Effect EffectDeaf()
        {
            NativeFunctions.CallBuiltIn(150);
            return NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }
    }
}