using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using SWLOR.Component.Combat.Enums;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using FeatType = SWLOR.NWN.API.NWScript.Enum.FeatType;
using ILogger = SWLOR.Shared.Abstractions.Contracts.ILogger;
using ImmunityType = NWN.Native.API.ImmunityType;
using ObjectType = NWN.Native.API.ObjectType;

namespace SWLOR.Component.Combat.Feature.Native
{
    public static unsafe class ResolveAttackRoll
    {
        private static readonly IScriptExecutor _executor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IItemService _itemService = ServiceContainer.GetService<IItemService>();
        private static readonly IStatService _statService = ServiceContainer.GetService<IStatService>();
        private static readonly IRandomService _random = ServiceContainer.GetService<IRandomService>();
        private static readonly ICombatService _combatService = ServiceContainer.GetService<ICombatService>();
        private static readonly IAbilityService _abilityService = ServiceContainer.GetService<IAbilityService>();
        private static readonly ILogger _logger = ServiceContainer.GetService<ILogger>();

        // Attack result constants
        private const int AttackResultAutomaticHit = 7;
        private const int AttackResultRegularHit = 1;
        private const int AttackResultDeflect = 2;
        private const int AttackResultCriticalHit = 3;
        private const int AttackResultMiss = 4;

        // Combat mode constants
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;

        // Modifier constants
        private const int CircumstanceBonus = 5;
        private const int WeaponFocusBonus = 5;
        private const int SuperiorWeaponFocusBonus = 5;
        private const int PointBlankShotBonus = 5;
        private const int BackstabBonus = 30;
        private const int PowerAttackPenalty = -5;
        private const int ImprovedPowerAttackPenalty = -10;
        private const int CloseRangePenalty = -20;
        private const int LongRangePenalty = -20;
        private const int MediumRangePenalty = -10;
        private const int ShortRangePenalty = -5;
        private const int TwoWeaponPenalty = -10;
        private const int FlurryStylePenalty = -10;
        private const int DuelistBonus = 5;

        // Range constants
        private const float CloseRange = 5.0f;
        private const float ShortRange = 20.0f;
        private const float MediumRange = 30.0f;
        private const float LongRange = 40.0f;

        // Backstab constants
        private const float BackstabAngleThreshold = 0.5f;

        // Critical constants
        private const int ImprovedCriticalBonus = 5;
        private const int PrecisionAim1Bonus = 2;
        private const int PrecisionAim2Bonus = 4;
        private const int CrushingStyleBonus = 15;
        private const int CrushingMasteryBonus = 15;

        // Deflection constants
        private const int SaberDeflectChance = 5;
        private const int ShieldDeflectChance = 10;

        // NPC object ID constant
        private const uint NpcActionTargetId = 2130706432;

        // Default values
        private const int DefaultMissedBy = 1;
        private const int DefaultToHitMod = 1;
        private const int DefaultToHitRoll = 1;

        // Optimized weapon feat lookups
        private static readonly Dictionary<BaseItemType, FeatType> _weaponFocusLookup = CreateWeaponFocusLookup();
        private static readonly Dictionary<BaseItemType, FeatType> _improvedCriticalLookup = CreateImprovedCriticalLookup();

        private static Dictionary<BaseItemType, FeatType> CreateWeaponFocusLookup()
        {
            var lookup = new Dictionary<BaseItemType, FeatType>();

            void AddItems(IEnumerable<BaseItemType> items, FeatType feat)
            {
                foreach (var item in items)
                    lookup[item] = feat;
            }

            lookup[BaseItemType.Gloves] = FeatType.WeaponFocus_UnarmedStrike;
            AddItems(_itemService.CreatureBaseItemTypes, FeatType.WeaponFocus_Creature);
            AddItems(_itemService.VibrobladeBaseItemTypes, FeatType.WeaponFocusVibroblades);
            AddItems(_itemService.FinesseVibrobladeBaseItemTypes, FeatType.WeaponFocusFinesseVibroblades);
            AddItems(_itemService.LightsaberBaseItemTypes, FeatType.WeaponFocusLightsabers);
            AddItems(_itemService.HeavyVibrobladeBaseItemTypes, FeatType.WeaponFocusHeavyVibroblades);
            AddItems(_itemService.PolearmBaseItemTypes, FeatType.WeaponFocusPolearms);
            AddItems(_itemService.TwinBladeBaseItemTypes, FeatType.WeaponFocusTwinBlades);
            AddItems(_itemService.SaberstaffBaseItemTypes, FeatType.WeaponFocusSaberstaffs);
            AddItems(_itemService.KatarBaseItemTypes, FeatType.WeaponFocusKatars);
            AddItems(_itemService.StaffBaseItemTypes, FeatType.WeaponFocus_Staff);
            AddItems(_itemService.PistolBaseItemTypes, FeatType.WeaponFocusPistol);
            AddItems(_itemService.ThrowingWeaponBaseItemTypes, FeatType.WeaponFocusThrowingWeapons);
            AddItems(_itemService.RifleBaseItemTypes, FeatType.WeaponFocusRifles);

            return lookup;
        }

        private static Dictionary<BaseItemType, FeatType> CreateImprovedCriticalLookup()
        {
            var lookup = new Dictionary<BaseItemType, FeatType>();

            void AddItems(IEnumerable<BaseItemType> items, FeatType feat)
            {
                foreach (var item in items)
                    lookup[item] = feat;
            }

            lookup[BaseItemType.Gloves] = FeatType.ImprovedCritical_UnarmedStrike;
            AddItems(_itemService.CreatureBaseItemTypes, FeatType.ImprovedCritical_Creature);
            AddItems(_itemService.VibrobladeBaseItemTypes, FeatType.ImprovedCriticalVibroblades);
            AddItems(_itemService.FinesseVibrobladeBaseItemTypes, FeatType.ImprovedCriticalFinesseVibroblades);
            AddItems(_itemService.LightsaberBaseItemTypes, FeatType.ImprovedCriticalLightsabers);
            AddItems(_itemService.HeavyVibrobladeBaseItemTypes, FeatType.ImprovedCriticalHeavyVibroblades);
            AddItems(_itemService.PolearmBaseItemTypes, FeatType.ImprovedCriticalPolearms);
            AddItems(_itemService.TwinBladeBaseItemTypes, FeatType.ImprovedCriticalTwinBlades);
            AddItems(_itemService.SaberstaffBaseItemTypes, FeatType.ImprovedCriticalSaberstaffs);
            AddItems(_itemService.KatarBaseItemTypes, FeatType.ImprovedCriticalKatars);
            AddItems(_itemService.StaffBaseItemTypes, FeatType.ImprovedCritical_Staff);
            AddItems(_itemService.PistolBaseItemTypes, FeatType.ImprovedCriticalPistol);
            AddItems(_itemService.ThrowingWeaponBaseItemTypes, FeatType.ImprovedCriticalThrowingWeapons);
            AddItems(_itemService.RifleBaseItemTypes, FeatType.ImprovedCriticalRifles);

            return lookup;
        }
        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessedField.Local
        private static ResolveAttackRollHook _callOriginal;

        [ScriptHandler<OnModuleLoad>]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, void> pHook = &OnResolveAttackRoll;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<ResolveAttackRollHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
        {
            _executor.ExecuteInScriptContext(() =>
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

                var attacker = CNWSCreature.FromPointer(thisPtr);
                var area = attacker.GetArea();

                ProfilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnResolveAttackRoll)}",
                    "Area", area.m_sTag.ToString(),
                    "ObjectType", "Creature");

                _logger.Write<AttackLogGroup>("Running OnResolveAttackRoll");
                var targetObject = CNWSObject.FromPointer(pTarget);
                if (targetObject == null)
                {
                    ProfilerPlugin.PopPerfScope();
                    return;
                }

                var attackerStats = attacker.m_pStats;

                var pCombatRound = attacker.m_pcCombatRound;

                _logger.Write<AttackLogGroup>("Attacker: " + attacker.GetFirstName().GetSimple(0) + ", defender " + targetObject.GetFirstName().GetSimple(0));

                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

                if (targetObject.m_nObjectType != (int)ObjectType.Creature)
                {
                    // Automatically hit non-creature targets.  Do not apply criticals.
                    _logger.Write<AttackLogGroup>("Placeable target.  Auto hit.");
                    pAttackData.m_nAttackResult = AttackResultAutomaticHit;
                    ProfilerPlugin.PopPerfScope();
                    return;
                }

                // If we get to this point, we are fighting a creature.  Pull the target's stats.
                var defender = CNWSCreature.FromPointer(pTarget);

                if (pCombatRound.m_bRoundStarted == 1)
                {
                    defender.m_ScriptVars.SetInt(new CExoString("RESOLVE_ATTACK_ROLL_DEFLECT_BLASTER"), 0);
                }

                var attackType = (uint)AttackType.Melee;
                var weapon = pCombatRound.GetCurrentAttackWeapon();

                // Check whether this is a ranged weapon. 
                if (weapon != null && pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
                {
                    attackType = (uint)AttackType.Ranged;
                }

                _logger.Write<AttackLogGroup>("Selected attack type " + attackType + ", weapon " + (weapon == null ? "none" : weapon.GetFirstName().GetSimple(0)));

                var weaponStyleAbilityOverride = GetWeaponStyleAbilityType(weapon, attacker);
                var attackerAccuracy = _statService.GetAccuracyNative(attacker, weapon, weaponStyleAbilityOverride);
                var defenderEvasion = _statService.GetEvasionNative(defender);

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
                    oidTarget = (uint)defender.m_ScriptVars.GetInt(new CExoString("I_LAST_ATTACKED"));
                }

                // If this is an NPC attacking, Store the attack on the NPC.
                if (attacker.m_pActionQueue.GetItem(0).oidTarget == NpcActionTargetId)
                {
                    _logger.Write<AttackLogGroup>("NPC attacking - storing target " + defender.m_idSelf);
                    attacker.m_ScriptVars.SetInt(new CExoString("I_LAST_ATTACKED"), (int)defender.m_idSelf);
                }

                // oidTarget will be 0 for a newly spawned NPC who hasn't been attacked yet.  Don't let them get taken by surprise in round 1. 
                if (oidTarget != 0 && oidTarget != attacker.m_idSelf)
                {
                    _logger.Write<AttackLogGroup>("Defender current target (" + oidTarget + ") is not attacker (" + attacker.m_idSelf + "). Assign circumstance bonus");
                    accuracyModifiers += CircumstanceBonus;
                }

                // Weapon focus feats.
                accuracyModifiers += WeaponFocusBonus * HasWeaponFocus(attacker, weapon);
                accuracyModifiers += SuperiorWeaponFocusBonus * HasSuperiorWeaponFocus(attacker, weapon);

                // Range bonuses and penalties
                accuracyModifiers += CalculateRangeModifiers(attackType, attacker, defender, weapon);

                // Backstab bonus calculation
                accuracyModifiers += CalculateBackstabBonus(attacker, defender);

                // Dual wield and weapon style modifiers
                var percentageModifier = CalculateDualWieldAndStyleModifiers(attacker, weapon);

                // Combat Mode - Power Attack (-5 ACC)
                if (attacker.m_nCombatMode == PowerAttackMode)
                {
                    accuracyModifiers += PowerAttackPenalty;
                    _logger.Write<AttackLogGroup>($"Applying Power Attack penalty: {PowerAttackPenalty}");
                }
                // Combat Mode - Improved Power Attack (-10 ACC)
                else if (attacker.m_nCombatMode == ImprovedPowerAttackMode)
                {
                    accuracyModifiers += ImprovedPowerAttackPenalty;
                    _logger.Write<AttackLogGroup>($"Applying Imp. Power Attack penalty: {ImprovedPowerAttackPenalty}");
                }

                // End modifiers
                //---------------------------------------------------------------------------------------------
                //---------------------------------------------------------------------------------------------
                //---------------------------------------------------------------------------------------------
                var attackRoll = _random.Next(1, 100);
                var hitRate = _combatService.CalculateHitRate(attackerAccuracy + accuracyModifiers, defenderEvasion, percentageModifier);
                var isHit = attackRoll <= hitRate;

                _logger.Write<AttackLogGroup>($"attackerAccuracy = {attackerAccuracy}, modifiers = {accuracyModifiers}, defenderEvasion = {defenderEvasion}");
                _logger.Write<AttackLogGroup>($"Hit Rate: {hitRate}, Roll = {attackRoll}");

                // Check for deflection
                var deflected = CheckDeflection(attackType, isHit, attacker, defender);
                if (deflected)
                    isHit = false;

                // Hit
                if (isHit)
                {
                    var criticalStat = attackerStats.GetDEXStat();
                    var criticalRoll = _random.Next(1, 100);
                    var criticalBonus = CalculateCriticalHitBonus(attacker, weapon);
                    var criticalRate = _combatService.CalculateCriticalRate(criticalStat, defender.m_pStats.GetSTRStat(), criticalBonus);

                    // Critical
                    if (criticalRoll <= criticalRate)
                    {
                        _logger.Write<AttackLogGroup>($"Critical hit");

                        // Critical Hit - populate variables for feedback
                        pAttackData.m_bCriticalThreat = 1;
                        pAttackData.m_nThreatRoll = 1;

                        if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                        {
                            _logger.Write<AttackLogGroup>($"Immune to critical hits");
                            // Immune!
                            var defenderName = (defender.GetFirstName().GetSimple() + " " + defender.GetLastName().GetSimple()).Trim();
                            attacker.SendFeedbackString(new CExoString($"{defenderName} is immune to critical hits!"));
                            pAttackData.m_nAttackResult = AttackResultRegularHit;
                        }
                        else
                        {
                            _logger.Write<AttackLogGroup>($"Not immune to critical hits - dealing crit damage");
                            pAttackData.m_nAttackResult = AttackResultCriticalHit;
                        }
                    }
                    // Regular Hit
                    else
                    {
                        _logger.Write<AttackLogGroup>($"Regular hit - attack result 1");
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                    }
                }
                // Miss
                else
                {
                    if (deflected)
                    {
                        _logger.Write<AttackLogGroup>($"Deflected - setting attack result to 2");
                        pAttackData.m_nAttackResult = AttackResultDeflect;
                    }
                    else
                    {
                        _logger.Write<AttackLogGroup>($"Miss - setting attack result to 4, missed by 0");
                        pAttackData.m_nAttackResult = AttackResultMiss;
                    }
                    pAttackData.m_nMissedBy = DefaultMissedBy;
                }

                _logger.Write<AttackLogGroup>($"Resolving NWN defensive effects");
                // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is 
                // accurate.
                attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);

                _logger.Write<AttackLogGroup>($"Building combat log message");
                var message = _combatService.BuildCombatLogMessageNative(
                    attacker,
                    defender,
                    pAttackData.m_nAttackResult,
                    hitRate);
                attacker.SendFeedbackString(new CExoString(message));
                defender.SendFeedbackString(new CExoString(message));

                _logger.Write<AttackLogGroup>($"Setting pAttackData results");
                pAttackData.m_nToHitMod = DefaultToHitMod;
                pAttackData.m_nToHitRoll = DefaultToHitRoll;

                _logger.Write<AttackLogGroup>($"Finished ResolveAttackRoll");

                ProfilerPlugin.PopPerfScope();
            });
        }

        private static int HasWeaponFocus(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.WeaponFocus_UnarmedStrike);
            }

            var baseItemType = (BaseItemType)weapon.m_nBaseItem;
            if (_weaponFocusLookup.TryGetValue(baseItemType, out var feat))
            {
                return attacker.m_pStats.HasFeat((ushort)feat);
            }

            _logger.Write<AttackLogGroup>("No weapon focus feat found.");
            return 0;
        }

        private static int HasImprovedCritical(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedCritical_UnarmedStrike);
            }

            var baseItemType = (BaseItemType)weapon.m_nBaseItem;
            if (_improvedCriticalLookup.TryGetValue(baseItemType, out var feat))
            {
                return attacker.m_pStats.HasFeat((ushort)feat);
            }

            _logger.Write<AttackLogGroup>("No improved critical feat found.");
            return 0;
        }

        private static int HasSuperiorWeaponFocus(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null) return 0;
            if (attacker.m_pStats.HasFeat((ushort)FeatType.SuperiorWeaponFocus) == 0) return 0;

            var baseItemType = (BaseItemType)weapon.m_nBaseItem;

            if (_itemService.StaffBaseItemTypes.Contains(baseItemType)) return 1;
            if (_itemService.PolearmBaseItemTypes.Contains(baseItemType)) return 1;
            if (_itemService.HeavyVibrobladeBaseItemTypes.Contains(baseItemType)) return 1;

            return 0;
        }

        private static int CalculateRangeModifiers(uint attackType, CNWSCreature attacker, CNWSCreature defender, CNWSItem weapon)
        {
            if (attackType != (uint)AttackType.Ranged)
                return 0;

            var attackerPos = attacker.m_vPosition;
            var defenderPos = defender.m_vPosition;

            // Calculate distance using X/Y coordinates only
            var range = Math.Sqrt(Math.Pow(attackerPos.x - defenderPos.x, 2) + Math.Pow(attackerPos.y - defenderPos.y, 2));

            _logger.Write<AttackLogGroup>($"Ranged attack at range {range}");

            // Close range (under 5.0)
            if (range < CloseRange)
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.PointBlankShot) == 1)
                    return PointBlankShotBonus;
                else if (weapon != null)
                    return CloseRangePenalty;
            }
            // Long range (over 40.0)
            else if (range > LongRange)
            {
                if (weapon != null && !_itemService.RifleBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
                    return LongRangePenalty;
                else
                    return MediumRangePenalty;
            }
            // Medium range (30.0 - 40.0)
            else if (range > MediumRange)
            {
                if (weapon != null && !_itemService.RifleBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
                    return MediumRangePenalty;
                else
                    return ShortRangePenalty;
            }
            // Short range (20.0 - 30.0)
            else if (weapon != null && range > ShortRange && !_itemService.RifleBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
            {
                return ShortRangePenalty;
            }

            return 0;
        }

        private static int CalculateBackstabBonus(CNWSCreature attacker, CNWSCreature defender)
        {
            // Get cached defender orientation or fall back to pre-combat position
            var defX = defender.m_ScriptVars.GetFloat(new CExoString("ATTACK_ORIENTATION_X"));
            var defY = defender.m_ScriptVars.GetFloat(new CExoString("ATTACK_ORIENTATION_Y"));

            if (defX == 0.0f && defY == 0.0f)
            {
                _logger.Write<AttackLogGroup>("Defender has not attacked yet, using pre-combat position.");
                var defFacing = defender.m_vOrientation;
                defX = (float)defFacing.x;
                defY = (float)defFacing.y;
            }

            // Calculate attacker's position relative to defender
            var attX = defender.m_vPosition.x - attacker.m_vPosition.x;
            var attY = defender.m_vPosition.y - attacker.m_vPosition.y;

            // Cache attacker's orientation for future rounds
            attacker.m_ScriptVars.SetFloat(new CExoString("ATTACK_ORIENTATION_X"), attX);
            attacker.m_ScriptVars.SetFloat(new CExoString("ATTACK_ORIENTATION_Y"), attY);

            // Calculate angle difference
            var delta = Math.Abs(Math.Atan2(attY, attX) - Math.Atan2(defY, defX));

            _logger.Write<AttackLogGroup>($"Attacker facing is {attX}, {attY}");
            _logger.Write<AttackLogGroup>($"Defender facing is {defX}, {defY}");

            if (delta <= BackstabAngleThreshold)
            {
                _logger.Write<AttackLogGroup>($"Backstab! Attacker angle (radians): {Math.Atan2(attY, attX)}, " +
                                          $"Defender angle (radians): {Math.Atan2(defY, defX)}");
                return BackstabBonus;
            }

            return 0;
        }

        private static int CalculateDualWieldAndStyleModifiers(CNWSCreature attacker, CNWSItem weapon)
        {
            var percentageModifier = 0;
            var offhand = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);

            if (weapon == null) return percentageModifier;

            var bDoubleWeapon = _itemService.TwinBladeBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem) ||
                               _itemService.SaberstaffBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem);
            var hasImprovedTwoWeaponFighting = attacker.m_pStats.HasFeat((ushort)FeatType.ImprovedTwoWeaponFighting) == 1;
            var isShieldEquipped = offhand != null && _itemService.ShieldBaseItemTypes.Contains((BaseItemType)offhand.m_nBaseItem);
            var isDualKatarsEquipped = _itemService.KatarBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem) &&
                                      offhand != null && _itemService.KatarBaseItemTypes.Contains((BaseItemType)offhand.m_nBaseItem);

            // Apply dual wield penalty
            if (bDoubleWeapon || !isShieldEquipped || !isDualKatarsEquipped)
            {
                if (!hasImprovedTwoWeaponFighting || weapon == offhand)
                    percentageModifier += TwoWeaponPenalty;

                var logMessage = $"Applying dual wield penalty. Offhand weapon: {(offhand?.GetFirstName().GetSimple() ?? weapon?.GetFirstName().GetSimple())}: {percentageModifier}";
                _logger.Write<AttackLogGroup>(logMessage);
            }

            // Staff Flurry penalty
            if (_itemService.StaffBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.FlurryStyle) == 1 &&
                attacker.m_pStats.HasFeat((ushort)FeatType.FlurryMastery) == 0)
            {
                percentageModifier += FlurryStylePenalty;
                _logger.Write<AttackLogGroup>($"Applying Flurry Style I penalty: {FlurryStylePenalty}%");
            }

            // Duelist bonus
            if (attacker.m_pStats.HasFeat((ushort)FeatType.Duelist) == 1 &&
                (_itemService.OneHandedMeleeItemTypes.Contains((BaseItemType)weapon.m_nBaseItem) ||
                 _itemService.ThrowingWeaponBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem)))
            {
                var isDuelistValid = offhand == null || _itemService.ShieldBaseItemTypes.Contains((BaseItemType)offhand.m_nBaseItem);
                if (isDuelistValid)
                {
                    percentageModifier += DuelistBonus;
                    _logger.Write<AttackLogGroup>($"Applying Duelist bonus: +{DuelistBonus}%");
                }
            }

            return percentageModifier;
        }

        private static bool CheckDeflection(uint attackType, bool isHit, CNWSCreature attacker, CNWSCreature defender)
        {
            var hasDeflected = defender.m_ScriptVars.GetInt(new CExoString("RESOLVE_ATTACK_ROLL_DEFLECT_BLASTER"));

            if (attackType != (uint)AttackType.Ranged || !isHit || hasDeflected != 0)
                return false;

            var defenderWeapon = defender.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
            var defenderOffhand = defender.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            var saberBlock = defenderWeapon != null && _itemService.LightsaberBaseItemTypes.Contains((BaseItemType)defenderWeapon.m_nBaseItem);
            var shieldBlock = defenderOffhand != null &&
                             defender.m_pStats.HasFeat((ushort)FeatType.Bulwark) == 1 &&
                             _itemService.ShieldBaseItemTypes.Contains((BaseItemType)defenderOffhand.m_nBaseItem);

            if (!saberBlock && !shieldBlock)
                return false;

            defender.m_ScriptVars.SetInt(new CExoString("RESOLVE_ATTACK_ROLL_DEFLECT_BLASTER"), 1);

            var deflectRoll = _random.Next(1, 100);
            var deflectChance = 0;

            if (saberBlock) deflectChance += SaberDeflectChance;
            if (shieldBlock) deflectChance += ShieldDeflectChance;

            var deflected = deflectRoll <= deflectChance;

            var feedbackString = deflected ? "*success*" : "*failure*";
            var attackerName = ColorToken.GetNameColorNative(attacker);
            var defenderName = ColorToken.GetNameColorNative(defender);
            feedbackString = ColorToken.Combat($"{defenderName} attempts to deflect {attackerName}'s ranged attack: {feedbackString}");

            attacker.SendFeedbackString(new CExoString(feedbackString));
            defender.SendFeedbackString(new CExoString(feedbackString));
            _logger.Write<AttackLogGroup>($"Deflect roll: {deflectRoll}, Hit: {!deflected}");

            return deflected;
        }

        private static int CalculateCriticalHitBonus(CNWSCreature attacker, CNWSItem weapon)
        {
            var criticalBonus = Math.Clamp((20 - attacker.m_pStats.GetCriticalHitRoll()) * 5, 0, 100);
            _logger.Write<AttackLogGroup>($"Base crit threat identified as: {criticalBonus}");

            criticalBonus += HasImprovedCritical(attacker, weapon) == 1 ? ImprovedCriticalBonus : 0;

            if (attacker.m_pStats.HasFeat((ushort)FeatType.PrecisionAim2) == 1)
                criticalBonus += PrecisionAim2Bonus;
            else if (attacker.m_pStats.HasFeat((ushort)FeatType.PrecisionAim1) == 1)
                criticalBonus += PrecisionAim1Bonus;

            // Staff crushing bonuses
            if (weapon != null && _itemService.StaffBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingMastery) == 1)
                    criticalBonus += CrushingMasteryBonus;
                else if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingStyle) == 1)
                    criticalBonus += CrushingStyleBonus;
            }

            return criticalBonus;
        }

        private static AbilityType GetWeaponStyleAbilityType(CNWSItem weapon, CNWSCreature attacker)
        {
            if (attacker.m_bPlayerCharacter == 0)
                return AbilityType.Invalid;

            if (weapon == null)
                return AbilityType.Invalid;

            var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();
            if (_itemService.LightsaberBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
            {
                if (_abilityService.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
                    return AbilityType.Perception;
            }
            else if (_itemService.SaberstaffBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
            {
                if (_abilityService.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
                    return AbilityType.Perception;
            }
            else if (_itemService.StaffBaseItemTypes.Contains((BaseItemType)weapon.m_nBaseItem))
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.FlurryStyle) == 1)
                    return AbilityType.Agility;
            }

            return AbilityType.Invalid;
        }
    }
}