using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using NWNX.NET;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class OnAIActionAttackObject
    {
        private const int ACTION_IN_PROGRESS = 1;
        private const int ACTION_COMPLETE = 2;
        private const int ACTION_FAILED = 3;

        private const int SANCTUARY_SAVE_FAILED = 1;
        private const float CNW_PATHFIND_TOLERANCE = 0.01f;

        private const ushort AISTATE_CREATURE_ABLE_TO_GO_HOSTILE = 0x0080;
        private const ushort AISTATE_CREATURE_USE_HANDS = 0x0004;

        private const int NWANIMBASE_ANIM_PAUSE = 0;
        private const int NWANIMBASE_ANIM_READY = 1;
        private const int NWANIMBASE_ANIM_ATTACK = 9;
        private const int FEEDBACK_ACTION_CANT_REACH_TARGET = 218;

        private const int CSERVERAIMASTER_AIACTION_ATTACKOBJECT = 12;
        private const int CSERVERAIMASTER_AIACTION_CHECKMOVETOOBJECTRADIUS = 17;
        private const int CSERVERAIMASTER_AIACTION_CHANGEFACINGOBJECT = 19;
        private const int CNWSOBJECTACTION_PARAMETER_INTEGER = 1;
        private const int CNWSOBJECTACTION_PARAMETER_FLOAT = 2;
        private const int CNWSOBJECTACTION_PARAMETER_OBJECT = 3;

        private const float RANGED_LINE_REPOSITION_ARC_LENGTH = 2.0f;
        private const float RANGED_LINE_REPOSITION_MAX_ANGLE = 0.7853982f;
        private const float RANGED_LINE_REPOSITION_COMPLETION_RANGE = 0.25f;

        private const int CNWSCOMBATROUND_TYPE_INVALID = 0;
        private const int CNWSCOMBATROUND_TYPE_ATTACK = 1;
        private const int CNWSCOMBATROUND_TYPE_REACTION = 3;
        private const int CNWSCOMBATROUND_TYPE_COMSTEP = 4;
        private const int CNWSCOMBATROUND_TYPE_COMSTEPFB = 5;
        private const int CNWSCOMBATROUND_TYPE_EQUIP = 6;
        private const int CNWSCOMBATROUND_TYPE_UNEQUIP = 7;
        private const int CNWSCOMBATROUND_TYPE_PARRY = 8;

        private const int WEAPON_ATTACK_TYPE_MAINHAND = 1;
        private const int WEAPON_ATTACK_TYPE_OFFHAND = 2;

        private static readonly Dictionary<uint, DateTime> _creatureAttackDelays = new();
        private static readonly Dictionary<uint, float> _rangedRepositionDirections = new();

        internal delegate int AIActionAttackObjectHook(void* pCreature, void* pNode);

        // ReSharper disable once NotAccessedField.Local
        private static AIActionAttackObjectHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, int> pHook = &HandleOnAIActionAttackObject;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN12CNWSCreature20AIActionAttackObjectEP20CNWSObjectActionNode");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<AIActionAttackObjectHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int HandleOnAIActionAttackObject(void* creature, void* node)
        {
            return ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                var pCreature = CNWSCreature.FromPointer(creature);
                var pNode = CNWSObjectActionNode.FromPointer(node);
                var pArea = pCreature.GetArea();
                var currentWeaponAttackType = pCreature.m_pcCombatRound.GetWeaponAttackType();
                var currentAttackWeapon = pCreature.m_pcCombatRound.GetCurrentAttackWeapon(
                    currentWeaponAttackType == WEAPON_ATTACK_TYPE_OFFHAND ? 1 : 0);
                var attackSkillType = currentAttackWeapon == null
                    ? Combat.GetEquippedWeaponSkillType(pCreature.m_idSelf)
                    : SWLOR.Game.Server.Service.Skill.GetSkillTypeByBaseItem(
                        (SWLOR.NWN.API.NWScript.Enum.Item.BaseItem)currentAttackWeapon.m_nBaseItem);
                var isPlayerCreature = pCreature.m_bPlayerCharacter == 1;
                var usesRangedWeapon = isPlayerCreature
                    ? pCreature.GetRangeWeaponEquipped() == 1
                    : Combat.IsRangedWeaponSkill(attackSkillType);

                // Clean up attack delay entry if creature no longer exists
                if (_creatureAttackDelays.ContainsKey(pCreature.m_idSelf) && !GetIsObjectValid(pCreature.m_idSelf))
                {
                    _creatureAttackDelays.Remove(pCreature.m_idSelf);
                    _rangedRepositionDirections.Remove(pCreature.m_idSelf);
                    Combat.ClearAttackSwingDebt(pCreature.m_idSelf);
                }

                // This action was just run... reset
                // the combat round update time.
                // - BKH - May/21/02
                pCreature.m_nLastCombatRoundUpdate = 6000;

                if (pCreature.GetDead() == 1 ||
                    pCreature.GetIsPCDying() == 1 ||
                    !IsAIState(AISTATE_CREATURE_ABLE_TO_GO_HOSTILE, pCreature) ||
                    !IsAIState(AISTATE_CREATURE_USE_HANDS, pCreature))
                {
                    // Clean up attack delay entry when creature can no longer attack
                    if (_creatureAttackDelays.ContainsKey(pCreature.m_idSelf))
                    {
                        _creatureAttackDelays.Remove(pCreature.m_idSelf);
                    }

                    _rangedRepositionDirections.Remove(pCreature.m_idSelf);

                    Combat.ClearAttackSwingDebt(pCreature.m_idSelf);

                    pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);
                    return ACTION_FAILED;
                }

                var oidAttackTarget = (uint)pNode.m_pParameter[0];

                // You cannot attack yourself
                if (oidAttackTarget == pCreature.m_idSelf)
                {
                    pCreature.SetAnimation(NWANIMBASE_ANIM_PAUSE);
                    return ACTION_FAILED;
                }

                var pGameObject = (CGameObject)NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(oidAttackTarget);

                var bTargetActive = false;
                if (pGameObject != null)
                {
                    if (pGameObject.AsNWSObject() != null)
                    {
                        if (pGameObject.AsNWSObject().GetDead() == 0)
                        {
                            bTargetActive = true;
                        }

                        if (pGameObject.AsNWSCreature() != null &&
                            pGameObject.AsNWSCreature().GetDead() == 0 &&
                            pGameObject.AsNWSCreature().m_bPlayerCharacter == 1 &&
                            pGameObject.AsNWSCreature().GetIsPCDying() == 1)
                        {
                            bTargetActive = true;
                        }

                        // If the target is invisible and we can't see or hear them,
                        // then they aren't an acceptable target.
                        var pVisNode = pCreature.GetVisibleListElement(oidAttackTarget);
                        if (pVisNode != null)
                        {
                            if (pVisNode.m_nSanctuary == SANCTUARY_SAVE_FAILED ||
                                (pVisNode.m_bInvisible == 1 &&
                                 pVisNode.m_bHeard == 0 &&
                                 pVisNode.m_bSeen == 0))
                            {
                                bTargetActive = false;
                            }
                        }
                        else
                        {
                            if (pGameObject.AsNWSCreature() != null &&
                                pCreature.m_bPlayerCharacter == 1)
                            {
                                bTargetActive = false;
                            }
                        }
                    }
                }

                if (bTargetActive)
                {
                    var pTarget = pGameObject.AsNWSObject();
                    if (TryCancelAttackForCombatLeash(pCreature, pNode, oidAttackTarget))
                    {
                        return ACTION_FAILED;
                    }

                    var vTargetPosition = pTarget.m_vPosition;
                    var pTargetArea = pTarget.GetArea();

                    var fMaxAttackRange = pCreature.MaxAttackRange(oidAttackTarget);
                    var fDesiredAttackRange = isPlayerCreature
                        ? ResolveEngineDesiredAttackRange(
                            pCreature.DesiredAttackRange(oidAttackTarget),
                            fMaxAttackRange,
                            usesRangedWeapon)
                        : ResolveWeaponEngagementRange(
                            Combat.GetWeaponEngagementRange(attackSkillType),
                            fMaxAttackRange,
                            usesRangedWeapon);
                    fMaxAttackRange = ResolveMaximumAttackRange(
                        fDesiredAttackRange,
                        fMaxAttackRange,
                        usesRangedWeapon);
                    if (pCreature.m_oidAttemptedAttackTarget == OBJECT_INVALID)
                    {
                        pCreature.m_oidAttemptedAttackTarget = oidAttackTarget;
                    }

                    const float fUseRange = 0;

                    if (pGameObject.AsNWSCreature() != null)
                    {
                        var pFUseRange = Marshal.AllocHGlobal(sizeof(float));

                        try
                        {
                            Marshal.StructureToPtr(fUseRange, pFUseRange, false);
                            pCreature.GetUseRange(oidAttackTarget, vTargetPosition, (float*)pFUseRange);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pFUseRange);
                        }
                    }

                    var bClearLineOfAttack = 0;

                    if (pArea != null && pArea == pTargetArea)
                    {
                        bClearLineOfAttack = pCreature.CheckAttackClearLineToTarget(oidAttackTarget, vTargetPosition, pArea);
                    }

                    var vDelta = new Vector(
                            pCreature.m_vPosition.x - vTargetPosition.x,
                            pCreature.m_vPosition.y - vTargetPosition.y,
                            pCreature.m_vPosition.z - vTargetPosition.z
                        );
                    var bOutsideAttackRange = (pTargetArea != pArea ||
                                               MagnitudeSquared(vDelta) > Sqr(fMaxAttackRange + CNW_PATHFIND_TOLERANCE));

                    if (bOutsideAttackRange || bClearLineOfAttack == 0)
                    {
                        if (pCreature.m_bPassiveAttackBehaviour == 1)
                        {
                            var newTarget = pCreature.GetNewCombatTarget(oidAttackTarget);
                            oidAttackTarget = OBJECT_INVALID;

                            if (newTarget != null)
                            {
                                oidAttackTarget = newTarget.m_idSelf;
                                pCreature.m_bPassiveAttackBehaviour = 1;
                            }

                            pCreature.ChangeAttackTarget(pNode, oidAttackTarget);
                            return (oidAttackTarget != OBJECT_INVALID ? ACTION_IN_PROGRESS : ACTION_FAILED);
                        }

                        uint oidArea;
                        if (pTargetArea != null)
                        {
                            oidArea = pTargetArea.m_idSelf;
                        }
                        else
                        {
                            if (pTarget.AsNWSCreature() != null && pCreature.m_oidEncounter == OBJECT_INVALID)
                            {
                                oidArea = pTarget.AsNWSCreature().m_oidDesiredArea;
                                vTargetPosition = pTarget.AsNWSCreature().m_vDesiredAreaLocation;
                            }
                            else
                            {
                                var newTarget = pCreature.GetNewCombatTarget(oidAttackTarget);
                                oidAttackTarget = OBJECT_INVALID;

                                if (newTarget != null)
                                {
                                    oidAttackTarget = newTarget.m_idSelf;
                                    pCreature.m_bPassiveAttackBehaviour = 1;
                                }

                                pCreature.ChangeAttackTarget(pNode, oidAttackTarget);
                                return (oidAttackTarget != OBJECT_INVALID ? ACTION_IN_PROGRESS : ACTION_FAILED);
                            }
                        }

                        if (pCreature.m_vLastAttackPosition != new Vector() &&
                            pCreature.m_vLastAttackPosition == pCreature.m_vPosition)
                        {
                            if (pCreature.m_bPlayerCharacter == 1)
                            {
                                pCreature.SendFeedbackMessage(FEEDBACK_ACTION_CANT_REACH_TARGET);
                            }

                            CNWSCreature newTarget = null;

                            if (!usesRangedWeapon)
                            {
                                newTarget = pCreature.GetNewCombatTarget(oidAttackTarget);
                            }
                            else
                            {
                                AlternateRangedRepositionDirection(pCreature.m_idSelf);
                            }

                            var bUpdateTarget = false;
                            if (newTarget != null)
                            {
                                oidAttackTarget = newTarget.m_idSelf;
                                pCreature.m_bPassiveAttackBehaviour = 1;
                                bUpdateTarget = true;
                            }
                            else if (pCreature.m_bPlayerCharacter == 1)
                            {
                                oidAttackTarget = OBJECT_INVALID;
                                bUpdateTarget = true;
                            }

                            if (bUpdateTarget)
                            {
                                pCreature.ChangeAttackTarget(pNode, oidAttackTarget);
                                return (oidAttackTarget != OBJECT_INVALID ? ACTION_IN_PROGRESS : ACTION_FAILED);
                            }
                        }
                        else
                        {
                            pCreature.m_vLastAttackPosition = pCreature.m_vPosition;
                        }

                        var fMoveToTargetRange = fDesiredAttackRange;
                        var fMoveToTargetMaxRange = fMaxAttackRange;
                        var fPathMoveRange = fMoveToTargetRange;
                        var vMoveTargetPosition = vTargetPosition;
                        var bTrackAttackTargetWhileMoving = true;

                        if (bClearLineOfAttack == 0)
                        {
                            float fPersonalSpaceRange;
                            if (pTarget.AsNWSCreature() != null)
                            {
                                fPersonalSpaceRange = pCreature.m_pcPathfindInformation.m_fCreaturePersonalSpace;
                                fPersonalSpaceRange += pTarget.AsNWSCreature().m_pcPathfindInformation.m_fCreaturePersonalSpace;
                            }
                            else
                            {
                                fPersonalSpaceRange = pCreature.m_pcPathfindInformation.m_fPersonalSpace;
                            }

                            var movementPlan = CreateBlockedLineMovementPlan(
                                pCreature.m_vPosition.x,
                                pCreature.m_vPosition.y,
                                pCreature.m_vPosition.z,
                                vTargetPosition.x,
                                vTargetPosition.y,
                                vTargetPosition.z,
                                pCreature.m_idSelf,
                                fDesiredAttackRange,
                                fPersonalSpaceRange,
                                bOutsideAttackRange,
                                usesRangedWeapon);
                            fMoveToTargetRange = movementPlan.AttackCheckRange;
                            fPathMoveRange = movementPlan.PathCompletionRange;
                            vMoveTargetPosition = new Vector(
                                movementPlan.DestinationX,
                                movementPlan.DestinationY,
                                movementPlan.DestinationZ);
                            bTrackAttackTargetWhileMoving = movementPlan.TrackAttackTarget;

                            pCreature.m_pcPathfindInformation.m_bUsePlotGridPath = 1;
                        }

                        var bRunToTarget = true;
                        var bLineOfSightRequired = true;

                        void* pOidAttackTarget = &oidAttackTarget;
                        pCreature.AddActionToFront(
                            CSERVERAIMASTER_AIACTION_ATTACKOBJECT,
                            pNode.m_nGroupActionId,
                            CNWSOBJECTACTION_PARAMETER_OBJECT,
                            pOidAttackTarget);

                        pCreature.AddActionToFront(
                            CSERVERAIMASTER_AIACTION_CHECKMOVETOOBJECTRADIUS,
                            pNode.m_nGroupActionId,
                            CNWSOBJECTACTION_PARAMETER_OBJECT, pOidAttackTarget,
                            CNWSOBJECTACTION_PARAMETER_INTEGER, &bRunToTarget,
                            CNWSOBJECTACTION_PARAMETER_FLOAT, &fMoveToTargetRange,
                            CNWSOBJECTACTION_PARAMETER_FLOAT, &fMoveToTargetMaxRange,
                            CNWSOBJECTACTION_PARAMETER_INTEGER, &bLineOfSightRequired);

                        pCreature.AddActionToFront(
                            CSERVERAIMASTER_AIACTION_CHANGEFACINGOBJECT,
                            pNode.m_nGroupActionId,
                            CNWSOBJECTACTION_PARAMETER_OBJECT, pOidAttackTarget);


                        if (pGameObject.AsNWSDoor() != null)
                        {
                            pCreature.AddMoveToPointActionToFront(
                                pNode.m_nGroupActionId,
                                vMoveTargetPosition,
                                oidArea,
                                OBJECT_INVALID,
                                bRunToTarget ? 1 : 0,
                                fPathMoveRange);
                        }
                        else
                        {
                            pCreature.AddMoveToPointActionToFront(
                                pNode.m_nGroupActionId,
                                vMoveTargetPosition,
                                oidArea,
                                bTrackAttackTargetWhileMoving ? oidAttackTarget : OBJECT_INVALID,
                                bRunToTarget ? 1 : 0,
                                fPathMoveRange);
                        }


                        return ACTION_COMPLETE;
                    }
                }

                _rangedRepositionDirections.Remove(pCreature.m_idSelf);



                if (pCreature.m_pcCombatRound == null)
                {
                    pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);
                    return ACTION_FAILED;
                }

                pCreature.m_vLastAttackPosition = new Vector();

                // Check if target is dead - if so, skip delay and handle immediately
                var pTargetObject = (CGameObject)NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(oidAttackTarget);
                var bTargetDead = false;
                if (pTargetObject != null)
                {
                    if (pTargetObject.AsNWSObject() != null && pTargetObject.AsNWSObject().GetDead() == 1)
                    {
                        bTargetDead = true;
                    }
                    else if (pTargetObject.AsNWSCreature() != null &&
                             pTargetObject.AsNWSCreature().m_bPlayerCharacter == 1 &&
                             pTargetObject.AsNWSCreature().GetIsPCDying() == 1)
                    {
                        bTargetDead = true;
                    }
                }

                var useDefaultMinimumDelay = Combat.HasNextAutoAttackNoDelay(pCreature.m_idSelf, attackSkillType);
                var calculatedDelay = Combat.CalculateAttackDelay(pCreature.m_idSelf);
                var effectiveAttackDelay = Combat.CalculateEffectiveAttackDelay(calculatedDelay, useDefaultMinimumDelay);

                var hasLimitedAttackDelayReduction = StatusEffect.TryGetLimitedAttackDelayReduction(
                    pCreature.m_idSelf,
                    attackSkillType,
                    out var limitedAttackDelayReductionPercent,
                    out var limitedAttackDelayReductionRemainingAttacks);
                var calculatedDelayWithoutLimitedReduction = hasLimitedAttackDelayReduction
                    ? Combat.CalculateAttackDelay(pCreature.m_idSelf, -limitedAttackDelayReductionPercent)
                    : calculatedDelay;
                var effectiveDelayWithoutLimitedReduction = Combat.CalculateEffectiveAttackDelay(
                    calculatedDelayWithoutLimitedReduction,
                    false);

                // The delay the attacker would have without a no-delay buff. Lowering the delay to
                // the floor is meaningless for a build already at the floor, so this is passed to
                // ConsumeAttacksPerSwing alongside useDefaultMinimumDelay to guarantee the buff is
                // worth an extra attack either way. Both values are equal for a build already at the
                // floor, so the buff state has to travel separately from the delays.
                var unbuffedAttackDelay = Combat.CalculateEffectiveAttackDelay(calculatedDelay, false);

                // Check attack delay before starting or processing combat
                // First attack is always instant, subsequent attacks respect delay
                // Skip delay check if target is dead
                if (_creatureAttackDelays.ContainsKey(pCreature.m_idSelf) &&
                    !bTargetDead)
                {
                    // Swings cannot animate faster than the engine's base delay. Effective delays
                    // below it are honored by resolving multiple attacks within a single swing.
                    var swingDelay = Combat.CalculateAttackSwingDelay(effectiveAttackDelay);
                    var timeSinceLastAttack = (DateTime.UtcNow - _creatureAttackDelays[pCreature.m_idSelf]).TotalMilliseconds;

                    if (timeSinceLastAttack < swingDelay)
                    {
                        // The engine only re-sends the Attack animation (and its attack burst,
                        // which drives the client's swing-variant randomization) when the
                        // animation field changes. Because this hook keeps the attack action
                        // alive across swings, the animation would otherwise stay at Attack
                        // forever and clients would loop the first swing variant. Once the
                        // swing's animation pause has elapsed, drop back to the combat-ready
                        // loop so the next swing registers as a fresh Attack animation.
                        if (pCreature.m_pcCombatRound.m_bRoundPaused == 0 &&
                            pCreature.m_nAnimation == NWANIMBASE_ANIM_ATTACK)
                        {
                            pCreature.SetAnimation(NWANIMBASE_ANIM_READY);
                        }

                        // Still in delay period, return in progress
                        return ACTION_IN_PROGRESS;
                    }
                }

                if (pCreature.m_pcCombatRound.m_bRoundStarted == 0)
                {
                    pCreature.m_pcCombatRound.StartCombatRound(oidAttackTarget);
                    pCreature.m_pcCombatRound.m_nRoundLength = 1000;
                }

                if (pCreature.m_pcCombatRound.m_bRoundPaused == 0)
                {
                    if (pCreature.m_pcCombatRound.GetActionPending() == 1)
                    {
                        var pPendingAction = pCreature.m_pcCombatRound.GetAction();

                        if (pPendingAction != null)
                        {
                            var nActionType = pPendingAction.m_nActionType;
                            var oidTarget = pPendingAction.m_oidTarget;
                            var nTimeAnimation = pPendingAction.m_nAnimationTime;

                            if (!bTargetActive &&
                                pCreature.m_pcCombatRound.m_oidNewAttackTarget == OBJECT_INVALID)
                            {
                                nActionType = CNWSCOMBATROUND_TYPE_INVALID;
                            }

                            switch (nActionType)
                            {
                                case CNWSCOMBATROUND_TYPE_ATTACK:
                                    {
                                        var nAnimation = pPendingAction.m_nAnimation;
                                        // One swing per delay cycle; the swing resolves one or more attacks
                                        // depending on how far the effective delay sits below the swing floor.
                                        var nAttacks = 1;
                                        var bOverrideAction = false;

                                        // Our current target is dead or not active, meaning we should not process the action
                                        // Since attacking a dead body would be kind of silly...
                                        if (!bTargetActive &&
                                            pCreature.m_pcCombatRound.m_oidNewAttackTarget == OBJECT_INVALID)
                                        {
                                            nActionType = CNWSCOMBATROUND_TYPE_INVALID;
                                            bOverrideAction = true;
                                        }

                                        if (!bOverrideAction)
                                        {
                                            pCreature.SetAnimation(nAnimation);

                                            if (oidAttackTarget != oidTarget)
                                            {
                                                if (pPendingAction.m_bActionRetargettable == 1)
                                                {
                                                    oidTarget = oidAttackTarget;
                                                }
                                                else
                                                {
                                                    pCreature.m_oidAttemptedAttackTarget = oidTarget;
                                                }
                                            }

                                            pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);

                                            // Set pause timer for single attack
                                            pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);

                                            // If the attack was switched somewhere in the combat code ie: Cleave
                                            // we grab the new target and stick with it
                                            if (pCreature.m_pcCombatRound.m_oidNewAttackTarget != OBJECT_INVALID)
                                            {
                                                var oidNewTarget = pCreature.m_pcCombatRound.m_oidNewAttackTarget;
                                                pCreature.m_bPassiveAttackBehaviour = 1;
                                                pCreature.ChangeAttackTarget(pNode, oidNewTarget);
                                                pCreature.m_pcCombatRound.m_oidNewAttackTarget = OBJECT_INVALID;
                                                oidTarget = oidNewTarget;
                                                bTargetActive = true;
                                            }

                                            // This is just in case we've changed targets in mid round... we want to be sure
                                            // that we're still locked on to the proper target
                                            if (TryCancelAttackForCombatLeash(pCreature, pNode, oidTarget))
                                            {
                                                return ACTION_FAILED;
                                            }

                                            pCreature.SetLockOrientationToObject(oidTarget);

                                            // Process the attack (delay already checked at function start)
                                            var isParalyzed = Combat.HandleParalyze(pCreature.m_idSelf);

                                            if (isParalyzed)
                                            {
                                                Log.Write(LogGroup.Attack, $"Creature {pCreature.m_idSelf:X8} is paralyzed, recomputing round");
                                                pCreature.m_pcCombatRound.RecomputeRound();
                                            }
                                            else
                                            {
                                                if (useDefaultMinimumDelay)
                                                {
                                                    Combat.ConsumeNextAutoAttackNoDelay(pCreature.m_idSelf, attackSkillType);
                                                }

                                                // Effective delays below the swing floor resolve extra attacks
                                                // within this swing. Extra attacks only apply to main-hand and
                                                // offhand weapon swings; natural creature weapon swings stay at
                                                // one attack because their attack-count regions cannot be
                                                // widened the same way. The matching attack-count region is
                                                // widened so the extra rolls keep the swing's weapon typing,
                                                // and the post-swing round recompute resets the counts.
                                                var nWeaponAttackType = pCreature.m_pcCombatRound.GetWeaponAttackType();
                                                if (nWeaponAttackType == WEAPON_ATTACK_TYPE_MAINHAND ||
                                                    nWeaponAttackType == WEAPON_ATTACK_TYPE_OFFHAND)
                                                {
                                                    nAttacks = Combat.ConsumeAttacksPerSwing(
                                                        pCreature.m_idSelf,
                                                        effectiveAttackDelay,
                                                        unbuffedAttackDelay,
                                                        useDefaultMinimumDelay,
                                                        effectiveDelayWithoutLimitedReduction,
                                                        useDefaultMinimumDelay
                                                            ? 0
                                                            : limitedAttackDelayReductionRemainingAttacks);

                                                    if (nAttacks > 1)
                                                    {
                                                        if (nWeaponAttackType == WEAPON_ATTACK_TYPE_OFFHAND)
                                                        {
                                                            pCreature.m_pcCombatRound.m_nOffHandAttacks += nAttacks - 1;
                                                        }
                                                        else
                                                        {
                                                            pCreature.m_pcCombatRound.m_nOnHandAttacks += nAttacks - 1;
                                                        }
                                                    }
                                                }

                                                pCreature.ResolveAttack(oidTarget, nAttacks, nTimeAnimation);
                                                bTargetActive = true;

                                                // Set the delay timestamp after the attack resolves
                                                // This ensures the first attack is instant, subsequent attacks respect delay
                                                _creatureAttackDelays[pCreature.m_idSelf] = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    break;

                                case CNWSCOMBATROUND_TYPE_PARRY:
                                    {
                                        var nWeaponAttackType = pCreature.m_pcCombatRound.GetWeaponAttackType();
                                        if (nWeaponAttackType == WEAPON_ATTACK_TYPE_OFFHAND)
                                        {
                                            var nAttackValueToUse = pCreature.m_pcCombatRound.m_nOffHandAttacksTaken;

                                            pCreature.m_pcCombatRound.m_nOffHandAttacksTaken = nAttackValueToUse + 1;
                                        }

                                        pCreature.m_pcCombatRound.SetCurrentAttack((byte)(pCreature.m_pcCombatRound.m_nCurrentAttack + 1));
                                    }
                                    break;

                                case CNWSCOMBATROUND_TYPE_COMSTEP:
                                case CNWSCOMBATROUND_TYPE_COMSTEPFB:
                                    {
                                        // Combat stepping disabled - just pause the round
                                        pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);
                                        pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);
                                    }
                                    break;

                                case CNWSCOMBATROUND_TYPE_REACTION:
                                    {
                                        pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);
                                        pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);
                                    }
                                    break;

                                case CNWSCOMBATROUND_TYPE_EQUIP:
                                    {
                                        pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);
                                        pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);
                                        if (pCreature.RunEquip(oidTarget, pPendingAction.m_nInventorySlot) == 1)
                                        {
                                            pCreature.m_pcCombatRound.RecomputeRound();
                                            _creatureAttackDelays[pCreature.m_idSelf] = DateTime.UtcNow;
                                            Combat.ClearAttackSwingDebt(pCreature.m_idSelf);
                                        }
                                    }
                                    break;

                                case CNWSCOMBATROUND_TYPE_UNEQUIP:
                                    {
                                        pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);
                                        pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);
                                        if (pCreature.RunUnequip(oidTarget, pPendingAction.m_oidTargetRepository, pPendingAction.m_nRepositoryX, pPendingAction.m_nRepositoryY, 0) == 1)
                                        {
                                            pCreature.m_pcCombatRound.RecomputeRound();
                                            _creatureAttackDelays[pCreature.m_idSelf] = DateTime.UtcNow;
                                            Combat.ClearAttackSwingDebt(pCreature.m_idSelf);
                                        }
                                    }
                                    break;

                                default:
                                    {
                                        // Handle any other action types that might not be explicitly handled
                                        // This should rarely be hit with the proper action types above
                                    }
                                    break;
                            }

                            pPendingAction.Dispose();
                            pPendingAction = null;
                        }
                    }
                    else if (bTargetActive)
                    {
                        // Build the next combat action only after the current attack has finished its damage phase.
                        pCreature.m_pcCombatRound.RecomputeRound();
                    }

                    if (bTargetActive == false)
                    {
                        _rangedRepositionDirections.Remove(pCreature.m_idSelf);
                        var newTarget = pCreature.GetNewCombatTarget(oidAttackTarget);
                        oidAttackTarget = OBJECT_INVALID;

                        if (newTarget != null)
                        {
                            oidAttackTarget = newTarget.m_idSelf;
                            pCreature.m_bPassiveAttackBehaviour = 1;
                        }

                        pCreature.ChangeAttackTarget(pNode, oidAttackTarget);

                        return (oidAttackTarget != OBJECT_INVALID
                            ? ACTION_IN_PROGRESS
                            : ACTION_FAILED);
                    }
                }

                return ACTION_IN_PROGRESS;
            });
        }

        private static BlockedLineMovementPlan CreateBlockedLineMovementPlan(
            float attackerX,
            float attackerY,
            float attackerZ,
            float targetX,
            float targetY,
            float targetZ,
            uint attackerId,
            float desiredAttackRange,
            float personalSpaceRange,
            bool isOutsideAttackRange,
            bool hasRangedWeapon)
        {
            if (isOutsideAttackRange)
            {
                return new BlockedLineMovementPlan(
                    targetX,
                    targetY,
                    targetZ,
                    desiredAttackRange,
                    desiredAttackRange,
                    true);
            }

            if (!hasRangedWeapon)
            {
                return new BlockedLineMovementPlan(
                    targetX,
                    targetY,
                    targetZ,
                    personalSpaceRange,
                    personalSpaceRange,
                    true);
            }

            var destination = GetRangedLineRepositionDestination(
                attackerX,
                attackerY,
                attackerZ,
                targetX,
                targetY,
                attackerId,
                desiredAttackRange);
            return new BlockedLineMovementPlan(
                destination.X,
                destination.Y,
                destination.Z,
                RANGED_LINE_REPOSITION_COMPLETION_RANGE,
                desiredAttackRange,
                false);
        }

        private static RepositionDestination GetRangedLineRepositionDestination(
            float attackerX,
            float attackerY,
            float attackerZ,
            float targetX,
            float targetY,
            uint attackerId,
            float desiredAttackRange)
        {
            var x = attackerX - targetX;
            var y = attackerY - targetY;
            var radius = MathF.Sqrt(x * x + y * y);
            var direction = GetRangedRepositionDirection(attackerId);
            var maximumArcStep =
                2f * radius * MathF.Sin(RANGED_LINE_REPOSITION_MAX_ANGLE / 2f);

            if (radius <= CNW_PATHFIND_TOLERANCE ||
                maximumArcStep <= RANGED_LINE_REPOSITION_COMPLETION_RANGE)
            {
                return new RepositionDestination(
                    targetX,
                    targetY + direction * desiredAttackRange,
                    attackerZ);
            }

            var angle = MathF.Min(
                RANGED_LINE_REPOSITION_MAX_ANGLE,
                RANGED_LINE_REPOSITION_ARC_LENGTH / radius);
            var cosine = MathF.Cos(angle);
            var sine = MathF.Sin(angle) * direction;

            return new RepositionDestination(
                targetX + x * cosine - y * sine,
                targetY + x * sine + y * cosine,
                attackerZ);
        }

        private static float GetRangedRepositionDirection(uint attackerId)
        {
            return _rangedRepositionDirections.TryGetValue(attackerId, out var direction)
                ? direction
                : (attackerId & 1) == 0
                    ? 1f
                    : -1f;
        }

        private static void AlternateRangedRepositionDirection(uint attackerId)
        {
            _rangedRepositionDirections[attackerId] = -GetRangedRepositionDirection(attackerId);
        }

        private readonly record struct BlockedLineMovementPlan(
            float DestinationX,
            float DestinationY,
            float DestinationZ,
            float PathCompletionRange,
            float AttackCheckRange,
            bool TrackAttackTarget);

        private readonly record struct RepositionDestination(float X, float Y, float Z);

        private static float ResolveWeaponEngagementRange(
            float weaponEngagementRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon)
                return weaponEngagementRange;

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= CNW_PATHFIND_TOLERANCE)
            {
                return weaponEngagementRange;
            }

            var maximumUsableRange = maxAttackRange - CNW_PATHFIND_TOLERANCE;
            return Math.Min(weaponEngagementRange, maximumUsableRange);
        }

        private static float ResolveEngineDesiredAttackRange(
            float desiredAttackRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon ||
                !float.IsNaN(desiredAttackRange) &&
                !float.IsInfinity(desiredAttackRange) &&
                desiredAttackRange > Combat.MeleeWeaponEngagementRange)
            {
                return desiredAttackRange;
            }

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= Combat.MeleeWeaponEngagementRange)
            {
                return Combat.RangedWeaponEngagementRange;
            }

            var maximumUsableRange = maxAttackRange - CNW_PATHFIND_TOLERANCE;
            return Math.Min(Combat.RangedWeaponEngagementRange, maximumUsableRange);
        }

        private static float ResolveMaximumAttackRange(
            float desiredAttackRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon)
                return maxAttackRange;

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= CNW_PATHFIND_TOLERANCE)
            {
                return desiredAttackRange;
            }

            return Math.Max(maxAttackRange, desiredAttackRange);
        }

        private static bool TryCancelAttackForCombatLeash(CNWSCreature pCreature, CNWSObjectActionNode pNode, uint target)
        {
            if (!AI.TryStartCombatLeashEvade(pCreature.m_idSelf, target))
                return false;

            _creatureAttackDelays.Remove(pCreature.m_idSelf);
            _rangedRepositionDirections.Remove(pCreature.m_idSelf);
            Combat.ClearAttackSwingDebt(pCreature.m_idSelf);
            pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);
            return true;
        }

        private static float MagnitudeSquared(Vector v)
        {
            return v.x * v.x + v.y * v.y + v.z * v.z;
        }

        private static float Sqr(float x)
        {
            return x * x;
        }

        private static bool IsAIState(ushort nAIState, CNWSCreature pCreature)
        {
            return ((pCreature.m_nAIState & nAIState) == nAIState);
        }
    }
}
