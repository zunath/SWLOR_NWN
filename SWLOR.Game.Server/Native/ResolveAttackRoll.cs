using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
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

            ProfilerPlugin.PushPerfScope($"NATIVE:{nameof(OnResolveAttackRoll)}", "RunScript", "Script");

            Log.Write(LogGroup.Attack, "Running OnResolveAttackRoll");
            var targetObject = CNWSObject.FromPointer(pTarget);
            if (targetObject == null)
            {
                ProfilerPlugin.PopPerfScope();
                return;
            }

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
                ProfilerPlugin.PopPerfScope();
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
            
            var weaponStyleAbilityOverride = GetWeaponStyleAbilityType(weapon, attacker);
            var attackerAccuracy = Stat.GetAccuracyNative(attacker, weapon, weaponStyleAbilityOverride);
            var defenderEvasion = Stat.GetEvasionNative(defender);

            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            // Modifiers - put in modifiers here based on the type of attack (and type of weapon etc.).
            var accuracyModifiers = 0;

            // Defender not targeting the attacker.
            // Dev note: the GetItem method always creates a new instance of CNWActionNode so there should be no NPEs.
            // Note: this always returns object invalid for NPCs (2130706432) as their actions aren't represented the same way.
            var oidTarget = defender.m_pActionQueue.GetItem(0).oidTarget;
            
            if (oidTarget == OBJECT_INVALID)
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
                accuracyModifiers += 5;
            }

            // Weapon focus feats.
            accuracyModifiers += 5 * HasWeaponFocus(attacker, weapon);
            accuracyModifiers += 5 * HasSuperiorWeaponFocus(attacker, weapon);

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
                        accuracyModifiers += 5;
                    }
                    else if (weapon != null)
                    {
                        accuracyModifiers -= 20;
                    }
                }
                else if (range > 40.0f)
                {
                    if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    {
                        accuracyModifiers -= 20;
                    }
                    else
                    {
                        accuracyModifiers -= 10;
                    }
                }
                else if (range > 30.0f)
                {
                    if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    {
                        accuracyModifiers -= 10;
                    }
                    else
                    {
                        accuracyModifiers -= 5;
                    }
                }
                else if (weapon != null && range > 20.0f && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                {
                    accuracyModifiers = -5;
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
                accuracyModifiers += 30;
            }

            // Dual wield penalty.
            var offhand = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            var bDoubleWeapon =
                weapon != null &&
                (Item.TwinBladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                 Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem));
            var hasImprovedTwoWeaponFighting = attackerStats.HasFeat((ushort)FeatType.ImprovedTwoWeaponFighting) == 1;
            var isShieldEquipped = offhand != null && Item.ShieldBaseItemTypes.Contains((BaseItem)offhand.m_nBaseItem);
            var isDualKatarsEquipped = weapon != null && Item.KatarBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) &&
                                       offhand != null && Item.KatarBaseItemTypes.Contains((BaseItem)offhand.m_nBaseItem);
            var percentageModifier = 0;

            if (weapon != null && (bDoubleWeapon || !isShieldEquipped || !isDualKatarsEquipped))
            {
                // Apply the base two weapon fighting penalty.
                if(!hasImprovedTwoWeaponFighting || weapon == offhand) // Main-hand ITWF has no penalty.
                    percentageModifier -= 10;

                var logMessage = "Applying dual wield penalty.  Offhand weapon: " + (offhand == null ? weapon?.GetFirstName().GetSimple() : offhand?.GetFirstName().GetSimple() + ": " + percentageModifier);
                Log.Write(LogGroup.Attack, logMessage);
            }

            if (weapon != null)
            {
                // Staff Flurry - (-10% TH)
                if (Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) &&
                    attackerStats.HasFeat((ushort)FeatType.FlurryStyle) == 1 &&
                    attackerStats.HasFeat((ushort)FeatType.FlurryMastery) == 0)
                {
                    percentageModifier -= 10;
                    Log.Write(LogGroup.Attack, "Applying Flurry Style I penalty: -10%");
                }

                // Duelist - (+5% TH)
                if(attackerStats.HasFeat((ushort)FeatType.Duelist) == 1)
                    if (Item.OneHandedMeleeItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                        Item.ThrowingWeaponBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    {
                        var isDuelistValid = offhand == null || Item.ShieldBaseItemTypes.Contains((BaseItem)offhand.m_nBaseItem);

                        if (isDuelistValid)
                        {
                            percentageModifier += 5;
                            Log.Write(LogGroup.Attack, "Applying Duelist bonus: +5%");
                        }
                    }
            }

            // Combat Mode - Power Attack (-5 ACC)
            if (attacker.m_nCombatMode == 2)
            {
                accuracyModifiers -= 5;
                Log.Write(LogGroup.Attack, "Applying Power Attack penalty: -5");
            }
            // Combat Mode - Improved Power Attack (-10 ACC)
            else if (attacker.m_nCombatMode == 3)
            {
                accuracyModifiers -= 10;
                Log.Write(LogGroup.Attack, "Applying Imp. Power Attack penalty: -10");
            }

            // End modifiers
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------
            var attackRoll = Random.Next(1, 100);
            var hitRate = Combat.CalculateHitRate(attackerAccuracy + accuracyModifiers, defenderEvasion, percentageModifier);
            var isHit = attackRoll <= hitRate;

            Log.Write(LogGroup.Attack, $"attackerAccuracy = {attackerAccuracy}, modifiers = {accuracyModifiers}, defenderEvasion = {defenderEvasion}");
            Log.Write(LogGroup.Attack, $"Hit Rate: {hitRate}, Roll = {attackRoll}");

            var defenderWeapon = defender.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
            var defenderOffhand = defender.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            var saberBlock = false;
            var shieldBlock = false;

            var defenderPER = defender.m_pStats.GetDEXStat();
            var defenderVIT = defender.m_pStats.GetCONStat();

            if(defenderWeapon != null && (
                (BaseItem)defenderWeapon.m_nBaseItem == BaseItem.Lightsaber ||
                (BaseItem)defenderWeapon.m_nBaseItem == BaseItem.Saberstaff))
            {
                saberBlock = true;
            }

            // Checking for Bulwark shield reflect - need to have the feat and a shield equipped
            if (defenderOffhand != null)
                shieldBlock = defender.m_pStats.HasFeat((ushort)FeatType.Bulwark) == 1 &&
                    Item.ShieldBaseItemTypes.Contains((BaseItem)defenderOffhand.m_nBaseItem);

            // Deflect Ranged Attacks
            var deflected = false;

            if (attackType == (uint)AttackType.Ranged &&            // Ranged Attacks only
                isHit && defender.GetFlatFooted() == 0 &&           // Only triggers on hits and the defender isn't incapacitated
                defender.m_pcCombatRound.m_bDeflectArrow == 0 &&    // Can only trigger once per combat round
                (shieldBlock || saberBlock))                        // Must have either a lightsaber or Bulwark + a shield equipped
            {
                var defenderStat = saberBlock ? defenderPER : defenderVIT;
                if (shieldBlock && saberBlock && (defenderVIT > defenderPER))
                    defenderStat = defenderVIT;

                defender.m_pcCombatRound.SetDeflectArrow(1);        // We set the Deflect Arrow var to true for this round so it doesn't fire again

                var deflectRoll = Random.Next(1, 100);
                var baseItemType = weapon == null ? BaseItem.Invalid : (BaseItem)weapon.m_nBaseItem;
                var attackerStat = weaponStyleAbilityOverride == AbilityType.Invalid 
                    ? Item.GetWeaponAccuracyAbilityType(baseItemType) 
                    : weaponStyleAbilityOverride;
                var attackerStatValue = Stat.GetStatValueNative(attacker, attackerStat);

                var statDelta = Math.Clamp((defenderStat - attackerStatValue) * 5, -50, 75);

                isHit = deflectRoll + statDelta < attackRoll;
                deflected = !isHit;

                var feedbackString = deflected ? "*success*" : "*failure*";
                var attackerName = ColorToken.GetNameColorNative(attacker);
                var defenderName = ColorToken.GetNameColorNative(defender);
                feedbackString = $"{defenderName} attempts to deflect {attackerName}'s ranged attack: {feedbackString}";

                attacker.SendFeedbackString(new CExoString(feedbackString));
                defender.SendFeedbackString(new CExoString(feedbackString));
                Log.Write(LogGroup.Attack, $"Deflect roll: {deflectRoll}, statDelta: {statDelta}, attackRoll: {attackRoll} -- Hit: {isHit}");
            }

            // Hit
            if (isHit)
            {
                var criticalStat = attackType == (uint)AttackType.Ranged
                    ? attackerStats.GetINTStat()
                    : attackerStats.GetDEXStat();
                var criticalRoll = Random.Next(1, 100);
                var criticalBonus = Math.Clamp((20 - attacker.m_pStats.GetCriticalHitRoll()) * 5, 0, 100); // GetCriticalHitRoll() returns the lowest d20 value that results in a crit, so we convert that to % bonus
                Log.Write(LogGroup.Attack, $"Base crit threat identified as: {criticalBonus}");
                criticalBonus += HasImprovedCritical(attacker, weapon) == 1 ? 5 : 0;
                if (attackerStats.HasFeat((ushort)FeatType.PrecisionAim2) == 1)
                    criticalBonus += 4;
                else if (attackerStats.HasFeat((ushort)FeatType.PrecisionAim1) == 1)
                    criticalBonus += 2;

                if(weapon != null && Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                {
                    if(attacker.m_pStats.HasFeat((ushort)FeatType.CrushingMastery) == 1)
                    {
                        criticalBonus += 15;
                    } 
                    if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingStyle) == 1)
                    {
                        criticalBonus += 15;
                    }
                }

                var criticalRate = Combat.CalculateCriticalRate(criticalStat, defender.m_pStats.GetINTStat(), criticalBonus);

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
                        var defenderName = (defender.GetFirstName().GetSimple() + " " + defender.GetLastName().GetSimple()).Trim();
                        attacker.SendFeedbackString(new CExoString($"{defenderName} is immune to critical hits!"));
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
                if(deflected)
                {
                    Log.Write(LogGroup.Attack, $"Deflected - setting attack result to 2");
                    pAttackData.m_nAttackResult = 2;
                } 
                else
                {
                    Log.Write(LogGroup.Attack, $"Miss - setting attack result to 4, missed by 0");
                    pAttackData.m_nAttackResult = 4;
                }
                pAttackData.m_nMissedBy = 1; // Dunno if this is needed by anything, but filling it out in case.
            }

            Log.Write(LogGroup.Attack, $"Resolving NWN defensive effects");
            // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is 
            // accurate.
            attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);

            Log.Write(LogGroup.Attack, $"Building combat log message");
            var message = Combat.BuildCombatLogMessageNative(
                attacker,
                defender,
                pAttackData.m_nAttackResult,
                hitRate);
            attacker.SendFeedbackString(new CExoString(message));
            defender.SendFeedbackString(new CExoString(message));

            Log.Write(LogGroup.Attack, $"Setting pAttackData results");
            pAttackData.m_nToHitMod = 1;
            pAttackData.m_nToHitRoll = 1;

            Log.Write(LogGroup.Attack, $"Finished ResolveAttackRoll");

            ProfilerPlugin.PopPerfScope();
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

        private static AbilityType GetWeaponStyleAbilityType(CNWSItem weapon, CNWSCreature attacker)
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
            else if (Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.FlurryStyle) == 1)
                    return AbilityType.Agility;
            }

            return AbilityType.Invalid;
        }
    }
}