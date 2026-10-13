using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Snapshots every character's equipped techniques into a "Default" saved build.
/// </summary>
public sealed class _29_CreateDefaultMimicryLoadouts : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var query = new DBQuery<Player>();
        var count = (int)DB.SearchCount(query);
        foreach (var player in DB.Search(query.AddPaging(count, 0)))
        {
            if (MimicryLoadouts.EnsureDefault(player))
                DB.Set(player);
        }
    }
}
