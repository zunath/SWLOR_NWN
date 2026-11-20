using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using System;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// Which weapon makes the off-hand attack, which weapon the Force accuracy reads, and the attack delay the sheet shows.
    /// </summary>
    public class CharacterSheetWeaponTiming
    {
        private ICombatAttackTimingService CombatAttackTiming { get; }
        private IItemService Item { get; }
        private IStatusEffectCombatEventService StatusEffectCombatEvent { get; }
        private ITwinPistolEquipmentService TwinPistolEquipment { get; }

        public CharacterSheetWeaponTiming(ICombatAttackTimingService combatAttackTiming, IItemService item, IStatusEffectCombatEventService statusEffectCombatEvent, ITwinPistolEquipmentService twinPistolEquipment)
        {
            CombatAttackTiming = combatAttackTiming;
            Item = item;
            StatusEffectCombatEvent = statusEffectCombatEvent;
            TwinPistolEquipment = twinPistolEquipment;
        }

        /// <summary>
        /// The weapon that makes the off-hand attack: a separate left-hand weapon, the second end of a double
        /// weapon, the owned glove of a Power Glove pair, or an active Twin Pistol companion.
        /// </summary>
        public uint GetOffhandAttackWeapon(ICharacterSheetHost host)
        {
            var weapon = TwinPistolEquipment.IsActivePair(host.Target)
                ? TwinPistolEquipment.GetCompanionWeapon(host.Target)
                : EquipmentPredicates.GetOffhandAttackWeapon(host.Target);
            return GetIsObjectValid(weapon) && Item.IsAttackWeaponType(GetBaseItemType(weapon)) ? weapon : OBJECT_INVALID;
        }

        public static uint SelectForceAccuracyWeapon(uint mainHand, uint offHand, bool isMainHandValid)
        {
            return isMainHandValid ? mainHand : offHand;
        }

        public (string Value, string Tooltip) GetAttackDelayInfo(ICharacterSheetHost host)
        {
            var attackSkillType = WeaponAttackTiming.GetTimingSkill(host.Target);
            StatusEffectCombatEvent.TryGetLimitedAttackDelayReduction(
                host.Target,
                attackSkillType,
                out var limitedAttackDelayReductionPercent,
                out _);
            var attackerDelayMilliseconds = CombatAttackTiming.CalculateAttackDelay(
                host.Target,
                limitedAttackDelayReductionPercent);
            var useDefaultMinimumDelay = CombatAttackTiming.HasNextAutoAttackNoDelay(host.Target, attackSkillType);
            var effectiveDelayMilliseconds = CombatRules.CalculateEffectiveAttackDelay(attackerDelayMilliseconds, useDefaultMinimumDelay);
            var attackerDelaySeconds = attackerDelayMilliseconds / 1000f;
            var baseDelaySeconds = CombatRules.BaseAttackDelayMilliseconds / 1000f;
            var effectiveDelaySeconds = effectiveDelayMilliseconds / 1000f;
            var swingDelaySeconds = CombatRules.CalculateAttackSwingDelay(effectiveDelayMilliseconds) / 1000f;

            string tooltip;
            if (useDefaultMinimumDelay)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s (next swing at {swingDelaySeconds:0.##}s resolves extra attacks at your fastest possible speed)";
            else if (attackerDelayMilliseconds <= CombatRules.BaseAttackDelayMilliseconds)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s ({baseDelaySeconds:0.##}s default minimum)";
            else if (effectiveDelayMilliseconds < CombatRules.BaseAttackDelayMilliseconds)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s (swings every {swingDelaySeconds:0.##}s resolve extra attacks)";
            else
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s ({attackerDelaySeconds:0.##}s attacker - {baseDelaySeconds:0.##}s default)";

            var mainHand = GetItemInSlot(InventorySlot.RightHand, host.Target);
            var offHand = GetOffhandAttackWeapon(host);
            var mainHandDelay = CombatAttackTiming.GetWeaponDelay(mainHand);
            var offHandDelay = CombatAttackTiming.GetWeaponDelay(offHand);
            if (mainHandDelay > 0 && offHandDelay > 0)
                tooltip += FormatPairedDelay(mainHandDelay, offHandDelay,
                    CombatAttackTiming.CalculateOffhandAttackDelayReduction(host.Target), effectiveDelayMilliseconds);

            return (
                $"{effectiveDelaySeconds:0.##}s",
                tooltip
            );
        }
    }
}
