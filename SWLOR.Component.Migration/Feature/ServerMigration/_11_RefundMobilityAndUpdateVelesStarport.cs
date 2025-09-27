using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _11_RefundMobilityAndUpdateVelesStarport: ServerMigrationBase, IServerMigration
    {
        public _11_RefundMobilityAndUpdateVelesStarport(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }

        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.Mobility, 1), 3}
        };

        public int Version => 11;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
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
                Logger.Write<MigrationLogGroup>($"Updated location of ship '{dbShip.CustomName}' ({dbShip.Id}) in Veles Starport.");
            }
        }
    }
}
