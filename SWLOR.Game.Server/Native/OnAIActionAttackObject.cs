using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using NWNX.NET;

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
        private const int FEEDBACK_ACTION_CANT_REACH_TARGET = 218;

        private const int CSERVERAIMASTER_AIACTION_ATTACKOBJECT = 12;
        private const int CSERVERAIMASTER_AIACTION_CHECKMOVETOOBJECTRADIUS = 17;
        private const int CSERVERAIMASTER_AIACTION_CHANGEFACINGOBJECT = 19;
        private const int CNWSOBJECTACTION_PARAMETER_INTEGER = 1;
        private const int CNWSOBJECTACTION_PARAMETER_FLOAT = 2;
        private const int CNWSOBJECTACTION_PARAMETER_OBJECT = 3;

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
        private const int BaseAttackDelay = 1750;

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

                if (!_creatureAttackDelays.ContainsKey(pCreature.m_idSelf))
                {
                    _creatureAttackDelays[pCreature.m_idSelf] = DateTime.MinValue;
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
                    var vTargetPosition = pTarget.m_vPosition;
                    var pTargetArea = pTarget.GetArea();

                    var fMaxAttackRange = pCreature.MaxAttackRange(oidAttackTarget);
                    var fDesiredAttackRange = pCreature.DesiredAttackRange(oidAttackTarget);
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

                            if (pCreature.GetRangeWeaponEquipped() == 0)
                            {
                                newTarget = pCreature.GetNewCombatTarget(oidAttackTarget);
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

                        if (bClearLineOfAttack == 0)
                        {
                            if (!bOutsideAttackRange)
                            {
                                if (pTarget.AsNWSCreature() != null)
                                {
                                    fMoveToTargetRange = pCreature.m_pcPathfindInformation.m_fCreaturePersonalSpace;
                                    fMoveToTargetRange += pTarget.AsNWSCreature().m_pcPathfindInformation.m_fCreaturePersonalSpace;
                                }
                                else
                                {
                                    fMoveToTargetRange = pCreature.m_pcPathfindInformation.m_fPersonalSpace;
                                }
                            }

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
                                vTargetPosition,
                                oidArea,
                                OBJECT_INVALID,
                                bRunToTarget ? 1 : 0,
                                fMoveToTargetRange);
                        }
                        else
                        {
                            pCreature.AddMoveToPointActionToFront(
                                pNode.m_nGroupActionId,
                                vTargetPosition,
                                oidArea,
                                oidAttackTarget,
                                bRunToTarget ? 1 : 0,
                                fMoveToTargetRange);
                        }


                        return ACTION_COMPLETE;
                    }
                }



                if (pCreature.m_pcCombatRound == null)
                {
                    pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);
                    return ACTION_FAILED;
                }

                pCreature.m_vLastAttackPosition = new Vector();

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
                                        pCreature.m_pcCombatRound.SetRoundPaused(1, pCreature.m_idSelf);
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
                                        }
                                    }
                                    break;
                                default:

                                    var delay = Combat.CalculateAttackDelay(pCreature.m_idSelf) + BaseAttackDelay;
                                    if ((DateTime.UtcNow - _creatureAttackDelays[pCreature.m_idSelf]).TotalMilliseconds < delay)
                                    {
                                        return ACTION_IN_PROGRESS;
                                    }
                                    else
                                    {
                                        var isParalyzed = Combat.HandleParalyze(pCreature.m_idSelf);

                                        _creatureAttackDelays[pCreature.m_idSelf] = DateTime.UtcNow;
                                        if (isParalyzed)
                                        {
                                            pCreature.m_pcCombatRound.RecomputeRound();
                                        }
                                        else
                                        {
                                            var result = DoAttack(pPendingAction, pCreature, oidAttackTarget, pNode);
                                            if (result)
                                                bTargetActive = true;
                                        }
                                    }

                                    break;
                            }

                            pPendingAction.Dispose();
                            pPendingAction = null;
                        }
                    }

                    if (bTargetActive == false)
                    {
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

        private static bool DoAttack(CNWSCombatRoundAction pPendingAction, CNWSCreature pCreature, uint oidAttackTarget, CNWSObjectActionNode pNode)
        {
            var oidTarget = pPendingAction.m_oidTarget;
            var nAttacks = 1;
            var nTimeAnimation = 750;
            var result = false;

            pCreature.SetAnimation(9);
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
            pCreature.m_pcCombatRound.SetPauseTimer(nTimeAnimation);
            
            if (pCreature.m_pcCombatRound.m_oidNewAttackTarget != OBJECT_INVALID)
            {
                var oidNewTarget = pCreature.m_pcCombatRound.m_oidNewAttackTarget;
                pCreature.m_bPassiveAttackBehaviour = 1;
                pCreature.ChangeAttackTarget(pNode, oidNewTarget);
                pCreature.m_pcCombatRound.m_oidNewAttackTarget = OBJECT_INVALID;
                oidTarget = oidNewTarget;
                result = true;
            }

            pCreature.ResolveAttack(oidTarget, nAttacks, nTimeAnimation);
            // Note: ProcessWeaponAbility is not available in SWLOR, so we'll skip this for now
            // _ability.ProcessWeaponAbility(pCreature.m_idSelf, oidTarget, 0.75f);

            return result;
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
