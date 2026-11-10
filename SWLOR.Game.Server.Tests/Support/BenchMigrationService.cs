#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A <see cref="MigrationService"/> whose seams answer for a test: the server migrations it finds are
/// <see cref="Definitions"/>, the character upgrade of the server vault and the login restoration only record
/// themselves in <see cref="Steps"/> (with the server migrations, in the order they run), and the engine's character
/// save answers <see cref="CanSave"/>.
/// </summary>
public sealed class BenchMigrationService : MigrationService
{
    public BenchMigrationService(InMemoryDBService db)
        : base(db, new OverflowStorageService(db), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>())
    {
    }

    /// <summary>What happened, in order: the migrations the stand-ins record, then <c>vault</c> and the login steps.</summary>
    public List<string> Steps { get; } = new();

    /// <summary>The server migrations the service finds.</summary>
    public List<IServerMigration> Definitions { get; } = new();

    /// <summary>Runs inside the character upgrade of the vault, to read the state the upgrade starts from.</summary>
    public Action? OnVaultUpgrade { get; set; }

    /// <summary>When set, the character upgrade of the vault throws it.</summary>
    public Exception? VaultFailure { get; set; }

    /// <summary>What the character upgrade of the vault returns.</summary>
    public bool VaultResult { get; set; } = true;

    /// <summary>What the engine's save of a character file answers.</summary>
    public bool CanSave { get; set; } = true;

    /// <summary>When set, the engine's save of a character file throws it.</summary>
    public Exception? SaveFailure { get; set; }

    protected override IEnumerable<IServerMigration> CreateServerMigrations() => Definitions;

    protected override bool RunVaultUpgrade()
    {
        Steps.Add("vault");
        OnVaultUpgrade?.Invoke();
        if (VaultFailure != null)
            throw VaultFailure;
        return VaultResult;
    }

    protected override bool SaveServerCharacter(uint player)
    {
        Steps.Add("save");
        if (SaveFailure != null)
            throw SaveFailure;
        return CanSave;
    }

    protected override void RestorePlayerAttributes(uint player) => Steps.Add("attributes");

    protected override void RestorePlayerFeats(uint player) => Steps.Add("feats");

    protected override void RefreshCombatReadinessEquipment(uint player) => Steps.Add("readiness");

    protected override void RefreshPlayerMaxHP(uint player) => Steps.Add("maxhp");

    protected override void RefreshPlayerEquipmentStats(uint player) => Steps.Add("equipment");
}
