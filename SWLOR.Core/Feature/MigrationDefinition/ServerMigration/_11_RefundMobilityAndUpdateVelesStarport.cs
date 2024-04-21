using SWLOR.Core.Entity;
using SWLOR.Core.LogService;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.MigrationService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.PropertyService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _11_RefundMobilityAndUpdateVelesStarport: ServerMigrationBase, IServerMigration
    {
        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.Mobility, 1), 3}
        };

        public int Version => 11;
        public void Migrate()
        {
            RefundPerksByMapping(_refundMap);
            UpdateVelesStarportShipLocations();
        }

        private void UpdateVelesStarportShipLocations()
        {
            const string StarportResref = "velesinterior";

            var dbQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship);
            var shipCount = (int)DB.SearchCount(dbQuery);
            var dbShips = DB.Search(dbQuery
                .AddPaging(shipCount, 0));

            var waypoint = GetWaypointByTag("VISCARA_LANDING");
            var position = GetPosition(waypoint);
            var facing = GetFacing(waypoint);

            foreach (var dbShip in dbShips)
            {
                var dockPosition = dbShip.Positions[PropertyLocationType.DockPosition];
                var lastNPCDockPosition = dbShip.Positions[PropertyLocationType.LastNPCDockPosition];

                if (dockPosition.AreaResref == StarportResref)
                {
                    dockPosition.X = position.X;
                    dockPosition.Y = position.Y;
                    dockPosition.Z = position.Z;
                    dockPosition.Orientation = facing;
                }

                if (lastNPCDockPosition.AreaResref == StarportResref)
                {
                    lastNPCDockPosition.X = position.X;
                    lastNPCDockPosition.Y = position.Y;
                    lastNPCDockPosition.Z = position.Z;
                    lastNPCDockPosition.Orientation = facing;
                }

                DB.Set(dbShip);
                Log.Write(LogGroup.Migration, $"Updated location of ship '{dbShip.CustomName}' ({dbShip.Id}) in Veles Starport.");
            }
        }
    }
}
