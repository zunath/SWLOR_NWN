using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The retired space item rule of the stored-item sweep. Worker threads convert each record's items and note what
/// owners are owed; <see cref="Apply"/> then, on the main thread, removes stored configuration modules and pays their
/// value into the owner's market till, and unlocks the Hull Design each converted deed keeps.
/// </summary>
public sealed class LegacySpaceItemSweep
{
    private const string PropertyOwnerPrefix = "property:";

    private readonly int _version;
    private readonly ConcurrentBag<(string Owner, string Key, int Credits, string[] Designs)> _pending = new();
    private readonly ConcurrentBag<(Type RecordType, string Id, string OwnerId, int Credits)> _storedConfigurations = new();
    private readonly HashSet<string> _paidOwners = new();
    private readonly LegacySpaceItemTally _totals = new();
    private readonly object _totalsLock = new();

    public LegacySpaceItemSweep(int version = StoredItemSweep.LegacySpaceItemsVersion) => _version = version;
    public LegacySpaceItemTally Totals => _totals;

    /// <summary>
    /// Converts one stored item. <paramref name="ownerId"/> is the player owed credits and designs (null when nobody
    /// is); a property id is passed as <see cref="ForProperty"/>. Returns whether the data changed; a stored
    /// configuration module is left in place and reported through <paramref name="rootConfiguration"/>.
    /// </summary>
    public bool Migrate(string data, Action<string> setData, string ownerId, string sourceKey = null, Action<LegacySpaceItemBlueprint, string> rootReplaced = null,
        Action<int> rootConfiguration = null)
    {
        var tally = new LegacySpaceItemTally();
        var changed = StoredObjectData.MigrateLegacySpaceItems(data, out var migrated, tally);
        if (changed)
            setData(migrated);
        if (tally.RootReplacement != null)
            rootReplaced?.Invoke(tally.RootReplacement, tally.RootReplacementResref);

        var nestedCredits = tally.Credits;
        if (tally.RootIsConfigurationModule)
        {
            LegacySpaceItemCatalog.TryGetConfigurationModule(StoredObjectData.ReadItemIdentity(data).Resref, out var rootCredits);
            nestedCredits -= rootCredits;
            rootConfiguration?.Invoke(rootCredits);
        }
        if (!string.IsNullOrWhiteSpace(ownerId) && (nestedCredits > 0 || tally.HullDesigns.Count > 0))
            _pending.Add((ownerId, (sourceKey ?? ownerId) + "#nested", Math.Max(0, nestedCredits), tally.HullDesigns.ToArray()));
        lock (_totalsLock)
            _totals.Add(tally);
        return changed;
    }

    public static string ForProperty(string propertyId) =>
        string.IsNullOrWhiteSpace(propertyId) ? null : PropertyOwnerPrefix + propertyId;

    /// <summary>
    /// Queues the refund for a configuration module the pass removes itself (one stored in a property category). The key
    /// names the stored item, so a rerun never pays it twice; <see cref="Flush"/> pays it before the record is saved.
    /// </summary>
    public void QueueRefund(string ownerId, string sourceKey, int credits)
    {
        if (!string.IsNullOrWhiteSpace(ownerId) && credits > 0)
            _pending.Add((ownerId, sourceKey + "#root", credits, Array.Empty<string>()));
    }

    /// <summary>A stored record that is itself a configuration module, removed by <see cref="Apply"/>.</summary>
    public void QueueRemoval<T>(string recordId, string ownerId, int credits) where T : EntityBase =>
        _storedConfigurations.Add((typeof(T), recordId, ownerId, credits));

    /// <summary>
    /// Main thread: pays the owners of queued configuration module records and whatever the pass still holds, then
    /// deletes the records. Returns the number of players paid or given designs.
    /// </summary>
    public int Apply(int version)
    {
        foreach (var (recordType, id, ownerId, credits) in _storedConfigurations)
        {
            if (recordType != typeof(InventoryItem) && recordType != typeof(MarketItem))
                throw new InvalidOperationException($"No removal is known for stored {recordType.Name} records.");
            if (!string.IsNullOrWhiteSpace(ownerId) && credits > 0)
                _pending.Add((ownerId, $"{recordType.Name}:{id}#root", credits, Array.Empty<string>()));
        }
        // Owners are saved before the deletes: a stop in between leaves the module in place and its refund keyed.
        Flush();
        foreach (var (recordType, id, _, _) in _storedConfigurations)
        {
            if (recordType == typeof(InventoryItem))
                DB.Delete<InventoryItem>(id);
            else
                DB.Delete<MarketItem>(id);
        }
        return _paidOwners.Count;
    }

    /// <summary>
    /// Main thread: pays every refund and design queued so far and saves each owner. The pass calls it before each
    /// batch of converted records is saved, so a stop at any point leaves either the old item with its refund already
    /// keyed (a rerun skips the payout) or the converted item with its refund paid.
    /// </summary>
    public void Flush()
    {
        var pending = new List<(string Owner, string Key, int Credits, string[] Designs)>();
        while (_pending.TryTake(out var entry))
            pending.Add(entry);
        foreach (var owned in pending.GroupBy(x => x.Owner))
        {
            var owner = owned.Key;
            var playerId = owner;
            if (owner.StartsWith(PropertyOwnerPrefix, StringComparison.Ordinal))
                playerId = DB.Get<WorldProperty>(owner[PropertyOwnerPrefix.Length..])?.OwnerPlayerId;
            var player = string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
            if (player == null)
            {
                Log.Write(LogGroup.Migration, $"Migration #{_version}: no player record for owner '{owner}'; {owned.Sum(x => x.Credits)} configuration module credits were not paid.", true);
                continue;
            }
            var changed = false;
            foreach (var entry in owned)
                changed |= Pay(player, entry.Credits, entry.Designs, entry.Credits > 0 ? entry.Key : null);
            if (changed)
            {
                DB.Set(player);
                _paidOwners.Add(owner);
            }
        }
    }

    /// <summary>
    /// Pays configuration module credits into a player market till and unlocks Hull Designs. True when it changed the
    /// record. A <paramref name="refundKey"/> already in <see cref="Player.RetiredSpaceRefunds"/> is not paid again; a
    /// new one is recorded with the credits, so one save carries both.
    /// </summary>
    public static bool Pay(Player player, int credits, IEnumerable<string> designs, string refundKey = null)
    {
        var changed = false;
        if (refundKey != null)
        {
            player.RetiredSpaceRefunds ??= new List<string>();
            if (player.RetiredSpaceRefunds.Contains(refundKey))
                credits = 0;
            else
            {
                player.RetiredSpaceRefunds.Add(refundKey);
                changed = true;
            }
        }
        if (credits > 0)
        {
            player.MarketTill += credits;
            player.UpgradeNotices ??= new List<string>();
            player.UpgradeNotices.Add($"Configuration modules were retired, since ship roles are now built into each hull. " +
                $"{credits} credits for yours were added to your market till; collect them at any market terminal.");
            changed = true;
        }
        player.UnlockedHullDesigns ??= new Dictionary<string, DateTime>();
        foreach (var key in designs)
        {
            if (player.UnlockedHullDesigns.TryAdd(key, DateTime.UtcNow))
                changed = true;
        }
        return changed;
    }
}