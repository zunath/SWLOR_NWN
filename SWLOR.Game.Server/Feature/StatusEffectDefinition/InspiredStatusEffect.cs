using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Inspired: time a watcher banks by watching performers, with the XP and riders of the dancer and musician they follow.
    /// The audience tick keeps its remaining time frozen while the holder watches. Its Standing Ovation and Requiem
    /// riders are read through shared interfaces, so combat and death never name this class.
    /// </summary>
    public sealed class InspiredStatusEffect : StatusEffectBase, IPreDamageStatusEffect, IDeathDebtWaiverStatusEffect
    {
        // The crafting skills Food also gives Control and Craftsmanship to.
        private static readonly SkillType[] _craftingSkills =
        {
            SkillType.Agriculture, SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication
        };

        // Standing Ovation's ten-minute cooldown is kept by player, so rebuilding or re-banking Inspired does not reset it.
        private static readonly Dictionary<string, DateTime> _lastOvationByPlayer = new();

        private bool _requiemUsed;

        public InspiredEffectData Data { get; }
        public override string Name => "Inspired";
        public override EffectIconType Icon => EffectIconType.InspiredStatusEffect;
        public override bool PersistsOnLogout => true;

        // The audience tick re-applies this effect often; it announces the first application itself.
        public override bool SendsApplicationMessage => false;

        /// <summary>True while the holder still has an unused Requiem to spend on a death.</summary>
        public bool HasRequiemAvailable => Data.Requiem && !_requiemUsed;

        public InspiredStatusEffect()
            : this(new InspiredEffectData())
        {
        }

        public InspiredStatusEffect(InspiredEffectData data)
        {
            Data = data;

            StatGroup.Stats[StatType.ExperiencePercentAdjustment] = data.XPBonusPercent;
            StatGroup.Stats[StatType.OutOfCombatMovementSpeedPercentAdjustment] = data.OutOfCombatMovementSpeedPercent;
            StatGroup.Stats[StatType.FishingCatchChancePercentAdjustment] = data.FishingCatchChancePercent;
            StatGroup.Abilities[AbilityType.Might] = data.Might;
            StatGroup.Abilities[AbilityType.Agility] = data.Agility;
            StatGroup.Abilities[AbilityType.Vitality] = data.Vitality;
            StatGroup.Abilities[AbilityType.Perception] = data.Perception;
            StatGroup.Abilities[AbilityType.Willpower] = data.Willpower;
            StatGroup.Abilities[AbilityType.Social] = data.Social;

            foreach (var skill in _craftingSkills)
            {
                StatGroup.CraftSkillBonuses[CraftSkillBonusType.Control][skill] = data.CraftBonus;
                StatGroup.CraftSkillBonuses[CraftSkillBonusType.Craftsmanship][skill] = data.CraftBonus;
            }
        }

        public override IStatusEffect Clone()
        {
            var clone = new InspiredStatusEffect(Data);
            clone.CarryStateFrom(this, false);
            return clone;
        }

        /// <summary>
        /// Keeps the Requiem a replaced Inspired already spent, unless the holder banked Inspired again since.
        /// </summary>
        public void CarryStateFrom(InspiredStatusEffect previous, bool bankedAgain)
        {
            _requiemUsed = !bankedAgain && previous._requiemUsed;
        }

        /// <summary>The holder banked Inspired again, which gives them a fresh Requiem.</summary>
        public void ResetRequiem()
        {
            _requiemUsed = false;
        }

        public bool TryConsumeDeathDebtWaiver()
        {
            if (!HasRequiemAvailable)
                return false;

            _requiemUsed = true;
            return true;
        }

        public void OnBeforeDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            TryStandingOvation(defender, GetCurrentHitPoints(defender), damage);
        }

        /// <summary>
        /// Damage over time, triggered damage, Burn and Poison skip the pre-damage callback, so the damaged event checks
        /// the same threshold after the fact. A hit the callback already answered has started the shared cooldown.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void CheckStandingOvationAfterDamage()
        {
            var player = OBJECT_SELF;
            var damage = GetTotalDamageDealt();
            if (damage <= 0 || GetCurrentHitPoints(player) <= 0)
                return;

            StatusEffect.GetStatusEffect<InspiredStatusEffect>(player)?
                .TryStandingOvation(player, GetCurrentHitPoints(player) + damage, damage);
        }

        private void TryStandingOvation(uint defender, int hitPointsBeforeDamage, int damage)
        {
            if (!Data.StandingOvation ||
                !GetIsObjectValid(defender) ||
                !PerformanceAudienceRules.StandingOvationTriggers(
                    hitPointsBeforeDamage, damage, GetMaxHitPoints(defender)))
                return;

            var now = DateTime.UtcNow;
            var playerId = GetObjectUUID(defender);
            DateTime? lastOvation = _lastOvationByPlayer.TryGetValue(playerId, out var lastProc) ? lastProc : null;
            if (!PerformanceAudienceRules.IsStandingOvationReady(lastOvation, now))
                return;

            RememberOvation(playerId, now);

            // The heal lands after the hit, so it cannot be cancelled by the damage it answers.
            DelayCommand(0f, () => HealStandingOvation(defender));
        }

        private static void RememberOvation(string playerId, DateTime now)
        {
            var expired = new List<string>();
            foreach (var (id, proc) in _lastOvationByPlayer)
            {
                if ((now - proc).TotalSeconds >= PerformanceAudienceRules.StandingOvationCooldownSeconds)
                    expired.Add(id);
            }

            foreach (var id in expired)
                _lastOvationByPlayer.Remove(id);

            _lastOvationByPlayer[playerId] = now;
        }

        private static void HealStandingOvation(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            var maxHP = GetMaxHitPoints(creature);
            var amount = PerformanceAudienceRules.StandingOvationHeal(maxHP);
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = Stat.CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maxHP);
            if (amount <= 0)
                return;

            Stat.ApplyHealingReceivedEffects(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
            SendMessageToPC(creature, "A standing ovation lifts you, restoring your health.");
        }
    }
}
