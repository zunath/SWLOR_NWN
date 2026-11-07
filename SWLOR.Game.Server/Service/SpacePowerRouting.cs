using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Power routing (plan section 10): the player at the Engineer station shifts the ship's power between weapons,
    /// shields and engines with four hotbar feats (<see cref="PowerRoutingRules"/>); their Engineering rank makes the
    /// routing's gain larger (W11n). Droids do not route power. The
    /// routing is saved on the ship status, read by the capacitor cost, shield regen, weapon damage and speed, and
    /// returns to Balanced when no player works the Engineer seat.
    /// </summary>
    public static partial class Space
    {
        private const int QuickBarSlotCount = 36;

        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void HandlePowerRoutingFeats()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            if (!PowerRoutingRules.TryGetMode(feat, out var mode))
                return;

            SetPowerRouting(OBJECT_SELF, mode);
        }

        /// <summary>
        /// Routes the ship's power for the player at its Engineer station. Returns false and tells them why when
        /// they cannot: they are not the Engineer, the routing is already set, or it changed too recently.
        /// </summary>
        public static bool SetPowerRouting(uint player, ShipPowerRouting mode)
        {
            if (!TryGetCrewStation(player, out var ship, out var seat) || seat != ShipCrewSeat.Engineer)
            {
                SendMessageToPC(player, ColorToken.Red("Only the crew member at the Engineer station can route power."));
                return false;
            }

            var status = GetShipStatus(ship);
            if (status == null)
                return false;

            if (IsShipIncapacitated(ship))
            {
                SendMessageToPC(player, ColorToken.Red("You cannot route power while the ship is incapacitated."));
                return false;
            }

            var now = DateTime.UtcNow;
            if (status.PowerRouting == mode)
            {
                SendMessageToPC(player, $"Power is already routed to {PowerRoutingRules.GetName(mode)}.");
                return false;
            }

            if (!PowerRoutingRules.CanChange(status.PowerRouting, mode, status.PowerRoutingChangedAt, now))
            {
                SendMessageToPC(player, ColorToken.Red(
                    $"You can route power again in {PowerRoutingRules.SecondsUntilChange(status.PowerRoutingChangedAt, now)} seconds."));
                return false;
            }

            status.PowerRouting = mode;
            status.PowerRoutingChangedAt = now;
            status.PowerRoutingBonusPercent = mode == ShipPowerRouting.Balanced
                ? 0
                : ShipSeatSkillRules.EngineeringRoutingPercent(GetPlayerSkillRank(player, SkillType.Engineering));
            SaveShipStatus(ship, status);
            ApplyShipSpeed(ship, status);

            var message = $"Power routed to {PowerRoutingRules.GetName(mode)}: {PowerRoutingRules.GetEffect(mode, status.PowerRoutingBonusPercent)}.";
            SendMessageToPC(player, message);
            SendMessageToPC(ship, $"{PlayerName.GetDisplayName(ship, player)}: {message}");
            Gui.PublishRefreshEvent(ship, new ShipHudRefreshEvent());
            return true;
        }

        /// <summary>Sets the pilot's movement to the ship's speed under its power routing, cloak and Afterburner.</summary>
        public static void ApplyShipSpeed(uint pilot, ShipStatus status)
        {
            if (status == null || !GetIsObjectValid(pilot) || !IsPlayerInSpaceMode(pilot))
                return;

            SetLocalFloat(pilot, Stat.ShipMovementSpeedVariable,
                (float)ShipAfterburnerRules.ApplySpeed(
                    ShipCloakRules.ApplySpeed(
                        PowerRoutingRules.ApplySpeed(status.SpeedMultiplier, status.PowerRouting, status.PowerRoutingBonusPercent),
                        GetCloakSpeedMultiplier(pilot)),
                    GetAfterburnerPercent(status)));
            Stat.ApplyCreatureMovementRate(pilot);
        }

        /// <summary>
        /// A module's capacitor use: its own, changed by the power routing for weapon modules and by the ship's capacitor
        /// use stat (a Wild Space environment).
        /// </summary>
        private static int GetModuleCapacitorCost(
            uint ship,
            ShipStatus status,
            ShipStatus.ShipStatusModule module,
            ShipModuleDetail detail)
        {
            var cost = ShipTraitRules.CapacitorCost(detail.CalculateCapacitorAction?.Invoke(ship, status, module.ModuleBonus) ?? 0, module.Traits);
            cost = PowerRoutingRules.ApplyCapacitorCost(cost, status.PowerRouting, ShipToggleRules.IsOffensive(detail.Type), status.PowerRoutingBonusPercent);
            return WildSpaceRules.AdjustCapacitorCost(cost, Stat.GetStatAdjustment(ship, StatType.SpaceCapacitorCostPercentAdjustment));
        }

        /// <summary>Puts each routing feat the Engineer's saved hotbar lacks in its first empty slot.</summary>
        private static void EnsurePowerRoutingOnHotBar(uint player)
        {
            foreach (var feat in PowerRoutingRules.Feats)
            {
                var firstEmpty = -1;
                var present = false;
                for (var index = 0; index < QuickBarSlotCount; index++)
                {
                    var slot = PlayerPlugin.GetQuickBarSlot(player, index);
                    if (slot.ObjectType == QuickBarSlotType.Feat && slot.INTParam1 == (int)feat)
                    {
                        present = true;
                        break;
                    }

                    if (firstEmpty < 0 && slot.ObjectType == QuickBarSlotType.Empty)
                        firstEmpty = index;
                }

                if (!present && firstEmpty >= 0)
                    PlayerPlugin.SetQuickBarSlot(player, firstEmpty, PlayerQuickBarSlot.UseFeat(feat));
            }
        }

        /// <summary>Takes the routing feats away when the player leaves the Engineer station.</summary>
        private static void RemovePowerRoutingFeats(uint player)
        {
            foreach (var feat in PowerRoutingRules.Feats)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }
        }
    }
}
