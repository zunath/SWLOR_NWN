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
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(849);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Tags the effect with the provided string.
        ///   - Any other tags in the link will be overwritten.
        /// </summary>
        public static Effect TagEffect(Effect eEffect, string sNewTag)
        {
            VM.StackPush(sNewTag);
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(850);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns the caster level of the creature who created the effect.
        ///   - If not created by a creature, returns 0.
        ///   - If created by a spell-like ability, returns 0.
        /// </summary>
        public static int GetEffectCasterLevel(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(851);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns the total duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDuration(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(852);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns the remaining duration of the effect in seconds.
        ///   - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        public static int GetEffectDurationRemaining(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(853);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns an effect that when applied will paralyze the target's legs, rendering
        ///   them unable to walk but otherwise unpenalized. This effect cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneImmobilize()
        {
            VM.Call(767);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates a cutscene ghost effect, this will allow creatures
        ///   to pathfind through other creatures without bumping into them
        ///   for the duration of the effect.
        /// </summary>
        public static Effect EffectCutsceneGhost()
        {
            VM.Call(757);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns TRUE if the item is cursed and cannot be dropped
        /// </summary>
        public static bool GetItemCursedFlag(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(744);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   When cursed, items cannot be dropped
        /// </summary>
        public static void SetItemCursedFlag(uint oItem, bool nCursed)
        {
            VM.StackPush(nCursed ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(745);
        }

        /// <summary>
        ///   Get the possessor of oItem
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessor(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(29);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the object possessed by oCreature with the tag sItemTag
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag)
        {
            VM.StackPush(sItemTag);
            VM.StackPush(oCreature);
            VM.Call(30);
            return VM.StackPopObject();
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
            VM.StackPush(sNewTag);
            VM.StackPush(nStackSize);
            VM.StackPush(oTarget);
            VM.StackPush(sResRef);
            VM.Call(31);
            return VM.StackPopObject();
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
            VM.StackPush((int)nInventorySlot);
            VM.StackPush(oItem);
            VM.Call(32);
        }

        /// <summary>
        ///   Unequip oItem from whatever slot it is currently in.
        /// </summary>
        public static void ActionUnequipItem(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(33);
        }

        /// <summary>
        ///   Pick up oItem from the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPickUpItem failed."
        /// </summary>
        public static void ActionPickUpItem(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(34);
        }

        /// <summary>
        ///   Put down oItem on the ground.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionPutDownItem failed."
        /// </summary>
        public static void ActionPutDownItem(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(35);
        }

        /// <summary>
        ///   Give oItem to oGiveTo
        ///   If oItem is not a valid item, or oGiveTo is not a valid object, nothing will
        ///   happen.
        /// </summary>
        public static void ActionGiveItem(uint oItem, uint oGiveTo)
        {
            VM.StackPush(oGiveTo);
            VM.StackPush(oItem);
            VM.Call(135);
        }

        /// <summary>
        ///   Take oItem from oTakeFrom
        ///   If oItem is not a valid item, or oTakeFrom is not a valid object, nothing
        ///   will happen.
        /// </summary>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom)
        {
            VM.StackPush(oTakeFrom);
            VM.StackPush(oItem);
            VM.Call(136);
        }

        /// <summary>
        ///   Create a Death effect
        ///   - nSpectacularDeath: if this is TRUE, the creature to which this effect is
        ///   applied will die in an extraordinary fashion
        ///   - nDisplayFeedback
        /// </summary>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true)
        {
            VM.StackPush(nDisplayFeedback ? 1 : 0);
            VM.StackPush(nSpectacularDeath ? 1 : 0);
            VM.Call(133);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Knockdown effect
        ///   This effect knocks creatures off their feet, they will sit until the effect
        ///   is removed. This should be applied as a temporary effect with a 3 second
        ///   duration minimum (1 second to fall, 1 second sitting, 1 second to get up).
        /// </summary>
        public static Effect EffectKnockdown()
        {
            VM.Call(134);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nChaMod);
            VM.StackPush(nWisMod);
            VM.StackPush(nIntMod);
            VM.StackPush(nConMod);
            VM.StackPush(nDexMod);
            VM.StackPush(nStrMod);
            VM.Call(138);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Entangle effect
        ///   When applied, this effect will restrict the creature's movement and apply a
        ///   (-2) to all attacks and a -4 to AC.
        /// </summary>
        public static Effect EffectEntangle()
        {
            VM.Call(130);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nSaveType);
            VM.StackPush(nValue);
            VM.StackPush(nSave);
            VM.Call(117);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Increase effect
        ///   - nBonus: size of attack bonus
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackIncrease(int nBonus, AttackBonus nModifierType = AttackBonus.Misc)
        {
            VM.StackPush((int)nModifierType);
            VM.StackPush(nBonus);
            VM.Call(118);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nLimit);
            VM.StackPush((int)nDamagePower);
            VM.StackPush(nAmount);
            VM.Call(119);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nDamageType);
            VM.StackPush(nBonus);
            VM.Call(120);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Magical and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Magical effects are removed by resting, and by dispel magic
        /// </summary>
        public static Effect MagicalEffect(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(112);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Supernatural and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Permanent supernatural effects are not removed by resting
        /// </summary>
        public static Effect SupernaturalEffect(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(113);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set the subtype of eEffect to Extraordinary and return eEffect.
        ///   (Effects default to magical if the subtype is not set)
        ///   Extraordinary effects are removed by resting, but not by dispel magic
        /// </summary>
        public static Effect ExtraordinaryEffect(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(114);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nDamageType);
            VM.StackPush((int)nModifyType);
            VM.StackPush(nValue);
            VM.Call(115);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the first in-game effect on oCreature.
        /// </summary>
        public static Effect GetFirstEffect(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(85);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Get the next in-game effect on oCreature.
        /// </summary>
        public static Effect GetNextEffect(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(86);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Remove eEffect from oCreature.
        ///   * No return value
        /// </summary>
        public static void RemoveEffect(uint oCreature, Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.StackPush(oCreature);
            VM.Call(87);
        }

        /// <summary>
        ///   * Returns TRUE if eEffect is a valid effect. The effect must have been applied to
        ///   * an object or else it will return FALSE
        /// </summary>
        public static bool GetIsEffectValid(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(88);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        ///   Get the duration type (DURATION_TYPE_*) of eEffect.
        ///   * Return value if eEffect is not valid: -1
        /// </summary>
        public static int GetEffectDurationType(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(89);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the subtype (SUBTYPE_*) of eEffect.
        ///   * Return value on error: 0
        /// </summary>
        public static int GetEffectSubType(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(90);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the object that created eEffect.
        ///   * Returns OBJECT_INVALID if eEffect is not a valid effect.
        /// </summary>
        public static uint GetEffectCreator(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(91);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Create a Heal effect. This should be applied as an instantaneous effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.
        /// </summary>
        public static Effect EffectHeal(int nDamageToHeal)
        {
            VM.StackPush(nDamageToHeal);
            VM.Call(78);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nDamagePower);
            VM.StackPush((int)nDamageType);
            VM.StackPush(nDamageAmount);
            VM.Call(79);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Increase effect
        ///   - bAbilityToIncrease: ABILITY_*
        /// </summary>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy)
        {
            VM.StackPush(nModifyBy);
            VM.StackPush((int)nAbilityToIncrease);
            VM.Call(80);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nLimit);
            VM.StackPush(nAmount);
            VM.StackPush((int)nDamageType);
            VM.Call(81);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Resurrection effect. This should be applied as an instantaneous effect.
        /// </summary>
        public static Effect EffectResurrection()
        {
            VM.Call(82);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nUseAppearAnimation ? 1 : 0);
            VM.StackPush(fDelaySeconds);
            VM.StackPush((int)nVisualEffectId);
            VM.StackPush(sCreatureResref);
            VM.Call(83);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary
        ///   except that the observers get no saving throw
        /// </summary>
        public static Effect EffectEthereal()
        {
            VM.Call(711);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Creates an effect that inhibits spells
        ///   - nPercent - percentage of failure
        ///   - nSpellSchool - the school of spells affected.
        /// </summary>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            VM.StackPush((int)nSpellSchool);
            VM.StackPush(nPercent);
            VM.Call(690);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Returns an effect that is guaranteed to dominate a creature
        ///   Like EffectDominated but cannot be resisted
        /// </summary>
        public static Effect EffectCutsceneDominated()
        {
            VM.Call(604);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that will petrify the target
        ///   * currently applies EffectParalyze and the stoneskin visual effect.
        /// </summary>
        public static Effect EffectPetrify()
        {
            VM.Call(583);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   returns an effect that is guaranteed to paralyze a creature.
        ///   this effect is identical to EffectParalyze except that it cannot be resisted.
        /// </summary>
        public static Effect EffectCutsceneParalyze()
        {
            VM.Call(585);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Decrease effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   /  decrease
        /// </summary>
        public static Effect EffectTurnResistanceDecrease(int nHitDice)
        {
            VM.StackPush(nHitDice);
            VM.Call(552);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turn Resistance Increase effect.
        ///   - nHitDice: a positive number representing the number of hit dice for the
        ///   increase
        /// </summary>
        public static Effect EffectTurnResistanceIncrease(int nHitDice)
        {
            VM.StackPush(nHitDice);
            VM.Call(553);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(sCreatureTemplate4);
            VM.StackPush(sCreatureTemplate3);
            VM.StackPush(sCreatureTemplate2);
            VM.StackPush(sCreatureTemplate1);
            VM.StackPush(nLooping);
            VM.Call(510);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nAnimation);
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.Call(480);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disappear effect to make the object "fly away" and then destroy
        ///   itself.
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectDisappear(int nAnimation = 1)
        {
            VM.StackPush(nAnimation);
            VM.Call(481);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Appear effect to make the object "fly in".
        ///   - nAnimation determines which appear and disappear animations to use. Most creatures
        ///   only have animation 1, although a few have 2 (like beholders)
        /// </summary>
        public static Effect EffectAppear(int nAnimation = 1)
        {
            VM.StackPush(nAnimation);
            VM.Call(482);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Modify Attacks effect to add attacks.
        ///   - nAttacks: maximum is 5, even with the effect stacked
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.
        /// </summary>
        public static Effect EffectModifyAttacks(int nAttacks)
        {
            VM.StackPush(nAttacks);
            VM.Call(485);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nDamageType);
            VM.StackPush((int)nRandomAmount);
            VM.StackPush(nDamageAmount);
            VM.Call(487);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nMissChanceType);
            VM.StackPush(nPercentage);
            VM.Call(477);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nSpellSchool);
            VM.StackPush(nTotalSpellLevelsAbsorbed);
            VM.StackPush(nMaxSpellLevelAbsorbed);
            VM.Call(472);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic Best effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            VM.StackPush(nCasterLevel);
            VM.Call(473);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Invisibility effect.
        ///   - nInvisibilityType: INVISIBILITY_TYPE_*
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType
        ///   is invalid.
        /// </summary>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType)
        {
            VM.StackPush((int)nInvisibilityType);
            VM.Call(457);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nMissType);
            VM.StackPush(nPercentage);
            VM.Call(458);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Darkness effect.
        /// </summary>
        public static Effect EffectDarkness()
        {
            VM.Call(459);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dispel Magic All effect.
        ///   If no parameter is specified, USE_CREATURE_LEVEL will be used. This will
        ///   cause the dispel effect to use the level of the creature that created the
        ///   effect.
        /// </summary>
        public static Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            VM.StackPush(nCasterLevel);
            VM.Call(460);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ultravision effect.
        /// </summary>
        public static Effect EffectUltravision()
        {
            VM.Call(461);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Negative Level effect.
        ///   - nNumLevels: the number of negative levels to apply.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.
        /// </summary>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false)
        {
            VM.StackPush(bHPBonus ? 1 : 0);
            VM.StackPush(nNumLevels);
            VM.Call(462);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Polymorph effect.
        /// </summary>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false)
        {
            VM.StackPush(nLocked ? 1 : 0);
            VM.StackPush(nPolymorphSelection);
            VM.Call(463);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sanctuary effect.
        ///   - nDifficultyClass: must be a non-zero, positive number
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.
        /// </summary>
        public static Effect EffectSanctuary(int nDifficultyClass)
        {
            VM.StackPush(nDifficultyClass);
            VM.Call(464);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a True Seeing effect.
        /// </summary>
        public static Effect EffectTrueSeeing()
        {
            VM.Call(465);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a See Invisible effect.
        /// </summary>
        public static Effect EffectSeeInvisible()
        {
            VM.Call(466);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Time Stop effect.
        /// </summary>
        public static Effect EffectTimeStop()
        {
            VM.Call(467);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Blindness effect.
        /// </summary>
        public static Effect EffectBlindness()
        {
            VM.Call(468);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Ability Decrease effect.
        ///   - nAbility: ABILITY_*
        ///   - nModifyBy: This is the amount by which to decrement the ability
        /// </summary>
        public static Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy)
        {
            VM.StackPush(nModifyBy);
            VM.StackPush((int)nAbility);
            VM.Call(446);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Attack Decrease effect.
        ///   - nPenalty
        ///   - nModifierType: ATTACK_BONUS_*
        /// </summary>
        public static Effect EffectAttackDecrease(int nPenalty, AttackBonus nModifierType = AttackBonus.Misc)
        {
            VM.StackPush((int)nModifierType);
            VM.StackPush(nPenalty);
            VM.Call(447);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Decrease effect.
        ///   - nPenalty
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Force)
        {
            VM.StackPush((int)nDamageType);
            VM.StackPush(nPenalty);
            VM.Call(448);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Damage Immunity Decrease effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity)
        {
            VM.StackPush(nPercentImmunity);
            VM.StackPush(nDamageType);
            VM.Call(449);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nDamageType);
            VM.StackPush((int)nModifyType);
            VM.StackPush(nValue);
            VM.Call(450);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nPercentChange);
            VM.Call(451);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nSaveType);
            VM.StackPush(nValue);
            VM.StackPush(nSave);
            VM.Call(452);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Decrease effect.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillDecrease(int nSkill, int nValue)
        {
            VM.StackPush(nValue);
            VM.StackPush(nSkill);
            VM.Call(453);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Decrease effect.
        /// </summary>
        public static Effect EffectSpellResistanceDecrease(int nValue)
        {
            VM.StackPush(nValue);
            VM.Call(454);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Activate oItem.
        /// </summary>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.StackPush((int)EngineStructure.Location, lTarget);
            VM.StackPush(oItem);
            VM.Call(424);
            return VM.StackPopStruct((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Create a Hit Point Change When Dying effect.
        ///   - fHitPointChangePerRound: this can be positive or negative, but not zero.
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.
        /// </summary>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound)
        {
            VM.StackPush(fHitPointChangePerRound);
            VM.Call(387);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Turned effect.
        ///   Turned effects are supernatural by default.
        /// </summary>
        public static Effect EffectTurned()
        {
            VM.Call(379);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nGoodEvil);
            VM.StackPush((int)nLawChaos);
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(355);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus nRacialType.
        ///   - eEffect
        ///   - nRacialType: RACIAL_TYPE_*
        /// </summary>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType)
        {
            VM.StackPush((int)nRacialType);
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(356);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Set eEffect to be versus traps.
        /// </summary>
        public static Effect VersusTrapEffect(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(357);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Skill Increase effect.
        ///   - nSkill: SKILL_*
        ///   - nValue
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.
        /// </summary>
        public static Effect EffectSkillIncrease(NWNSkillType nSkill, int nValue)
        {
            VM.StackPush(nValue);
            VM.StackPush((int)nSkill);
            VM.Call(351);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Temporary Hitpoints effect.
        ///   - nHitPoints: a positive integer
        ///   * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.
        /// </summary>
        public static Effect EffectTemporaryHitpoints(int nHitPoints)
        {
            VM.StackPush(nHitPoints);
            VM.Call(314);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.Call(295);
            return VM.StackPopStruct((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Creates a Damage Immunity Increase effect.
        ///   - nDamageType: DAMAGE_TYPE_*
        ///   - nPercentImmunity
        /// </summary>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity)
        {
            VM.StackPush(nPercentImmunity);
            VM.StackPush((int)nDamageType);
            VM.Call(275);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create an Immunity effect.
        ///   - nImmunityType: IMMUNITY_TYPE_*
        /// </summary>
        public static Effect EffectImmunity(ImmunityType nImmunityType)
        {
            VM.StackPush((int)nImmunityType);
            VM.Call(273);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Haste effect.
        /// </summary>
        public static Effect EffectHaste()
        {
            VM.Call(270);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Slow effect.
        /// </summary>
        public static Effect EffectSlow()
        {
            VM.Call(271);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Poison effect.
        ///   - nPoisonType: POISON_*
        /// </summary>
        public static Effect EffectPoison(Poison nPoisonType)
        {
            VM.StackPush((int)nPoisonType);
            VM.Call(250);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Disease effect.
        ///   - nDiseaseType: DISEASE_*
        /// </summary>
        public static Effect EffectDisease(Disease nDiseaseType)
        {
            VM.StackPush((int)nDiseaseType);
            VM.Call(251);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Silence effect.
        /// </summary>
        public static Effect EffectSilence()
        {
            VM.Call(252);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Spell Resistance Increase effect.
        ///   - nValue: size of spell resistance increase
        /// </summary>
        public static Effect EffectSpellResistanceIncrease(int nValue)
        {
            VM.StackPush(nValue);
            VM.Call(212);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(bMissEffect ? 1 : 0);
            VM.StackPush((int)nBodyPart);
            VM.StackPush(oEffector);
            VM.StackPush((int)nBeamVisualEffect);
            VM.Call(207);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)EngineStructure.Effect, eParentEffect);
            VM.StackPush((int)EngineStructure.Effect, eChildEffect);
            VM.Call(199);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   * Create a Visual Effect that can be applied to an object.
        ///   - nVisualEffectId
        ///   - nMissEffect: if this is TRUE, a random vector near or past the target will
        ///   be generated, on which to play the effect
        /// </summary>
        public static Effect EffectVisualEffect(VisualEffect visualEffectID, bool nMissEffect = false, float fScale = 1.0f, Vector3 vTranslate = new Vector3(), Vector3 vRotate = new Vector3())
        {
            VM.StackPush(vRotate);
            VM.StackPush(vTranslate);
            VM.StackPush(fScale);
            VM.StackPush(nMissEffect ? 1 : 0);
            VM.StackPush((int)visualEffectID);

            VM.Call(180);

            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Apply eEffect to oTarget.
        /// </summary>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f)
        {
            VM.StackPush(fDuration);
            VM.StackPush(oTarget);
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.StackPush((int)nDurationType);
            VM.Call(220);
        }

        /// <summary>
        ///   Get the effect type (EFFECT_TYPE_*) of eEffect.
        ///   * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT
        /// </summary>
        public static EffectTypeScript GetEffectType(Effect eEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.Call(170);
            return (EffectTypeScript)VM.StackPopInt();
        }

        /// <summary>
        ///   Create an Area Of Effect effect in the area of the creature it is applied to.
        ///   If the scripts are not specified, default ones will be used.
        /// </summary>
        public static Effect EffectAreaOfEffect(AreaOfEffect nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "")
        {
            VM.StackPush(sOnExitScript);
            VM.StackPush(sHeartbeatScript);
            VM.StackPush(sOnEnterScript);
            VM.StackPush((int)nAreaEffect);
            VM.Call(171);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Regenerate effect.
        ///   - nAmount: amount of damage to be regenerated per time interval
        ///   - fIntervalSeconds: length of interval in seconds
        /// </summary>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds)
        {
            VM.StackPush(fIntervalSeconds);
            VM.StackPush(nAmount);
            VM.Call(164);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush(nPercentChange);
            VM.Call(165);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Charm effect
        /// </summary>
        public static Effect EffectCharmed()
        {
            VM.Call(156);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Confuse effect
        /// </summary>
        public static Effect EffectConfused()
        {
            VM.Call(157);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Frighten effect
        /// </summary>
        public static Effect EffectFrightened()
        {
            VM.Call(158);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Dominate effect
        /// </summary>
        public static Effect EffectDominated()
        {
            VM.Call(159);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Daze effect
        /// </summary>
        public static Effect EffectDazed()
        {
            VM.Call(160);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Stun effect
        /// </summary>
        public static Effect EffectStunned()
        {
            VM.Call(161);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Sleep effect
        /// </summary>
        public static Effect EffectSleep()
        {
            VM.Call(154);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Paralyze effect
        /// </summary>
        public static Effect EffectParalyze()
        {
            VM.Call(148);
            return VM.StackPopStruct((int)EngineStructure.Effect);
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
            VM.StackPush((int)nImmunityToSpell);
            VM.Call(149);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        ///   Create a Deaf effect
        /// </summary>
        public static Effect EffectDeaf()
        {
            VM.Call(150);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }


        /// <summary>
        ///  Get the integer parameter of eEffect at nIndex.
        ///  nIndex bounds: 0 to 7 inclusive
        ///  Some experimentation will be needed to find the right index for the value you wish to determine.
        ///  Returns: the value or 0 on error/when not set.
        /// </summary>
        public static int GetEffectInteger(Effect eEffect, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int) EngineStructure.Effect, eEffect.Handle);
            VM.Call(939);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Get the float parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or 0.0f on error/when not set.
        /// </summary>
        public static float GetEffectFloat(Effect eEffect, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Effect, eEffect.Handle);
            VM.Call(940);
            return VM.StackPopFloat();
        }

        /// <summary>
        /// Get the string parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 5 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or "" on error/when not set.
        /// </summary>
        public static string GetEffectString(Effect eEffect, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Effect, eEffect.Handle);
            VM.Call(941);
            return VM.StackPopString();
        }
        
        /// <summary>
        /// Get the object parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 3 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or OBJECT_INVALID on error/when not set.
        /// </summary>
        public static uint GetEffectObject(Effect eEffect, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Effect, eEffect.Handle);
            VM.Call(942);
            return VM.StackPopObject();
        }

        /// <summary>
        /// Get the vector parameter of eEffect at nIndex.
        /// * nIndex bounds: 0 to 1 inclusive
        /// * Some experimentation will be needed to find the right index for the value you wish to determine.
        /// Returns: the value or {0.0f, 0.0f, 0.0f} on error/when not set.
        /// </summary>
        public static Vector3 GetEffectVector(Effect eEffect, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Effect, eEffect.Handle);
            VM.Call(943);
            return VM.StackPopVector();
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
            VM.StackPush(sData);
            VM.StackPush(fInterval);
            VM.StackPush(sOnIntervalScript);
            VM.StackPush(sOnRemovedScript);
            VM.StackPush(sOnAppliedScript);
            VM.Call(955);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        /// Get the effect that last triggered an EffectRunScript() script.
        /// Note: This can be used to get the creator or tag, among others, of the EffectRunScript() in one of its scripts.
        /// </summary>
        /// <returns>The effect that last triggered an EffectRunScript() script.</returns>
        public static Effect GetLastRunScriptEffect()
        {
            VM.Call(956);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        /// Get the script type (RUNSCRIPT_EFFECT_SCRIPT_TYPE_*) of the last triggered EffectRunScript() script.
        /// * Returns 0 when called outside of an EffectRunScript() script.
        /// </summary>
        /// <returns></returns>
        public static int GetLastRunScriptEffectScriptType()
        {
            VM.Call(957);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Hides the effect icon of eEffect and of all effects currently linked to it.
        /// </summary>
        /// <param name="eEffect"></param>
        /// <returns></returns>
        public static Effect HideEffectIcon(Effect eEffect)
        {
            VM.Call(958);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }

        /// <summary>
        /// Create an Icon effect.
        /// * nIconID: The effect icon (EFFECT_ICON_*) to display.
        ///            Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases.
        /// Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255.
        /// </summary>
        public static Effect EffectIcon(EffectIconType nIconId)
        {
            VM.StackPush((int)nIconId);
            VM.Call(959);
            return VM.StackPopStruct((int)EngineStructure.Effect);
        }
    }
}