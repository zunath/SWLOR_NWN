using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The next release's only server migration. Every database change for that release goes here as another step,
/// never as another version (<c>Migrations.md</c>, "One migration per release"). Steps:
/// <list type="bullet">
/// <item>Moves market listings still saved under the removed Tunic category (20) to Armor so the Armor filter finds
/// them; the combat upgrade's category remap missed that id. New listings already get Armor from their base item.</item>
/// </list>
/// </summary>
public sealed class _32_NextReleaseServerMigration : IServerMigration
{
    public const int RemovedTunicCategory = 20;

    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        MoveTunicListingsToArmor();
    }

    /// <summary>
    /// A listing's saved record and its search entry are written separately, so either can hold 20 while the other
    /// says Armor: this first checks every saved record, then re-saves every listing whose search entry still holds 20.
    /// </summary>
    private void MoveTunicListingsToArmor()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<MarketItem>());
        new ParallelStoredItemPass(progress).Process<MarketItem>(MoveTunicToArmor);
        ReindexTunicSearchEntries();
    }

    public static bool MoveTunicToArmor(MarketItem listing)
    {
        if ((int)listing.Category != RemovedTunicCategory)
            return false;

        listing.Category = MarketCategoryType.Armor;
        return true;
    }

    // After the record pass every saved record is off 20, so any search entry still at 20 is stale; saving the
    // listing again rewrites that entry from the record.
    private static void ReindexTunicSearchEntries()
    {
        var query = new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.Category), RemovedTunicCategory);
        var count = (int)DB.SearchCount(query);
        if (count == 0)
            return;

        // A failed save does not stop the rest; the migration still fails afterward so version 32 is not recorded.
        var failures = new List<Exception>();
        foreach (var id in DB.SearchIds(query.AddPaging(count, 0)).ToList())
        {
            try
            {
                var listing = DB.Get<MarketItem>(id);
                if (listing == null)
                    continue;

                MoveTunicToArmor(listing);
                DB.Set(listing);
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException($"Could not move the search entry of MarketItem {id} to Armor.", ex));
            }
        }

        if (failures.Count > 0)
            throw new AggregateException($"{failures.Count} tunic listing search entries could not be moved to Armor.", failures);
    }
}
