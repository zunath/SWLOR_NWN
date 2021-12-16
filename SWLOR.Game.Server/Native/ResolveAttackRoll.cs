using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

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

            var hookPtr = Internal.NativeFunctions.RequestHook(new IntPtr(FunctionsLinux._ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject), (IntPtr)pHook, -1000000);
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

            Console.WriteLine("Running OnResolveAttackRoll");
            var targetObject = CNWSObject.FromPointer(pTarget);
            if (targetObject == null)
                return;

            var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
            var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
            CNWSCombatRound pCombatRound = attacker.m_pcCombatRound;
            CNWSCombatAttackData pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

            if (targetObject.m_nObjectType != (int)ObjectType.Creature)
            {
                // Automatically hit non-creature targets.  Do not apply criticals.
                pAttackData.m_nAttackResult = 7; // Automatic hit.
                return;
            }

            // If we get to this point, we are fighting a creature.  Pull the target's stats.
            CNWSCreatureStats defenderStats = CNWSCreatureStats.FromPointer(pTarget);
            CNWSCreature defender = CNWSCreature.FromPointer(defenderStats.m_pBaseCreature);

            // Determine the type of attack.
            // - Check for an override variable on the creature.  If set, remove it and use that value.  This is 
            //   to allow abilities to specify the type of attack roll they make. 
            // - If no override, check the weapon used to make the attack (main or offhand).
            // - Check the weapon for an override variable.  If it exists, use it (but don't remove it). 
            // - Else, pick Melee (Might) for melee weapons and Ranged (Perception) for ranged weapons.
            // - Spirit (Willpower) attacks can only be selected by variable.
            var attackType = (uint)AttackType.Melee; // Default to melee.
            CNWSItem weapon = null;

            var attackOverride = attacker.m_ScriptVars.GetInt(new CExoString("ATTACK_TYPE_OVERRIDE"));

            if (attackOverride != 0 & attackOverride < 4)
            {
                attackType = (uint) attackOverride;
                attacker.m_ScriptVars.DestroyInt(new CExoString("ATTACK_TYPE_OVERRIDE"));
            }
            else
            {
                // Get a reference to the weapon being used.
                // First, figure out what sort of attack we're doing.  From NWN.Native.API.WeaponAttackType 
                // MainhandWeapon = 1,
                // OffhandWeapon = 2,
                // CreatureLeftWeapon = 3,
                // CreatureRightWeapon = 4,
                // CreatureBiteWeapon = 5,
                // AdditionalWeapon = 6,
                // Unarmed = 7,
                // AdditionalUnarmed = 8 
                uint nativeAttackType = pAttackData.m_nWeaponAttackType;

                switch (nativeAttackType)
                {
                    case 1:
                    case 6:
                        weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
                        break;
                    case 2:
                        weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
                        break;
                    case 3:
                        weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponLeft);
                        break;
                    case 4:
                        weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponRight);
                        break;
                    case 5:
                        weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponBite);
                        break;
                    case 7:
                    case 8:
                        // No weapon.
                        break;
                }

                // If no weapon, use default attack type (Might).  Otherwise, look further.
                if (weapon != null)
                {
                    attackOverride = weapon.m_ScriptVars.GetInt(new CExoString("ATTACK_TYPE_OVERRIDE"));

                    if (attackOverride != 0 & attackOverride < 4)
                    {
                        attackType = (uint)attackOverride;
                    }
                    else if (pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
                    {
                        attackType = (uint) AttackType.Ranged;
                    }
                }
            }

            // We now have our attack type defined.  Pull the relevant attributes, defaulting to melee.
            var attackAttribute = attackerStats.m_nStrengthModifier;
            var defendAttribute = defenderStats.m_nStrengthModifier;

            switch (attackType)
            {
                case 2:
                    attackAttribute = attackerStats.m_nDexterityModifier;
                    defendAttribute = defenderStats.m_nDexterityModifier;
                    break;
                case 3:
                    attackAttribute = attackerStats.m_nWisdomModifier;
                    defendAttribute = defenderStats.m_nWisdomModifier;
                    break;
            }

            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            // Modifiers - put in modifiers here based on the type of attack (and type of weapon etc.).
            int modifiers = 0;
            if (defender.m_nState == 6) // Stunned
            {
                defendAttribute = 0;
            }

            // End modifiers
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            int roll = Service.Random.Next(1, 100);
            int bonus = attackAttribute - defendAttribute + modifiers;
            // Update the hit roll and modifier to give player feedback.  
            // Hit roll is 1-100
            // Modifier is the delta between the attacker & target attributes, updated for any modifiers.
            pAttackData.m_nToHitRoll = (byte)roll;
            pAttackData.m_nToHitMod = (byte) bonus;
            int result = roll - 50 + bonus;

            if (result < 0)
            {
                // Miss
                pAttackData.m_nAttackResult = 4;
                pAttackData.m_nMissedBy = (byte) Math.Abs(result); // Dunno if this is needed by anything, but filling it out in case.
            }
            else if (result >= 30)
            {
                // Critical Hit - populate variables for feedback
                // Putting result - 30 here isn't great, as it will show (result - 30) + (original modifiers).  But good enough for now.
                pAttackData.m_bCriticalThreat = 1;
                pAttackData.m_nThreatRoll = (byte)(result - 30);
                
                if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                {
                    // Immune!
                    CNWCCMessageData pData = new CNWCCMessageData();
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
    }
}