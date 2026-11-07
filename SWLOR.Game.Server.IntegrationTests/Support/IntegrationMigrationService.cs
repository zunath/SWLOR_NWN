using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The real migration service with the calls that need the running server replaced: the vault pass (it reads
/// <c>.bic</c> files and the game's 2DAs), the engine's character save, and the re-reading of a logged-in character's stats.
/// Server migrations, the migration versions and the player initialization run as in the server.
/// </summary>
public class IntegrationMigrationService : MigrationService
{
    private readonly IEnumerable<IServerMigration>? _migrations;

    /// <param name="migrations">The server migrations to run instead of the ones the assemblies hold.</param>
    public IntegrationMigrationService(IDBService db, IEnumerable<IServerMigration>? migrations = null)
        : base(db, new OverflowStorageService(db))
    {
        _migrations = migrations;
    }

    /// <summary>
    /// The given migrations, or every migration the server assembly holds. The base class searches every loaded assembly,
    /// which would also pick up the stand-in migrations of other tests.
    /// </summary>
    protected override IEnumerable<IServerMigration> CreateServerMigrations() =>
        _migrations ?? typeof(IServerMigration).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => (IServerMigration)Activator.CreateInstance(type)!);

    protected override bool RunVaultUpgrade() => true;

    protected override bool SaveServerCharacter(uint player) => true;

    protected override void RestorePlayerAttributes(uint player)
    {
    }

    protected override void RestorePlayerFeats(uint player)
    {
    }

    protected override void RefreshCombatReadinessEquipment(uint player)
    {
    }

    protected override void RefreshPlayerMaxHP(uint player)
    {
    }

    protected override void RefreshPlayerEquipmentStats(uint player)
    {
    }
}
