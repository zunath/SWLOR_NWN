using NWN.Native.API;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Resolves both equipped melee weapons within the existing animation and delay gate.
    /// NWN chooses the off-hand once CurrentAttack reaches OnHandAttacks + AdditionalAttacks
    /// + BonusEffectAttacks. Merely increasing ResolveAttack's count does not select both hands.
    /// </summary>
    public static class WeaponAttackCycle
    {
        private sealed record PendingHand(uint Target, uint MainWeapon, uint OffWeapon,
            int AttackIndex, int Count, int Duration);

        private static readonly Dictionary<uint, PendingHand> _pendingHands = new();

        /// <summary>Starts both hands on one time gate, resolving each when its swing begins.</summary>
        public static void ResolveDualWield(CNWSCreature creature, uint target, int attacks, int cycleDuration)
        {
            var attacker = creature.m_idSelf;
            CancelPendingHand(attacker);
            var round = creature.m_pcCombatRound;
            var firstAttack = round.m_nCurrentAttack;
            var duration = WeaponAttackAnimation.CalculateSwingDuration(cycleDuration, 2);
            StatusEffect.BeginNativeAttackSwing(attacker);
            try
            {
                creature.ResolveAttack(target, (attacks + 1) / 2, duration);
                WeaponAttackAnimation.Capture(creature, firstAttack, cycleDuration, pendingOffHand: true);
                var pending = new PendingHand(target,
                    GetItemInSlot(InventorySlot.RightHand, attacker),
                    GetItemInSlot(InventorySlot.LeftHand, attacker),
                    round.m_nCurrentAttack, attacks / 2, duration);
                _pendingHands[attacker] = pending;
                // Module ownership lets this callback release the cycle even if the attacker
                // is destroyed. Retain IDs, never native object pointers, across the delay.
                AssignCommand(GetModule(), () => DelayCommand(
                    (duration + WeaponAttackAnimation.TransitionDuration) / 1000f,
                    () => CompleteOffHand(attacker, pending)));
            }
            catch
            {
                _pendingHands.Remove(attacker);
                WeaponAttackAnimation.Cancel(attacker);
                StatusEffect.EndNativeAttackSwing(attacker);
                throw;
            }
        }

        public static void CancelPendingHand(uint attacker)
        {
            if (!_pendingHands.Remove(attacker)) return;
            WeaponAttackAnimation.Cancel(attacker);
            StatusEffect.EndNativeAttackSwing(attacker);
        }

        private static void CompleteOffHand(uint attacker, PendingHand pending)
        {
            if (!_pendingHands.TryGetValue(attacker, out var current) || !ReferenceEquals(current, pending)) return;
            try
            {
                var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker)?.AsNWSCreature();
                var target = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(pending.Target)?.AsNWSObject();
                if (!CanCompleteOffHand(creature, target, pending))
                {
                    WeaponAttackAnimation.Cancel(attacker);
                    return;
                }

                var round = creature.m_pcCombatRound;
                creature.SetAnimation(9);
                round.SetRoundPaused(1, attacker);
                round.SetPauseTimer(pending.Duration);
                creature.ResolveAttack(pending.Target, pending.Count, pending.Duration);
                WeaponAttackAnimation.CompleteOffHand(creature, pending.AttackIndex);
            }
            catch
            {
                WeaponAttackAnimation.Cancel(attacker);
                throw;
            }
            finally
            {
                _pendingHands.Remove(attacker);
                StatusEffect.EndNativeAttackSwing(attacker);
            }
        }

        private static bool CanCompleteOffHand(CNWSCreature creature, CNWSObject target, PendingHand pending)
        {
            if (creature == null || target == null || creature.GetDead() != 0 ||
                creature.GetIsPCDying() != 0 || target.GetDead() != 0 ||
                target.AsNWSCreature()?.GetIsPCDying() == 1 ||
                (creature.m_nAIState & 0x84) != 0x84 ||
                creature.m_pcCombatRound?.m_nCurrentAttack != pending.AttackIndex ||
                !WeaponAttackAnimation.HasQueuedAttack(creature, pending.Target) ||
                !WeaponAttackAnimation.IsPlaying(creature) ||
                GetItemInSlot(InventorySlot.RightHand, creature.m_idSelf) != pending.MainWeapon ||
                GetItemInSlot(InventorySlot.LeftHand, creature.m_idSelf) != pending.OffWeapon)
                return false;

            var area = creature.GetArea();
            if (area == null || area != target.GetArea()) return false;
            var visibility = creature.GetVisibleListElement(pending.Target);
            if (visibility != null && (visibility.m_nSanctuary == 1 ||
                visibility.m_bInvisible == 1 && visibility.m_bHeard == 0 && visibility.m_bSeen == 0))
                return false;
            if (visibility == null && creature.m_bPlayerCharacter == 1 && target.AsNWSCreature() != null)
                return false;
            var range = creature.MaxAttackRange(pending.Target) + 0.01f;
            var x = creature.m_vPosition.x - target.m_vPosition.x;
            var y = creature.m_vPosition.y - target.m_vPosition.y;
            var z = creature.m_vPosition.z - target.m_vPosition.z;
            return x * x + y * y + z * z <= range * range &&
                creature.CheckAttackClearLineToTarget(pending.Target, target.m_vPosition, area) != 0;
        }

        public static int PrepareDualWieldAttacks(CNWSCombatRound round, int attacks)
        {
            attacks = Math.Clamp(attacks, 2, Combat.MaxAttacksPerSwing * 2);
            // GetAttack has 50 slots; leave the final slot available for the engine's
            // post-roll cursor. Ordinary rounds restart well before reaching this bound.
            attacks = Math.Min(attacks, 49 - round.m_nCurrentAttack);
            if (attacks < 2)
                return 0;

            // An odd final roll can result from a single remaining no-delay charge.
            round.m_nOnHandAttacks = round.m_nCurrentAttack + (attacks + 1) / 2;
            round.m_nOffHandAttacks = attacks / 2;
            round.m_nAdditionalAttacks = 0;
            round.m_nBonusEffectAttacks = 0;
            round.m_nOffHandAttacksTaken = 0;
            round.m_nExtraAttacksTaken = 0;
            return attacks;
        }
    }
}
