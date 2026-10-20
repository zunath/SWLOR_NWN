using System;
using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Ability = SWLOR.Game.Server.Service.Ability;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Budgets the companion as a ranged roll without exposing its carrier to native melee resolution.</summary>
    public static unsafe class PairedRangedAttackCycle
    {
        private sealed record Pending(uint Target, WeaponShotProfile Main, WeaponShotProfile Companion,
            uint Ammunition, bool Unlimited, int AttackIndex, int Count);
        private static readonly Dictionary<uint, Pending> PendingHands = new();

        public static void Resolve(CNWSCreature creature, uint target, int attacks, int duration)
        {
            var actor = creature.m_idSelf;
            Cancel(actor);
            var main = WeaponShotProfile.Capture(actor, GetItemInSlot(InventorySlot.RightHand, actor), SkillType.TwinPistol);
            var companion = WeaponShotProfile.Capture(actor, TwinPistolEquipment.GetCompanionWeapon(actor), SkillType.TwinPistol);
            var ammunition = GetItemInSlot(InventorySlot.Bullets, actor);
            var unlimited = creature.m_bMagicalBulletsEquipped != 0;
            var round = creature.m_pcCombatRound;
            // GetAttack owns fifty slots. Keep the post-roll cursor and the borrowed
            // companion slot inside that native buffer, including unusual haste batches.
            attacks = Math.Clamp(attacks, 0, Combat.MaxAttacksPerSwing * 2);
            var mainCapacity = Math.Max(0, 49 - round.m_nCurrentAttack);
            attacks = Math.Min(attacks, mainCapacity * 2);
            // Native ranged ammunition may be debited after ResolveAttack returns.
            // Reserve its share before scheduling the companion instead of treating
            // that still-visible stack as fresh ammunition for the second launcher.
            if (!unlimited)
                attacks = Math.Min(attacks, GetIsObjectValid(ammunition) ? GetItemStackSize(ammunition) : 0);
            if (attacks == 0) return;
            StatusEffect.BeginNativeAttackSwing(actor);
            try
            {
                var mainAttacks = (attacks + 1) / 2;
                // Every native roll belongs to the real launcher. The carrier never selects a hand.
                round.m_nOnHandAttacks = round.m_nCurrentAttack + mainAttacks;
                round.m_nOffHandAttacks = 0;
                round.m_nAdditionalAttacks = 0;
                round.m_nBonusEffectAttacks = 0;
                round.m_nOffHandAttacksTaken = 0;
                round.m_nExtraAttacksTaken = 0;
                creature.ResolveAttack(target, mainAttacks, duration);
                var pending = new Pending(target, main, companion, ammunition, unlimited,
                    round.m_nCurrentAttack, attacks / 2);
                PendingHands[actor] = pending;
                if (pending.Count == 0)
                {
                    Finish(actor, pending);
                    return;
                }
                AssignCommand(GetModule(), () => DelayCommand(.45f, () => Finish(actor, pending)));
            }
            catch
            {
                PendingHands.Remove(actor);
                StatusEffect.EndNativeAttackSwing(actor);
                throw;
            }
        }

        public static void Cancel(uint actor)
        {
            if (PendingHands.Remove(actor)) StatusEffect.EndNativeAttackSwing(actor);
        }

        private static void Finish(uint actor, Pending pending)
        {
            if (!PendingHands.TryGetValue(actor, out var current) || !ReferenceEquals(current, pending)) return;
            try
            {
                for (var count = 0; count < pending.Count; count++)
                {
                    var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor)?.AsNWSCreature();
                    var target = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(pending.Target)?.AsNWSObject();
                    if (!CanLaunch(creature, target, pending) || !ConsumeAmmunition(creature, pending)) break;
                    ResolveCompanion(creature, target, pending.Companion);
                }
            }
            finally
            {
                PendingHands.Remove(actor);
                StatusEffect.EndNativeAttackSwing(actor);
            }
        }

        private static bool CanLaunch(CNWSCreature creature, CNWSObject target, Pending pending)
        {
            if (creature == null || target == null || creature.GetDead() != 0 || creature.GetIsPCDying() != 0 ||
                target.GetDead() != 0 || target.AsNWSCreature()?.GetIsPCDying() == 1 ||
                (creature.m_nAIState & 0x84) != 0x84 || creature.m_pcCombatRound == null ||
                creature.m_pcCombatRound.m_nCurrentAttack != pending.AttackIndex ||
                !WeaponAttackAnimation.HasQueuedAttack(creature, pending.Target) ||
                !TwinPistolEquipment.IsActivePair(creature.m_idSelf) ||
                GetItemInSlot(InventorySlot.RightHand, creature.m_idSelf) != pending.Main.Weapon ||
                TwinPistolEquipment.GetCompanionWeapon(creature.m_idSelf) != pending.Companion.Weapon ||
                !pending.Main.IsUnchanged() || !pending.Companion.IsUnchanged()) return false;
            var area = creature.GetArea();
            if (area == null || area != target.GetArea()) return false;
            var visible = creature.GetVisibleListElement(pending.Target);
            if (visible != null && (visible.m_nSanctuary == 1 ||
                visible.m_bInvisible == 1 && visible.m_bSeen == 0 && visible.m_bHeard == 0)) return false;
            if (visible == null && creature.m_bPlayerCharacter == 1 && target.AsNWSCreature() != null) return false;
            var range = creature.MaxAttackRange(pending.Target) + .01f;
            var delta = GetPosition(creature.m_idSelf) - GetPosition(pending.Target);
            return delta.LengthSquared() <= range * range &&
                creature.CheckAttackClearLineToTarget(pending.Target, target.m_vPosition, area) != 0;
        }

        private static bool ConsumeAmmunition(CNWSCreature creature, Pending pending)
        {
            if (pending.Unlimited) return creature.m_bMagicalBulletsEquipped != 0;
            var ammunition = GetItemInSlot(InventorySlot.Bullets, creature.m_idSelf);
            if (ammunition != pending.Ammunition || !GetIsObjectValid(ammunition)) return false;
            var quantity = GetItemStackSize(ammunition);
            if (quantity <= 0) return false;
            if (quantity == 1) DestroyObject(ammunition);
            else SetItemStackSize(ammunition, quantity - 1);
            return true;
        }

        private static void ResolveCompanion(CNWSCreature creature, CNWSObject target, WeaponShotProfile profile)
        {
            var actor = creature.m_idSelf;
            var round = creature.m_pcCombatRound;
            var data = round.GetAttack(round.m_nCurrentAttack);
            var original = WeaponAttackAnimation.Roll.Read(data);
            var threat = data.m_bCriticalThreat;
            var threatRoll = data.m_nThreatRoll;
            var toHitRoll = data.m_nToHitRoll;
            var toHitModifier = data.m_nToHitMod;
            var missedBy = data.m_nMissedBy;
            using var selectedWeapon = RangedWeaponAttackContext.SelectProfile(actor, profile);
            try
            {
                data.m_bRangedAttack = 1;
                data.m_nWeaponAttackType = 1;
                data.m_nAttackResult = 4;
                data.m_bCriticalThreat = 0;
                data.m_nThreatRoll = 0;
                for (var index = 0; index < 32; index++) data.m_nDamage[index] = 0;
                ResolveAttackRoll.ResolveWeaponRoll((void*)creature.Pointer, (void*)target.Pointer);
                var landed = ResolveAttackRoll.IsSuccessfulAttackResult(data.m_nAttackResult);
                PairedRangedPresentation.OnCompanionLaunch(actor, target.m_idSelf, landed);
                if (!landed) return;
                var queued = UsePerkFeat.HasQueuedWeaponAbility(actor, SkillType.TwinPistol, round.m_nCurrentAttack);
                var impactActions = new List<Action<int>>();
                var convertedDamage = new List<Action>();
                Func<int> resolveDamageTaken = null;
                GetDamageRoll.ResolveWeaponDamage((void*)creature.m_pStats.Pointer, (void*)target.Pointer, 0,
                    data.m_nAttackResult == 3 ? 1 : 0, convertedDamageScheduler: convertedDamage.Add,
                    weaponImpactScheduler: impactActions.Add,
                    damageTakenResolverCapture: resolution => resolveDamageTaken = resolution);
                var targetId = target.m_idSelf;
                var weaponId = profile.Weapon;
                var launchArea = GetArea(actor);
                var travel = PairedRangedPresentation.FlightSeconds(GetDistanceBetween(actor, targetId));
                if (queued)
                {
                    var applyQueuedHit = UsePerkFeat.CaptureQueuedWeaponAbilityHit(actor, targetId, weaponId, () =>
                    {
                        Ability.RecordLeadershipWeaponHit(actor, targetId);
                        Loot.RecordWeaponHitFindBonuses(actor, targetId);
                    });
                    if (applyQueuedHit != null)
                        AssignCommand(GetModule(), () => DelayCommand(travel, applyQueuedHit));
                    return;
                }
                var shot = RangedWeaponAttackContext.GetRoll(actor, target.m_idSelf, round.m_nCurrentAttack);
                if (shot != null && (shot.Damage > 0 || impactActions.Count > 0 || convertedDamage.Count > 0))
                {
                    var damage = shot.Damage;
                    var damageType = shot.DamageType;
                    var skill = profile.Skill;
                    var attribution = CombatRecorder.CaptureAttackAttribution(actor);
                    AssignCommand(GetModule(), () => DelayCommand(travel, () =>
                    {
                        // Area-exit cleanup cannot cancel a shot already in flight, so both sides must still be where it launched.
                        if (!GetIsObjectValid(actor) || !GetIsObjectValid(targetId) || GetIsDead(targetId) ||
                            GetArea(actor) != launchArea || GetArea(targetId) != launchArea) return;
                        using var recorderScope = CombatRecorder.BeginAttribution(attribution);
                        var resolvedDamage = resolveDamageTaken?.Invoke() ?? damage;
                        shot.Damage = resolvedDamage;
                        CombatPoint.RecordWeaponAttackPoint(actor, targetId, weaponId, skill);
                        Ability.RecordLeadershipWeaponHit(actor, targetId);
                        Loot.RecordWeaponHitFindBonuses(actor, targetId);
                        TwinPistolMastery.RecordOrdinaryHit(actor, targetId, true);
                        foreach (var applyImpact in impactActions) applyImpact(resolvedDamage);
                        ResolvedCombatDamage.Apply(actor, targetId, resolvedDamage, damageType);
                        foreach (var applyConvertedDamage in convertedDamage) applyConvertedDamage();
                    }));
                }
            }
            finally
            {
                original.Write(data);
                data.m_bCriticalThreat = threat;
                data.m_nThreatRoll = threatRoll;
                data.m_nToHitRoll = toHitRoll;
                data.m_nToHitMod = toHitModifier;
                data.m_nMissedBy = missedBy;
            }
        }
    }
}
