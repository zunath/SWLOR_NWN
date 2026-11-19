using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The next release's server migration. Moves market listings still saved under the removed Tunic category (20) to
/// Armor so the market's Armor filter finds them; the combat upgrade's category remap missed that id.
/// </summary>
public sealed class _32_MoveTunicListingsToArmor : IServerMigration
{
    public const int RemovedTunicCategory = 20;

    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        var query = BuildTunicListingQuery();
        var count = (int)DB.SearchCount(query);
        var progress = new ServerMigrationProgress(Version, (nameof(MarketItem), count));
        progress.BeginSection<MarketItem>();
        if (count > 0)
        {
            foreach (var listing in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                listing.Category = MarketCategoryType.Armor;
                DB.Set(listing);
                progress.RecordProcessed(true);
            }
        }

        progress.FinishSection();
    }

    public static DBQuery<MarketItem> BuildTunicListingQuery() =>
        new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.Category), RemovedTunicCategory);
}
