using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using Alignment = SWLOR.Game.Server.Core.NWScript.Enum.Alignment;
using DamageType = SWLOR.Game.Server.Core.NWScript.Enum.DamageType;
using RacialType = SWLOR.Game.Server.Core.NWScript.Enum.RacialType;
using SpellSchool = SWLOR.Game.Server.Core.NWScript.Enum.SpellSchool;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Returns the string tag set for the provided effect.
        ///   - If no tag has been set, returns an empty string.
        /// </summary>
        public static string GetEffectTag(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(849);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Tags the effect with the provided string.
        ///   - Any other tags in the link will be overwritten.
        /// </summary>
        public static Effect TagEffect(Effect eEffect, string sNewTag)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sNewTag);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(850);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns the caster level of the creature who created the effect.
        ///   - If not created by a creature, returns 0.
        ///   - If created by a spell-like ability, returns 0.
        /// </summary>
        public static int GetEffectCasterLevel(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(851);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns the total duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDuration(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(852);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns the remaining duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDurationRemaining(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(853);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns an effect that when applied will paralyze the target's legs, rendering
        ///   them unable to walk but otherwise unpenalized. This effect cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneImmobilize()
        {
            Internal.NativeFunctions.CallBuiltIn(767);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates a cutscene ghost effect, this will allow creatures
        ///   to pathfind through other creatures without bumping into them
        ///   for the duration of the effect.
        /// </summary>
        public static Effect EffectCutsceneGhost()
        {
            Internal.NativeFunctions.CallBuiltIn(757);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns TRUE if the item is cursed and cannot be dropped
        /// </summary>
        public static bool GetItemCursedFlag(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(744);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   When cursed, items cannot be dropped
        /// </summary>
        public static void SetItemCursedFlag(uint oItem, bool nCursed)
        {
            Internal.NativeFunctions.StackPushInteger(nCursed ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(745);
        }

        /// <summary>
        ///   Get the possessor of oItem
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessor(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(29);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the object possessed by oCreature with the tag sItemTag
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sItemTag);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(30);
            return Internal.NativeFunctions.StackPopObject();
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
            Internal.NativeFunctions.StackPushStringUTF8(sNewTag);
            Internal.NativeFunctions.StackPushInteger(nStackSize);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushStringUTF8(sResRef);
            Internal.NativeFunctions.CallBuiltIn(31);
            return Internal.NativeFunctions.StackPopObject();
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
            Internal.NativeFunctions.StackPushInteger((int)nInventorySlot);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(32);
        }

        /// <summary>
        ///   Unequip oItem from whatever slot it is currently in.
        /// </summary>
        public static void ActionUnequipItem(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(33);
        }

        /// <summary>
        ///   Pick up oItem from the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPickUpItem failed."
        /// </summary>
        public static void ActionPickUpItem(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(34);
        }

        /// <summary>
        ///   Put down oItem on the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPutDownItem failed."
        /// </summary>
        public static void ActionPutDownItem(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(35);
        }

        /// <summary>
        ///   Give oItem to oGiveTo
        ///   If oItem is not a valid item, or oGiveTo is not a valid object, nothing will
        ///   happen.
        /// </summary>
        public static void ActionGiveItem(uint oItem, uint oGiveTo)
        {
            Internal.NativeFunctions.StackPushObject(oGiveTo);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(135);
        }

        /// <summary>
        ///   Take oItem from oTakeFrom
        ///   If oItem is not a valid item, or oTakeFrom is not a valid object, nothing
        ///   will happen.
        /// </summary>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom)
        {
            Internal.NativeFunctions.StackPushObject(oTakeFrom);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(136);
        }

        /// <summary>
        ///   Create a Death effect
        ///   - nSpectacularDeath: if this is TRUE, the creature to which this effect is
        ///   applied will die in an extraordinary fashion
        ///   - nDisplayFeedback
        /// </summary>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true)
        {
            Internal.NativeFunctions.StackPushInteger(nDisplayFeedback ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nSpectacularDeath ? 1 : 0);
            Internal.NativeFunctions.CallBuiltIn(133);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Knockdown effect
        ///   This effect knocks creatures off their feet, they will sit until the effect
        ///   is removed. This should be applied as a temporary effect with a 3 second
        ///   duration minimum (1 second to fall, 1 second sitting, 1 second to get up).
        /// </summary>
        public static Effect EffectKnockdown()
        {
            Internal.NativeFunctions.CallBuiltIn(134);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nChaMod);
            Internal.NativeFunctions.StackPushInteger(nWisMod);
            Internal.NativeFunctions.StackPushInteger(nIntMod);
            Internal.NativeFunctions.StackPushInteger(nConMod);
            Internal.NativeFunctions.StackPushInteger(nDexMod);
            Internal.NativeFunctions.StackPushInteger(nStrMod);
            Internal.NativeFunctions.CallBuiltIn(138);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Entangle effect
        ///   When applied, this effect will restrict the creature's movement and apply a
        ///   (-2) to all attacks and a -4 to AC.
        /// </summary>
        public static Effect EffectEntangle()
        {
            Internal.NativeFunctions.CallBuiltIn(130);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushInteger(nSave);
            Internal.NativeFunctions.CallBuiltIn(117);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Increase effect
        ///   - nBonus: size of attack bonus
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackIncrease(int nBonus, AttackBonus nModifierType = AttackBonus.Misc)
        {
            Internal.NativeFunctions.StackPushInteger((int)nModifierType);
            Internal.NativeFunctions.StackPushInteger(nBonus);
            Internal.NativeFunctions.CallBuiltIn(118);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nLimit);
            Internal.NativeFunctions.StackPushInteger((int)nDamagePower);
            Internal.NativeFunctions.StackPushInteger(nAmount);
            Internal.NativeFunctions.CallBuiltIn(119);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Increase effect
        ///   - nBonus: DAMAGE_BONUS_*
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may
        ///   result in odd behaviour.
        /// </summary>
        public static Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = DamageType.Magical)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger(nBonus);
            Internal.NativeFunctions.CallBuiltIn(120);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Magical and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Magical effects are removed by resting, and by dispel magic
        /// </summary>
        public static Effect MagicalEffect(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(112);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Supernatural and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Permanent supernatural effects are not removed by resting
        /// </summary>
        public static Effect SupernaturalEffect(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(113);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Extraordinary and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Extraordinary effects are removed by resting, but not by dispel magic
        /// </summary>
        public static Effect ExtraordinaryEffect(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(114);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger((int)nModifyType);
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.CallBuiltIn(115);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the first in-game effect on oCreature.
        /// </summary>
        public static Effect GetFirstEffect(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(85);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the next in-game effect on oCreature.
        /// </summary>
        public static Effect GetNextEffect(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(86);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Remove eEffect from oCreature.
        ///   * No return value
        /// </summary>
        public static void RemoveEffect(uint oCreature, Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(87);
        }

        /// <summary>
        ///   * Returns TRUE if eEffect is a valid effect. The effect must have been applied to
        ///   * an object or else it will return FALSE
        /// </summary>
        public static bool GetIsEffectValid(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(88);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Get the duration type (DURATION_TYPE_*) of eEffect.
        ///   * Return value if eEffect is not valid: -1
        /// </summary>
        public static int GetEffectDurationType(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(89);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the subtype (SUBTYPE_*) of eEffect.
        ///   * Return value on error: 0
        /// </summary>
        public static int GetEffectSubType(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(90);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the object that created eEffect.
        ///   * Returns OBJECT_INVALID if eEffect is not a valid effect.
        /// </summary>
        public static uint GetEffectCreator(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(91);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Create a Heal effect. This should be applied as an instantaneous effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.
        /// </summary>
        public static Effect EffectHeal(int nDamageToHeal)
        {
            Internal.NativeFunctions.StackPushInteger(nDamageToHeal);
            Internal.NativeFunctions.CallBuiltIn(78);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage effect
        ///   - nDamageAmount: amount of damage to be dealt. This should be applied as an
        ///   instantaneous effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nDamagePower: DAMAGE_POWER_*
        /// </summary>
        public static Effect EffectDamage(int nDamageAmount, DamageType nDamageType = DamageType.Magical,
            DamagePower nDamagePower = DamagePower.Normal)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDamagePower);
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger(nDamageAmount);
            Internal.NativeFunctions.CallBuiltIn(79);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Increase effect
        ///   - bAbilityToIncrease: ABILITY_*
        /// </summary>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy)
        {
            Internal.NativeFunctions.StackPushInteger(nModifyBy);
            Internal.NativeFunctions.StackPushInteger((int)nAbilityToIncrease);
            Internal.NativeFunctions.CallBuiltIn(80);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nLimit);
            Internal.NativeFunctions.StackPushInteger(nAmount);
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.CallBuiltIn(81);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Resurrection effect. This should be applied as an instantaneous effect.
        /// </summary>
        public static Effect EffectResurrection()
        {
            Internal.NativeFunctions.CallBuiltIn(82);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nUseAppearAnimation ? 1 : 0);
            Internal.NativeFunctions.StackPushFloat(fDelaySeconds);
            Internal.NativeFunctions.StackPushInteger((int)nVisualEffectId);
            Internal.NativeFunctions.StackPushStringUTF8(sCreatureResref);
            Internal.NativeFunctions.CallBuiltIn(83);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary
        ///   except that the observers get no saving throw
        /// </summary>
        public static Effect EffectEthereal()
        {
            Internal.NativeFunctions.CallBuiltIn(711);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates an effect that inhibits spells
        ///   - nPercent - percentage of failure
        ///   - nSpellSchool - the school of spells affected.
        /// </summary>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            Internal.NativeFunctions.StackPushInteger((int)nSpellSchool);
            Internal.NativeFunctions.StackPushInteger(nPercent);
            Internal.NativeFunctions.CallBuiltIn(690);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect that is guaranteed to dominate a creature
        ///   Like EffectDominated but cannot be resisted
        /// </summary>
        public static Effect EffectCutsceneDominated()
        {
            Internal.NativeFunctions.CallBuiltIn(604);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that will petrify the target
        ///   * currently applies EffectParalyze and the stoneskin visual effect.
        /// </summary>
        public static Effect EffectPetrify()
        {
            Internal.NativeFunctions.CallBuiltIn(583);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that is guaranteed to paralyze a creature.
        ///   this effect is identical to EffectParalyze except that it cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneParalyze()
        {
            Internal.NativeFunctions.CallBuiltIn(585);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Decrease effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   /  decrease
        /// </summary>
        public static Effect EffectTurnResistanceDecrease(int nHitDice)
        {
            Internal.NativeFunctions.StackPushInteger(nHitDice);
            Internal.NativeFunctions.CallBuiltIn(552);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Increase effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   increase
        /// </summary>
        public static Effect EffectTurnResistanceIncrease(int nHitDice)
        {
            Internal.NativeFunctions.StackPushInteger(nHitDice);
            Internal.NativeFunctions.CallBuiltIn(553);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushStringUTF8(sCreatureTemplate4);
            Internal.NativeFunctions.StackPushStringUTF8(sCreatureTemplate3);
            Internal.NativeFunctions.StackPushStringUTF8(sCreatureTemplate2);
            Internal.NativeFunctions.StackPushStringUTF8(sCreatureTemplate1);
            Internal.NativeFunctions.StackPushInteger(nLooping);
            Internal.NativeFunctions.CallBuiltIn(510);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nAnimation);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocation);
            Internal.NativeFunctions.CallBuiltIn(480);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disappear effect to make the object "fly away" and then destroy
        ///   itself.
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectDisappear(int nAnimation = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nAnimation);
            Internal.NativeFunctions.CallBuiltIn(481);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Appear effect to make the object "fly in".
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectAppear(int nAnimation = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nAnimation);
            Internal.NativeFunctions.CallBuiltIn(482);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Modify Attacks effect to add attacks.
        ///   - nAttacks: maximum is 5, even with the effect stacked
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.
        /// </summary>
        public static Effect EffectModifyAttacks(int nAttacks)
        {
            Internal.NativeFunctions.StackPushInteger(nAttacks);
            Internal.NativeFunctions.CallBuiltIn(485);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger((int)nRandomAmount);
            Internal.NativeFunctions.StackPushInteger(nDamageAmount);
            Internal.NativeFunctions.CallBuiltIn(487);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nMissChanceType);
            Internal.NativeFunctions.StackPushInteger(nPercentage);
            Internal.NativeFunctions.CallBuiltIn(477);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nSpellSchool);
            Internal.NativeFunctions.StackPushInteger(nTotalSpellLevelsAbsorbed);
            Internal.NativeFunctions.StackPushInteger(nMaxSpellLevelAbsorbed);
            Internal.NativeFunctions.CallBuiltIn(472);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic Best effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            Internal.NativeFunctions.StackPushInteger(nCasterLevel);
            Internal.NativeFunctions.CallBuiltIn(473);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Invisibility effect.
        ///   - nInvisibilityType: INVISIBILITY_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType
        ///   is invalid.
        /// </summary>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nInvisibilityType);
            Internal.NativeFunctions.CallBuiltIn(457);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nMissType);
            Internal.NativeFunctions.StackPushInteger(nPercentage);
            Internal.NativeFunctions.CallBuiltIn(458);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Darkness effect.
        /// </summary>
        public static Effect EffectDarkness()
        {
            Internal.NativeFunctions.CallBuiltIn(459);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic All effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            Internal.NativeFunctions.StackPushInteger(nCasterLevel);
            Internal.NativeFunctions.CallBuiltIn(460);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ultravision effect.
        /// </summary>
        public static Effect EffectUltravision()
        {
            Internal.NativeFunctions.CallBuiltIn(461);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Negative Level effect.
        ///   - nNumLevels: the number of negative levels to apply.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.
        /// </summary>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false)
        {
            Internal.NativeFunctions.StackPushInteger(bHPBonus ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nNumLevels);
            Internal.NativeFunctions.CallBuiltIn(462);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Polymorph effect.
        /// </summary>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false)
        {
            Internal.NativeFunctions.StackPushInteger(nLocked ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nPolymorphSelection);
            Internal.NativeFunctions.CallBuiltIn(463);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sanctuary effect.
        ///   - nDifficultyClass: must be a non-zero, positive number
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.
        /// </summary>
        public static Effect EffectSanctuary(int nDifficultyClass)
        {
            Internal.NativeFunctions.StackPushInteger(nDifficultyClass);
            Internal.NativeFunctions.CallBuiltIn(464);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a True Seeing effect.
        /// </summary>
        public static Effect EffectTrueSeeing()
        {
            Internal.NativeFunctions.CallBuiltIn(465);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a See Invisible effect.
        /// </summary>
        public static Effect EffectSeeInvisible()
        {
            Internal.NativeFunctions.CallBuiltIn(466);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Time Stop effect.
        /// </summary>
        public static Effect EffectTimeStop()
        {
            Internal.NativeFunctions.CallBuiltIn(467);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Blindness effect.
        /// </summary>
        public static Effect EffectBlindness()
        {
            Internal.NativeFunctions.CallBuiltIn(468);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Decrease effect.
        ///   - nAbility: ABILITY_*
        ///   - nModifyBy: This is the amount by which to decrement the ability
        /// </summary>
        public static Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy)
        {
            Internal.NativeFunctions.StackPushInteger(nModifyBy);
            Internal.NativeFunctions.StackPushInteger((int)nAbility);
            Internal.NativeFunctions.CallBuiltIn(446);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Decrease effect.
        ///   - nPenalty
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackDecrease(int nPenalty, AttackBonus nModifierType = AttackBonus.Misc)
        {
            Internal.NativeFunctions.StackPushInteger((int)nModifierType);
            Internal.NativeFunctions.StackPushInteger(nPenalty);
            Internal.NativeFunctions.CallBuiltIn(447);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Decrease effect.
        ///   - nPenalty
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Magical)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger(nPenalty);
            Internal.NativeFunctions.CallBuiltIn(448);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Immunity Decrease effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity)
        {
            Internal.NativeFunctions.StackPushInteger(nPercentImmunity);
            Internal.NativeFunctions.StackPushInteger(nDamageType);
            Internal.NativeFunctions.CallBuiltIn(449);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.StackPushInteger((int)nModifyType);
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.CallBuiltIn(450);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nPercentChange);
            Internal.NativeFunctions.CallBuiltIn(451);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushInteger(nSave);
            Internal.NativeFunctions.CallBuiltIn(452);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Decrease effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillDecrease(int nSkill, int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushInteger(nSkill);
            Internal.NativeFunctions.CallBuiltIn(453);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Decrease effect.
        /// </summary>
        public static Effect EffectSpellResistanceDecrease(int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.CallBuiltIn(454);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Activate oItem.
        /// </summary>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(424);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Create a Hit Point Change When Dying effect.
        ///   - fHitPointChangePerRound: this can be positive or negative, but not zero.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.
        /// </summary>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound)
        {
            Internal.NativeFunctions.StackPushFloat(fHitPointChangePerRound);
            Internal.NativeFunctions.CallBuiltIn(387);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turned effect.
        ///   Turned effects are supernatural by default.
        /// </summary>
        public static Effect EffectTurned()
        {
            Internal.NativeFunctions.CallBuiltIn(379);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nGoodEvil);
            Internal.NativeFunctions.StackPushInteger((int)nLawChaos);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(355);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus nRacialType.
        ///   - eEffect
        ///   - nRacialType: RACIAL_TYPE_*
        /// </summary>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nRacialType);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(356);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus traps.
        /// </summary>
        public static Effect VersusTrapEffect(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(357);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Increase effect.
        ///   - nSkill: SKILL_*
        ///   - nValue
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillIncrease(Skill nSkill, int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushInteger((int)nSkill);
            Internal.NativeFunctions.CallBuiltIn(351);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Temporary Hitpoints effect.
        ///   - nHitPoints: a positive integer
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.
        /// </summary>
        public static Effect EffectTemporaryHitpoints(int nHitPoints)
        {
            Internal.NativeFunctions.StackPushInteger(nHitPoints);
            Internal.NativeFunctions.CallBuiltIn(314);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.CallBuiltIn(295);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Creates a Damage Immunity Increase effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity)
        {
            Internal.NativeFunctions.StackPushInteger(nPercentImmunity);
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.CallBuiltIn(275);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Immunity effect.
        ///   - nImmunityType: IMMUNITY_TYPE_*
        /// </summary>
        public static Effect EffectImmunity(ImmunityType nImmunityType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nImmunityType);
            Internal.NativeFunctions.CallBuiltIn(273);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Haste effect.
        /// </summary>
        public static Effect EffectHaste()
        {
            Internal.NativeFunctions.CallBuiltIn(270);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Slow effect.
        /// </summary>
        public static Effect EffectSlow()
        {
            Internal.NativeFunctions.CallBuiltIn(271);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Poison effect.
        ///   - nPoisonType: POISON_*
        /// </summary>
        public static Effect EffectPoison(Poison nPoisonType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nPoisonType);
            Internal.NativeFunctions.CallBuiltIn(250);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disease effect.
        ///   - nDiseaseType: DISEASE_*
        /// </summary>
        public static Effect EffectDisease(Disease nDiseaseType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDiseaseType);
            Internal.NativeFunctions.CallBuiltIn(251);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Silence effect.
        /// </summary>
        public static Effect EffectSilence()
        {
            Internal.NativeFunctions.CallBuiltIn(252);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Increase effect.
        ///   - nValue: size of spell resistance increase
        /// </summary>
        public static Effect EffectSpellResistanceIncrease(int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.CallBuiltIn(212);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(bMissEffect ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nBodyPart);
            Internal.NativeFunctions.StackPushObject(oEffector);
            Internal.NativeFunctions.StackPushInteger((int)nBeamVisualEffect);
            Internal.NativeFunctions.CallBuiltIn(207);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eParentEffect);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eChildEffect);
            Internal.NativeFunctions.CallBuiltIn(199);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   * Create a Visual Effect that can be applied to an object.
        ///   - nVisualEffectId
        ///   - nMissEffect: if this is TRUE, a random vector near or past the target will
        ///   be generated, on which to play the effect
        /// </summary>
        public static Effect EffectVisualEffect(VisualEffect visualEffectID, bool nMissEffect = false)
        {
            Internal.NativeFunctions.StackPushInteger(nMissEffect ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)visualEffectID);
            Internal.NativeFunctions.CallBuiltIn(180);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Apply eEffect to oTarget.
        /// </summary>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fDuration);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.StackPushInteger((int)nDurationType);
            Internal.NativeFunctions.CallBuiltIn(220);
        }

        /// <summary>
        ///   Get the effect type (EFFECT_TYPE_*) of eEffect.
        ///   * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT
        /// </summary>
        public static EffectTypeScript GetEffectType(Effect eEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect);
            Internal.NativeFunctions.CallBuiltIn(170);
            return (EffectTypeScript)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Create an Area Of Effect effect in the area of the creature it is applied to.
        ///   If the scripts are not specified, default ones will be used.
        /// </summary>
        public static Effect EffectAreaOfEffect(AreaOfEffect nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "")
        {
            Internal.NativeFunctions.StackPushStringUTF8(sOnExitScript);
            Internal.NativeFunctions.StackPushStringUTF8(sHeartbeatScript);
            Internal.NativeFunctions.StackPushStringUTF8(sOnEnterScript);
            Internal.NativeFunctions.StackPushInteger((int)nAreaEffect);
            Internal.NativeFunctions.CallBuiltIn(171);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Regenerate effect.
        ///   - nAmount: amount of damage to be regenerated per time interval
        ///   - fIntervalSeconds: length of interval in seconds
        /// </summary>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds)
        {
            Internal.NativeFunctions.StackPushFloat(fIntervalSeconds);
            Internal.NativeFunctions.StackPushInteger(nAmount);
            Internal.NativeFunctions.CallBuiltIn(164);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger(nPercentChange);
            Internal.NativeFunctions.CallBuiltIn(165);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Charm effect
        /// </summary>
        public static Effect EffectCharmed()
        {
            Internal.NativeFunctions.CallBuiltIn(156);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Confuse effect
        /// </summary>
        public static Effect EffectConfused()
        {
            Internal.NativeFunctions.CallBuiltIn(157);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Frighten effect
        /// </summary>
        public static Effect EffectFrightened()
        {
            Internal.NativeFunctions.CallBuiltIn(158);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dominate effect
        /// </summary>
        public static Effect EffectDominated()
        {
            Internal.NativeFunctions.CallBuiltIn(159);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Daze effect
        /// </summary>
        public static Effect EffectDazed()
        {
            Internal.NativeFunctions.CallBuiltIn(160);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Stun effect
        /// </summary>
        public static Effect EffectStunned()
        {
            Internal.NativeFunctions.CallBuiltIn(161);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sleep effect
        /// </summary>
        public static Effect EffectSleep()
        {
            Internal.NativeFunctions.CallBuiltIn(154);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Paralyze effect
        /// </summary>
        public static Effect EffectParalyze()
        {
            Internal.NativeFunctions.CallBuiltIn(148);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
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
            Internal.NativeFunctions.StackPushInteger((int)nImmunityToSpell);
            Internal.NativeFunctions.CallBuiltIn(149);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Deaf effect
        /// </summary>
        public static Effect EffectDeaf()
        {
            Internal.NativeFunctions.CallBuiltIn(150);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Effect);
        }


        /// <summary>
        ///  Get the integer parameter of eEffect at nIndex.
        ///  nIndex bounds: 0 to 7 inclusive
        ///  Some experimentation will be needed to find the right index for the value you wish to determine.
        ///  Returns: the value or 0 on error/when not set.
        /// </summary>
        public static int GetEffectInteger(Effect eEffect, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int) EngineStructure.Effect, eEffect.Handle);
            Internal.NativeFunctions.CallBuiltIn(939);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        /// Get the float parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or 0.0f on error/when not set.
        /// </summary>
        public static float GetEffectFloat(Effect eEffect, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect.Handle);
            Internal.NativeFunctions.CallBuiltIn(940);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        /// Get the string parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 5 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or "" on error/when not set.
        /// </summary>
        public static string GetEffectString(Effect eEffect, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect.Handle);
            Internal.NativeFunctions.CallBuiltIn(941);
            return Internal.NativeFunctions.StackPopString();
        }
        
        /// <summary>
        /// Get the object parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or OBJECT_INVALID on error/when not set.
        /// </summary>
        public static uint GetEffectObject(Effect eEffect, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect.Handle);
            Internal.NativeFunctions.CallBuiltIn(942);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        /// Get the vector parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 1 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or {0.0f, 0.0f, 0.0f} on error/when not set.
        /// </summary>
        public static Vector3 GetEffectVector(Effect eEffect, int nIndex)
        {
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eEffect.Handle);
            Internal.NativeFunctions.CallBuiltIn(943);
            return Internal.NativeFunctions.StackPopVector();
        }

    }
}