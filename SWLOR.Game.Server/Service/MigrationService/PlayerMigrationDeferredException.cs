namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// A live equipment conversion could not unequip a worn item because its wearer has no inventory space. The
/// conversion stops before changing the item, so it can run again once space is free.
/// </summary>
public sealed class PlayerMigrationDeferredException : InvalidOperationException
{
    public PlayerMigrationDeferredException(string message) : base(message) { }
}
