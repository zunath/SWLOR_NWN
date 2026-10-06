using System;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public static class ShipFittingMigration
    {
        public static void CaptureAudit()
        {
            foreach(var ship in ShipEquipmentTransfers.AllShips())
                if(ship.Status?.FittingVersion<ShipFittingConversion.CurrentVersion && DB.Get<ShipFittingAudit>("ship-fitting-audit/"+ship.Id)==null)
                    DB.Set(new ShipFittingAudit {Id="ship-fitting-audit/"+ship.Id,ShipId=ship.Id,OriginalShip=JsonConvert.SerializeObject(ship)});
        }
        private static void Reclaim(PlayerShip ship)
        {
            var owner=DB.Get<Player>(ship.OwnerPlayerId);if(owner==null)return;
            foreach(var module in ShipFittedStats.Modules(ship.Status).Concat(ship.Status.ConfigurationModules.Values).Concat(ship.Status.RefitRecovery.Values))
            {
                var audit=DB.Get<ShipItemAudit>("ship-item/"+module.ItemInstanceId);if(audit==null||audit.Reclaimed)continue;
                var source=DB.Get<ShipItemProvenance>("ship-provenance/"+module.ItemInstanceId);
                var reclaimed=ShipMaterialReclamation.Apply(owner.SpaceEconomy,module.ItemInstanceId,audit.OriginalResref,source);
                if(reclaimed)DB.Set(owner);
                audit.Reclaimed=reclaimed;audit.OwnerPlayerId=owner.Id;audit.ReclamationNote=reclaimed?"Verified fractional materials credited to ship owner.":"No verified material source; original item retained for audit.";DB.Set(audit);
            }
        }
        public static void Migrate()
        {
            var converted=0;var retained=0;
            for(var offset=0;;offset+=200)
            {
                var ships=DB.Search(new DBQuery<PlayerShip>().OrderBy(nameof(PlayerShip.Id)).AddPaging(200,offset)).ToArray();
                foreach(var ship in ships)
                {
                    if(ship.Status?.FittingVersion>=ShipFittingConversion.CurrentVersion)continue;
                    var id="ship-fitting-audit/"+ship.Id;
                    var audit=DB.Get<ShipFittingAudit>(id)??new ShipFittingAudit {Id=id,ShipId=ship.Id,OriginalShip=JsonConvert.SerializeObject(ship)};
                    DB.Set(audit);
                    try
                    {
                        if(ship.Status==null||!ShipFittingCatalog.Default.Hulls.ContainsKey(ship.Status.ItemTag))throw new InvalidOperationException("Ship hull has no declared conversion; original persistent data retained.");
                        ship.Status=ShipFittingConversion.Convert(ship.Status,sourceIdentity:ship.Id);DB.Set(ship);
                        audit.Completed=true;audit.Error=null;DB.Set(audit);Reclaim(ship);converted++;
                    }
                    catch(InvalidOperationException ex){audit.Error=ex.Message;DB.Set(audit);retained++;}
                }
                if(ships.Length<200)break;
            }
            Log.Write(LogGroup.Migration,$"Persistent ship fitting conversion: {converted} completed; {retained} retained with audit records. Ownership, interiors, permissions and docking positions are preserved.",true);
        }
    }
}
