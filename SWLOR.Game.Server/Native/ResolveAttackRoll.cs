using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using ObjectType = NWN.Native.API.ObjectType;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ResolveAttackRoll
    {
        // Attack result constants
        internal const int AttackResultAutomaticHit = 7;
        internal const int AttackResultRegularHit = 1;
        internal const int AttackResultDeflect = 2;
        internal const int AttackResultCriticalHit = 3;
        internal const int AttackResultMiss = 4;

        // Default values
        private const int DefaultMissedBy = 1;
        private const int DefaultToHitMod = 1;
        private const int DefaultToHitRoll = 1;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.ResolveAttackRoll);
        }

        internal static void ResolveWeaponRoll(void* thisPtr, void* pTarget)
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
                    "Script", "NATIVE:OnResolveAttackRoll",
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
                // This API accepts WeaponAttackType, not a boolean off-hand flag.
                var weapon = RangedWeaponAttackContext.GetWeapon(attacker.m_idSelf,
                    WeaponAttackCycle.GetAttackWeapon(attacker, pCombatRound.GetWeaponAttackType()));
                // Committed ranged projectiles retain their original resolution semantics.
                if (pAttackData.m_bRangedAttack == 0 && !WeaponAttackEligibility.IsAllowed(
                        attacker.m_idSelf, weapon?.m_idSelf ?? OBJECT_INVALID, WeaponAttackPhase.Resolve))
                {
                    pAttackData.m_nAttackResult = AttackResultMiss;
                    pAttackData.m_nMissedBy = DefaultMissedBy;
                    pAttackData.m_nToHitMod = DefaultToHitMod;
                    pAttackData.m_nToHitRoll = DefaultToHitRoll;
                    ProfilerPlugin.PopPerfScope();
                    return;
                }
                UsePerkFeat.BeginWeaponAttackRoll(attacker.m_idSelf);
                var weaponSkillType = weapon == null
                    ? SkillType.Invalid
                    : GameServices.Skill.GetWeaponSkill(attacker.m_idSelf, weapon.m_idSelf);
                GetDamageRoll.PrepareForceConversionStanceAttack(attacker, targetObject, weaponSkillType);
                var abilitySkillType = UsePerkFeat.TryGetQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, out var queuedAbility, attacker.m_pcCombatRound.m_nCurrentAttack)
                    ? GameServices.CombatQueries.GetAbilitySkillType(attacker.m_idSelf, queuedAbility)
                    : weaponSkillType;
                var techniqueAccuracy = MeleeTechnique.PrepareOrdinaryAttempt(
                    attacker.m_idSelf, targetObject.m_idSelf, weaponSkillType, queuedAbility != null,
                    pCombatRound.m_nCurrentAttack);
                var shot = RangedWeaponAttackContext.BeginRoll(attacker.m_idSelf,
                    weapon?.m_idSelf ?? OBJECT_INVALID, targetObject.m_idSelf, pCombatRound.m_nCurrentAttack);
                shot.Ordinary = queuedAbility == null;
                if (shot.Ordinary)
                    shot.Bonuses = PairedRangedTraits.RecordOrdinaryAttempt(attacker.m_idSelf, targetObject.m_idSelf, shot.Id);

                if (targetObject.m_nObjectType != (int)ObjectType.Creature)
                {
                    // Automatically hit non-creature targets.  Do not apply criticals.
                    Log.Write(LogGroup.Attack, "Placeable target.  Auto hit.");
                    pAttackData.m_nAttackResult = AttackResultAutomaticHit;
                    if (UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, pCombatRound.m_nCurrentAttack))
                    {
                        UsePerkFeat.ReserveQueuedWeaponAbilityAttack(
                            attacker.m_idSelf, pCombatRound.m_nCurrentAttack, weapon?.m_idSelf ?? OBJECT_INVALID);
                    }
                    else
                    {
                        StatusEffectCombatEvent.NotifyAttackAttemptStatusEffects(
                            attacker.m_idSelf,
                            weaponSkillType);
                    }
                    ProfilerPlugin.PopPerfScope();
                    return;
                }

                ResolveCreatureTargetRoll(pTarget, attacker, targetObject, attackerStats, pCombatRound, pAttackData,
                    weapon, weaponSkillType, abilitySkillType, queuedAbility, techniqueAccuracy, shot);

                ProfilerPlugin.PopPerfScope();
            });
        }

        private static void ResolveCreatureTargetRoll(void* pTarget, CNWSCreature attacker, CNWSObject targetObject,
            CNWSCreatureStats attackerStats, CNWSCombatRound pCombatRound, CNWSCombatAttackData pAttackData,
            CNWSItem weapon, SkillType weaponSkillType, SkillType abilitySkillType, AbilityDetail queuedAbility,
            int techniqueAccuracy, RangedWeaponAttackContext.Roll shot)
        {
            // If we get to this point, we are fighting a creature.  Pull the target's stats.
            var defender = CNWSCreature.FromPointer(pTarget);

            var attackType = (uint)AttackType.Melee;

            // Check whether this is a ranged weapon.
            if (weapon != null && pAttackData.m_bRangedAttack == 1 && attacker.GetRangeWeaponEquipped() == 1)
            {
                attackType = (uint)AttackType.Ranged;
            }

            Log.Write(LogGroup.Attack, "Selected attack type " + attackType + ", weapon " + (weapon == null ? "none" : weapon.GetFirstName().GetSimple(0)));

            var attackerAccuracy = AttackRollAccuracy.GetAttackerAccuracy(attacker, defender, targetObject, weapon,
                queuedAbility, abilitySkillType, techniqueAccuracy, shot);
            var defenderEvasion = AttackRollDefense.GetDefenderEvasion(attacker, defender, abilitySkillType);
            var accuracyModifiers = AttackRollAccuracy.GetAccuracyModifiers(attackType, attacker, defender, weapon);

            var attackRoll = Random.D100(1);
            var hitChanceModifier = AttackRollAccuracy.GetHitChanceModifier(attacker, defender, weaponSkillType,
                abilitySkillType, queuedAbility);
            var hitRate = CombatRules.CalculateHitRate(
                attackerAccuracy + accuracyModifiers,
                defenderEvasion,
                hitChanceModifier,
                GameServices.CombatQueries.GetMinimumHitRate(attacker.m_idSelf));
            // Lucky Chamber advances on every ranged attack attempt, including misses and
            // deflections. Its returned bonus is only consulted if this attempt lands.
            var autoAttackCycleCriticalRate =
                GameServices.CombatDamageModifiers.PrepareAutoAttackCycleCriticalRate(attacker.m_idSelf, weaponSkillType);
            var isHit = GameServices.CombatAbilityHit.GetAutoAttackHitResolutionOverride() ?? attackRoll <= hitRate;
            var accuracyEvasionMiss = !isHit;

            Log.Write(LogGroup.Attack, $"attackerAccuracy = {attackerAccuracy}, modifiers = {accuracyModifiers}, defenderEvasion = {defenderEvasion}");
            Log.Write(LogGroup.Attack, $"Hit Rate: {hitRate}, Roll = {attackRoll}");

            // Each weapon roll gets its own deflection attempt, including later hands
            // and accelerated attacks in the same native combat round.
            var deflectionSource = AttackRollDefense.CheckDeflection(
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
                    GameServices.CombatDamageTaken.ApplyRangedDeflectionReflection(defender.m_idSelf, attacker.m_idSelf, weaponSkillType);
            }

            // Hit
            if (isHit)
            {
                AttackRollCritical.ResolveHitResult(attacker, defender, attackerStats, pAttackData, weapon,
                    weaponSkillType, autoAttackCycleCriticalRate);

                GameServices.CombatActivity.TrackAttackActivity(attacker.m_idSelf);
            }
            // Miss
            else
            {
                GameServices.CombatDamageTaken.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);
                if (accuracyEvasionMiss && GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf))
                    PairedRangedTraits.RecordAccuracyEvasionMiss(defender.m_idSelf);
                GameServices.CombatActivity.TrackAttackActivity(attacker.m_idSelf);

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

            AttackRollSettlement.SettleAttackRoll(attacker, defender, targetObject, pAttackData, pCombatRound, weapon,
                weaponSkillType, abilitySkillType, queuedAbility, shot, isHit, hitRate, autoAttackCycleCriticalRate);

            AttackRollFeedback.SendAttackFeedback(attacker, defender, pAttackData, hitRate, weaponSkillType,
                deflectionSource);

            Log.Write(LogGroup.Attack, $"Setting pAttackData results");
            pAttackData.m_nToHitMod = DefaultToHitMod;
            pAttackData.m_nToHitRoll = DefaultToHitRoll;

            Log.Write(LogGroup.Attack, $"Finished ResolveAttackRoll");
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
    }
}
