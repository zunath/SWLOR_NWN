namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// A character file or record is older than <see cref="CharacterFileUpgrade.OldestUpgradableVersion"/>. This build no
/// longer has the migrations it needs, so a retry cannot help: the character stays as saved until an administrator
/// upgrades it with an older build.
/// </summary>
public sealed class UnsupportedCharacterVersionException : InvalidOperationException
{
    public UnsupportedCharacterVersionException(string message) : base(message) { }
}
