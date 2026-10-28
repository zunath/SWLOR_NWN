using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Converts every retired ship deed and module to the space revamp's items (plan section 15). Registered ships go
/// first, each in turn: the hull becomes its new ship with the same interior and its old model as a Hull Design,
/// modules move to their new rows, configuration modules are refunded into the owner's market till, modules that no
/// longer fit go to the owner's overflow storage, and the ship's stats are rebuilt. Then the stored-item sweep
/// converts deeds and modules in banks, storage, the market, saved creatures and contracts. Characters' own items,
/// perks and Rebuild Token follow in the character upgrade (<c>_26_ConvertSpaceItems</c>).
/// </summary>
public sealed class _30_ConvertSpaceItems : IServerMigration
{
    public int Version => 30;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        LegacySpaceItemBlueprints.Load();
        ConvertShips();
        StoredItemSweep.Migrate(Version);
    }

    /// <summary>
    /// Saves each ship's returned modules, then pays its owner, then saves the ship, so a failure leaves a ship still
    /// retired (converted again on the next boot) rather than modules lost. Returned modules keep fixed ids, so a
    /// retry replaces them, and the payout is keyed by the ship id in the owner ledger
    /// (<see cref="Player.RetiredSpaceRefunds"/>), so the retry neither pays nor announces it a second time.
    /// </summary>
    private void ConvertShips()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<PlayerShip>());
        var count = progress.BeginSection<PlayerShip>();
        var totals = new LegacySpaceItemTally();
        var (ships, hulls, returned, kept) = (0, 0, 0, 0);
        if (count > 0)
        {
            foreach (var ship in DB.Search(new DBQuery<PlayerShip>().AddPaging(count, 0)))
            {
                var owner = string.IsNullOrWhiteSpace(ship.OwnerPlayerId) ? null : DB.Get<Player>(ship.OwnerPlayerId);
                var result = PlayerShipConversion.Convert(ship, GetWillpower(owner));
                if (!result.Changed)
                {
                    if (result.ShipTag == null)
                        Log.Write(LogGroup.Migration, $"Migration #{Version}: ship {ship.Id} has an unknown hull '{ship.Status?.ItemTag}' and was left unchanged.", true);
                    progress.RecordProcessed(false);
                    continue;
                }

                foreach (var module in result.ReturnedModules)
                    DB.Set(module);
                if (owner != null)
                {
                    var shipKey = $"{nameof(PlayerShip)}:{ship.Id}";
                    var alreadyHandled = owner.RetiredSpaceRefunds != null && owner.RetiredSpaceRefunds.Contains(shipKey);
                    var changed = LegacySpaceItemSweep.Pay(owner, result.Items.Credits, result.Items.HullDesigns, shipKey);
                    if (!alreadyHandled && (result.HullConverted || result.ReturnedModules.Count > 0))
                    {
                        owner.UpgradeNotices ??= new List<string>();
                        owner.UpgradeNotices.Add(BuildNotice(ship, result));
                        changed = true;
                    }
                    if (changed)
                        DB.Set(owner);
                }
                else if (result.Items.Credits > 0 || result.Items.HullDesigns.Count > 0)
                {
                    Log.Write(LogGroup.Migration, $"Migration #{Version}: ship {ship.Id} has no owner record; {result.Items.Credits} credits were not paid.", true);
                }
                DB.Set(ship);

                ships++;
                hulls += result.HullConverted ? 1 : 0;
                returned += result.ReturnedModules.Count;
                kept += result.ModulesKept;
                totals.Add(result.Items);
                progress.RecordProcessed(true);
            }
        }
        progress.FinishSection();
        Log.Write(LogGroup.Migration, $"Migration #{Version}: Rebuilt {ships} ships ({hulls} retired hulls converted); {kept} modules fitted, " +
            $"{returned} returned to overflow storage, {totals.ConfigurationModulesRefunded} configuration modules refunded for {totals.Credits} credits. " +
            $"Items: {string.Join(", ", totals.Items.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}"))}.", true);
    }

    private static int GetWillpower(Player owner) =>
        owner == null ? 0 : owner.BaseStats.GetValueOrDefault(AbilityType.Willpower) + owner.UpgradedStats.GetValueOrDefault(AbilityType.Willpower);

    private static string BuildNotice(PlayerShip ship, PlayerShipConversionResult result)
    {
        var name = DB.Get<WorldProperty>(ship.PropertyId)?.CustomName;
        var hull = RevampShipCatalog.Hulls.First(x => x.Tag == result.ShipTag).Name;
        var notice = string.IsNullOrWhiteSpace(name) ? $"Your starship is now a {hull}." : $"Your starship '{name}' is now a {hull}.";
        if (result.HullConverted)
            notice += ship.HullDesignAppearance > 0
                ? " Its interior is unchanged, and it keeps its look as a Hull Design you can refit to on any ship of that model."
                : " Its interior is unchanged.";
        if (result.ReturnedModules.Count > 0)
            notice += $" {result.ReturnedModules.Count} module(s) no longer fit it; claim them with /overflow.";
        return notice;
    }
}
