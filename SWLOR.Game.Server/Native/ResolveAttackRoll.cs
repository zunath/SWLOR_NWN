using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using System.Runtime.InteropServices;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using ImmunityType = NWN.Native.API.ImmunityType;
using ObjectType = NWN.Native.API.ObjectType;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ResolveAttackRoll
    {
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
        private const int PowerAttackPenalty = -5;
        private const int ImprovedPowerAttackPenalty = -10;
        private const int CloseRangePenalty = -20;
        private const int LongRangePenalty = -20;
        private const int MediumRangePenalty = -10;
        private const int ShortRangePenalty = -5;

        // Range constants
        private const float CloseRange = 5.0f;
        private const float ShortRange = 20.0f;
        private const float MediumRange = 30.0f;
        private const float LongRange = 40.0f;

        // NPC object ID constant
        private const uint NpcActionTargetId = 2130706432;

        // Default values
        private const int DefaultMissedBy = 1;
        private const int DefaultToHitMod = 1;
        private const int DefaultToHitRoll = 1;
        private const string DeflectionAttemptedDefendersVariable = "RESOLVE_ATTACK_ROLL_DEFLECTION_ATTEMPTED";

        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessedField.Local
        private static ResolveAttackRollHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
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
            ServerManager.Executor.ExecuteInScriptContext(() =>
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
                 * Critical hits use SWLOR's weapon skill plus a small PER vs VIT bonus and StatType modifiers.
                 * Crit immunity applies as normal.
                 */

                var attacker = CNWSCreature.FromPointer(thisPtr);
                var area = attacker.GetArea();

                ProfilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnResolveAttackRoll)}",
                    "Area", area.m_sTag.ToString(),
                    "ObjectType", "Creature");

                Log.Write(LogGroup.Attack, "Running OnResolveAttackRoll");
                var targetObject = CNWSObject.FromPointer(pTarget);
                if (targetObject == null)
                {
                    ProfilerPlugin.PopPerfScope();
                    return;
                }

                var attackerStats = attacker.m_pStats;

                var pCombatRound = attacker.m_pcCombatRound;

                Log.Write(LogGroup.Attack, "Attacker: " + attacker.GetFirstName().GetSimple(0) + ", defender " + targetObject.GetFirstName().GetSimple(0));

                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

                if (targetObject.m_nObjectType != (int)ObjectType.Creature)
                {
                    // Automatically hit non-creature targets.  Do not apply criticals.
                    Log.Write(LogGroup.Attack, "Placeable target.  Auto hit.");
                    pAttackData.m_nAttackResult = AttackResultAutomaticHit;
                    ProfilerPlugin.PopPerfScope();
                    return;
                }

                // If we get to this point, we are fighting a creature.  Pull the target's stats.
                var defender = CNWSCreature.FromPointer(pTarget);

                // Start each attacker combat round with an empty defender set. Tracking every
                // attempted defender in one round-local value preserves Cleave target switching
                // without accumulating one persistent script variable per defender.
                if (pCombatRound.m_bRoundStarted == 1 && pCombatRound.m_nCurrentAttack == 0)
                {
                    ResetDeflectionAttemptedDefenders(attacker);
                }

                var attackType = (uint)AttackType.Melee;
                var weapon = pCombatRound.GetCurrentAttackWeapon();
                var weaponSkillType = weapon == null
                    ? SkillType.Invalid
                    : SWLOR.Game.Server.Service.Skill.GetSkillTypeByBaseItem((BaseItem)weapon.m_nBaseItem);

                // Check whether this is a ranged weapon.
                if (weapon != null && pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
                {
                    attackType = (uint)AttackType.Ranged;
                }

                Log.Write(LogGroup.Attack, "Selected attack type " + attackType + ", weapon " + (weapon == null ? "none" : weapon.GetFirstName().GetSimple(0)));

                var attackerAccuracy = Stat.GetAccuracyNative(attacker, weapon);
                attackerAccuracy = Combat.ApplyStatusSourceAccuracyModifiers(
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    attackerAccuracy);
                var defenderEvasion = Stat.GetEvasionNative(defender, weaponSkillType);
                defenderEvasion = Combat.ApplySideAttackEvasionIgnore(
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    weaponSkillType,
                    defenderEvasion);

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
                    Log.Write(LogGroup.Attack, "NPC attacking - storing target " + defender.m_idSelf);
                    attacker.m_ScriptVars.SetInt(new CExoString("I_LAST_ATTACKED"), (int)defender.m_idSelf);
                }

                // oidTarget will be 0 for a newly spawned NPC who hasn't been attacked yet.  Don't let them get taken by surprise in round 1.
                if (oidTarget != 0 && oidTarget != attacker.m_idSelf)
                {
                    Log.Write(LogGroup.Attack, "Defender current target (" + oidTarget + ") is not attacker (" + attacker.m_idSelf + "). Assign circumstance bonus");
                    accuracyModifiers += CircumstanceBonus;
                }

                // Range bonuses and penalties
                accuracyModifiers += CalculateRangeModifiers(attackType, attacker, defender, weapon);

                // Combat Mode - Power Attack (-5 ACC)
                if (attacker.m_nCombatMode == PowerAttackMode)
                {
                    accuracyModifiers += PowerAttackPenalty;
                    Log.Write(LogGroup.Attack, $"Applying Power Attack penalty: {PowerAttackPenalty}");
                }
                // Combat Mode - Improved Power Attack (-10 ACC)
                else if (attacker.m_nCombatMode == ImprovedPowerAttackMode)
                {
                    accuracyModifiers += ImprovedPowerAttackPenalty;
                    Log.Write(LogGroup.Attack, $"Applying Imp. Power Attack penalty: {ImprovedPowerAttackPenalty}");
                }

                // End modifiers
                //---------------------------------------------------------------------------------------------
                //---------------------------------------------------------------------------------------------
                //---------------------------------------------------------------------------------------------
                var attackRoll = Random.D100(1);
                var hitChanceModifier =
                    Combat.GetSideAttackHitChanceAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType) +
                    Combat.GetHitChanceAgainstSunderedTargetAdjustment(attacker.m_idSelf, defender.m_idSelf);
                var hitRate = Combat.CalculateHitRate(
                    attackerAccuracy + accuracyModifiers,
                    defenderEvasion,
                    hitChanceModifier);
                var isHit = Combat.GetAutoAttackHitResolutionOverride() ?? attackRoll <= hitRate;

                Log.Write(LogGroup.Attack, $"attackerAccuracy = {attackerAccuracy}, modifiers = {accuracyModifiers}, defenderEvasion = {defenderEvasion}");
                Log.Write(LogGroup.Attack, $"Hit Rate: {hitRate}, Roll = {attackRoll}");

                // Check for deflection
                var deflectionSource = CheckDeflection(
                    isHit,
                    attackType,
                    weaponSkillType,
                    attacker,
                    defender);
                var deflected = deflectionSource != DeflectionSource.None;
                if (deflected)
                {
                    isHit = false;

                    // Deflecting Return is a Ranged Deflection rider. Shield Deflection never triggers it.
                    if (deflectionSource == DeflectionSource.Ranged)
                        Combat.ApplyRangedDeflectionReflection(defender.m_idSelf, attacker.m_idSelf, weaponSkillType);
                }

                // Hit
                if (isHit)
                {
                    if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType))
                    {
                        Log.Write(LogGroup.Attack, $"Queued weapon ability hit - attack result 1");
                        pAttackData.m_nAttackResult = AttackResultRegularHit;
                    }
                    else
                    {
                        var criticalStat = attackerStats.GetDEXStat();
                        var criticalRoll = Random.D100(1);
                        var criticalModifier = CalculateCriticalRateModifier(attacker, defender, weaponSkillType);
                        criticalModifier += Combat.ConsumeNextAttackGuardedHitCriticalRateBonus(attacker.m_idSelf);
                        criticalModifier += Combat.ConsumeNextAutoAttackCriticalRateBonus(attacker.m_idSelf, weaponSkillType);
                        criticalModifier += Combat.PrepareOpeningAutoAttack(attacker.m_idSelf, weaponSkillType);
                        criticalModifier += Combat.GetAutoAttackCriticalRateAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType);
                        criticalModifier += Combat.GetSideAttackCriticalRateAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType);
                        criticalModifier += Combat.GetBackAttackCriticalRateAdjustment(attacker.m_idSelf, defender.m_idSelf, weaponSkillType);
                        var criticalSkillRank = GetCriticalSkillRank(attacker, weapon);
                        var criticalRate = Combat.CalculateCriticalRate(
                            criticalStat,
                            defender.m_pStats.GetCONStat(),
                            criticalSkillRank,
                            criticalModifier);

                        // Critical
                        if (criticalRoll <= criticalRate)
                        {
                            Log.Write(LogGroup.Attack, $"Critical hit");

                            // Critical Hit - populate variables for feedback
                            pAttackData.m_bCriticalThreat = 1;
                            pAttackData.m_nThreatRoll = 1;

                            if (Combat.TryUseIncomingCriticalHitDowngrade(defender.m_idSelf, 1))
                            {
                                Log.Write(LogGroup.Attack, $"Critical hit downgraded by defender stats");
                                TemporaryStatModifier.Replace(
                                    defender.m_idSelf,
                                    StatType.CurrentIncomingAttackMinimumDamage,
                                    1,
                                    6,
                                    StatType.CurrentIncomingAttackMinimumDamage);
                                pAttackData.m_nAttackResult = AttackResultRegularHit;
                            }
                            else if (defender.m_pStats.GetEffectImmunity((byte)ImmunityType.CriticalHit, attacker) == 1)
                            {
                                Log.Write(LogGroup.Attack, $"Immune to critical hits");
                                // Immune!
                                var defenderName = PlayerName.GetDisplayName(attacker.m_idSelf, defender.m_idSelf);
                                attacker.SendFeedbackString(new CExoString($"{defenderName} is immune to critical hits!"));
                                pAttackData.m_nAttackResult = AttackResultRegularHit;
                            }
                            else
                            {
                                Log.Write(LogGroup.Attack, $"Not immune to critical hits - dealing crit damage");
                                pAttackData.m_nAttackResult = AttackResultCriticalHit;
                            }
                        }
                        // Regular Hit
                        else
                        {
                            Log.Write(LogGroup.Attack, $"Regular hit - attack result 1");
                            pAttackData.m_nAttackResult = AttackResultRegularHit;
                        }
                    }

                    Combat.TrackAttackActivity(attacker.m_idSelf);
                }
                // Miss
                else
                {
                    Combat.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);
                    Combat.TrackAttackActivity(attacker.m_idSelf);

                    if (deflected)
                    {
                        Log.Write(LogGroup.Attack, $"Deflected - setting attack result to 2");
                        pAttackData.m_nAttackResult = AttackResultDeflect;
                    }
                    else
                    {
                        Log.Write(LogGroup.Attack, $"Miss - setting attack result to 4, missed by 0");
                        pAttackData.m_nAttackResult = AttackResultMiss;
                    }
                    pAttackData.m_nMissedBy = DefaultMissedBy;
                }

                // Embattled ramps from every attempted hostile attack (hit, miss, or deflect), not only
                // landed hits. Refresh no-ops unless the defender owns the trait.
                if (GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf))
                    EmbattledStatusEffect.Refresh(defender.m_idSelf, attacker.m_idSelf);

                Log.Write(LogGroup.Attack, $"Resolving NWN defensive effects");
                // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is
                // accurate.
                var wasSuccessfulBeforeDefensiveEffects = IsSuccessfulAttackResult(pAttackData.m_nAttackResult);
                attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);
                if (wasSuccessfulBeforeDefensiveEffects &&
                    !IsSuccessfulAttackResult(pAttackData.m_nAttackResult))
                {
                    Combat.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);
                }

                Log.Write(LogGroup.Attack, $"Building combat log message");
                var attackerMessage = BuildAttackFeedbackMessage(
                    attacker.m_idSelf,
                    attacker,
                    defender,
                    pAttackData.m_nAttackResult,
                    hitRate,
                    weaponSkillType,
                    deflectionSource);
                var defenderMessage = BuildAttackFeedbackMessage(
                    defender.m_idSelf,
                    attacker,
                    defender,
                    pAttackData.m_nAttackResult,
                    hitRate,
                    weaponSkillType,
                    deflectionSource);
                attacker.SendFeedbackString(new CExoString(attackerMessage));
                defender.SendFeedbackString(new CExoString(defenderMessage));

                Log.Write(LogGroup.Attack, $"Setting pAttackData results");
                pAttackData.m_nToHitMod = DefaultToHitMod;
                pAttackData.m_nToHitRoll = DefaultToHitRoll;

                Log.Write(LogGroup.Attack, $"Finished ResolveAttackRoll");

                ProfilerPlugin.PopPerfScope();
            });
        }

        private static string BuildAttackFeedbackMessage(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int hitRate,
            SkillType weaponSkillType,
            DeflectionSource deflectionSource)
        {
            if (IsSuccessfulAttackResult(attackResultType) &&
                UsePerkFeat.TryGetQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, out var queuedAbility))
            {
                return Combat.BuildAbilityCombatLogMessage(
                    observer,
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    queuedAbility.Name,
                    attackResultType,
                    hitRate);
            }

            return Combat.BuildCombatLogMessageNative(
                observer,
                attacker,
                defender,
                attackResultType,
                hitRate,
                deflectionSource);
        }

        /// <summary>
        /// Returns whether the native attack result represents a landed hit. The damage-roll hook
        /// also runs for attacks that the engine later discards, so on-hit riders must use this
        /// result instead of treating every damage calculation as a successful attack.
        /// </summary>
        internal static bool IsSuccessfulAttackResult(int attackResultType)
        {
            return attackResultType == AttackResultAutomaticHit ||
                   attackResultType == AttackResultRegularHit ||
                   attackResultType == AttackResultCriticalHit;
        }

        private static int CalculateRangeModifiers(uint attackType, CNWSCreature attacker, CNWSCreature defender, CNWSItem weapon)
        {
            if (attackType != (uint)AttackType.Ranged)
                return 0;

            var attackerPos = attacker.m_vPosition;
            var defenderPos = defender.m_vPosition;

            // Calculate distance using X/Y coordinates only
            var range = Math.Sqrt(Math.Pow(attackerPos.x - defenderPos.x, 2) + Math.Pow(attackerPos.y - defenderPos.y, 2));

            Log.Write(LogGroup.Attack, $"Ranged attack at range {range}");

            // Close range (under 5.0)
            if (range < CloseRange)
            {
                if (weapon != null)
                    return CloseRangePenalty;
            }
            // Long range (over 40.0)
            else if (range > LongRange)
            {
                if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    return LongRangePenalty;
                else
                    return MediumRangePenalty;
            }
            // Medium range (30.0 - 40.0)
            else if (range > MediumRange)
            {
                if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    return MediumRangePenalty;
                else
                    return ShortRangePenalty;
            }
            // Short range (20.0 - 30.0)
            else if (weapon != null && range > ShortRange && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                return ShortRangePenalty;
            }

            return 0;
        }

        private static DeflectionSource CheckDeflection(
            bool isHit,
            uint attackType,
            SkillType weaponSkillType,
            CNWSCreature attacker,
            CNWSCreature defender)
        {
            var attemptedDefenders = GetLocalString(attacker.m_idSelf, DeflectionAttemptedDefendersVariable) ?? string.Empty;
            var defenderToken = $"|{defender.m_idSelf}|";

            if (!isHit ||
                attemptedDefenders.Contains(defenderToken, StringComparison.Ordinal) ||
                weaponSkillType == SkillType.Invalid ||
                !Combat.IsHostileAttackSource(defender.m_idSelf, attacker.m_idSelf) ||
                UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType))
                return DeflectionSource.None;

            var (source, deflectChance) = GetDeflectionChance(defender, attackType);
            if (deflectChance <= 0)
                return DeflectionSource.None;

            SetLocalString(
                attacker.m_idSelf,
                DeflectionAttemptedDefendersVariable,
                $"{attemptedDefenders}{defenderToken}");

            var deflectRoll = Random.D100(1);
            var deflected = deflectRoll <= deflectChance;
            if (deflected)
            {
                Stat.ApplyDeflectionEffectsNative(defender, source);

                var deflectionName = Combat.GetDeflectionResultName(source);
                attacker.SendFeedbackString(new CExoString(BuildDeflectionFeedback(attacker.m_idSelf, attacker, defender, deflectionName)));
                defender.SendFeedbackString(new CExoString(BuildDeflectionFeedback(defender.m_idSelf, attacker, defender, deflectionName)));
            }

            Log.Write(LogGroup.Attack, $"Deflect roll: {deflectRoll}, Chance: {deflectChance}, Hit: {!deflected}");

            return deflected ? source : DeflectionSource.None;
        }

        private static void ResetDeflectionAttemptedDefenders(CNWSCreature attacker)
        {
            DeleteLocalString(attacker.m_idSelf, DeflectionAttemptedDefendersVariable);
        }

        private static string BuildDeflectionFeedback(uint observer, CNWSCreature attacker, CNWSCreature defender, string deflectionName)
        {
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{defenderName}'s {deflectionName} negates {attackerName}'s attack.");
        }

        private static (DeflectionSource Source, int Chance) GetDeflectionChance(CNWSCreature defender, uint attackType)
        {
            var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);
            if (shieldDeflection > 0)
                return (DeflectionSource.Shield, shieldDeflection);

            if (attackType == (uint)AttackType.Ranged)
            {
                var rangedDeflection = Stat.GetRangedDeflectionChanceNative(defender);
                return rangedDeflection > 0
                    ? (DeflectionSource.Ranged, rangedDeflection)
                    : (DeflectionSource.None, 0);
            }

            var meleeDeflection = Stat.GetMeleeDeflectionChanceNative(defender);
            if (meleeDeflection > 0)
                return (DeflectionSource.Melee, meleeDeflection);

            return (DeflectionSource.None, 0);
        }

        private static int CalculateCriticalRateModifier(CNWSCreature attacker, CNWSCreature defender, SkillType skillType)
        {
            var criticalModifier = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.CriticalRatePercentAdjustment);
            criticalModifier += Combat.GetSkillCriticalRatePercentAdjustment(attacker.m_idSelf, skillType);
            criticalModifier += Combat.GetCriticalRateAgainstSunderedTargetAdjustment(attacker.m_idSelf, defender.m_idSelf);

            Log.Write(LogGroup.Attack, $"SWLOR crit rate modifier: {criticalModifier}");

            return criticalModifier;
        }

        private static int GetCriticalSkillRank(CNWSCreature attacker, CNWSItem weapon)
        {
            var skillType = weapon == null
                ? SkillType.Invalid
                : SWLOR.Game.Server.Service.Skill.GetSkillTypeByBaseItem((BaseItem)weapon.m_nBaseItem);

            if (attacker.m_bPlayerCharacter == 1)
            {
                if (skillType == SkillType.Invalid)
                    return 0;

                var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                return dbPlayer?.Skills.TryGetValue(skillType, out var skill) == true
                    ? skill.Rank
                    : 0;
            }

            var npcStats = Stat.GetNPCStatsNative(attacker);

            return skillType != SkillType.Invalid && npcStats.Skills.TryGetValue(skillType, out var skillRank)
                ? skillRank
                : npcStats.Level;
        }

    }
}
