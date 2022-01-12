using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using FeatType = SWLOR.Game.Server.Core.NWScript.Enum.FeatType;
using ImmunityType = NWN.Native.API.ImmunityType;
using InventorySlot = NWN.Native.API.InventorySlot;
using ItemPropertyType = SWLOR.Game.Server.Core.NWScript.Enum.Item.ItemPropertyType;
using ObjectType = NWN.Native.API.ObjectType;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ResolveAttackRoll
    {
        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessField.Local
        private static ResolveAttackRollHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, void> pHook = &OnResolveAttackRoll;

            var hookPtr = VM.RequestHook(new IntPtr(FunctionsLinux._ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject), (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<ResolveAttackRollHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
        {
            /*
             * Custom attack logic for SWLOR.  This is done as a skill vs skill opposed roll, so most default NWN logic
             * does not apply.
             * 
             * The following default NWN functions don't exist in this engine.
             * - Miss on 1
             * - Hit on 20
             * - Parry
             * - Coup de Grace
             * - Sneak Attack (/Death Attack)
             * 
             * Armor Class doesn't exist, and non-creature objects are hit automatically. 
             * Critical hits come from beating the opposed roll by 30 or more.  Crit immunity applies as normal.
             * 
             * A stunned or incapable defender does not add their skill to the opposed roll.  
             * 
             */

            Log.Write(LogGroup.Attack, "Running OnResolveAttackRoll");
            var targetObject = CNWSObject.FromPointer(pTarget);
            if (targetObject == null)
                return;

            var attacker = CNWSCreature.FromPointer(thisPtr);
            var attackerStats = attacker.m_pStats;

            // Support various types of attacks.
            // - Regular combat round attacks
            // - Touch attacks and other scripted attacks from feats.
            var pCombatRound = attacker.m_pcCombatRound;

            Log.Write(LogGroup.Attack, "Attacker: " + attacker.GetFirstName().GetSimple(0) + ", defender " + targetObject.GetFirstName().GetSimple(0));

            var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
            var isOffhandAttack = (int)pAttackData.m_nWeaponAttackType == 2;

            if (targetObject.m_nObjectType != (int)ObjectType.Creature)
            {
                // Automatically hit non-creature targets.  Do not apply criticals.
                Log.Write(LogGroup.Attack, "Placeable target.  Auto hit.");
                pAttackData.m_nAttackResult = 7; // Automatic hit.
                return;
            }

            // If we get to this point, we are fighting a creature.  Pull the target's stats.
            //CNWSCreatureStats defenderStats = CNWSCreatureStats.FromPointer(pTarget);
            var defender = CNWSCreature.FromPointer(pTarget);
            var defenderStats = defender.m_pStats;

            // Determine the type of attack.
            // - Check for an override variable on the creature.  If set, remove it and use that value.  This is 
            //   to allow abilities to specify the type of attack roll they make. 
            // - If no override, check the weapon used to make the attack (main or offhand).
            // - Check the weapon for an override property.  If it exists, use it (but don't remove it). 
            // - Else, pick Melee (Might) for melee weapons and Ranged (Perception) for ranged weapons.
            // - Spirit (Willpower) attacks can only be selected by variable.
            var attackType = (uint)AttackType.Melee; // Default to melee.
            CNWSItem weapon = null;

            var attackOverride = attacker.m_ScriptVars.GetInt(new CExoString("ATTACK_TYPE_OVERRIDE"));
            weapon = pCombatRound.GetCurrentAttackWeapon();

            if (attackOverride != 0 & attackOverride < 4)
            {
                attackType = (uint) attackOverride;
                attacker.m_ScriptVars.DestroyInt(new CExoString("ATTACK_TYPE_OVERRIDE"));
            }
            else
            {
                // Check whether this is a ranged weapon. 
                if (weapon != null && pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
                {
                    attackType = (uint)AttackType.Ranged;
                }
            }

            Log.Write(LogGroup.Attack, "Selected attack type " + attackType + ", weapon " + (weapon == null ? "none":weapon.GetFirstName().GetSimple(0)) );

            // We now have our attack type defined.  Pull the relevant attributes, defaulting to melee.
            int attackAttribute = attackerStats.m_nStrengthModifier;
            int defendAttribute = defenderStats.m_nStrengthModifier;
            
            switch (attackType)
            {
                case (uint)AttackType.Ranged:
                    attackAttribute = attackerStats.m_nDexterityModifier;
                    defendAttribute = defenderStats.m_nDexterityModifier;
                    break;
                case (uint)AttackType.Spirit:
                    attackAttribute = attackerStats.m_nWisdomModifier;
                    defendAttribute = defenderStats.m_nWisdomModifier;
                    break;
            }

            // Weapon Finesse - set which weapons are allowed here.
            var bFinessable = weapon == null;
            if (!bFinessable)
            {
                bFinessable =   Item.FinesseVibrobladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                                Item.LightsaberBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) || 
                                Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                                Item.TwinBladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem);
            }

            if (attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFinesse) == 1 &&
                attackerStats.m_nDexterityBase > attackerStats.m_nStrengthBase &&
                attackType == (uint) AttackType.Melee && 
                bFinessable)
            {
                Log.Write(LogGroup.Attack, "Finesse attack");
                attackAttribute = attackerStats.m_nDexterityModifier;
                defendAttribute = defenderStats.m_nDexterityModifier;
            }

            // Check for an override on the weapon itself.
            // If no weapon, use default attack type (Might).  Otherwise, look further.
            if (weapon != null)
            {
                // Iterate over properties and look for a Primary Stat property.
                for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                {
                    var ip = weapon.GetPassiveProperty(index);
                    if (ip != null && ip.m_nPropertyName == (ushort)ItemPropertyType.PrimaryStat)
                    {
                        switch (ip.m_nSubType)
                        {
                            case (ushort) AbilityType.Might:
                                attackAttribute = attackerStats.m_nStrengthModifier;
                                defendAttribute = defenderStats.m_nStrengthModifier;
                                break;
                            case (ushort)AbilityType.Perception:
                                attackAttribute = attackerStats.m_nDexterityModifier;
                                defendAttribute = defenderStats.m_nDexterityModifier;
                                break;
                            case (ushort)AbilityType.Willpower:
                                attackAttribute = attackerStats.m_nWisdomModifier;
                                defendAttribute = defenderStats.m_nWisdomModifier;
                                break;
                            case (ushort)AbilityType.Vitality:
                                attackAttribute = attackerStats.m_nConstitutionModifier;
                                defendAttribute = defenderStats.m_nConstitutionModifier;
                                break;
                            case (ushort)AbilityType.Social: // Well it could happen, I suppose.
                                attackAttribute = attackerStats.m_nCharismaModifier;
                                defendAttribute = defenderStats.m_nCharismaModifier;
                                break;
                        }
                    }
                }
            }

            // Check for negative modifiers.  A modifier of -2 is represented as 254.
            if (attackAttribute > 128) attackAttribute -= 256;
            if (defendAttribute > 128) defendAttribute -= 256;

            Log.Write(LogGroup.Attack, "Attacker attribute modifier: " + attackAttribute +", defender attribute modifier: " + defendAttribute);

            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            // Modifiers - put in modifiers here based on the type of attack (and type of weapon etc.).
            var modifiers = 0;

            // Weapon AB or EB.
            if (weapon != null)
            {
                // Retrieve item properties and cost table values. 
                foreach (var ip in weapon.m_lstPassiveProperties)
                {
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.AttackBonus ||
                        ip.m_nPropertyName == (ushort)ItemPropertyType.EnhancementBonus)
                    {
                        Log.Write(LogGroup.Attack, "Weapon has attack or enhancement bonus: " + ip.m_nCostTableValue);
                        modifiers += 5 * ip.m_nCostTableValue;
                    }
                }
            }

            // Defender Evasion (AC) bonuses.  Stored in the Base AC field of the creature (accessed via NWNX_Creature for non-Native scripts).
            // To support NPCs, also read the Natural Base field.
            var defenderEvasion = defenderStats.m_nACArmorBase + defenderStats.m_nACNaturalBase;
            if (defenderEvasion > 0)
            {
                modifiers -= 5 * defenderEvasion;
                Log.Write(LogGroup.Attack, "Defender has evasion bonus: " + defenderEvasion); 
            }

            // Defender Evasion (AC) bonuses and penalties from effects.
            foreach (CGameEffect effect in defender.m_appliedEffects)
            {
                if (effect.m_nType == (ushort) EffectTrueType.ACIncrease)
                {
                    Log.Write(LogGroup.Attack, "Defender has AC increase: " + effect.GetInteger(1));
                    // The magnitude is Effect Integer 1, see https://nwnlexicon.com/index.php?title=EffectACIncrease
                    modifiers -= 5 * effect.GetInteger(1);

                }
                else if (effect.m_nType == (ushort)EffectTrueType.ACDecrease)
                {
                    Log.Write(LogGroup.Attack, "Defender has AC decrease: " + effect.GetInteger(1));
                    modifiers -= 5 * effect.GetInteger(1);
                }
            }

            // Defender stunned
            if (defender.m_nState == 6) // Stunned
            {
                defendAttribute = 0;
            }

            // Dual wield penalty.
            var offhand = attacker.m_pInventory.GetItemInSlot((uint) InventorySlot.LeftHand);
            var bDoubleWeapon = 
                weapon != null && 
                (Item.TwinBladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                 Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem));

            if (bDoubleWeapon ||
                (offhand != null && offhand.m_nBaseItem != (uint) BaseItem.LargeShield && 
                 offhand.m_nBaseItem != (uint) BaseItem.SmallShield && offhand.m_nBaseItem != (uint) BaseItem.TowerShield))
            {
                var log = "Applying dual wield penalty.  Offhand weapon: " + (offhand == null ? weapon.GetFirstName().GetSimple() : offhand.GetFirstName().GetSimple() + " -");
                // Note - we have retired Two Weapon Fighting and Ambidexterity as feats.  We have costed them
                // in to the proficiency perks rather than granting them separately. 

                if (!bDoubleWeapon && Item.GetWeaponSize((BaseItem)offhand.m_nBaseItem) >= attacker.m_nCreatureSize)
                {
                    // Unless the offhand weapon size is smaller than the creature size (i.e. Small vs Medium), apply additional penalty. 
                    modifiers -= 10;
                    log += "- offhand weapon is unwieldy -";
                }

                // Apply the base two weapon fighting penalty. 
                modifiers -= 10;
                Log.Write(LogGroup.Attack, log);
            }

            // Defender not targeting the attacker.
            // Dev note: the GetItem method always creates a new instance of CNWActionNode so there should be no NPEs.
            // Note: this always returns object invalid for NPCs (2130706432) as their actions aren't represented the same way.
            var oidTarget = defender.m_pActionQueue.GetItem(0).oidTarget;
            
            if (oidTarget == NWScript.OBJECT_INVALID)
            {
                oidTarget = (uint) defender.m_ScriptVars.GetInt(new CExoString("I_LAST_ATTACKED"));
            }

            // If this is an NPC attacking, Store the attack on the NPC. 
            if (attacker.m_pActionQueue.GetItem(0).oidTarget == 2130706432)
            {
                Log.Write(LogGroup.Attack, "NPC attacking - storing target "+ defender.m_idSelf);
                attacker.m_ScriptVars.SetInt(new CExoString("I_LAST_ATTACKED"), (int)defender.m_idSelf);
            }

            // oidTarget will be 0 for a newly spawned NPC who hasn't been attacked yet.  Don't let them get taken by surprise in round 1. 
            if (oidTarget != 0 && oidTarget != attacker.m_idSelf)
            {
                Log.Write(LogGroup.Attack, "Defender current target ("+oidTarget +") is not attacker ("+attacker.m_idSelf+"). Assign circumstance bonus");
                modifiers += 5;
            }

            // Weapon focus feats.
            modifiers += 5 * HasWeaponFocus(attacker, weapon);
            modifiers += 5 * HasSuperiorWeaponFocus(attacker, weapon);

            // Range bonuses and penalties.
            if (attackType == (uint)AttackType.Ranged || attackType == (uint)AttackType.Spirit)
            {
                var attackerPos = attacker.m_vPosition;
                var defenderPos = defender.m_vPosition;

                // Note - calculating distance solely via X/Y co-ordinates.  NWN doesn't have a true Z.
                var range = Math.Pow(Math.Pow((attackerPos.x - defenderPos.x), 2) + Math.Pow((attackerPos.y - defenderPos.y),2), 0.5);
                     
                Log.Write(LogGroup.Attack, "Ranged attack at range " + range);
                if (range < 5.0f)
                {
                    // Force powers or point blank shot feat make close range an advantage.
                    if (attackType == 3 || attacker.m_pStats.HasFeat((ushort)FeatType.PointBlankShot) == 1)
                    {
                        modifiers += 5;
                    }
                    else
                    {
                        modifiers -= 20;
                    }
                }
                else if (range > 40.0f)
                {
                    modifiers -= 20;
                }
                else if (range > 30.0f)
                {
                    modifiers -= 10;
                }
                else if (range > 20.0f)
                {
                    modifiers = -5;
                }
                
            }

            // Attacking from behind.  Does not apply to Force attacks.
            // Vectors are X=-1 to +1, Y=-1 to +1, Z=0.  
            // If the absolute difference between the two vectors is less than 0.5, treat as a backstab.
            var attFacing = attacker.m_vOrientation;
            var defFacing = defender.m_vOrientation;

            if (attackType != (uint)AttackType.Spirit && 
                (Math.Abs(attFacing.x - defFacing.x) + Math.Abs(attFacing.y-defFacing.y) < 0.5))
            {
                Log.Write(LogGroup.Attack, "Backstab!  Attacker facing: " + attFacing.x + ", " + attFacing.y + "," + attFacing.z + 
                                           ", Defender facing: " + defFacing.x + ", " + defFacing.y + "," + defFacing.z);
                modifiers += 30;
            }

            // End modifiers
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            var roll = Service.Random.Next(1, 100);
            var bonus = 10 * attackAttribute - 10 * defendAttribute + modifiers;
            // Update the hit roll and modifier to give player feedback.  
            // Hit roll is 1-100
            // Modifier is the delta between the attacker & target attributes, updated for any modifiers.
            // NWN can only display numbers up to 32.  So divide by 4 so that the numbers make sense.
            Log.Write(LogGroup.Attack, "Roll: " + roll + ", bonus: " + bonus);
            pAttackData.m_nToHitRoll = (byte) (roll / 4);
            pAttackData.m_nToHitMod = (byte) (bonus / 4);
            var result = roll - 50 + bonus;

            var criticalRange = 40;
            if (weapon != null)
            {
                var threatRange = Item.GetCriticalThreatRange((BaseItem)weapon.m_nBaseItem);
                if (threatRange == 3)
                {
                    criticalRange = 20;
                }
                else if (threatRange == 2)
                {
                    criticalRange = 30;
                }
            }

            if (HasImprovedCritical(attacker, weapon) == 1) criticalRange -= 10;

            if (result < 0)
            { 
                // Miss
                pAttackData.m_nAttackResult = 4;
                pAttackData.m_nMissedBy = (byte) Math.Abs(result); // Dunno if this is needed by anything, but filling it out in case.
            }
            else if (result >= criticalRange)
            {
                // Critical Hit - populate variables for feedback
                // Putting result - crit range here isn't great, as it will show (result - XX) + (original modifiers).  But good enough for now.
                pAttackData.m_bCriticalThreat = 1;
                pAttackData.m_nThreatRoll = (byte)(result - criticalRange);
                
                if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                {
                    // Immune!
                    var pData = new CNWCCMessageData();
                    pData.SetObjectID(0, attacker.m_idSelf);
                    pData.SetInteger(0, 126); //Critical Hit Immunity Feedback
                    pAttackData.m_alstPendingFeedback.Add(pData);
                    pAttackData.m_nAttackResult = 1;                    
                }
                else
                {
                    pAttackData.m_nAttackResult = 3;
                }

            }
            else
            {
                // Normal hit.
                pAttackData.m_nAttackResult = 1;
            }

            // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is 
            // accurate.
            attacker.ResolveDefensiveEffects(defender, result >= 0 ? 1 : 0) ;
        }


        private static int HasWeaponFocus(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_UnarmedStrike);
            }

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            // Unarmed strike (glove)
            if (baseItemType == BaseItem.Gloves &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_UnarmedStrike) == 1)
            {
                return 1;
            }

            // Creature weapons
            if (Item.CreatureBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_Creature) == 1)
            {
                return 1;
            }

            // Vibroblades
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusVibroblades) == 1)
            {
                return 1;
            }

            // Finesse Vibroblades
            if (Item.FinesseVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusFinesseVibroblades) == 1)
            {
                return 1;
            }

            // Lightsabers
            if (Item.LightsaberBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusLightsabers) == 1)
            {
                return 1;
            }

            // Heavy Vibroblades
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusHeavyVibroblades) == 1)
            {
                return 1;
            }

            // Polearms
            if (Item.PolearmBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusPolearms) == 1)
            {
                return 1;
            }

            // Twin Blades
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusTwinBlades) == 1)
            {
                return 1;
            }

            // Saberstaffs
            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusSaberstaffs) == 1)
            {
                return 1;
            }

            // Katars
            if (Item.KatarBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusKatars) == 1)
            {
                return 1;
            }

            // Staves
            if (Item.StaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_Staff) == 1)
            {
                return 1;
            }

            // Pistols
            if (Item.PistolBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusPistol) == 1)
            {
                return 1;
            }

            // Throwing Weapons
            if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusThrowingWeapons) == 1)
            {
                return 1;
            }

            // Rifles
            if (Item.RifleBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocusRifles) == 1)
            {
                return 1;
            }

            Log.Write(LogGroup.Attack, "No weapon focus feat found.");
            return 0;
        }


        private static int HasImprovedCritical(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_UnarmedStrike);
            }

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            // Unarmed strike (glove)
            if (baseItemType == BaseItem.Gloves &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCritical_UnarmedStrike) == 1)
            {
                return 1;
            }

            // Creature weapons
            if (Item.CreatureBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCritical_Creature) == 1)
            {
                return 1;
            }

            // Vibroblades
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalVibroblades) == 1)
            {
                return 1;
            }

            // Finesse Vibroblades
            if (Item.FinesseVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalFinesseVibroblades) == 1)
            {
                return 1;
            }

            // Lightsabers
            if (Item.LightsaberBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalLightsabers) == 1)
            {
                return 1;
            }

            // Heavy Vibroblades
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalHeavyVibroblades) == 1)
            {
                return 1;
            }

            // Polearms
            if (Item.PolearmBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalPolearms) == 1)
            {
                return 1;
            }

            // Twin Blades
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalTwinBlades) == 1)
            {
                return 1;
            }

            // Saberstaffs
            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalSaberstaffs) == 1)
            {
                return 1;
            }

            // Katars
            if (Item.KatarBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalKatars) == 1)
            {
                return 1;
            }

            // Staves
            if (Item.StaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCritical_Staff) == 1)
            {
                return 1;
            }

            // Pistols
            if (Item.PistolBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalPistol) == 1)
            {
                return 1;
            }

            // Throwing Weapons
            if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalThrowingWeapons) == 1)
            {
                return 1;
            }

            // Rifles
            if (Item.RifleBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCriticalRifles) == 1)
            {
                return 1;
            }

            Log.Write(LogGroup.Attack, "No improved critical feat found.");
            return 0;
        }

        private static int HasSuperiorWeaponFocus(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null) return 0;
            if (attacker.m_pStats.HasFeat((ushort)FeatType.SuperiorWeaponFocus) == 0) return 0;

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            if (Item.StaffBaseItemTypes.Contains(baseItemType)) return 1;
            if (Item.PolearmBaseItemTypes.Contains(baseItemType)) return 1;
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType)) return 1;

            return 0;
        }
    }
}