using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestContractService;

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
    private readonly ConcurrentBag<string> _unachievableContracts = new();

    /// <summary>Saved as the takedown reason of a contract withdrawn by <see cref="RetireUnachievableContracts"/>.</summary>
    public const string RetiredContractReason = "Withdrawn by the space update: it asked for a ship configuration module, which was retired.";

    public LegacySpaceItemSweep(int version = StoredItemSweep.LegacySpaceItemsVersion) => _version = version;
    public LegacySpaceItemTally Totals => _totals;

    /// <summary>The published contracts the pass found asking for a retired configuration module.</summary>
    public IReadOnlyCollection<string> UnachievableContracts => _unachievableContracts.ToArray();

    /// <summary>
    /// Whether a contract asks for an item nothing replaces: a configuration module. Characters lose those modules in the
    /// upgrade and any copy picked up later is refunded and destroyed, so such a contract can never be completed.
    /// </summary>
    public static bool AsksForRetiredItem(IEnumerable<QuestContractObjective> objectives) =>
        objectives?.Any(x => LegacySpaceItemCatalog.TryGetConfigurationModule(x.ItemResref, out _)) == true;

    /// <summary>Worker thread: notes a published contract that asks for a retired item, withdrawn after the pass.</summary>
    public void NoteContract(QuestContract contract)
    {
        if (contract.Status == QuestContractStatus.Published && AsksForRetiredItem(contract.Objectives))
            _unachievableContracts.Add(contract.Id);
    }

    /// <summary>
    /// Main thread, after the pass has saved every contract: withdraws each contract <see cref="NoteContract"/> found, the
    /// way a cancellation does, refunding the escrowed reward and the posting fee to its author
    /// (<see cref="QuestContractBoard.RetireContract"/>). A withdrawn contract is no longer published, so a rerun skips it.
    /// Returns the number of contracts noted.
    /// </summary>
    public int RetireUnachievableContracts()
    {
        var contracts = UnachievableContracts;
        foreach (var contractId in contracts)
            QuestContractBoard.RetireContract(contractId, RetiredContractReason,
                "withdrawn: it asked for a ship configuration module, which the space update retired",
                "asked for a ship configuration module, which the space update retired, so it was withdrawn.");
        return contracts.Count;
    }

    /// <summary>
    /// Converts one stored item. <paramref name="ownerId"/> is the player owed credits and designs (null when nobody
    /// is); a property id is passed as <see cref="ForProperty"/>. Returns whether the data changed; a stored
    /// configuration module is left in place and reported through <paramref name="rootConfiguration"/>.
    /// </summary>
    public bool Migrate(string data, Action<string> setData, string ownerId, string sourceKey = null, Action<LegacySpaceItemBlueprint, string> rootReplaced = null,
        Action<int> rootConfiguration = null)
    {
        var changed = Convert(data, setData, rootReplaced, rootConfiguration, out var nestedCredits, out var designs);
        if (!string.IsNullOrWhiteSpace(ownerId) && (nestedCredits > 0 || designs.Count > 0))
            _pending.Add((ownerId, (sourceKey ?? ownerId) + "#nested", nestedCredits, designs.ToArray()));
        return changed;
    }

    /// <summary>
    /// Converts one contract reward or delivery item. Its recipient is known only when it is claimed (a held
    /// submission goes to the contract's author when it wins), so what the conversion owes travels on the item in the
    /// same record write: the credits for configuration modules taken out of it (<see cref="QuestContractItem.RetiredSpaceCredits"/>)
    /// and the Hull Design its deed keeps (<see cref="QuestContractItem.HullDesigns"/>). Whoever claims the item is
    /// granted them with it, once. A rerun finds nothing left to convert and adds nothing. A configuration module that
    /// is the item itself stays, and the claim's live conversion pays for it.
    /// </summary>
    public bool MigrateContractItem(QuestContractItem item)
    {
        var changed = Convert(item.Data, value => item.Data = value,
            (blueprint, resref) => (item.Name, item.Resref, item.IconResref) = (blueprint.Name, resref, blueprint.IconResref),
            null, out var credits, out var designs);
        if (credits > 0)
        {
            item.RetiredSpaceCredits += credits;
            changed = true;
        }
        foreach (var design in designs)
        {
            item.HullDesigns ??= new List<string>();
            if (item.HullDesigns.Contains(design))
                continue;
            item.HullDesigns.Add(design);
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// Points a contract's collection objectives for retired deeds and modules at the items they became, with the new
    /// names, so the converted items satisfy them. Two retired items that became the same item make one objective for
    /// their combined quantity, since a quest tracks each item once (<see cref="MigrateItemProgresses"/>). Objectives
    /// for configuration modules stay, since nothing replaces them; their published contracts are withdrawn (<see cref="NoteContract"/>).
    /// </summary>
    public static bool MigrateObjectives(List<QuestContractObjective> objectives)
    {
        if (objectives == null)
            return false;
        var changed = false;
        foreach (var objective in objectives)
        {
            if (!LegacySpaceItemCatalog.TryGetReplacement(objective.ItemResref, out var replacement))
                continue;
            objective.ItemResref = replacement;
            objective.ItemName = LegacySpaceItemBlueprints.Get(replacement).Name;
            changed = true;
        }
        if (!changed)
            return false;

        var merged = new List<QuestContractObjective>();
        foreach (var objective in objectives)
        {
            var same = merged.FirstOrDefault(x => string.Equals(x.ItemResref, objective.ItemResref, StringComparison.OrdinalIgnoreCase));
            if (same == null)
                merged.Add(objective);
            else
                same.Quantity += objective.Quantity;
        }
        objectives.Clear();
        objectives.AddRange(merged);
        return true;
    }

    /// <summary>
    /// Moves a contract quest's remaining item counts from retired deeds and modules to the items they became, adding
    /// up counts that now share an item, to match <see cref="MigrateObjectives"/>.
    /// </summary>
    public static bool MigrateItemProgresses(Dictionary<string, int> progresses)
    {
        if (progresses == null)
            return false;
        var changed = false;
        foreach (var (resref, remaining) in progresses.ToList())
        {
            if (!LegacySpaceItemCatalog.TryGetReplacement(resref, out var replacement))
                continue;
            progresses.Remove(resref);
            progresses[replacement] = progresses.GetValueOrDefault(replacement) + remaining;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// Converts one item and reports what its owner is owed: the credits of the configuration modules taken out of it
    /// (not counting the item itself when it is one) and the Hull Designs its deeds keep.
    /// </summary>
    private bool Convert(string data, Action<string> setData, Action<LegacySpaceItemBlueprint, string> rootReplaced,
        Action<int> rootConfiguration, out int nestedCredits, out List<string> designs)
    {
        var tally = new LegacySpaceItemTally();
        var changed = StoredObjectData.MigrateLegacySpaceItems(data, out var migrated, tally);
        if (changed)
            setData(migrated);
        if (tally.RootReplacement != null)
            rootReplaced?.Invoke(tally.RootReplacement, tally.RootReplacementResref);

        nestedCredits = tally.Credits;
        if (tally.RootIsConfigurationModule)
        {
            LegacySpaceItemCatalog.TryGetConfigurationModule(StoredObjectData.ReadItemIdentity(data).Resref, out var rootCredits);
            nestedCredits -= rootCredits;
            rootConfiguration?.Invoke(rootCredits);
        }
        nestedCredits = Math.Max(0, nestedCredits);
        designs = tally.HullDesigns.ToList();
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
            if (recordType != typeof(InventoryItem) && recordType != typeof(MarketItem) && recordType != typeof(OverflowItem))
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
            else if (recordType == typeof(OverflowItem))
                DB.Delete<OverflowItem>(id);
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