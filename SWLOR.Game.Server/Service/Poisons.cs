using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Consumes weapon poison coatings on weapon hits. Vial items stamp the coating locals onto a
    /// weapon; every damaging swing with a coated weapon spends a charge and applies the venom
    /// debuff, throttled by a short internal cooldown so fast dual-wield attacks do not multiply
    /// poison output.
    /// </summary>
    public static class Poisons
    {
        public const string CoatingTierVariable = "POISON_COATING_TIER";
        public const string CoatingChargesVariable = "POISON_COATING_CHARGES";
        public const string CoatingPotencyVariable = "POISON_COATING_POTENCY";

        private const string NextApplyVariable = "POISON_COATING_NEXT_APPLY";
        private const int InternalCooldownSeconds = 6;
        private static readonly DateTime _epoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ConsumePoisonCoating()
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

            var now = (int)(DateTime.UtcNow - _epoch).TotalSeconds;
            if (GetLocalInt(attacker, NextApplyVariable) > now)
                return;

            SetLocalInt(attacker, NextApplyVariable, now + InternalCooldownSeconds);

            var tier = GetLocalInt(weapon, CoatingTierVariable);
            var potency = GetLocalInt(weapon, CoatingPotencyVariable);
            var durationSeconds = GetVenomDurationSeconds(tier);

            StatusEffect.ApplyStatusEffect(attacker, defender, new VenomStatusEffect(potency), durationSeconds);

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

        public static float GetVenomDurationSeconds(int tier)
        {
            return 6 + Math.Clamp(tier, 1, 5) * 6;
        }
    }
}
