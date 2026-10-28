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

    private readonly ConcurrentDictionary<string, int> _credits = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _designs = new();
    private readonly ConcurrentBag<(Type RecordType, string Id, string OwnerId, int Credits)> _storedConfigurations = new();
    private readonly LegacySpaceItemTally _totals = new();
    private readonly object _totalsLock = new();

    public LegacySpaceItemTally Totals => _totals;

    /// <summary>
    /// Converts one stored item. <paramref name="ownerId"/> is the player owed credits and designs (null when nobody
    /// is); a property id is passed as <see cref="ForProperty"/>. Returns whether the data changed; a stored
    /// configuration module is left in place and reported through <paramref name="rootConfiguration"/>.
    /// </summary>
    public bool Migrate(string data, Action<string> setData, string ownerId, Action<LegacySpaceItemBlueprint, string> rootReplaced = null,
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
        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            if (nestedCredits > 0)
                _credits.AddOrUpdate(ownerId, nestedCredits, (_, current) => current + nestedCredits);
            foreach (var key in tally.HullDesigns)
                _designs.GetOrAdd(ownerId, _ => new ConcurrentDictionary<string, byte>())[key] = 0;
        }
        lock (_totalsLock)
            _totals.Add(tally);
        return changed;
    }

    public static string ForProperty(string propertyId) =>
        string.IsNullOrWhiteSpace(propertyId) ? null : PropertyOwnerPrefix + propertyId;

    /// <summary>A stored record that is itself a configuration module, removed by <see cref="Apply"/>.</summary>
    public void QueueRemoval<T>(string recordId, string ownerId, int credits) where T : EntityBase =>
        _storedConfigurations.Add((typeof(T), recordId, ownerId, credits));

    /// <summary>
    /// Main thread: deletes queued configuration module records, then pays every owner and unlocks their designs.
    /// Returns the number of players paid or given designs.
    /// </summary>
    public int Apply(int version)
    {
        foreach (var (recordType, id, ownerId, credits) in _storedConfigurations)
        {
            if (recordType == typeof(InventoryItem))
                DB.Delete<InventoryItem>(id);
            else if (recordType == typeof(MarketItem))
                DB.Delete<MarketItem>(id);
            else
                throw new InvalidOperationException($"No removal is known for stored {recordType.Name} records.");
            if (!string.IsNullOrWhiteSpace(ownerId) && credits > 0)
                _credits.AddOrUpdate(ownerId, credits, (_, current) => current + credits);
        }

        var owners = _credits.Keys.Concat(_designs.Keys).Distinct().ToList();
        var paid = 0;
        foreach (var owner in owners)
        {
            var playerId = owner;
            if (owner.StartsWith(PropertyOwnerPrefix, StringComparison.Ordinal))
                playerId = DB.Get<WorldProperty>(owner[PropertyOwnerPrefix.Length..])?.OwnerPlayerId;
            var player = string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
            var credits = _credits.TryGetValue(owner, out var owed) ? owed : 0;
            if (player == null)
            {
                Log.Write(LogGroup.Migration, $"Migration #{version}: no player record for owner '{owner}'; {credits} configuration module credits were not paid.", true);
                continue;
            }
            if (Pay(player, credits, _designs.TryGetValue(owner, out var designs) ? designs.Keys : Array.Empty<string>()))
            {
                DB.Set(player);
                paid++;
            }
        }
        return paid;
    }

    /// <summary>Pays configuration module credits into a player's market till and unlocks Hull Designs. True when it changed the record.</summary>
    public static bool Pay(Player player, int credits, IEnumerable<string> designs)
    {
        var changed = false;
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
