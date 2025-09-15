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
        /// Returns the string tag set for the provided effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the tag for</param>
        /// <returns>The string tag. Returns an empty string if no tag has been set</returns>
        public static string GetEffectTag(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectTag(eEffect);
        }

        /// <summary>
        /// Tags the effect with the provided string.
        /// </summary>
        /// <param name="eEffect">The effect to tag</param>
        /// <param name="sNewTag">The new tag to set</param>
        /// <returns>The tagged effect</returns>
        /// <remarks>Any other tags in the link will be overwritten.</remarks>
        public static Effect TagEffect(Effect eEffect, string sNewTag)
        {
            return global::NWN.Core.NWScript.TagEffect(eEffect, sNewTag);
        }

        /// <summary>
        /// Returns the caster level of the creature who created the effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the caster level for</param>
        /// <returns>The caster level. Returns 0 if not created by a creature or if created by a spell-like ability</returns>
        public static int GetEffectCasterLevel(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectCasterLevel(eEffect);
        }

        /// <summary>
        /// Returns the total duration of the effect in seconds.
        /// </summary>
        /// <param name="eEffect">The effect to get the duration for</param>
        /// <returns>The total duration in seconds. Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY</returns>
        public static int GetEffectDuration(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDuration(eEffect);
        }

        /// <summary>
        /// Returns the remaining duration of the effect in seconds.
        /// </summary>
        /// <param name="eEffect">The effect to get the remaining duration for</param>
        /// <returns>The remaining duration in seconds. Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY</returns>
        public static int GetEffectDurationRemaining(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDurationRemaining(eEffect);
        }

        /// <summary>
        /// Returns an effect that when applied will paralyze the target's legs, rendering them unable to walk but otherwise unpenalized.
        /// </summary>
        /// <returns>The cutscene immobilize effect</returns>
        /// <remarks>This effect cannot be resisted.</remarks>
        public static Effect EffectCutsceneImmobilize()
        {
            return global::NWN.Core.NWScript.EffectCutsceneImmobilize();
        }

        /// <summary>
        /// Creates a cutscene ghost effect.
        /// </summary>
        /// <returns>The cutscene ghost effect</returns>
        /// <remarks>This will allow creatures to pathfind through other creatures without bumping into them for the duration of the effect.</remarks>
        public static Effect EffectCutsceneGhost()
        {
            return global::NWN.Core.NWScript.EffectCutsceneGhost();
        }

        /// <summary>
        /// Returns true if the item is cursed and cannot be dropped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item is cursed, false otherwise</returns>
        public static bool GetItemCursedFlag(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemCursedFlag(oItem) != 0;
        }

        /// <summary>
        /// Sets the cursed flag on the specified item.
        /// </summary>
        /// <param name="oItem">The item to set the cursed flag for</param>
        /// <param name="nCursed">Whether the item is cursed</param>
        /// <remarks>When cursed, items cannot be dropped.</remarks>
        public static void SetItemCursedFlag(uint oItem, bool nCursed)
        {
            global::NWN.Core.NWScript.SetItemCursedFlag(oItem, nCursed ? 1 : 0);
        }

        /// <summary>
        /// Gets the possessor of the specified item.
        /// </summary>
        /// <param name="oItem">The item to get the possessor for</param>
        /// <returns>The possessor of the item. Returns OBJECT_INVALID on error</returns>
        public static uint GetItemPossessor(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemPossessor(oItem);
        }

        /// <summary>
        /// Gets the object possessed by the creature with the specified tag.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="sItemTag">The item tag to search for</param>
        /// <returns>The possessed object. Returns OBJECT_INVALID on error</returns>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag)
        {
            return global::NWN.Core.NWScript.GetItemPossessedBy(oCreature, sItemTag);
        }

        /// <summary>
        /// Creates an item with the specified template in the target's inventory.
        /// </summary>
        /// <param name="sResRef">The item template to create</param>
        /// <param name="oTarget">The target to create the item for (default: OBJECT_SELF)</param>
        /// <param name="nStackSize">The stack size of the item to be created (default: 1)</param>
        /// <param name="sNewTag">If this string is not empty, it will replace the default tag from the template (default: empty string)</param>
        /// <returns>The object that has been created. Returns OBJECT_INVALID on error. If the item created was merged into an existing stack of similar items, the function will return the merged stack object. If the merged stack overflowed, the function will return the overflowed stack that was created</returns>
        public static uint CreateItemOnObject(string sResRef, uint oTarget = OBJECT_INVALID, int nStackSize = 1,
            string sNewTag = "")
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.CreateItemOnObject(sResRef, oTarget, nStackSize, sNewTag);
        }

        /// <summary>
        /// Equips the specified item into the specified inventory slot.
        /// </summary>
        /// <param name="oItem">The item to equip</param>
        /// <param name="nInventorySlot">The inventory slot to equip the item to (INVENTORY_SLOT_* constants)</param>
        /// <remarks>If an error occurs, the log file will contain "ActionEquipItem failed." If the creature already has an item equipped in the slot specified, it will be unequipped automatically by the call to ActionEquipItem. In order for ActionEquipItem to succeed the creature must be able to equip the item normally. This means that: 1) The item is in the creature's inventory, 2) The item must already be identified (if magical), 3) The creature has the level required to equip the item (if magical and ILR is on), 4) The creature possesses the required feats to equip the item (such as weapon proficiencies).</remarks>
        public static void ActionEquipItem(uint oItem, InventorySlot nInventorySlot)
        {
            global::NWN.Core.NWScript.ActionEquipItem(oItem, (int)nInventorySlot);
        }

        /// <summary>
        /// Unequips the specified item from whatever slot it is currently in.
        /// </summary>
        /// <param name="oItem">The item to unequip</param>
        public static void ActionUnequipItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionUnequipItem(oItem);
        }

        /// <summary>
        /// Picks up the specified item from the ground.
        /// </summary>
        /// <param name="oItem">The item to pick up</param>
        /// <remarks>If an error occurs, the log file will contain "ActionPickUpItem failed."</remarks>
        public static void ActionPickUpItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionPickUpItem(oItem);
        }

        /// <summary>
        /// Puts down the specified item on the ground.
        /// </summary>
        /// <param name="oItem">The item to put down</param>
        /// <remarks>If an error occurs, the log file will contain "ActionPutDownItem failed."</remarks>
        public static void ActionPutDownItem(uint oItem)
        {
            global::NWN.Core.NWScript.ActionPutDownItem(oItem);
        }

        /// <summary>
        /// Gives the specified item to the specified target.
        /// </summary>
        /// <param name="oItem">The item to give</param>
        /// <param name="oGiveTo">The target to give the item to</param>
        /// <remarks>If oItem is not a valid item, or oGiveTo is not a valid object, nothing will happen.</remarks>
        public static void ActionGiveItem(uint oItem, uint oGiveTo)
        {
            global::NWN.Core.NWScript.ActionGiveItem(oItem, oGiveTo);
        }

        /// <summary>
        /// Takes the specified item from the specified source.
        /// </summary>
        /// <param name="oItem">The item to take</param>
        /// <param name="oTakeFrom">The source to take the item from</param>
        /// <remarks>If oItem is not a valid item, or oTakeFrom is not a valid object, nothing will happen.</remarks>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom)
        {
            global::NWN.Core.NWScript.ActionTakeItem(oItem, oTakeFrom);
        }

        /// <summary>
        /// Creates a Death effect.
        /// </summary>
        /// <param name="nSpectacularDeath">If true, the creature to which this effect is applied will die in an extraordinary fashion (default: false)</param>
        /// <param name="nDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <returns>The Death effect</returns>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true)
        {
            return global::NWN.Core.NWScript.EffectDeath(nSpectacularDeath ? 1 : 0, nDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        /// Creates a Knockdown effect.
        /// </summary>
        /// <returns>The Knockdown effect</returns>
        /// <remarks>This effect knocks creatures off their feet, they will sit until the effect is removed. This should be applied as a temporary effect with a 3 second duration minimum (1 second to fall, 1 second sitting, 1 second to get up).</remarks>
        public static Effect EffectKnockdown()
        {
            return global::NWN.Core.NWScript.EffectKnockdown();
        }

        /// <summary>
        /// Creates a Curse effect.
        /// </summary>
        /// <param name="nStrMod">Strength modifier (default: 1)</param>
        /// <param name="nDexMod">Dexterity modifier (default: 1)</param>
        /// <param name="nConMod">Constitution modifier (default: 1)</param>
        /// <param name="nIntMod">Intelligence modifier (default: 1)</param>
        /// <param name="nWisMod">Wisdom modifier (default: 1)</param>
        /// <param name="nChaMod">Charisma modifier (default: 1)</param>
        /// <returns>The Curse effect</returns>
        public static Effect EffectCurse(int nStrMod = 1, int nDexMod = 1, int nConMod = 1, int nIntMod = 1,
            int nWisMod = 1, int nChaMod = 1)
        {
            return global::NWN.Core.NWScript.EffectCurse(nStrMod, nDexMod, nConMod, nIntMod, nWisMod, nChaMod);
        }

        /// <summary>
        /// Creates an Entangle effect.
        /// </summary>
        /// <returns>The Entangle effect</returns>
        /// <remarks>When applied, this effect will restrict the creature's movement and apply a (-2) to all attacks and a -4 to AC.</remarks>
        public static Effect EffectEntangle()
        {
            return global::NWN.Core.NWScript.EffectEntangle();
        }

        /// <summary>
        /// Creates a Saving Throw Increase effect.
        /// </summary>
        /// <param name="nSave">The saving throw type (SAVING_THROW_* constants, not SAVING_THROW_TYPE_*)</param>
        /// <param name="nValue">The size of the saving throw increase</param>
        /// <param name="nSaveType">The saving throw type (SAVING_THROW_TYPE_* constants, e.g., SAVING_THROW_TYPE_ACID) (default: SavingThrowType.All)</param>
        /// <returns>The Saving Throw Increase effect</returns>
        /// <remarks>Possible save types: SAVING_THROW_ALL, SAVING_THROW_FORT, SAVING_THROW_REFLEX, SAVING_THROW_WILL</remarks>
        public static Effect EffectSavingThrowIncrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All)
        {
            return global::NWN.Core.NWScript.EffectSavingThrowIncrease(nSave, nValue, (int)nSaveType);
        }

        /// <summary>
        /// Creates an Attack Increase effect.
        /// </summary>
        /// <param name="nBonus">The size of attack bonus</param>
        /// <param name="nModifierType">The attack bonus type (ATTACK_BONUS_* constants) (default: AttackBonus.Misc)</param>
        /// <returns>The Attack Increase effect</returns>
        /// <remarks>On SWLOR, this is used for Accuracy.</remarks>
        public static Effect EffectAccuracyIncrease(int nBonus, AttackBonus nModifierType = AttackBonus.Misc)
        {
            return global::NWN.Core.NWScript.EffectAttackIncrease(nBonus, (int)nModifierType);
        }

        /// <summary>
        /// Creates a Damage Reduction effect.
        /// </summary>
        /// <param name="nAmount">The amount of damage reduction</param>
        /// <param name="nDamagePower">The damage power type (DAMAGE_POWER_* constants)</param>
        /// <param name="nLimit">How much damage the effect can absorb before disappearing. Set to zero for infinite (default: 0)</param>
        /// <returns>The Damage Reduction effect</returns>
        public static Effect EffectDamageReduction(int nAmount, DamagePower nDamagePower, int nLimit = 0)
        {
            return global::NWN.Core.NWScript.EffectDamageReduction(nAmount, (int)nDamagePower, nLimit);
        }

        /// <summary>
        /// Creates a Damage Increase effect.
        /// </summary>
        /// <param name="nBonus">The damage bonus (DAMAGE_BONUS_* constants)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <returns>The Damage Increase effect</returns>
        /// <remarks>You must use the DAMAGE_BONUS_* constants! Using other values may result in odd behavior.</remarks>
        public static Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = DamageType.Force)
        {
            return global::NWN.Core.NWScript.EffectDamageIncrease(nBonus, (int)nDamageType);
        }

        /// <summary>
        /// Sets the subtype of the effect to Magical and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as magical</param>
        /// <returns>The magical effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Magical effects are removed by resting, and by dispel magic.</remarks>
        public static Effect MagicalEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.MagicalEffect(eEffect);
        }

        /// <summary>
        /// Sets the subtype of the effect to Supernatural and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as supernatural</param>
        /// <returns>The supernatural effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Permanent supernatural effects are not removed by resting.</remarks>
        public static Effect SupernaturalEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.SupernaturalEffect(eEffect);
        }

        /// <summary>
        /// Sets the subtype of the effect to Extraordinary and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as extraordinary</param>
        /// <returns>The extraordinary effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Extraordinary effects are removed by resting, but not by dispel magic.</remarks>
        public static Effect ExtraordinaryEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.ExtraordinaryEffect(eEffect);
        }

        /// <summary>
        /// Creates an AC Increase effect.
        /// </summary>
        /// <param name="nValue">The size of AC increase</param>
        /// <param name="nModifyType">The armor class modifier type (AC_*_BONUS constants) (default: ArmorClassModiferType.Dodge)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: AC.VsDamageTypeAll)</param>
        /// <returns>The AC Increase effect</returns>
        /// <remarks>Default value for nDamageType should only ever be used in this function prototype.</remarks>
        public static Effect EffectACIncrease(int nValue,
            ArmorClassModiferType nModifyType = ArmorClassModiferType.Dodge,
            AC nDamageType = AC.VsDamageTypeAll)
        {
            return global::NWN.Core.NWScript.EffectACIncrease(nValue, (int)nModifyType, (int)nDamageType);
        }

        /// <summary>
        /// Gets the first in-game effect on the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the first effect for</param>
        /// <returns>The first effect on the creature</returns>
        public static Effect GetFirstEffect(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFirstEffect(oCreature);
        }

        /// <summary>
        /// Gets the next in-game effect on the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the next effect for</param>
        /// <returns>The next effect on the creature</returns>
        public static Effect GetNextEffect(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetNextEffect(oCreature);
        }

        /// <summary>
        /// Removes the specified effect from the creature.
        /// </summary>
        /// <param name="oCreature">The creature to remove the effect from</param>
        /// <param name="eEffect">The effect to remove</param>
        public static void RemoveEffect(uint oCreature, Effect eEffect)
        {
            global::NWN.Core.NWScript.RemoveEffect(oCreature, eEffect);
        }

        /// <summary>
        /// Returns true if the effect is a valid effect.
        /// </summary>
        /// <param name="eEffect">The effect to check</param>
        /// <returns>True if the effect is valid, false otherwise</returns>
        /// <remarks>The effect must have been applied to an object or else it will return false.</remarks>
        public static bool GetIsEffectValid(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetIsEffectValid(eEffect) == 1;
        }

        /// <summary>
        /// Gets the duration type of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the duration type for</param>
        /// <returns>The duration type (DURATION_TYPE_* constants). Returns -1 if the effect is not valid</returns>
        public static int GetEffectDurationType(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectDurationType(eEffect);
        }

        /// <summary>
        /// Gets the subtype of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the subtype for</param>
        /// <returns>The subtype (SUBTYPE_* constants). Returns 0 on error</returns>
        public static int GetEffectSubType(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectSubType(eEffect);
        }

        /// <summary>
        /// Gets the object that created the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the creator for</param>
        /// <returns>The object that created the effect. Returns OBJECT_INVALID if the effect is not valid</returns>
        public static uint GetEffectCreator(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectCreator(eEffect);
        }

        /// <summary>
        /// Creates a Heal effect.
        /// </summary>
        /// <param name="nDamageToHeal">The amount of damage to heal</param>
        /// <returns>The Heal effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectHeal(int nDamageToHeal)
        {
            return global::NWN.Core.NWScript.EffectHeal(nDamageToHeal);
        }

        /// <summary>
        /// Creates a Damage effect.
        /// </summary>
        /// <param name="nDamageAmount">The amount of damage to be dealt</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <param name="nDamagePower">The damage power (DAMAGE_POWER_* constants) (default: DamagePower.Normal)</param>
        /// <returns>The Damage effect</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectDamage(int nDamageAmount, DamageType nDamageType = DamageType.Force,
            DamagePower nDamagePower = DamagePower.Normal)
        {
            return global::NWN.Core.NWScript.EffectDamage(nDamageAmount, (int)nDamageType, (int)nDamagePower);
        }

        /// <summary>
        /// Creates an Ability Increase effect.
        /// </summary>
        /// <param name="nAbilityToIncrease">The ability to increase (ABILITY_* constants)</param>
        /// <param name="nModifyBy">The amount to modify the ability by</param>
        /// <returns>The Ability Increase effect</returns>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy)
        {
            return global::NWN.Core.NWScript.EffectAbilityIncrease((int)nAbilityToIncrease, nModifyBy);
        }

        /// <summary>
        /// Creates a Damage Resistance effect that removes the first nAmount points of damage of the specified type.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nAmount">The amount of damage resistance</param>
        /// <param name="nLimit">The limit of damage resistance (infinite if 0) (default: 0)</param>
        /// <returns>The Damage Resistance effect</returns>
        public static Effect EffectDamageResistance(DamageType nDamageType, int nAmount, int nLimit = 0)
        {
            return global::NWN.Core.NWScript.EffectDamageResistance((int)nDamageType, nAmount, nLimit);
        }

        /// <summary>
        /// Creates a Resurrection effect.
        /// </summary>
        /// <returns>The Resurrection effect</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectResurrection()
        {
            return global::NWN.Core.NWScript.EffectResurrection();
        }

        /// <summary>
        /// Creates a Summon Creature effect.
        /// </summary>
        /// <param name="sCreatureResref">The creature resource reference to summon</param>
        /// <param name="nVisualEffectId">The visual effect ID (VFX_* constants) (default: VisualEffect.Vfx_Com_Sparks_Parry)</param>
        /// <param name="fDelaySeconds">Delay between the visual effect being played and the creature being added to the area (default: 0.0f)</param>
        /// <param name="nUseAppearAnimation">Whether the creature should play its "appear" animation when summoned (default: false)</param>
        /// <param name="nUnsummonVisualEffectId">The visual effect ID for unsummoning (default: VisualEffect.Vfx_Imp_Unsummon)</param>
        /// <param name="oSummonToAdd">The object to add the summoned creature to (default: OBJECT_INVALID)</param>
        /// <returns>The Summon Creature effect</returns>
        /// <remarks>The creature is created and placed into the caller's party/faction. If nUseAppearAnimation is zero, it will just fade in somewhere near the target. If the value is 1 it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures).</remarks>
        public static Effect EffectSummonCreature(string sCreatureResref, VisualEffect nVisualEffectId = VisualEffect.Vfx_Com_Sparks_Parry,
            float fDelaySeconds = 0.0f, bool nUseAppearAnimation = false, VisualEffect nUnsummonVisualEffectId = VisualEffect.Vfx_Imp_Unsummon, uint oSummonToAdd = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.EffectSummonCreature(sCreatureResref, (int)nVisualEffectId, fDelaySeconds, nUseAppearAnimation ? 1 : 0, (int)nUnsummonVisualEffectId, oSummonToAdd);
        }

        /// <summary>
        /// Returns an effect of type EFFECT_TYPE_ETHEREAL.
        /// </summary>
        /// <returns>The Ethereal effect</returns>
        /// <remarks>Works just like EffectSanctuary except that the observers get no saving throw.</remarks>
        public static Effect EffectEthereal()
        {
            return global::NWN.Core.NWScript.EffectEthereal();
        }

        /// <summary>
        /// Creates an effect that inhibits spells.
        /// </summary>
        /// <param name="nPercent">The percentage of failure (default: 100)</param>
        /// <param name="nSpellSchool">The school of spells affected (default: SpellSchool.General)</param>
        /// <returns>The Spell Failure effect</returns>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            return global::NWN.Core.NWScript.EffectSpellFailure(nPercent, (int)nSpellSchool);
        }

        /// <summary>
        /// Returns an effect that is guaranteed to dominate a creature.
        /// </summary>
        /// <returns>The Cutscene Dominated effect</returns>
        /// <remarks>Like EffectDominated but cannot be resisted.</remarks>
        public static Effect EffectCutsceneDominated()
        {
            return global::NWN.Core.NWScript.EffectCutsceneDominated();
        }

        /// <summary>
        /// Returns an effect that will petrify the target.
        /// </summary>
        /// <returns>The Petrify effect</returns>
        /// <remarks>Currently applies EffectParalyze and the stoneskin visual effect.</remarks>
        public static Effect EffectPetrify()
        {
            return global::NWN.Core.NWScript.EffectPetrify();
        }

        /// <summary>
        /// Returns an effect that is guaranteed to paralyze a creature.
        /// </summary>
        /// <returns>The Cutscene Paralyze effect</returns>
        /// <remarks>This effect is identical to EffectParalyze except that it cannot be resisted.</remarks>
        public static Effect EffectCutsceneParalyze()
        {
            return global::NWN.Core.NWScript.EffectCutsceneParalyze();
        }

        /// <summary>
        /// Creates a Turn Resistance Decrease effect.
        /// </summary>
        /// <param name="nHitDice">A positive number representing the number of hit dice for the decrease</param>
        /// <returns>The Turn Resistance Decrease effect</returns>
        public static Effect EffectTurnResistanceDecrease(int nHitDice)
        {
            return global::NWN.Core.NWScript.EffectTurnResistanceDecrease(nHitDice);
        }

        /// <summary>
        /// Creates a Turn Resistance Increase effect.
        /// </summary>
        /// <param name="nHitDice">A positive number representing the number of hit dice for the increase</param>
        /// <returns>The Turn Resistance Increase effect</returns>
        public static Effect EffectTurnResistanceIncrease(int nHitDice)
        {
            return global::NWN.Core.NWScript.EffectTurnResistanceIncrease(nHitDice);
        }

        /// <summary>
        /// Creates a Swarm effect.
        /// </summary>
        /// <param name="nLooping">If true, for the duration of the effect when one creature created by this effect dies, the next one in the list will be created</param>
        /// <param name="sCreatureTemplate1">The first creature template</param>
        /// <param name="sCreatureTemplate2">The second creature template (default: empty string)</param>
        /// <param name="sCreatureTemplate3">The third creature template (default: empty string)</param>
        /// <param name="sCreatureTemplate4">The fourth creature template (default: empty string)</param>
        /// <returns>The Swarm effect</returns>
        /// <remarks>If the last creature in the list dies, we loop back to the beginning and sCreatureTemplate1 will be created, and so on...</remarks>
        public static Effect EffectSwarm(int nLooping, string sCreatureTemplate1, string sCreatureTemplate2 = "",
            string sCreatureTemplate3 = "", string sCreatureTemplate4 = "")
        {
            return global::NWN.Core.NWScript.EffectSwarm(nLooping, sCreatureTemplate1, sCreatureTemplate2, sCreatureTemplate3, sCreatureTemplate4);
        }

        /// <summary>
        /// Creates a Disappear/Appear effect.
        /// </summary>
        /// <param name="lLocation">The location where the object will reappear</param>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Disappear/Appear effect</returns>
        /// <remarks>The object will "fly away" for the duration of the effect and will reappear at the specified location. Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectDisappearAppear(Location lLocation, int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectDisappearAppear(lLocation, nAnimation);
        }

        /// <summary>
        /// Creates a Disappear effect to make the object "fly away" and then destroy itself.
        /// </summary>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Disappear effect</returns>
        /// <remarks>Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectDisappear(int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectDisappear(nAnimation);
        }

        /// <summary>
        /// Creates an Appear effect to make the object "fly in".
        /// </summary>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Appear effect</returns>
        /// <remarks>Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectAppear(int nAnimation = 1)
        {
            return global::NWN.Core.NWScript.EffectAppear(nAnimation);
        }

        /// <summary>
        /// Creates a Modify Attacks effect to add attacks.
        /// </summary>
        /// <param name="nAttacks">The number of attacks to add (maximum is 5, even with the effect stacked)</param>
        /// <returns>The Modify Attacks effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5</returns>
        public static Effect EffectModifyAttacks(int nAttacks)
        {
            return global::NWN.Core.NWScript.EffectModifyAttacks(nAttacks);
        }

        /// <summary>
        /// Creates a Damage Shield effect which does (nDamageAmount + nRandomAmount) damage to any melee attacker on a successful attack.
        /// </summary>
        /// <param name="nDamageAmount">An integer value for the base damage</param>
        /// <param name="nRandomAmount">The random damage bonus (DAMAGE_BONUS_* constants)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <returns>The Damage Shield effect</returns>
        /// <remarks>You must use the DAMAGE_BONUS_* constants! Using other values may result in odd behavior.</remarks>
        public static Effect EffectDamageShield(int nDamageAmount, DamageBonus nRandomAmount, DamageType nDamageType)
        {
            return global::NWN.Core.NWScript.EffectDamageShield(nDamageAmount, (int)nRandomAmount, (int)nDamageType);
        }

        /// <summary>
        /// Creates a Miss Chance effect.
        /// </summary>
        /// <param name="nPercentage">The miss chance percentage (1-100 inclusive)</param>
        /// <param name="nMissChanceType">The miss chance type (MISS_CHANCE_TYPE_* constants) (default: MissChanceType.Normal)</param>
        /// <returns>The Miss Chance effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100</returns>
        public static Effect EffectMissChance(int nPercentage, MissChanceType nMissChanceType = MissChanceType.Normal)
        {
            return global::NWN.Core.NWScript.EffectMissChance(nPercentage, (int)nMissChanceType);
        }

        /// <summary>
        /// Creates a Spell Level Absorption effect.
        /// </summary>
        /// <param name="nMaxSpellLevelAbsorbed">The maximum spell level that will be absorbed by the effect</param>
        /// <param name="nTotalSpellLevelsAbsorbed">The maximum number of spell levels that will be absorbed by the effect (default: 0)</param>
        /// <param name="nSpellSchool">The spell school (SPELL_SCHOOL_* constants) (default: SpellSchool.General)</param>
        /// <returns>The Spell Level Absorption effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool is invalid</returns>
        public static Effect EffectSpellLevelAbsorption(int nMaxSpellLevelAbsorbed, int nTotalSpellLevelsAbsorbed = 0,
            SpellSchool nSpellSchool = SpellSchool.General)
        {
            return global::NWN.Core.NWScript.EffectSpellLevelAbsorption(nMaxSpellLevelAbsorbed, nTotalSpellLevelsAbsorbed, (int)nSpellSchool);
        }

        /// <summary>
        /// Creates a Dispel Magic Best effect.
        /// </summary>
        /// <param name="nCasterLevel">The caster level for the dispel effect (default: USE_CREATURE_LEVEL)</param>
        /// <returns>The Dispel Magic Best effect</returns>
        /// <remarks>If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.</remarks>
        public static Effect EffectDispelMagicBest(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            return global::NWN.Core.NWScript.EffectDispelMagicBest(nCasterLevel);
        }

        /// <summary>
        /// Creates an Invisibility effect.
        /// </summary>
        /// <param name="nInvisibilityType">The invisibility type (INVISIBILITY_TYPE_* constants)</param>
        /// <returns>The Invisibility effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType is invalid</returns>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType)
        {
            return global::NWN.Core.NWScript.EffectInvisibility((int)nInvisibilityType);
        }

        /// <summary>
        /// Creates a Concealment effect.
        /// </summary>
        /// <param name="nPercentage">The concealment percentage (1-100 inclusive)</param>
        /// <param name="nMissType">The miss chance type (MISS_CHANCE_TYPE_* constants) (default: MissChanceType.Normal)</param>
        /// <returns>The Concealment effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100</returns>
        public static Effect EffectConcealment(int nPercentage, MissChanceType nMissType = MissChanceType.Normal)
        {
            return global::NWN.Core.NWScript.EffectConcealment(nPercentage, (int)nMissType);
        }

        /// <summary>
        /// Creates a Darkness effect.
        /// </summary>
        /// <returns>The Darkness effect</returns>
        public static Effect EffectDarkness()
        {
            return global::NWN.Core.NWScript.EffectDarkness();
        }

        /// <summary>
        /// Creates a Dispel Magic All effect.
        /// </summary>
        /// <param name="nCasterLevel">The caster level for the dispel effect (default: USE_CREATURE_LEVEL)</param>
        /// <returns>The Dispel Magic All effect</returns>
        /// <remarks>If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.</remarks>
        public static Effect EffectDispelMagicAll(int nCasterLevel = USE_CREATURE_LEVEL)
        {
            return global::NWN.Core.NWScript.EffectDispelMagicAll(nCasterLevel);
        }

        /// <summary>
        /// Creates an Ultravision effect.
        /// </summary>
        /// <returns>The Ultravision effect</returns>
        public static Effect EffectUltravision()
        {
            return global::NWN.Core.NWScript.EffectUltravision();
        }

        /// <summary>
        /// Creates a Negative Level effect.
        /// </summary>
        /// <param name="nNumLevels">The number of negative levels to apply</param>
        /// <param name="bHPBonus">Whether to apply HP bonus (default: false)</param>
        /// <returns>The Negative Level effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100</returns>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false)
        {
            return global::NWN.Core.NWScript.EffectNegativeLevel(nNumLevels, bHPBonus ? 1 : 0);
        }

        /// <summary>
        /// Creates a Polymorph effect.
        /// </summary>
        /// <param name="nPolymorphSelection">The polymorph selection</param>
        /// <param name="nLocked">Whether the polymorph is locked (default: false)</param>
        /// <returns>The Polymorph effect</returns>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false)
        {
            return global::NWN.Core.NWScript.EffectPolymorph(nPolymorphSelection, nLocked ? 1 : 0);
        }

        /// <summary>
        /// Creates a Sanctuary effect.
        /// </summary>
        /// <param name="nDifficultyClass">The difficulty class (must be a non-zero, positive number)</param>
        /// <returns>The Sanctuary effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0</returns>
        public static Effect EffectSanctuary(int nDifficultyClass)
        {
            return global::NWN.Core.NWScript.EffectSanctuary(nDifficultyClass);
        }

        /// <summary>
        /// Creates a True Seeing effect.
        /// </summary>
        /// <returns>The True Seeing effect</returns>
        public static Effect EffectTrueSeeing()
        {
            return global::NWN.Core.NWScript.EffectTrueSeeing();
        }

        /// <summary>
        /// Creates a See Invisible effect.
        /// </summary>
        /// <returns>The See Invisible effect</returns>
        public static Effect EffectSeeInvisible()
        {
            return global::NWN.Core.NWScript.EffectSeeInvisible();
        }

        /// <summary>
        /// Creates a Time Stop effect.
        /// </summary>
        /// <returns>The Time Stop effect</returns>
        public static Effect EffectTimeStop()
        {
            return global::NWN.Core.NWScript.EffectTimeStop();
        }

        /// <summary>
        /// Creates a Blindness effect.
        /// </summary>
        /// <returns>The Blindness effect</returns>
        public static Effect EffectBlindness()
        {
            return global::NWN.Core.NWScript.EffectBlindness();
        }

        /// <summary>
        /// Creates an Ability Decrease effect.
        /// </summary>
        /// <param name="nAbility">The ability to decrease (ABILITY_* constants)</param>
        /// <param name="nModifyBy">The amount by which to decrement the ability</param>
        /// <returns>The Ability Decrease effect</returns>
        public static Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy)
        {
            return global::NWN.Core.NWScript.EffectAbilityDecrease((int)nAbility, nModifyBy);
        }

        /// <summary>
        /// Creates an Attack Decrease effect.
        /// </summary>
        /// <param name="nPenalty">The penalty amount</param>
        /// <param name="nModifierType">The attack bonus type (ATTACK_BONUS_* constants) (default: AttackBonus.Misc)</param>
        /// <returns>The Attack Decrease effect</returns>
        /// <remarks>On SWLOR, this is used for Accuracy.</remarks>
        public static Effect EffectAccuracyDecrease(int nPenalty, AttackBonus nModifierType = AttackBonus.Misc)
        {
            return global::NWN.Core.NWScript.EffectAttackDecrease(nPenalty, (int)nModifierType);
        }

        /// <summary>
        /// Creates a Damage Decrease effect.
        /// </summary>
        /// <param name="nPenalty">The penalty amount</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <returns>The Damage Decrease effect</returns>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Force)
        {
            return global::NWN.Core.NWScript.EffectDamageDecrease(nPenalty, (int)nDamageType);
        }

        /// <summary>
        /// Creates a Damage Immunity Decrease effect.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nPercentImmunity">The percentage of immunity to decrease</param>
        /// <returns>The Damage Immunity Decrease effect</returns>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity)
        {
            return global::NWN.Core.NWScript.EffectDamageImmunityDecrease(nDamageType, nPercentImmunity);
        }

        /// <summary>
        /// Creates an AC Decrease effect.
        /// </summary>
        /// <param name="nValue">The AC decrease value</param>
        /// <param name="nModifyType">The armor class modifier type (AC_* constants) (default: ArmorClassModiferType.Dodge)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: AC.VsDamageTypeAll)</param>
        /// <returns>The AC Decrease effect</returns>
        /// <remarks>Default value for nDamageType should only ever be used in this function prototype.</remarks>
        public static Effect EffectACDecrease(int nValue,
            ArmorClassModiferType nModifyType = ArmorClassModiferType.Dodge,
            AC nDamageType = AC.VsDamageTypeAll)
        {
            return global::NWN.Core.NWScript.EffectACDecrease(nValue, (int)nModifyType, (int)nDamageType);
        }

        /// <summary>
        /// Creates a Movement Speed Decrease effect.
        /// </summary>
        /// <param name="nPercentChange">The percentage change (range 0 through 99)</param>
        /// <returns>The Movement Speed Decrease effect</returns>
        /// <remarks>0 = no change in speed, 50 = 50% slower, 99 = almost immobile</remarks>
        public static Effect EffectMovementSpeedDecrease(int nPercentChange)
        {
            return global::NWN.Core.NWScript.EffectMovementSpeedDecrease(nPercentChange);
        }

        /// <summary>
        /// Creates a Saving Throw Decrease effect.
        /// </summary>
        /// <param name="nSave">The saving throw type (SAVING_THROW_* constants, not SAVING_THROW_TYPE_*)</param>
        /// <param name="nValue">The size of the saving throw decrease</param>
        /// <param name="nSaveType">The saving throw type (SAVING_THROW_TYPE_* constants, e.g., SAVING_THROW_TYPE_ACID) (default: SavingThrowType.All)</param>
        /// <returns>The Saving Throw Decrease effect</returns>
        /// <remarks>Possible save types: SAVING_THROW_ALL, SAVING_THROW_FORT, SAVING_THROW_REFLEX, SAVING_THROW_WILL</remarks>
        public static Effect EffectSavingThrowDecrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All)
        {
            return global::NWN.Core.NWScript.EffectSavingThrowDecrease(nSave, nValue, (int)nSaveType);
        }

        /// <summary>
        /// Creates a Skill Decrease effect.
        /// </summary>
        /// <param name="nSkill">The skill to decrease</param>
        /// <param name="nValue">The amount to decrease the skill by</param>
        /// <returns>The Skill Decrease effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid</returns>
        public static Effect EffectSkillDecrease(int nSkill, int nValue)
        {
            return global::NWN.Core.NWScript.EffectSkillDecrease(nSkill, nValue);
        }

        /// <summary>
        /// Creates a Spell Resistance Decrease effect.
        /// </summary>
        /// <param name="nValue">The amount to decrease spell resistance by</param>
        /// <returns>The Spell Resistance Decrease effect</returns>
        public static Effect EffectSpellResistanceDecrease(int nValue)
        {
            return global::NWN.Core.NWScript.EffectSpellResistanceDecrease(nValue);
        }

        /// <summary>
        /// Activates the specified item.
        /// </summary>
        /// <param name="oItem">The item to activate</param>
        /// <param name="lTarget">The target location</param>
        /// <param name="oTarget">The target object (default: OBJECT_SELF)</param>
        /// <returns>The Activate Item event</returns>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.EventActivateItem(oItem, lTarget, oTarget);
        }

        /// <summary>
        /// Creates a Hit Point Change When Dying effect.
        /// </summary>
        /// <param name="fHitPointChangePerRound">The hit point change per round (can be positive or negative, but not zero)</param>
        /// <returns>The Hit Point Change When Dying effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0</returns>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound)
        {
            return global::NWN.Core.NWScript.EffectHitPointChangeWhenDying(fHitPointChangePerRound);
        }

        /// <summary>
        /// Creates a Turned effect.
        /// </summary>
        /// <returns>The Turned effect</returns>
        /// <remarks>Turned effects are supernatural by default.</remarks>
        public static Effect EffectTurned()
        {
            return global::NWN.Core.NWScript.EffectTurned();
        }

        /// <summary>
        /// Sets the effect to be versus a specific alignment.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nLawChaos">The law/chaos alignment (ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL) (default: Alignment.All)</param>
        /// <param name="nGoodEvil">The good/evil alignment (ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL) (default: Alignment.All)</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusAlignmentEffect(Effect eEffect,
            Alignment nLawChaos = Alignment.All,
            Alignment nGoodEvil = Alignment.All)
        {
            return global::NWN.Core.NWScript.VersusAlignmentEffect(eEffect, (int)nLawChaos, (int)nGoodEvil);
        }

        /// <summary>
        /// Sets the effect to be versus a specific racial type.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nRacialType">The racial type (RACIAL_TYPE_* constants)</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType)
        {
            return global::NWN.Core.NWScript.VersusRacialTypeEffect(eEffect, (int)nRacialType);
        }

        /// <summary>
        /// Sets the effect to be versus traps.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusTrapEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.VersusTrapEffect(eEffect);
        }

        /// <summary>
        /// Creates a Skill Increase effect.
        /// </summary>
        /// <param name="nSkill">The skill to increase (SKILL_* constants)</param>
        /// <param name="nValue">The amount to increase the skill by</param>
        /// <returns>The Skill Increase effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid</returns>
        public static Effect EffectSkillIncrease(NWNSkillType nSkill, int nValue)
        {
            return global::NWN.Core.NWScript.EffectSkillIncrease((int)nSkill, nValue);
        }

        /// <summary>
        /// Creates a Temporary Hitpoints effect.
        /// </summary>
        /// <param name="nHitPoints">A positive integer for the hit points</param>
        /// <returns>The Temporary Hitpoints effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0</returns>
        public static Effect EffectTemporaryHitpoints(int nHitPoints)
        {
            return global::NWN.Core.NWScript.EffectTemporaryHitpoints(nHitPoints);
        }

        /// <summary>
        /// Creates a conversation event.
        /// </summary>
        /// <returns>The conversation event</returns>
        /// <remarks>This only creates the event. The event won't actually trigger until SignalEvent() is called using this created conversation event as an argument. For example: SignalEvent(oCreature, EventConversation()); Once the event has been signaled, the script associated with the OnConversation event will run on the creature oCreature. To specify the OnConversation script that should run, view the Creature Properties on the creature and click on the Scripts Tab. Then specify a script for the OnConversation event.</remarks>
        public static Event EventConversation()
        {
            return global::NWN.Core.NWScript.EventConversation();
        }

        /// <summary>
        /// Creates a Damage Immunity Increase effect.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nPercentImmunity">The percentage of immunity to increase</param>
        /// <returns>The Damage Immunity Increase effect</returns>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity)
        {
            return global::NWN.Core.NWScript.EffectDamageImmunityIncrease((int)nDamageType, nPercentImmunity);
        }

        /// <summary>
        /// Creates an Immunity effect.
        /// </summary>
        /// <param name="nImmunityType">The immunity type (IMMUNITY_TYPE_* constants)</param>
        /// <returns>The Immunity effect</returns>
        public static Effect EffectImmunity(ImmunityType nImmunityType)
        {
            return global::NWN.Core.NWScript.EffectImmunity((int)nImmunityType);
        }

        /// <summary>
        /// Creates a Haste effect.
        /// </summary>
        /// <returns>The Haste effect</returns>
        public static Effect EffectHaste()
        {
            return global::NWN.Core.NWScript.EffectHaste();
        }

        /// <summary>
        /// Creates a Slow effect.
        /// </summary>
        /// <returns>The Slow effect</returns>
        public static Effect EffectSlow()
        {
            return global::NWN.Core.NWScript.EffectSlow();
        }

        /// <summary>
        /// Creates a Poison effect.
        /// </summary>
        /// <param name="nPoisonType">The poison type (POISON_* constants)</param>
        /// <returns>The Poison effect</returns>
        public static Effect EffectPoison(Poison nPoisonType)
        {
            return global::NWN.Core.NWScript.EffectPoison((int)nPoisonType);
        }

        /// <summary>
        /// Creates a Disease effect.
        /// </summary>
        /// <param name="nDiseaseType">The disease type (DISEASE_* constants)</param>
        /// <returns>The Disease effect</returns>
        public static Effect EffectDisease(Disease nDiseaseType)
        {
            return global::NWN.Core.NWScript.EffectDisease((int)nDiseaseType);
        }

        /// <summary>
        /// Creates a Silence effect.
        /// </summary>
        /// <returns>The Silence effect</returns>
        public static Effect EffectSilence()
        {
            return global::NWN.Core.NWScript.EffectSilence();
        }

        /// <summary>
        /// Creates a Spell Resistance Increase effect.
        /// </summary>
        /// <param name="nValue">The size of spell resistance increase</param>
        /// <returns>The Spell Resistance Increase effect</returns>
        public static Effect EffectSpellResistanceIncrease(int nValue)
        {
            return global::NWN.Core.NWScript.EffectSpellResistanceIncrease(nValue);
        }

        /// <summary>
        /// Creates a Beam effect.
        /// </summary>
        /// <param name="nBeamVisualEffect">The beam visual effect (VFX_BEAM_* constants)</param>
        /// <param name="oEffector">The creature the beam is emitted from</param>
        /// <param name="nBodyPart">The body part (BODY_NODE_* constants)</param>
        /// <param name="bMissEffect">If true, the beam will fire to a random vector near or past the target (default: false)</param>
        /// <returns>The Beam effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is not valid</returns>
        public static Effect EffectBeam(VisualEffect nBeamVisualEffect, uint oEffector, BodyNode nBodyPart, bool bMissEffect = false)
        {
            return global::NWN.Core.NWScript.EffectBeam((int)nBeamVisualEffect, oEffector, (int)nBodyPart, bMissEffect ? 1 : 0);
        }

        /// <summary>
        /// Links the two supplied effects, returning the child effect as a child of the parent effect.
        /// </summary>
        /// <param name="eChildEffect">The child effect to link</param>
        /// <param name="eParentEffect">The parent effect to link to</param>
        /// <returns>The linked child effect</returns>
        /// <remarks>When applying linked effects if the target is immune to all valid effects all other effects will be removed as well. This means that if you apply a visual effect and a silence effect (in a link) and the target is immune to the silence effect that the visual effect will get removed as well. Visual Effects are not considered "valid" effects for the purposes of determining if an effect will be removed or not and as such should never be packaged only with other visual effects in a link.</remarks>
        public static Effect EffectLinkEffects(Effect eChildEffect, Effect eParentEffect)
        {
            return global::NWN.Core.NWScript.EffectLinkEffects(eChildEffect, eParentEffect);
        }

        /// <summary>
        /// Creates a Visual Effect that can be applied to an object.
        /// </summary>
        /// <param name="visualEffectID">The visual effect ID</param>
        /// <param name="nMissEffect">If true, a random vector near or past the target will be generated, on which to play the effect (default: false)</param>
        /// <param name="fScale">The scale of the effect (default: 1.0f)</param>
        /// <param name="vTranslate">The translation vector (default: new Vector3())</param>
        /// <param name="vRotate">The rotation vector (default: new Vector3())</param>
        /// <returns>The Visual Effect</returns>
        public static Effect EffectVisualEffect(VisualEffect visualEffectID, bool nMissEffect = false, float fScale = 1.0f, Vector3 vTranslate = new Vector3(), Vector3 vRotate = new Vector3())
        {
            return global::NWN.Core.NWScript.EffectVisualEffect((int)visualEffectID, nMissEffect ? 1 : 0, fScale, vTranslate, vRotate);
        }

        /// <summary>
        /// Applies the specified effect to the target object.
        /// </summary>
        /// <param name="nDurationType">The duration type of the effect</param>
        /// <param name="eEffect">The effect to apply</param>
        /// <param name="oTarget">The target object to apply the effect to</param>
        /// <param name="fDuration">The duration of the effect (default: 0.0f)</param>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f)
        {
            global::NWN.Core.NWScript.ApplyEffectToObject((int)nDurationType, eEffect, oTarget, fDuration);
        }

        /// <summary>
        /// Gets the effect type of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the type for</param>
        /// <returns>The effect type (EFFECT_TYPE_* constants). Returns EFFECT_INVALIDEFFECT if the effect is invalid</returns>
        public static EffectTypeScript GetEffectType(Effect eEffect)
        {
            return (EffectTypeScript)global::NWN.Core.NWScript.GetEffectType(eEffect);
        }

        /// <summary>
        /// Creates an Area Of Effect effect in the area of the creature it is applied to.
        /// </summary>
        /// <param name="nAreaEffect">The area of effect type</param>
        /// <param name="sOnEnterScript">The script to run when entering the area (default: empty string)</param>
        /// <param name="sHeartbeatScript">The script to run on heartbeat (default: empty string)</param>
        /// <param name="sOnExitScript">The script to run when exiting the area (default: empty string)</param>
        /// <returns>The Area Of Effect effect</returns>
        /// <remarks>If the scripts are not specified, default ones will be used.</remarks>
        public static Effect EffectAreaOfEffect(AreaOfEffect nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "")
        {
            return global::NWN.Core.NWScript.EffectAreaOfEffect((int)nAreaEffect, sOnEnterScript, sHeartbeatScript, sOnExitScript);
        }

        /// <summary>
        /// Creates a Regenerate effect.
        /// </summary>
        /// <param name="nAmount">The amount of damage to be regenerated per time interval</param>
        /// <param name="fIntervalSeconds">The length of interval in seconds</param>
        /// <returns>The Regenerate effect</returns>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds)
        {
            return global::NWN.Core.NWScript.EffectRegenerate(nAmount, fIntervalSeconds);
        }

        /// <summary>
        /// Creates a Movement Speed Increase effect.
        /// </summary>
        /// <param name="nPercentChange">The percentage change (range 0 through 99)</param>
        /// <returns>The Movement Speed Increase effect</returns>
        /// <remarks>0 = no change in speed, 50 = 50% faster, 99 = almost twice as fast</remarks>
        public static Effect EffectMovementSpeedIncrease(int nPercentChange)
        {
            return global::NWN.Core.NWScript.EffectMovementSpeedIncrease(nPercentChange);
        }

        /// <summary>
        /// Creates a Charm effect.
        /// </summary>
        /// <returns>The Charm effect</returns>
        public static Effect EffectCharmed()
        {
            return global::NWN.Core.NWScript.EffectCharmed();
        }

        /// <summary>
        /// Creates a Confuse effect.
        /// </summary>
        /// <returns>The Confuse effect</returns>
        public static Effect EffectConfused()
        {
            return global::NWN.Core.NWScript.EffectConfused();
        }

        /// <summary>
        /// Creates a Frighten effect.
        /// </summary>
        /// <returns>The Frighten effect</returns>
        public static Effect EffectFrightened()
        {
            return global::NWN.Core.NWScript.EffectFrightened();
        }

        /// <summary>
        /// Creates a Dominate effect.
        /// </summary>
        /// <returns>The Dominate effect</returns>
        public static Effect EffectDominated()
        {
            return global::NWN.Core.NWScript.EffectDominated();
        }

        /// <summary>
        /// Creates a Daze effect.
        /// </summary>
        /// <returns>The Daze effect</returns>
        public static Effect EffectDazed()
        {
            return global::NWN.Core.NWScript.EffectDazed();
        }

        /// <summary>
        /// Creates a Stun effect.
        /// </summary>
        /// <returns>The Stun effect</returns>
        public static Effect EffectStunned()
        {
            return global::NWN.Core.NWScript.EffectStunned();
        }

        /// <summary>
        /// Creates a Sleep effect.
        /// </summary>
        /// <returns>The Sleep effect</returns>
        public static Effect EffectSleep()
        {
            return global::NWN.Core.NWScript.EffectSleep();
        }

        /// <summary>
        /// Creates a Paralyze effect.
        /// </summary>
        /// <returns>The Paralyze effect</returns>
        public static Effect EffectParalyze()
        {
            return global::NWN.Core.NWScript.EffectParalyze();
        }

        /// <summary>
        /// Creates a Spell Immunity effect.
        /// </summary>
        /// <param name="nImmunityToSpell">The spell to be immune to (SPELL_* constants) (default: Spell.AllSpells)</param>
        /// <returns>The Spell Immunity effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is invalid</returns>
        /// <remarks>There is a known bug with this function. There must be a parameter specified when this is called (even if the desired parameter is SPELL_ALL_SPELLS), otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned.</remarks>
        public static Effect EffectSpellImmunity(Spell nImmunityToSpell = Spell.AllSpells)
        {
            return global::NWN.Core.NWScript.EffectSpellImmunity((int)nImmunityToSpell);
        }

        /// <summary>
        /// Creates a Deaf effect.
        /// </summary>
        /// <returns>The Deaf effect</returns>
        public static Effect EffectDeaf()
        {
            return global::NWN.Core.NWScript.EffectDeaf();
        }


        /// <summary>
        /// Gets the integer parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the integer parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 7 inclusive)</param>
        /// <returns>The integer value or 0 on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static int GetEffectInteger(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectInteger(eEffect, nIndex);
        }

        /// <summary>
        /// Gets the float parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the float parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 3 inclusive)</param>
        /// <returns>The float value or 0.0f on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static float GetEffectFloat(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectFloat(eEffect, nIndex);
        }

        /// <summary>
        /// Gets the string parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the string parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 5 inclusive)</param>
        /// <returns>The string value or empty string on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static string GetEffectString(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectString(eEffect, nIndex);
        }
        
        /// <summary>
        /// Gets the object parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the object parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 3 inclusive)</param>
        /// <returns>The object value or OBJECT_INVALID on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static uint GetEffectObject(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectObject(eEffect, nIndex);
        }

        /// <summary>
        /// Gets the vector parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the vector parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 1 inclusive)</param>
        /// <returns>The vector value or {0.0f, 0.0f, 0.0f} on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static Vector3 GetEffectVector(Effect eEffect, int nIndex)
        {
            return global::NWN.Core.NWScript.GetEffectVector(eEffect, nIndex);
        }

        /// <summary>
        /// Creates a RunScript effect.
        /// </summary>
        /// <param name="sOnAppliedScript">An optional script to execute when the effect is applied (default: empty string)</param>
        /// <param name="sOnRemovedScript">An optional script to execute when the effect is removed (default: empty string)</param>
        /// <param name="sOnIntervalScript">An optional script to execute every fInterval seconds (default: empty string)</param>
        /// <param name="fInterval">The interval in seconds, must be >0.0f if an interval script is set (default: 0.0f)</param>
        /// <param name="sData">An optional string of data saved in the effect, retrievable with GetEffectString() at index 0 (default: empty string)</param>
        /// <returns>The RunScript effect</returns>
        /// <remarks>When applied as instant effect, only sOnAppliedScript will fire. In the scripts, OBJECT_SELF will be the object the effect is applied to. Very low interval values may have an adverse effect on performance.</remarks>
        public static Effect EffectRunScript(string sOnAppliedScript = "", string sOnRemovedScript = "", string sOnIntervalScript = "", float fInterval = 0.0f, string sData = "")
        {
            return global::NWN.Core.NWScript.EffectRunScript(sOnAppliedScript, sOnRemovedScript, sOnIntervalScript, fInterval, sData);
        }

        /// <summary>
        /// Gets the effect that last triggered an EffectRunScript() script.
        /// </summary>
        /// <returns>The effect that last triggered an EffectRunScript() script</returns>
        /// <remarks>This can be used to get the creator or tag, among others, of the EffectRunScript() in one of its scripts.</remarks>
        public static Effect GetLastRunScriptEffect()
        {
            return global::NWN.Core.NWScript.GetLastRunScriptEffect();
        }

        /// <summary>
        /// Gets the script type of the last triggered EffectRunScript() script.
        /// </summary>
        /// <returns>The script type (RUNSCRIPT_EFFECT_SCRIPT_TYPE_* constants). Returns 0 when called outside of an EffectRunScript() script</returns>
        public static int GetLastRunScriptEffectScriptType()
        {
            return global::NWN.Core.NWScript.GetLastRunScriptEffectScriptType();
        }

        /// <summary>
        /// Hides the effect icon of the effect and of all effects currently linked to it.
        /// </summary>
        /// <param name="eEffect">The effect to hide the icon for</param>
        /// <returns>The effect with hidden icon</returns>
        public static Effect HideEffectIcon(Effect eEffect)
        {
            return global::NWN.Core.NWScript.HideEffectIcon(eEffect);
        }

        /// <summary>
        /// Creates an Icon effect.
        /// </summary>
        /// <param name="nIconId">The effect icon (EFFECT_ICON_* constants) to display</param>
        /// <returns>The Icon effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255</returns>
        /// <remarks>Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases.</remarks>
        public static Effect EffectIcon(EffectIconType nIconId)
        {
            return global::NWN.Core.NWScript.EffectIcon((int)nIconId);
        }

        /// <summary>
        /// Sets the subtype of the effect to Unyielding and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as unyielding</param>
        /// <returns>The unyielding effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Unyielding effects are not removed by resting, death or dispel magic, only by RemoveEffect(). Note: effects that modify state, Stunned/Knockdown/Deaf etc, WILL be removed on death.</remarks>
        public static Effect UnyieldingEffect(Effect eEffect)
        {
            return global::NWN.Core.NWScript.UnyieldingEffect(eEffect);
        }

        /// <summary>
        /// Sets the effect to ignore immunities and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set to ignore immunities</param>
        /// <returns>The effect that ignores immunities</returns>
        public static Effect IgnoreEffectImmunity(Effect eEffect)
        {
            return global::NWN.Core.NWScript.IgnoreEffectImmunity(eEffect);
        }

        /// <summary>
        /// Creates a Pacified effect, making the creature unable to attack anyone.
        /// </summary>
        /// <returns>The Pacified effect</returns>
        public static Effect EffectPacified()
        {
            return global::NWN.Core.NWScript.EffectPacified();
        }

        /// <summary>
        /// Returns the given effect's Link ID.
        /// </summary>
        /// <param name="eEffect">The effect to get the link ID for</param>
        /// <returns>The link ID string</returns>
        /// <remarks>There are no guarantees about this identifier other than it is unique and the same for all effects linked to it.</remarks>
        public static string GetEffectLinkId(Effect eEffect)
        {
            return global::NWN.Core.NWScript.GetEffectLinkId(eEffect);
        }

        /// <summary>
        /// Creates a bonus feat effect.
        /// </summary>
        /// <param name="nFeat">The feat (FEAT_* constants)</param>
        /// <returns>The Bonus Feat effect</returns>
        /// <remarks>These act like the Bonus Feat item property, and do not work as feat prerequisites for levelup purposes.</remarks>
        public static Effect EffectBonusFeat(int nFeat)
        {
            return global::NWN.Core.NWScript.EffectBonusFeat(nFeat);
        }

        /// <summary>
        /// Provides immunity to the effects of EffectTimeStop.
        /// </summary>
        /// <returns>The Time Stop Immunity effect</returns>
        /// <remarks>This allows actions during other creatures' time stop effects.</remarks>
        public static Effect EffectTimeStopImmunity()
        {
            return global::NWN.Core.NWScript.EffectTimeStopImmunity();
        }

        /// <summary>
        /// Forces the creature to always walk.
        /// </summary>
        /// <returns>The Force Walk effect</returns>
        public static Effect EffectForceWalk()
        {
            return global::NWN.Core.NWScript.EffectForceWalk();
        }

        /// <summary>
        /// Sets the effect creator.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="oCreator">The creator of the effect. Can be OBJECT_INVALID</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectCreator(Effect eEffect, uint oCreator)
        {
            return global::NWN.Core.NWScript.SetEffectCreator(eEffect, oCreator);
        }

        /// <summary>
        /// Sets the effect caster level.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nCasterLevel">The caster level of the effect for the purposes of dispel magic and GetEffectCasterLevel. Must be >= 0</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectCasterLevel(Effect eEffect, int nCasterLevel)
        {
            return global::NWN.Core.NWScript.SetEffectCasterLevel(eEffect, nCasterLevel);
        }

        /// <summary>
        /// Sets the effect spell ID.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nSpellId">The spell ID for the purposes of effect stacking, dispel magic and GetEffectSpellId. Must be >= -1 (-1 being invalid/no spell)</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectSpellId(Effect eEffect, Spell nSpellId)
        {
            return global::NWN.Core.NWScript.SetEffectSpellId(eEffect, (int)nSpellId);
        }
    }
}