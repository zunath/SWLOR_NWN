using System.Linq;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Service.CombatService
{
    internal static class CombatAttackTiming
    {
        public static int CalculateAttackDelay(uint attacker)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, attacker);

            var rightHandDelay = CombatAttackTiming.GetWeaponDelay(rightHand);
            var leftHandDelay = CombatAttackTiming.ApplyOffhandAttackDelayReduction(attacker, CombatAttackTiming.GetWeaponDelay(leftHand));

            var delay = CombatFormula.CalculateEquippedWeaponDelayUnits(rightHandDelay, leftHandDelay);
            if (delay == 0)
            {
                var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, attacker);
                var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, attacker);
                var creatureBite = GetItemInSlot(InventorySlot.CreatureBite, attacker);

                var creatureDelays = new[]
                {
                    CombatAttackTiming.GetWeaponDelay(creatureRight),
                    CombatAttackTiming.GetWeaponDelay(creatureLeft),
                    CombatAttackTiming.GetWeaponDelay(creatureBite)
                };

                delay = creatureDelays
                    .Where(creatureDelay => creatureDelay > 0)
                    .DefaultIfEmpty(0)
                    .Min();
            }

            var reductionPercentage = CombatAttackTiming.CalculateAttackDelayReduction(attacker);

            return CombatFormula.CalculateAttackDelayMillisecondsFromDelayUnits(delay, reductionPercentage);
        }

        public static int CalculateAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.AttackDelayReductionPercent);

            return Math.Clamp(totalReduction, -50, 50);
        }

        public static int CalculateOffhandAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.OffhandAttackDelayReductionPercent);

            return Math.Min(Math.Max(totalReduction, 0), 50);
        }

        public static int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds)
        {
            var attackDebt = CombatState.GetAttackSwingDebt(attacker);
            var attacks = CombatFormula.CalculateAttacksPerSwing(effectiveDelayMilliseconds, attackDebt, out var updatedAttackDebt);

            CombatState.UpdateAttackSwingDebt(attacker, updatedAttackDebt);

            return attacks;
        }

        public static void ClearAttackSwingDebt(uint attacker)
        {
            CombatState.ClearAttackSwingDebt(attacker);
        }

        public static bool HandleParalyze(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return false;

            for (var effect = GetFirstEffect(attacker); GetIsEffectValid(effect); effect = GetNextEffect(attacker))
            {
                if (GetEffectType(effect) != EffectTypeScript.Paralyze)
                    continue;

                Messaging.SendMessageNearbyToPlayers(
                    attacker,
                    receiver => $"{PlayerName.GetDisplayName(receiver, attacker)} is paralyzed and cannot act!");
                return true;
            }

            return false;
        }

        private static int ApplyOffhandAttackDelayReduction(uint attacker, int offhandDelay)
        {
            if (offhandDelay <= 0)
                return offhandDelay;

            var reductionPercentage = CombatAttackTiming.CalculateOffhandAttackDelayReduction(attacker);
            return CombatFormula.ApplyPercentReduction(offhandDelay, reductionPercentage);
        }

        private static int GetWeaponDelay(uint item)
        {
            if (!GetIsObjectValid(item))
                return 0;

            var delay = 0;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                {
                    delay += GetItemPropertyCostTableValue(ip) * 10;
                }
            }

            return delay;
        }
    }
}
