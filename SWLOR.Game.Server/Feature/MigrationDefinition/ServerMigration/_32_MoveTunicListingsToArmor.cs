using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The next release's server migration. Moves market listings still saved under the removed Tunic category (20) to
/// Armor so the market's Armor filter finds them; the combat upgrade's category remap missed that id. It checks each
/// listing's saved record rather than its search entry, so a listing whose search entry already says Armor but whose
/// record does not is still corrected.
/// </summary>
public sealed class _32_MoveTunicListingsToArmor : IServerMigration
{
    public const int RemovedTunicCategory = 20;

    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<MarketItem>());
        new ParallelStoredItemPass(progress).Process<MarketItem>(MoveTunicToArmor);
    }

    public static bool MoveTunicToArmor(MarketItem listing)
    {
        if ((int)listing.Category != RemovedTunicCategory)
            return false;

        listing.Category = MarketCategoryType.Armor;
        return true;
    }
}
