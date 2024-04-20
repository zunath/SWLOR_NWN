using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using Random = SWLOR.Game.Server.Service.Random;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class AIActionAttackObject
    {
        private enum ActionResult
        {
            InProgress = 1,
            Complete = 2,
            Failed = 3
        }

        private enum AttackResult
        {
            Invalid = 0,
            HitSuccessful = 1,
            Parried = 2,
            CriticalHit = 3,
            Miss = 4,
            AttackResisted = 5,
            AttackFailed = 6,
            AutomaticHit = 7,
            TargetConcealed = 8,
            AttackerMissChance = 9,
            DevastatingCritical = 10,
        }

        private class CombatData
        {
            public CombatData(CNWSCreature creature)
            {
                Creature = creature;
            }

            public CNWSCreature Creature { get; set; }

            public long LastTickMs { get; set; }

            public long Cooldown { get; set; }

            public uint Target { get; set; }

            public bool IsBusy { get; set; }

            public void EndCombat()
            {
                Creature.m_oidAttackTarget = OBJECT_INVALID;
                Creature.m_vLastAttackPosition = new Vector(0f, 0f, 0f);
                Creature.m_oidCurrentActionTarget = OBJECT_INVALID;
                Creature.SetLockOrientationToObject(OBJECT_INVALID);
                Creature.SetAnimation((int)Animation.Ready);
            }

            public bool IsTargetValid(CNWSObject target)
            {
                if (target == null)
                {
                    return false;
                }

                if (target.m_nObjectType != (int)ObjectType.Creature &&
                    target.m_nObjectType != (int)ObjectType.Door &&
                    target.m_nObjectType != (int)ObjectType.Placeable)
                {
                    return false;
                }

                if (target.m_idSelf == Creature.m_idSelf)
                {
                    return false;
                }

                if (target.GetDead() == 1)
                {
                    return false;
                }

                var visNode = Creature.GetVisibleListElement(target.m_idSelf);
                if (visNode != null)
                {
                    if (visNode.m_bInvisible == 1 && visNode.m_bSeen == 0 && visNode.m_bHeard == 0)
                    {
                        return false;
                    }
                }
                else if (target.AsNWSCreature() != null) // todo check for dm
                {
                    return false;
                }

                return true;
            }

            private float VectorMagnitudeSquared(Vector3 v)
            {
                return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            }

            private float Square(float val)
            {
                return val * val;
            }

            public bool IsTargetInRange(CNWSObject target)
            {
                if (Creature.m_oidArea != target.m_oidArea)
                    return false;

                if (Creature.CheckAttackClearLineToTarget(target.m_idSelf, target.m_vPosition, target.GetArea()) == 0)
                {
                    return false;
                }

                var distance = VectorMagnitudeSquared(
                    new Vector3(
                        Creature.m_vPosition.x - target.m_vPosition.x,
                        Creature.m_vPosition.y - target.m_vPosition.y,
                        Creature.m_vPosition.z - target.m_vPosition.z));
                
                var maxDistance = Square(Creature.MaxAttackRange(target.m_idSelf) + 0.01f);

                if (distance > maxDistance)
                {
                    return false;
                }

                return true;
            }

            public void DoRangeAdjustment()
            {
                IsBusy = true;

                Scheduler.Schedule(() =>
                {
                    var target = NWNXLib.AppManager().m_pServerExoApp.GetGameObject(Target)?.AsNWSObject();

                    if (target != null)
                    {
                        var distance = VectorMagnitudeSquared(
                            new Vector3(
                                Creature.m_vPosition.x - target.m_vPosition.x,
                                Creature.m_vPosition.y - target.m_vPosition.y,
                                Creature.m_vPosition.z - target.m_vPosition.z));

                        var desiredDistance = Square(Creature.DesiredAttackRange(Target));

                        if (Math.Abs(distance - desiredDistance) > 0.25f)
                            Creature.DoCombatStep(0, 1000, Target);

                        IsBusy = false;
                    }
                }, TimeSpan.FromMilliseconds(1000));
            }

            public void DoAttack()
            {
                IsBusy = true;

                var duration = Random.Next(3000);
                Scheduler.Schedule(() =>
                {
                    var target = NWNXLib.AppManager().m_pServerExoApp.GetGameObject(Target)?.AsNWSObject();

                    if (target != null)
                    {
                        Creature.ActivityManager(0x4);
                        Creature.SetAnimation((int)Animation.Attack);

                        var round = Creature.m_pcCombatRound;
                        var attackData = round.GetAttack(0);

                        round.m_nAttackGroup = 1;
                        round.m_nCurrentAttack = 1;
                        attackData.m_nAttackGroup = 1;

                        var animationTime = (ushort)duration;

                        switch (Random.D3(1))
                        {
                            case 0:
                                attackData.m_nAttackResult = (int)AttackResult.Parried;
                                attackData.m_nReactionAnimation = (int)Animation.Parry;
                                attackData.m_nReactionDelay = (ushort)(animationTime / 2);
                                attackData.m_nReactionAnimationLength = animationTime;
                                attackData.m_nAnimationLength = animationTime;
                                break;
                            case 1:
                                attackData.m_nAttackResult = (int)AttackResult.Miss;
                                attackData.m_nReactionAnimation = (int)Animation.Dodge;
                                attackData.m_nReactionDelay = 0;
                                attackData.m_nReactionAnimationLength = animationTime;
                                attackData.m_nAnimationLength = animationTime;
                                break;
                            case 2:
                                attackData.m_nAttackResult = (int)AttackResult.HitSuccessful;
                                attackData.m_nReactionAnimation = (int)Animation.Damage;
                                attackData.m_nReactionDelay = (ushort)(animationTime / 2);
                                attackData.m_nReactionAnimationLength = animationTime;
                                attackData.m_nAnimationLength = animationTime;
                                break;
                        }

                        IsBusy = false;
                    }
                    else
                    {
                        EndCombat();
                    }
                }, TimeSpan.FromMilliseconds(duration));
            }

            public void RecalculateCombat()
            {
                if (Creature.m_nDesiredCombatMode != (int)CombatMode.None)
                {
                    Creature.SetCombatMode(Creature.m_nDesiredCombatMode, 1);
                    Creature.m_nDesiredCombatMode = (int)CombatMode.None;
                }
                Creature.SetLockOrientationToObject(Target);

                var round = Creature.m_pcCombatRound;
                var attackData = round.GetAttack(0);

                round.m_nAttackGroup = 1;
                round.m_nCurrentAttack = 1;
                attackData.m_nAttackGroup = 1;

                DoRangeAdjustment();
                DoAttack();
            }

            public ActionResult Tick()
            {
                var now = DateTime.UtcNow.Ticks;
                var diff = now - LastTickMs;
                if (diff == 0)
                {
                    return ActionResult.InProgress;
                }

                if (Cooldown > 0)
                {
                    Cooldown -= Math.Min(diff, Cooldown);
                }

                if (!IsBusy)
                {
                    RecalculateCombat();
                    return ActionResult.InProgress;
                }

                return ActionResult.InProgress;
            }

        }

        internal delegate uint AIActionAttackObjectHook(void* thisPtr, void* pNode);

        // ReSharper disable once NotAccessedField.Local
        private static AIActionAttackObjectHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, uint> pHook = &OnAIActionAttackObject;
            var hookPtr = VM.RequestHook(NativeLibrary.GetExport(
                    NativeLibrary.GetMainProgramHandle(),
                    "_ZN12CNWSCreature20AIActionAttackObjectEP20CNWSObjectActionNode"),
                (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<AIActionAttackObjectHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static uint OnAIActionAttackObject(void* thisPtr, void* pNode)
        {
            var attacker = CNWSCreature.FromPointer(thisPtr);
            var actionNode = CNWSObjectActionNode.FromPointer(pNode);
            var targetId = (uint)actionNode.m_pParameter[0];
            var target = NWNXLib.AppManager().m_pServerExoApp.GetGameObject(targetId).AsNWSCreature();
            var combatData = new CombatData(attacker);

            if (!combatData.IsTargetValid(target))
            {
                combatData.EndCombat();
                return (int)ActionResult.Failed;
            }

            if (!combatData.IsTargetInRange(target))
            {
                combatData.EndCombat();
                return (int)ActionResult.Failed;
            }

            combatData.Target = targetId;

            return (uint)combatData.Tick();
        }
    }
}
