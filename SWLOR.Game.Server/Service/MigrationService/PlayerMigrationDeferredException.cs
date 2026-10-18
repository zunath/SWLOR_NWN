namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>A recoverable live-object precondition leaves both migration checkpoints pending.</summary>
public sealed class PlayerMigrationDeferredException : InvalidOperationException
{
    public PlayerMigrationDeferredException(string message) : base(message) { }
}
