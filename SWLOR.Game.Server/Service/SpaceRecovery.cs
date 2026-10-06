using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        public static bool RescueFittedShipPilot(uint player)
        {
            if (!IsPlayerInSpaceMode(player)) return false;
            var record = DB.Get<Player>(GetObjectUUID(player));
            var ship = DB.Get<PlayerShip>(record.ActiveShipId);
            if (ship?.Status.FittingVersion != ShipFittingConversion.CurrentVersion) return false;
            var settlement = ShipRecovery.Defeat(ship.Status);
            DB.Set(ship);
            var property = DB.Get<WorldProperty>(ship.PropertyId);
            property.Positions.Remove(PropertyLocationType.CurrentPosition); DB.Set(property);
            foreach (var site in DB.Search(new DBQuery<SpaceSite>()).Where(x => x.Claims.Values.Any(c => c.ShipId == ship.Id && c.State == SpaceWorkState.Reserved)).ToArray())
            {
                foreach (var claim in site.Claims.Values.Where(c => c.ShipId == ship.Id && c.State == SpaceWorkState.Reserved).Select(c => c.Id).ToArray()) SpaceWorkClaims.Cancel(site, claim);
                DB.Set(site);
            }
            if (GetIsDead(player) || GetCurrentHitPoints(player) <= 0)
            {
                SetLocalBool(player, "SHIP_RESCUED_DEATH", true);
                DelayCommand(0f, () => DeleteLocalBool(player, "SHIP_RESCUED_DEATH"));
                ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            }
            var inside = GetLocalLocation(player, "SPACE_INSTANCE_LOCATION");
            ExitSpaceMode(player);
            Stat.ApplyCreatureMovementRate(player);
            if (GetIsObjectValid(GetAreaFromLocation(inside))) AssignCommand(player, () => { ClearAllActions(); ActionJumpToLocation(inside); });
            if (settlement.Applied)
            {
                SendMessageToPC(player, $"Your ship was recovered at its last dock. Hull recovery: {settlement.RecoveryCredits} credits, plus equipment service.");
                if (settlement.LostCargo.Count > 0) SendMessageToPC(player, "Lost cargo: " + string.Join(", ", settlement.LostCargo.Select(x => $"{x.Value} {x.Key}")));
                if (Property.TryGetLoadedInstance(ship.PropertyId, out var instance))
                    foreach (var passenger in instance.Players.Where(x => x != player))
                        SendMessageToPC(passenger, "The ship was recovered at its last dock. You are safe inside.");
            }
            return true;
        }
    }
}
