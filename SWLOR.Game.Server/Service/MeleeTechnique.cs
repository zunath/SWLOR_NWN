using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Enemy-bound melee sequences driven by ability metadata and source-scoped stats.</summary>
    public static class MeleeTechnique
    {
        private static readonly Dictionary<uint, OwnerState> Owners = new();
        private static readonly Dictionary<uint, Activation> Activations = new();
        private static readonly Dictionary<uint, OrdinaryAttempt> OrdinaryAttempts = new();
        private static readonly Dictionary<uint, DirectDamageSnapshot> DirectDamage = new();

        /// <summary>One notification per completed, landed cast that spent matching setup.</summary>
        public static event Action<uint, uint, MeleeTechniqueDetail, MeleeTechniqueStage> TechniqueSpent;

        public static int GetLineStat(uint creature, MeleeTechniqueLine line, StatType payload) =>
            Stat.GetStatSources(creature, payload)
                .Where(source => source[StatType.TechniqueLine] == (int)line)
                .Sum(source => source[payload]);

        public static string ValidateActivation(uint creature, AbilityDetail ability)
        {
            if (ability?.MeleeTechnique == null)
                return string.Empty;
            if (!EquipmentPredicates.HasUnarmedGrip(creature))
                return "Equip a Power Glove with an empty off hand or another Power Glove.";
            if (ability.IsHostileAbility && !GetOwner(creature).Resources.CanActivate(DateTime.UtcNow))
                return "Wait a moment before using another hand-to-hand attack.";
            return string.Empty;
        }

        /// <summary>Called after all paid requirements, before an impact is launched.</summary>
        public static void BeginPaidActivation(uint creature, uint target, AbilityDetail ability)
        {
            if (ability == null)
                return;
            var detail = ability.MeleeTechnique;
            if (detail?.ClearSetup == true)
                ClearSetup(creature);
            if (!ability.IsHostileAbility)
                return;

            var owner = GetOwner(creature);
            Refresh(creature, owner);
            if (owner.Reactions.WindowTarget != 0 && owner.Reactions.WindowTarget != target)
                ClearReactiveWindows(creature, owner);
            OrdinaryAttempts.Remove(creature);
            Activations.Remove(creature);
            var now = DateTime.UtcNow;
            owner.LastHostileActivity = now;

            if (detail == null || !EquipmentPredicates.HasUnarmedGrip(creature))
            {
                ClearSetup(creature);
                return;
            }

            var matching = owner.Line == detail.Line && owner.Sequence.Matches(target, now);
            var stage = matching && (detail.ConsumedStages & owner.Sequence.Stage) != 0
                ? owner.Sequence.Stage : MeleeTechniqueStage.None;
            var retaliation = stage == MeleeTechniqueStage.Distraction && detail.CreatedStage == MeleeTechniqueStage.None
                ? owner.Reactions.TakeRetaliation(target, now) : 0;
            if (!detail.PreservesMatchingSetup || !matching)
                ClearSetup(creature);

            owner.Resources.LockAction(now);
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var off = GetItemInSlot(InventorySlot.LeftHand, creature);
            var conditionalPower = stage == MeleeTechniqueStage.None ? 0 :
                detail.GetStagePower(stage) + GetLineStat(creature, detail.Line, StatType.TechniqueConditionalPower) + retaliation;
            Activations[creature] = new Activation
            {
                Ability = ability,
                Target = target,
                Stage = stage,
                ConditionalPower = conditionalPower,
                ConditionalAccuracy = stage == MeleeTechniqueStage.None ? 0 :
                    GetLineStat(creature, detail.Line, StatType.TechniqueConditionalAccuracy),
                Weapon = main,
                OffHand = off,
                Area = GetArea(creature),
                WeaponDamage = WeaponDamage.GetEffectiveDMG(creature, main),
                WeaponDamageType = WeaponDamage.GetDamageType(main)
            };
        }

        public static int GetConditionalPower(uint creature, uint target, int impactIndex = 0)
        {
            if (!TryGetActivation(creature, target, out var activation))
                return 0;
            return TechniqueSequence.SplitPower(activation.ConditionalPower,
                activation.Ability.MeleeTechnique.ImpactCount, impactIndex);
        }

        public static int GetConditionalAccuracy(uint creature, uint target) =>
            TryGetActivation(creature, target, out var activation) ? activation.ConditionalAccuracy : 0;

        public static MeleeTechniqueStage GetSetupStage(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return MeleeTechniqueStage.None;
            Refresh(creature, owner);
            return owner.Sequence.Matches(target, DateTime.UtcNow) ? owner.Sequence.Stage : MeleeTechniqueStage.None;
        }

        public static int GetPendingWeavePower(uint creature, uint target) =>
            GetSetupStage(creature, target) == MeleeTechniqueStage.Opening ? Owners[creature].Sequence.WeavePower : 0;

        public static int GetPendingRetaliationPower(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            return owner.Reactions.GetRetaliation(target, DateTime.UtcNow);
        }

        public static int? GetCapturedWeaponDamage(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamage : null;

        public static CombatDamageType? GetCapturedWeaponDamageType(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamageType : null;

        public static bool IsImpactValid(uint creature, uint target, AbilityDetail ability) =>
            ability?.MeleeTechnique == null ||
            TryGetActivation(creature, target, out var activation) && activation.Ability == ability;

        public static void CompleteActivation(uint creature, AbilityImpactSummary summary)
        {
            if (!Activations.TryGetValue(creature, out var activation) || activation.Ability != summary?.Ability)
                return;
            Activations.Remove(creature);
            var detail = activation.Ability.MeleeTechnique;
            var landed = summary.ImpactedTargetCount > 0;
            if (!landed || !IsGripSnapshotValid(creature, activation.Weapon, activation.OffHand, activation.Area) ||
                !GetIsObjectValid(activation.Target) || GetArea(activation.Target) != activation.Area ||
                !GetIsReactionTypeHostile(activation.Target, creature))
                return;

            var owner = GetOwner(creature);
            if (activation.Stage != MeleeTechniqueStage.None)
            {
                if (detail.RefundEligible)
                {
                    var requested = GetLineStat(creature, detail.Line, StatType.TechniqueRefundStamina);
                    var cooldown = GetLineStat(creature, detail.Line, StatType.TechniqueRefundCooldownSeconds);
                    var paid = Combat.GetPaidAbilityStaminaCost(creature, activation.Ability);
                    if (owner.Resources.TryRefund(paid, requested, cooldown, DateTime.UtcNow, out var amount))
                        Combat.RestoreAbilityHitStamina(creature, activation.Ability, amount, retainMinimumCost: false);
                }
                TechniqueSpent?.Invoke(creature, activation.Target, detail, activation.Stage);
                if (detail.CreatedStage == MeleeTechniqueStage.None && IsValidEnemy(creature, activation.Target))
                    TryOpenFinisherWindow(creature, activation.Target, detail.Line, activation.Stage, owner);
            }

            if (!IsValidEnemy(creature, activation.Target) || detail.CreatedStage == MeleeTechniqueStage.None ||
                detail.CreatesStageOnlyWhenSequenced && activation.Stage == MeleeTechniqueStage.None)
                return;

            var duration = detail.SetupDurationSeconds + GetLineStat(creature, detail.Line, StatType.TechniqueWindowExtensionSeconds);
            var status = detail.SourceOwnedSetupStatusEffectFactory?.Invoke();
            if (status != null)
            {
                if (!StatusEffect.ApplyStatusEffect(creature, activation.Target, status, duration, activation.WeaponDamageType))
                    return;
                duration = (int)Math.Ceiling(status.GetRemainingDurationSeconds(DateTime.UtcNow));
            }
            else
            {
                StatusEffect.ApplyStatusEffect(creature, creature,
                    detail.CreatedStage == MeleeTechniqueStage.Finish
                        ? new FinishStatusEffect() : new OpeningStatusEffect(), duration);
            }
            owner.Line = detail.Line;
            owner.Skill = summary.SkillType;
            owner.Area = activation.Area;
            owner.MainHand = activation.Weapon;
            owner.OffHand = activation.OffHand;
            owner.SourceOwnedStatusType = status?.GetType();
            owner.OrdinaryAccuracy = detail.OrdinaryAccuracyBonus +
                (status == null ? 0 : GetLineStat(creature, detail.Line, StatType.TechniqueDebuffTargetOrdinaryAccuracy));
            var weave = detail.CreatedStage == MeleeTechniqueStage.Opening &&
                        GetLineStat(creature, detail.Line, StatType.TechniqueWeaveEnabled) > 0
                ? GetLineStat(creature, detail.Line, StatType.TechniqueWeavePower) : 0;
            owner.Sequence.Begin(activation.Target, detail.CreatedStage, DateTime.UtcNow.AddSeconds(duration), weave);
            if (detail.CreatedStage == MeleeTechniqueStage.Distraction && status != null)
                owner.Reactions.BeginBrace(activation.Target, owner.Sequence.ExpiresAt,
                    GetLineStat(creature, detail.Line, StatType.TechniqueSetupBraceWindowSeconds), DateTime.UtcNow);
        }

        private static void TryOpenFinisherWindow(uint creature, uint target, MeleeTechniqueLine line,
            MeleeTechniqueStage stage, OwnerState owner)
        {
            var evasion = GetLineStat(creature, line, StatType.TechniqueOpeningFinisherEvasionPercent);
            var movement = GetLineStat(creature, line, StatType.TechniqueOpeningFinisherMovementSpeedPercent);
            var haste = GetLineStat(creature, line, StatType.TechniqueFinishFinisherHastePercent);
            if (stage == MeleeTechniqueStage.Opening && evasion <= 0 && movement <= 0 ||
                stage == MeleeTechniqueStage.Finish && haste <= 0)
                return;
            var duration = GetLineStat(creature, line, StatType.TechniqueFinisherWindowDurationSeconds);
            if (!owner.Reactions.TryOpenWindow(target, stage, duration,
                    GetLineStat(creature, line, StatType.TechniqueFinisherWindowCooldownSeconds), DateTime.UtcNow))
                return;
            StatusEffect.RemoveStatusEffect(creature, typeof(SlipAwayStatusEffect), false);
            StatusEffect.RemoveStatusEffect(creature, typeof(PressForwardStatusEffect), false);
            owner.WindowLine = line;
            if (stage == MeleeTechniqueStage.Opening)
                StatusEffect.ApplyStatusEffect(creature, creature, new SlipAwayStatusEffect(evasion, movement), duration);
            else
                StatusEffect.ApplyStatusEffect(creature, creature, new PressForwardStatusEffect(haste), duration);
        }

        public static void AbortActivation(uint creature) => Activations.Remove(creature);

        /// <summary>One sample per real native roll, including misses, excluding queued replacements.</summary>
        public static int PrepareOrdinaryAttempt(uint creature, uint target, SkillType skill, bool queuedReplacement, int attackIndex)
        {
            OrdinaryAttempts.Remove(creature);
            if (!Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            if (owner.Reactions.WindowTarget != 0 && owner.Reactions.WindowTarget != target)
                ClearReactiveWindows(creature, owner);
            if (queuedReplacement || owner.Sequence.Stage == MeleeTechniqueStage.None)
                return 0;
            if (skill != owner.Skill || !owner.Sequence.Matches(target, DateTime.UtcNow))
            {
                ClearSetup(creature);
                return 0;
            }
            var power = owner.Sequence.TakeWeave(target, DateTime.UtcNow);
            OrdinaryAttempts[creature] = new OrdinaryAttempt(target, skill, attackIndex, power);
            return owner.OrdinaryAccuracy;
        }

        /// <summary>Duplicate/discarded damage queries cannot pay the sampled weave again.</summary>
        public static int TakeOrdinaryPower(uint creature, uint target, SkillType skill, int attackIndex, bool landed)
        {
            if (!landed || !OrdinaryAttempts.TryGetValue(creature, out var attempt) ||
                attempt.Target != target || attempt.Skill != skill || attempt.AttackIndex != attackIndex)
                return 0;
            OrdinaryAttempts.Remove(creature);
            return attempt.Power;
        }

        public static int ApplyOrdinaryDamageModifiers(uint creature, int damage, SkillType skill, CombatDamageType damageType)
        {
            var adjustment = Stat.GetStatSources(creature, StatType.OrdinarySkillDamagePercentAdjustment)
                .Where(source => source[StatType.OrdinarySkillDamagePercentAdjustmentSkillType] == (int)skill)
                .Sum(source => source[StatType.OrdinarySkillDamagePercentAdjustment]);
            damage = Combat.ApplyPercentDamageAdjustment(damage, adjustment);
            if (damageType.IsPhysicalDamageType())
                damage = Combat.ApplyPercentDamageAdjustment(damage,
                    -Stat.GetStatAdjustment(creature, StatType.OrdinaryPhysicalDamagePenaltyPercent));
            return damage;
        }

        public static int GetStaminaDiscount(uint creature, AbilityDetail ability, int cost, bool consume)
        {
            if (cost <= 0 || ability?.MeleeTechnique == null || !EquipmentPredicates.HasUnarmedGrip(creature) ||
                !Owners.TryGetValue(creature, out var owner) || owner.DiscountLine != ability.MeleeTechnique.Line)
                return 0;
            Refresh(creature, owner);
            var amount = consume ? owner.Resources.TakeDiscount(cost, DateTime.UtcNow) :
                Math.Min(cost, owner.Resources.GetDiscount(DateTime.UtcNow));
            if (consume && amount > 0)
                StatusEffect.RemoveStatusEffect(creature, typeof(PaybackStatusEffect), false);
            return amount;
        }

        /// <summary>Captures actual HP before a typed direct hit; periodic/triggered/reflected paths never enter.</summary>
        public static void PrepareDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct || attacker == defender ||
                !GetIsObjectValid(defender) || !GetIsObjectValid(attacker) ||
                !GetIsReactionTypeHostile(attacker, defender) || !EquipmentPredicates.HasUnarmedGrip(defender))
                return;
            DirectDamage[defender] = new DirectDamageSnapshot(attacker,
                Math.Max(0, GetCurrentHitPoints(defender) - TemporaryHitPointEffects.GetRemaining(defender)), DateTime.UtcNow);
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void OnDirectDamageApplied()
        {
            var creature = OBJECT_SELF;
            if (!DirectDamage.Remove(creature, out var snapshot) ||
                snapshot.Attacker != GetLastDamager(creature) ||
                (DateTime.UtcNow - snapshot.CapturedAt).TotalSeconds > 1 || GetIsDead(creature))
                return;
            var hp = Math.Max(0, GetCurrentHitPoints(creature) - TemporaryHitPointEffects.GetRemaining(creature));
            if (hp >= snapshot.HitPoints || !EquipmentPredicates.HasUnarmedGrip(creature) ||
                !GetIsReactionTypeHostile(snapshot.Attacker, creature))
                return;

            var owner = GetOwner(creature);
            owner.LastHostileActivity = DateTime.UtcNow;
            Refresh(creature, owner);
            if (owner.Sequence.Stage == MeleeTechniqueStage.Distraction &&
                owner.Sequence.Matches(snapshot.Attacker, DateTime.UtcNow) &&
                owner.Reactions.TryGrantRetaliation(snapshot.Attacker,
                    GetLineStat(creature, owner.Line, StatType.TechniqueSetupRetaliationPower), owner.Sequence.ExpiresAt,
                    GetLineStat(creature, owner.Line, StatType.TechniqueSetupRetaliationCooldownSeconds), DateTime.UtcNow))
                StatusEffect.ApplyStatusEffect(creature, creature, new RetaliationStatusEffect(),
                    (float)Math.Max(0, (owner.Sequence.ExpiresAt - DateTime.UtcNow).TotalSeconds));
            foreach (var source in Stat.GetStatSources(creature, StatType.DirectHostileDamageNextTechniqueStaminaDiscount))
            {
                var line = (MeleeTechniqueLine)source[StatType.TechniqueLine];
                if (line == MeleeTechniqueLine.None || !owner.Resources.TryGrantDiscount(
                    source[StatType.DirectHostileDamageNextTechniqueStaminaDiscount],
                    source[StatType.DirectHostileDamageNextTechniqueDiscountDurationSeconds],
                    source[StatType.DirectHostileDamageNextTechniqueDiscountCooldownSeconds], DateTime.UtcNow))
                    continue;
                owner.DiscountLine = line;
                StatusEffect.ApplyStatusEffect(creature, creature, new PaybackStatusEffect(),
                    source[StatType.DirectHostileDamageNextTechniqueDiscountDurationSeconds]);
                break;
            }
        }

        public static void ClearSetup(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return;
            if (owner.SourceOwnedStatusType != null && GetIsObjectValid(owner.Sequence.Target))
                StatusEffect.RemoveStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature, false);
            owner.SourceOwnedStatusType = null;
            owner.Sequence.Clear();
            owner.Reactions.ClearSetupBenefits();
            OrdinaryAttempts.Remove(creature);
            if (GetIsObjectValid(creature))
            {
                StatusEffect.RemoveStatusEffect(creature, typeof(OpeningStatusEffect), false);
                StatusEffect.RemoveStatusEffect(creature, typeof(FinishStatusEffect), false);
                StatusEffect.RemoveStatusEffect(creature, typeof(RetaliationStatusEffect), false);
            }
        }

        public static void OnStanceChanged(uint creature)
        {
            ClearSetup(creature);
            if (Owners.TryGetValue(creature, out var owner))
                ClearReactiveWindows(creature, owner);
        }

        private static void ClearReactiveWindows(uint creature, OwnerState owner)
        {
            owner.Reactions.ClearWindow();
            if (!GetIsObjectValid(creature))
                return;
            StatusEffect.RemoveStatusEffect(creature, typeof(SlipAwayStatusEffect), false);
            StatusEffect.RemoveStatusEffect(creature, typeof(PressForwardStatusEffect), false);
        }

        public static void ClearBenefits(uint creature)
        {
            ClearSetup(creature);
            Activations.Remove(creature);
            if (Owners.TryGetValue(creature, out var owner))
            {
                owner.Resources.ClearBenefits();
                ClearReactiveWindows(creature, owner);
            }
            if (GetIsObjectValid(creature))
                StatusEffect.RemoveStatusEffect(creature, typeof(PaybackStatusEffect), false);
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip() => OnEquipmentChanged(GetPCItemLastEquippedBy());

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip() => OnEquipmentChanged(GetPCItemLastUnequippedBy());

        private static void OnEquipmentChanged(uint creature)
        {
            StatusEffect.RemoveInvalidEquipmentEffects(creature);
            ClearSetup(creature);
            Activations.Remove(creature);
            if (!EquipmentPredicates.HasUnarmedGrip(creature))
                ClearBenefits(creature);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnExit()
        {
            var creature = GetExitingObject();
            ClearBenefits(creature);
            ClearTargetReferences(creature);
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath()
        {
            ClearBenefits(OBJECT_SELF);
            ClearTargetReferences(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void RefreshAll()
        {
            foreach (var (creature, owner) in Owners.ToArray())
            {
                if (!GetIsObjectValid(creature))
                {
                    ClearSetup(creature);
                    Owners.Remove(creature);
                    Activations.Remove(creature);
                    DirectDamage.Remove(creature);
                    continue;
                }
                Refresh(creature, owner);
            }
            foreach (var creature in DirectDamage.Where(pair => (DateTime.UtcNow - pair.Value.CapturedAt).TotalSeconds > 1)
                         .Select(pair => pair.Key).ToArray())
                DirectDamage.Remove(creature);
        }

        private static void ClearTargetReferences(uint target)
        {
            foreach (var creature in Owners.Where(pair => pair.Value.Sequence.Target == target || pair.Value.Reactions.WindowTarget == target)
                         .Select(pair => pair.Key).ToArray())
            {
                ClearSetup(creature);
                ClearReactiveWindows(creature, Owners[creature]);
            }
        }

        private static OwnerState GetOwner(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                Owners[creature] = owner = new OwnerState();
            return owner;
        }

        private static void Refresh(uint creature, OwnerState owner)
        {
            var combatEnded = (DateTime.UtcNow - owner.LastHostileActivity).TotalSeconds > 30 && !Combat.HasRecentHostileCombatActivity(creature);
            if (!GetIsObjectValid(creature) || GetIsDead(creature) || !EquipmentPredicates.HasUnarmedGrip(creature) || combatEnded)
            {
                ClearBenefits(creature);
                return;
            }
            if (owner.Reactions.WindowTarget != 0 &&
                (owner.Reactions.GetWindowStage(DateTime.UtcNow) == MeleeTechniqueStage.None ||
                 !IsValidEnemy(creature, owner.Reactions.WindowTarget) ||
                 GetLineStat(creature, owner.WindowLine, StatType.TechniqueFinisherWindowDurationSeconds) <= 0))
                ClearReactiveWindows(creature, owner);
            if (owner.Sequence.Stage == MeleeTechniqueStage.None)
                return;
            if (!owner.Sequence.Matches(owner.Sequence.Target, DateTime.UtcNow) ||
                !IsValidEnemy(creature, owner.Sequence.Target) ||
                !IsGripSnapshotValid(creature, owner.MainHand, owner.OffHand, owner.Area) ||
                owner.SourceOwnedStatusType != null && !StatusEffect.HasStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature))
                ClearSetup(creature);
        }

        private static bool TryGetActivation(uint creature, uint target, out Activation activation)
        {
            return Activations.TryGetValue(creature, out activation) && activation.Target == target &&
                   IsGripSnapshotValid(creature, activation.Weapon, activation.OffHand, activation.Area) &&
                   IsValidEnemy(creature, target);
        }

        private static bool IsGripSnapshotValid(uint creature, uint main, uint off, uint area) =>
            GetIsObjectValid(creature) && !GetIsDead(creature) && EquipmentPredicates.HasUnarmedGrip(creature) &&
            GetArea(creature) == area && GetItemInSlot(InventorySlot.RightHand, creature) == main &&
            GetItemInSlot(InventorySlot.LeftHand, creature) == off;

        private static bool IsValidEnemy(uint creature, uint target) =>
            GetIsObjectValid(target) && !GetIsDead(target) && GetArea(creature) == GetArea(target) &&
            GetIsReactionTypeHostile(target, creature);

        private sealed class OwnerState
        {
            public TechniqueSequence Sequence { get; } = new();
            public TechniqueResourceState Resources { get; } = new();
            public TechniqueReactiveState Reactions { get; } = new();
            public MeleeTechniqueLine Line { get; set; }
            public MeleeTechniqueLine WindowLine { get; set; }
            public MeleeTechniqueLine DiscountLine { get; set; }
            public SkillType Skill { get; set; }
            public int OrdinaryAccuracy { get; set; }
            public Type SourceOwnedStatusType { get; set; }
            public uint MainHand { get; set; }
            public uint OffHand { get; set; }
            public uint Area { get; set; }
            public DateTime LastHostileActivity { get; set; }
        }

        private sealed class Activation
        {
            public AbilityDetail Ability { get; init; }
            public uint Target { get; init; }
            public MeleeTechniqueStage Stage { get; init; }
            public int ConditionalPower { get; init; }
            public int ConditionalAccuracy { get; init; }
            public uint Weapon { get; init; }
            public uint OffHand { get; init; }
            public uint Area { get; init; }
            public int WeaponDamage { get; init; }
            public CombatDamageType WeaponDamageType { get; init; }
        }

        private sealed record OrdinaryAttempt(uint Target, SkillType Skill, int AttackIndex, int Power);
        private sealed record DirectDamageSnapshot(uint Attacker, int HitPoints, DateTime CapturedAt);
    }
}
