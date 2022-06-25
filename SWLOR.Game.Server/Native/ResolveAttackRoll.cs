using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.LogService;
using Ability = SWLOR.Game.Server.Service.Ability;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using FeatType = SWLOR.Game.Server.Core.NWScript.Enum.FeatType;
using ImmunityType = NWN.Native.API.ImmunityType;
using InventorySlot = NWN.Native.API.InventorySlot;
using ObjectType = NWN.Native.API.ObjectType;
using Random = SWLOR.Game.Server.Service.Random;

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
             * Custom attack logic for SWLOR. Most default NWN logic does not apply.
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
             */

            Log.Write(LogGroup.Attack, "Running OnResolveAttackRoll");
            var targetObject = CNWSObject.FromPointer(pTarget);
            if (targetObject == null)
                return;

            var attacker = CNWSCreature.FromPointer(thisPtr);
            var attackerStats = attacker.m_pStats;

            var pCombatRound = attacker.m_pcCombatRound;

            Log.Write(LogGroup.Attack, "Attacker: " + attacker.GetFirstName().GetSimple(0) + ", defender " + targetObject.GetFirstName().GetSimple(0));

            var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
            
            if (targetObject.m_nObjectType != (int)ObjectType.Creature)
            {
                // Automatically hit non-creature targets.  Do not apply criticals.
                Log.Write(LogGroup.Attack, "Placeable target.  Auto hit.");
                pAttackData.m_nAttackResult = 7; // Automatic hit.
                return;
            }

            // If we get to this point, we are fighting a creature.  Pull the target's stats.
            var defender = CNWSCreature.FromPointer(pTarget);

            var attackType = (uint)AttackType.Melee; 
            var weapon = pCombatRound.GetCurrentAttackWeapon();

            // Check whether this is a ranged weapon. 
            if (weapon != null && pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
            {
                attackType = (uint)AttackType.Ranged;
            }

            Log.Write(LogGroup.Attack, "Selected attack type " + attackType + ", weapon " + (weapon == null ? "none":weapon.GetFirstName().GetSimple(0)) );
            
            var strongStyleAbilityOverride = GetStrongStyleAbilityType(weapon, attacker);
            var zenMarksmanshipAbilityOverride = GetZenMarksmanshipAbilityType(weapon, attacker);
            var attackerAccuracy = Stat.GetAccuracyNative(attacker, weapon, 
                strongStyleAbilityOverride == AbilityType.Invalid 
                    ? zenMarksmanshipAbilityOverride 
                    : strongStyleAbilityOverride);
            var defenderEvasion = Stat.GetEvasionNative(defender);

            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            // Modifiers - put in modifiers here based on the type of attack (and type of weapon etc.).
            var modifiers = 0;

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

            // Effects - Attacker
            foreach (var effect in attacker.m_appliedEffects)
            {
                if (effect.m_nType == (int)EffectTypeEngine.AttackDecrease)
                {
                    modifiers -= 5 * effect.GetInteger(0);
                }
                else if (effect.m_nType == (int)EffectTypeEngine.AttackIncrease)
                {
                    modifiers += 5 * effect.GetInteger(0);
                }
            }

            // Effects - Defender
            foreach (var effect in defender.m_appliedEffects)
            {
                if (effect.m_nType == (int)EffectTypeEngine.ACDecrease)
                {
                    modifiers += 5 * effect.GetInteger(1);
                }
                else if (effect.m_nType == (int)EffectTypeEngine.ACIncrease)
                {
                    modifiers -= 5 * effect.GetInteger(1);
                }
            }

            // Weapon focus feats.
            modifiers += 5 * HasWeaponFocus(attacker, weapon);
            modifiers += 5 * HasSuperiorWeaponFocus(attacker, weapon);

            // Range bonuses and penalties.
            if (attackType == (uint)AttackType.Ranged)
            {
                var attackerPos = attacker.m_vPosition;
                var defenderPos = defender.m_vPosition;

                // Note - calculating distance solely via X/Y co-ordinates.  NWN doesn't have a true Z.
                var range = Math.Pow(Math.Pow((attackerPos.x - defenderPos.x), 2) + Math.Pow((attackerPos.y - defenderPos.y),2), 0.5);
                     
                Log.Write(LogGroup.Attack, "Ranged attack at range " + range);
                if (range < 5.0f)
                {
                    // Force powers or point blank shot feat make close range an advantage.
                    if (attacker.m_pStats.HasFeat((ushort)FeatType.PointBlankShot) == 1)
                    {
                        modifiers += 5;
                    }
                    else if (weapon != null)
                    {
                        modifiers -= 20;
                    }
                }
                else if (range > 40.0f)
                {
                    if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    {
                        modifiers -= 20;
                    }
                    else
                    {
                        modifiers -= 10;
                    }
                }
                else if (range > 30.0f)
                {
                    if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    {
                        modifiers -= 10;
                    }
                    else
                    {
                        modifiers -= 5;
                    }
                }
                else if (weapon != null && range > 20.0f && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                {
                    modifiers = -5;
                }
                
            }

            // Attacking from behind.  Does not apply to Force attacks.
            // Vectors are X=-1 to +1, Y=-1 to +1, Z=0.  
            // If the absolute difference between the two vectors is less than 0.5 radians, treat as a backstab.
            //
            // m_vOrientation does not update during combat, even if the creature is moving a lot, turning to attack
            // etc.  So cache the orientation we have when we attack, and only fall back to m_vOrientation if 
            // a creature hasn't attacked yet.  Clear these variables on PCs if not in combat in heartbeat.

            var defX = defender.m_ScriptVars.GetFloat(new CExoString("ATTACK_ORIENTATION_X"));
            var defY = defender.m_ScriptVars.GetFloat(new CExoString("ATTACK_ORIENTATION_Y"));

            if (defX == 0.0f && defY == 0.0f)
            {
                Log.Write(LogGroup.Attack, "Defender has not attacked yet, using pre-combat position.");
                var defFacing = defender.m_vOrientation;
                defX = (float)defFacing.x;
                defY = (float)defFacing.y;
            }

            var attX = defender.m_vPosition.x - attacker.m_vPosition.x;
            var attY = defender.m_vPosition.y - attacker.m_vPosition.y;

            attacker.m_ScriptVars.SetFloat(new CExoString("ATTACK_ORIENTATION_X"), attX);
            attacker.m_ScriptVars.SetFloat(new CExoString("ATTACK_ORIENTATION_Y"), attY);

            var delta = Math.Abs(Math.Atan2(attY, attX) - Math.Atan2(defY, defX));
            Log.Write(LogGroup.Attack, "Attacker facing is " + attX + ", " + attY);
            Log.Write(LogGroup.Attack, "Defender facing is " + defX + ", " + defY);

            if (delta <= 0.5)
            {
                Log.Write(LogGroup.Attack, "Backstab!  Attacker angle (radians): " + Math.Atan2(attY, attX) + 
                                           ", Defender angle (radians): " + Math.Atan2(defY, defX));
                modifiers += 30;
            }

            // Dual wield penalty.
            var offhand = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            var bDoubleWeapon =
                weapon != null &&
                (Item.TwinBladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                 Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem));
            var percentageModifier = 0;

            if (bDoubleWeapon ||
                (offhand != null &&
                 offhand.m_nBaseItem != (uint)BaseItem.LargeShield &&
                 offhand.m_nBaseItem != (uint)BaseItem.SmallShield &&
                 offhand.m_nBaseItem != (uint)BaseItem.TowerShield))
            {
                var logMessage = "Applying dual wield penalty.  Offhand weapon: " + (offhand == null ? weapon.GetFirstName().GetSimple() : offhand.GetFirstName().GetSimple() + " -");
                // Note - we have retired Two Weapon Fighting and Ambidexterity as feats.  We have costed them
                // in to the proficiency perks rather than granting them separately. 

                if (!bDoubleWeapon && Item.GetWeaponSize((BaseItem)offhand.m_nBaseItem) >= attacker.m_nCreatureSize)
                {
                    // Unless the offhand weapon size is smaller than the creature size (i.e. Small vs Medium), apply additional penalty. 
                    percentageModifier -= 10;
                    logMessage += "- offhand weapon is unwieldy -";
                }

                // Apply the base two weapon fighting penalty. 
                percentageModifier -= 20;
                Log.Write(LogGroup.Attack, logMessage);
            }

            // End modifiers
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            var attackRoll = Random.Next(1, 100);
            var hitRate = Combat.CalculateHitRate(attackerAccuracy + modifiers, defenderEvasion, percentageModifier);
            var isHit = attackRoll <= hitRate;

            Log.Write(LogGroup.Attack, $"attackerAccuracy = {attackerAccuracy}, modifiers = {modifiers}, defenderEvasion = {defenderEvasion}");
            Log.Write(LogGroup.Attack, $"Hit Rate: {hitRate}, Roll = {attackRoll}");

            // Hit
            if (isHit)
            {
                var criticalStat = attackType == (uint)AttackType.Ranged
                    ? attackerStats.m_nIntelligenceBase
                    : attackerStats.m_nDexterityBase;
                var criticalRoll = Random.Next(1, 100);
                var criticalBonus = HasImprovedCritical(attacker, weapon) == 1 ? 5 : 0;
                if (attackerStats.HasFeat((ushort)FeatType.PrecisionAim2) == 1)
                    criticalBonus += 4;
                else if (attackerStats.HasFeat((ushort)FeatType.PrecisionAim1) == 1)
                    criticalBonus += 2;

                var criticalRate = Combat.CalculateCriticalRate(criticalStat, defender.m_pStats.m_nIntelligenceBase, criticalBonus);

                // Critical
                if (criticalRoll <= criticalRate)
                {
                    Log.Write(LogGroup.Attack, $"Critical hit");

                    // Critical Hit - populate variables for feedback
                    pAttackData.m_bCriticalThreat = 1;
                    pAttackData.m_nThreatRoll = 1;

                    if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                    {
                        Log.Write(LogGroup.Attack, $"Immune to critical hits");
                        // Immune!
                        var pData = new CNWCCMessageData();
                        pData.SetObjectID(0, defender.m_idSelf);
                        pData.SetInteger(0, 126); //Critical Hit Immunity Feedback
                        pAttackData.m_alstPendingFeedback.Add(pData);
                        pAttackData.m_nAttackResult = 1;
                    }
                    else
                    {
                        Log.Write(LogGroup.Attack, $"Not immune to critical hits - dealing crit damage");
                        pAttackData.m_nAttackResult = 3;
                    }
                }
                // Regular Hit
                else
                {
                    Log.Write(LogGroup.Attack, $"Regular hit - attack result 1");
                    pAttackData.m_nAttackResult = 1;
                }
            }
            // Miss
            else
            {
                Log.Write(LogGroup.Attack, $"Miss - setting attack result to 4, missed by 0");
                pAttackData.m_nAttackResult = 4;
                pAttackData.m_nMissedBy = 1; // Dunno if this is needed by anything, but filling it out in case.
            }

            Log.Write(LogGroup.Attack, $"Resolving NWN defensive effects");
            // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is 
            // accurate.
            attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);

            Log.Write(LogGroup.Attack, $"Building combat log message");
            var message = Combat.BuildCombatLogMessage(
                (attacker.GetFirstName().GetSimple() + " " + attacker.GetLastName().GetSimple()).Trim(),
                (defender.GetFirstName().GetSimple() + " " + defender.GetLastName().GetSimple()).Trim(),
                pAttackData.m_nAttackResult,
                hitRate);
            attacker.SendFeedbackString(new CExoString(message));
            defender.SendFeedbackString(new CExoString(message));

            Log.Write(LogGroup.Attack, $"Setting pAttackData results");
            pAttackData.m_nToHitMod = 1;
            pAttackData.m_nToHitRoll = 1;

            Log.Write(LogGroup.Attack, $"Finished ResolveAttackRoll");
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

        private static AbilityType GetStrongStyleAbilityType(CNWSItem weapon, CNWSCreature attacker)
        {
            if (attacker.m_bPlayerCharacter == 0)
                return AbilityType.Invalid;

            if (weapon == null)
                return AbilityType.Invalid;

            var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();
            if (Item.LightsaberBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                if (Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
                    return AbilityType.Perception;
            }
            else if (Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                if (Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
                    return AbilityType.Perception;
            }

            return AbilityType.Invalid;
        }

        private static AbilityType GetZenMarksmanshipAbilityType(CNWSItem weapon, CNWSCreature attacker)
        {
            if (attacker.m_pStats.HasFeat((ushort)FeatType.ZenArchery) == 0)
                return AbilityType.Invalid;

            var baseItem = (BaseItem)weapon.m_nBaseItem;
            if (!Item.PistolBaseItemTypes.Contains(baseItem) &&
                !Item.RifleBaseItemTypes.Contains(baseItem) &&
                !Item.ThrowingWeaponBaseItemTypes.Contains(baseItem))
            {
                return AbilityType.Invalid;
            }

            var weaponAccuracy = Item.GetWeaponAccuracyAbilityType(baseItem);

            switch (weaponAccuracy)
            {
                case AbilityType.Perception when attacker.m_pStats.m_nWisdomBase > attacker.m_pStats.m_nDexterityBase:
                case AbilityType.Agility when attacker.m_pStats.m_nWisdomBase > attacker.m_pStats.m_nIntelligenceBase:
                    return AbilityType.Willpower;
                default:
                    return AbilityType.Invalid;
            }
        }
    }
}