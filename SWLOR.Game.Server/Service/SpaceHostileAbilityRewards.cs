using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a pilot's perks grant after a hostile space ability resolves: a temporary Evasion or Movement Speed
    /// bonus (the Ace style's Mobile Footwork and Kiting Instinct). It listens to
    /// <see cref="SpaceCombatEvents.HostileAbilityUsed"/>, so only space abilities trigger it and a pilot's ground
    /// abilities never grant it.
    /// </summary>
    public static class SpaceHostileAbilityRewards
    {
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Subscribe()
        {
            SpaceCombatEvents.HostileAbilityUsed -= OnHostileAbilityUsed;
            SpaceCombatEvents.HostileAbilityUsed += OnHostileAbilityUsed;
        }

        private static void OnHostileAbilityUsed(SpaceAbilityEvent e)
        {
            if (!GetIsObjectValid(e.Activator) || !Space.IsShipInSpace(e.Activator))
                return;

            Grant(
                e.Activator,
                StatType.EvasionPercentAdjustment,
                StatType.SpaceHostileAbilityUsedEvasionPercentAdjustment,
                StatType.SpaceHostileAbilityUsedEvasionDurationSeconds);
            Grant(
                e.Activator,
                StatType.MovementSpeedPercentAdjustment,
                StatType.SpaceHostileAbilityUsedMovementSpeedPercentAdjustment,
                StatType.SpaceHostileAbilityUsedMovementSpeedDurationSeconds);
        }

        private static void Grant(uint ship, StatType granted, StatType amountStat, StatType durationStat)
        {
            var amount = Stat.GetStatAdjustment(ship, amountStat);
            var duration = Stat.GetStatAdjustment(ship, durationStat);
            if (amount == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(ship, granted, amount, duration, amountStat);
        }

        /// <summary>Ends the bonuses when the pilot leaves space, so none follow them onto the ground.</summary>
        public static void Clear(uint ship)
        {
            TemporaryStatModifier.Consume(
                ship,
                StatType.EvasionPercentAdjustment,
                StatType.SpaceHostileAbilityUsedEvasionPercentAdjustment);
            TemporaryStatModifier.Consume(
                ship,
                StatType.MovementSpeedPercentAdjustment,
                StatType.SpaceHostileAbilityUsedMovementSpeedPercentAdjustment);
        }
    }
}
