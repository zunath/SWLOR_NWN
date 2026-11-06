using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PoisonsService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Consumes weapon poison coatings on weapon hits. Vial items stamp the coating locals onto a
    /// weapon; every damaging swing with a coated weapon spends a charge and applies the venom
    /// debuff, throttled by a short internal cooldown so fast dual-wield attacks do not multiply
    /// poison output.
    /// </summary>
    public class PoisonsService : IPoisonsService
    {
        public const string CoatingTierVariable = "POISON_COATING_TIER";
        public const string CoatingChargesVariable = "POISON_COATING_CHARGES";
        public const string CoatingPotencyVariable = "POISON_COATING_POTENCY";

        private const string NextApplyVariable = "POISON_COATING_NEXT_APPLY";
        public const int InternalCooldownSeconds = 6;

        private readonly DateTime _epoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void ConsumePoisonCoating()
        {
            var attacker = OBJECT_SELF;
            var defender = StringToObject(EventsPlugin.GetEventData("DEFENDER"));

            if (!GetIsObjectValid(defender) || GetIsDead(defender))
                return;

            // Consume the coating on the exact weapon that landed the hit so a dual-wielder's two
            // coatings are spent independently instead of always draining the right hand.
            var weapon = StringToObject(EventsPlugin.GetEventData("WEAPON"));
            if (!GetIsObjectValid(weapon) || GetLocalInt(weapon, CoatingChargesVariable) <= 0)
                return;

            var now = (int)(UtcNow() - _epoch).TotalSeconds;
            if (GetLocalInt(attacker, NextApplyVariable) > now)
                return;

            SetLocalInt(attacker, NextApplyVariable, now + InternalCooldownSeconds);

            var tier = GetLocalInt(weapon, CoatingTierVariable);
            var potency = GetLocalInt(weapon, CoatingPotencyVariable);
            var durationSeconds = GetVenomDurationSeconds(tier);
            var damagePerTick = GetCoatingVenomDamagePerTick(tier);

            // Refresh the existing instance so attacks near the six-second boundary cannot
            // continually replace Venom before its first damage tick.
            var venom = GetStatusEffect(defender, typeof(VenomStatusEffect), attacker) as VenomStatusEffect;
            var applied = venom != null && !venom.IsFlaggedForRemoval
                ? RefreshStatusEffectDuration(defender, typeof(VenomStatusEffect), attacker, durationSeconds)
                : ApplyStatusEffect(attacker, defender, new VenomStatusEffect(potency, damagePerTick), durationSeconds);
            if (applied)
            {
                venom?.UpdatePotency(damagePerTick, potency);
                AddEspionageCombatPoint(attacker, defender);
            }

            var charges = GetLocalInt(weapon, CoatingChargesVariable) - 1;
            if (charges > 0)
            {
                SetLocalInt(weapon, CoatingChargesVariable, charges);
                return;
            }

            DeleteLocalInt(weapon, CoatingTierVariable);
            DeleteLocalInt(weapon, CoatingChargesVariable);
            DeleteLocalInt(weapon, CoatingPotencyVariable);
            SendMessageToPC(attacker, $"The venom coating on {GetName(weapon)} has worn off.");
        }

        public int GetCoatingVenomDamagePerTick(int tier)
        {
            return PoisonCoatingTiers.GetVenomDamagePerTick(tier);
        }

        public float GetVenomDurationSeconds(int tier)
        {
            return PoisonCoatingTiers.GetVenomDurationSeconds(tier);
        }

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source) =>
            StatusEffect.GetStatusEffect(creature, statusEffectClass, source);

        protected virtual bool RefreshStatusEffectDuration(uint creature, Type statusEffectClass, uint source, float durationSeconds) =>
            StatusEffect.RefreshStatusEffectDuration(creature, statusEffectClass, source, durationSeconds);

        protected virtual bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds) =>
            StatusEffect.ApplyStatusEffect(source, creature, statusEffect, durationSeconds);

        protected virtual void AddEspionageCombatPoint(uint player, uint creature) =>
            CombatPoint.AddCombatPoint(player, creature, SkillType.Espionage);
    }
}
