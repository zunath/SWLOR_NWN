using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Moves player-placed Chancellor chairs so they render where they were placed after the model's mesh shift.</summary>
public sealed class _28_ShiftPlacedChancellorChairs : IServerMigration
{
    public int Version => 28;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var query = new DBQuery<WorldProperty>()
            .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.ChairChancellor);
        var count = (int)DB.SearchCount(query);
        var changed = 0;
        foreach (var structure in DB.Search(query.AddPaging(count, 0)))
        {
            if (!ChancellorChairMigration.Migrate(structure)) continue;
            DB.Set(structure);
            changed++;
        }

        Log.Write(LogGroup.Migration, $"Migration #28: Moved {changed}/{count} placed Chancellor chairs to match the chair model.", true);
    }
}
