using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The Player City Revamp's record changes. Each slice of the revamp adds its own step to <see cref="Migrate"/>:
/// <list type="bullet">
/// <item>Elections (<see cref="ElectionMigration"/>): kind and stage dates on any open election, and a citizen-since
/// time for current citizens.</item>
/// <item>City Affairs (<see cref="CityAffairsMigration"/>): the Handle City Affairs permission on city permission
/// records, and officers, modifiers, and neglect counts started empty.</item>
/// </list>
/// </summary>
public sealed class _32_PlayerCityRevamp : IServerMigration
{
    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        ElectionMigration.Run(Version);
        CityAffairsMigration.Run(Version);
    }
}
