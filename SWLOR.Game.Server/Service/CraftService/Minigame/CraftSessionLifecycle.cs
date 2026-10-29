using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The life of one item craft, from the moment its materials are committed to its success, failure or abandonment. The
/// committed materials are saved on the player, so a logout, death, window close or server restart still settles the attempt
/// as a failure under the loss rule in <see cref="CraftOutcomeRules"/>.
/// <para>
/// Crash safety. The player record saves at once, but the inventory is in the character file, which is written only on an export that
/// follows the change by a moment. A crash in between leaves a record and a file that disagree about where the items are. The craft
/// file sequence settles which is current: every transfer between the record and the inventory advances it
/// (<see cref="ICraftSessionWorld.AdvanceFileSequence"/>), and the character file carries the value it was written at. After a crash the
/// file's value tells whether it holds a transfer.
/// </para>
/// <list type="bullet">
/// <item>A debit (<see cref="Start"/>) saves its sequence on the session (<see cref="CraftSessionRecord.DebitSequence"/>). A login whose file
/// is below it finds the components, enhancements and blueprint charge still in the file, so the session is discarded and nothing is returned.</item>
/// <item>A credit (a refund, a held item given back, the finished item) is written to <see cref="Player.PendingCraftReturns"/> with its sequence
/// in the same record save that removes it from its source, so the record always accounts for the items in one place or the other. A login whose
/// file is below the entry's sequence gives the items again; a file at or above it drops the entry. The redo is stored as a new entry, so it is
/// itself protected until a file holds it. Entries are also dropped while playing once an export has covered them.</item>
/// <item>A login compares every entry and the debit with the one sequence the file loaded with, before anything advances it, so each transfer
/// ends up applied once: a file at or above a sequence holds the transfer, and a file below it lacks every effect of it.</item>
/// <item>A credit is only made after the debit it draws on is in the file: <see cref="Fail"/> and <see cref="Succeed"/> flush the character first
/// when the export of the debit has not run, so a file below a credit's sequence always still lacks what the credit gives.</item>
/// <item>Records saved before the sequence existed read as 0, which no file is below, so they settle as they always did.</item>
/// </list>
/// </summary>
public sealed class CraftSessionLifecycle
{
    public const string HeldItemsMessage =
        "Your inventory is full. Items from your craft wait for you; make room, then open a crafting station again.";

    private readonly ICraftSessionWorld _world;
    private readonly IMinigameRandom _random;

    public CraftSessionLifecycle(ICraftSessionWorld world, IMinigameRandom random)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>True when the saved craft is the one the session describes.</summary>
    public static bool IsActive(Player dbPlayer, CraftSession session) =>
        session != null && dbPlayer.ActiveCraftSession != null && dbPlayer.ActiveCraftSession.SessionId == session.SessionId;

    /// <summary>
    /// Commits the recipe's components, the selected enhancements and the blueprint, charging the blueprint's credits and one
    /// licensed run as the original crafting window did. Nothing is taken when any step is refused.
    /// </summary>
    public CraftSessionStartResult Start(uint player, Player dbPlayer, CraftSessionStartRequest request)
    {
        DropExportedReturns(player, dbPlayer);
        if (dbPlayer.ActiveCraftSession != null)
            Fail(player, dbPlayer, CraftSessionEndReason.Replaced);

        if (!_world.CanCraft(player, request.Recipe))
            return CraftSessionStartResult.Refused("Recipe requirements not met!");

        var recipe = _world.GetRecipe(request.Recipe);
        if (request.HasBlueprint)
        {
            var problem = _world.ValidateBlueprint(player, request.BlueprintItem, request.Recipe);
            if (!string.IsNullOrEmpty(problem))
                return CraftSessionStartResult.Refused(problem);
        }

        // Every check runs before anything is taken, so a refusal never has items to give back.
        if (request.EnhancementItems.Any(item => item != OBJECT_INVALID && !_world.HasEnhancement(player, item)))
            return CraftSessionStartResult.Refused("An enhancement is no longer in your inventory.");

        var components = new List<string>();
        if (!_world.TryTakeComponents(player, recipe, components))
            return CraftSessionStartResult.Refused("Missing components!");

        var enhancements = request.EnhancementItems
            .Select(item => item == OBJECT_INVALID ? string.Empty : _world.TakeEnhancement(player, item))
            .ToList();

        var blueprint = request.HasBlueprint ? _world.ChargeBlueprint(player, request.BlueprintItem) : null;
        var sessionId = Guid.NewGuid().ToString();

        // The components, enhancements and blueprint charge are out of the inventory in memory; the file still has them until it is written.
        var debitSequence = _world.AdvanceFileSequence(player);
        dbPlayer.ActiveCraftSession = new CraftSessionRecord
        {
            DebitSequence = debitSequence,
            SessionId = sessionId,
            Recipe = request.Recipe,
            Skill = recipe.Skill,
            HasBlueprint = blueprint != null,
            BlueprintLevel = blueprint?.Level ?? 0,
            StartedUtc = _world.UtcNow,
            Components = components,
            Enhancements = enhancements
        };
        _world.SaveRecord(dbPlayer);
        _world.PersistCharacter(player);
        _world.BeginStationWork(player);
        _world.PlayStationWork(player, recipe.Skill);

        return CraftSessionStartResult.Started(new CraftSession(sessionId, request.Recipe, blueprint));
    }

    /// <summary>
    /// Finishes a craft: creates the item, applies the enhancements whose slot thresholds the quality meets, the food and
    /// blueprint bonuses and the vendor value, records the recipe as crafted and grants XP. Returns null when the session is no
    /// longer the player's saved craft, so a repeated request cannot create a second item.
    /// <para>
    /// The XP is held to the pace the minigame's XP multipliers assume (<see cref="CraftMinigameXp.PaceShare"/>): it is the full amount
    /// when the <paramref name="actionsTaken"/> actions took at least <see cref="CraftMinigameXp.ActionSeconds"/> each since the craft began,
    /// and less in proportion when they were resolved faster.
    /// </para>
    /// </summary>
    public CraftSessionSuccess Succeed(uint player, Player dbPlayer, CraftSession session, MinigameMeter quality, int actionsTaken)
    {
        if (!IsActive(dbPlayer, session))
            return null;

        var record = dbPlayer.ActiveCraftSession;
        DropExportedReturns(player, dbPlayer);
        FlushDebit(player, record);
        var recipe = _world.GetRecipe(record.Recipe);
        var firstTime = !dbPlayer.CraftedRecipes.ContainsKey(record.Recipe);
        var blueprintLevel = session.Blueprint?.Level ?? 0;
        var filledSlots = FilledSlots(record);

        var outcome = CraftOutcomeRules.CalculateSuccess(
            quality,
            recipe.Level,
            PacedBaseXp(record, recipe, GetRank(dbPlayer, recipe), actionsTaken),
            firstTime,
            blueprintLevel,
            recipe.Category == RecipeCategoryType.Food,
            filledSlots);

        var item = _world.CreateFinishedItem(recipe);
        _world.ApplyVendorValue(item, outcome.VendorValueBonus);

        foreach (var transfer in outcome.Enhancements)
        {
            if (transfer.Transfers)
            {
                _world.ApplyEnhancement(item, record.Enhancements[transfer.Slot - 1], recipe);
                _world.Message(player, ColorToken.Green("Enhancement applied successfully."));
            }
            else
            {
                _world.Message(player, ColorToken.Red(
                    $"Enhancement failed to apply: quality did not reach {transfer.ThresholdPercent}%."));
            }
        }

        if (recipe.Category == RecipeCategoryType.Food)
            _world.ApplyFoodBonus(item, outcome.FoodDurationBonusSeconds, outcome.FoodBonusCharges);

        if (session.HasBlueprint)
            _world.ApplyBlueprintBonuses(player, item, recipe, session.Blueprint);

        if (firstTime)
            dbPlayer.CraftedRecipes[record.Recipe] = DateTime.UtcNow;

        // The output is listed as a pending return in the save that ends the session, before any of it reaches the inventory: a stop
        // after that save leaves a return the next login gives again, and a stop before it leaves the session to settle as unfinished.
        // The XP the craft earned is entitled in the same save, so a stop between this save and the award loses neither.
        var units = _world.PrepareUnits(player, item, recipe);
        var output = units.Count > 0 ? RecordReturn(player, dbPlayer, units.Select(unit => unit.Serialized).ToList()) : null;
        dbPlayer.ActiveCraftSession = null;
        if (outcome.Xp > 0)
            AddPendingXp(dbPlayer, recipe.Skill, outcome.Xp);
        _world.SaveRecord(dbPlayer);

        // The full quantity stays accounted for: units in the inventory stay in the return, the rest move to the held items until there is room.
        var held = units.Where(unit => !_world.DeliverUnit(player, unit)).Select(unit => unit.Serialized).ToList();
        if (held.Count > 0)
        {
            foreach (var serialized in held)
                output.Items.Remove(serialized);
            if (output.Items.Count == 0)
                dbPlayer.PendingCraftReturns.Remove(output);

            dbPlayer.HeldCraftItems ??= new List<string>();
            dbPlayer.HeldCraftItems.AddRange(held);
            _world.SaveRecord(dbPlayer);
            _world.Message(player, HeldItemsMessage);
        }

        _world.PersistCharacter(player);
        _world.EndStationWork(player);

        // XP last, from the entitlement the settlement saved: it is cleared and saved before it is given.
        GrantPendingXp(player, dbPlayer);
        _world.LogCraft(player, $"successfully crafted '{recipe.Resref}'.");

        return new CraftSessionSuccess(item, outcome);
    }

    /// <summary>
    /// Ends the saved craft as a failure: each committed component stack and enhancement comes back on a roll above the loss
    /// chance, and the attempt still grants 15% of the base XP. Items that do not fit the inventory are held on the player until
    /// there is room. Does nothing when no craft is saved.
    /// <para>
    /// A caller that played the craft passes the <paramref name="actionsTaken"/>, and the XP is held to the modeled pace as
    /// <see cref="Succeed"/> holds it. A settlement with no window (a death, a login, a restart, a craft replaced by a new one) does not
    /// know them and passes null: the XP is then not scaled, because nothing was resolved at a pace to measure.
    /// </para>
    /// </summary>
    public CraftSettlement Fail(uint player, Player dbPlayer, CraftSessionEndReason reason, int? actionsTaken = null)
    {
        var record = dbPlayer.ActiveCraftSession;
        if (record == null)
            return CraftSettlement.None;

        DropExportedReturns(player, dbPlayer);
        FlushDebit(player, record);
        var recipe = _world.GetRecipe(record.Recipe);
        var enhancements = record.Enhancements.Where(e => !string.IsNullOrEmpty(e)).ToList();
        var failure = CraftOutcomeRules.RollFailure(
            _random,
            enhancements.Count,
            record.Components.Count,
            actionsTaken.HasValue
                ? PacedBaseXp(record, recipe, GetRank(dbPlayer, recipe), actionsTaken.Value)
                : CraftMinigameXp.BaseXp(recipe, GetRank(dbPlayer, recipe)),
            record.BlueprintLevel);

        var kept = new List<string>();
        for (var index = 0; index < enhancements.Count; index++)
        {
            if (failure.EnhancementsKept[index])
                kept.Add(enhancements[index]);
        }

        for (var index = 0; index < record.Components.Count; index++)
        {
            if (failure.ComponentsKept[index])
                kept.Add(record.Components[index]);
        }

        var total = enhancements.Count + record.Components.Count;
        dbPlayer.HeldCraftItems ??= new List<string>();
        dbPlayer.HeldCraftItems.AddRange(kept);
        dbPlayer.ActiveCraftSession = null;

        // The failure's XP is entitled on the record, saved with the settlement it belongs to. It is given after the save (at once for a
        // living player), and stays there while the player is dead or when the server stops before it is given.
        if (failure.Xp > 0)
            AddPendingXp(dbPlayer, recipe.Skill, failure.Xp);

        _world.SaveRecord(dbPlayer);
        _world.EndStationWork(player);

        // Returns what fits, and exports the character when it did. When nothing comes back the file already lacks the committed
        // materials (the start exported it), so the record and the file agree.
        ReturnHeldItems(player, dbPlayer);

        if (reason != CraftSessionEndReason.Broken)
        {
            _world.Message(player, ColorToken.Red(
                $"Your unfinished craft failed. {total - kept.Count} of {total} items were lost."));
        }

        // XP last, from the entitlement the settlement saved.
        GrantPendingXp(player, dbPlayer);
        _world.LogCraft(player, $"failed to craft '{record.Recipe}' ({reason}).");

        return new CraftSettlement(true, total - kept.Count, kept.Count, failure.Xp);
    }

    /// <summary>
    /// Settles a craft left over from a login, comparing the player record with the character file the player just loaded: a crash can
    /// leave a file that predates item transfers the record already counts. Gives again each credit the file lacks, discards a session
    /// whose debit the file lacks, then settles whatever craft is left as <see cref="SettleLeftover"/> does. Call it once at login, before
    /// anything changes the inventory; it is for a freshly loaded file only, because while playing the memory is current and the file is behind.
    /// </summary>
    public CraftSettlement RecoverAtLogin(uint player, Player dbPlayer, CraftSessionEndReason reason)
    {
        var file = _world.LoadedSequence(player);
        var changed = false;
        var placed = new List<string>();

        var pending = dbPlayer.PendingCraftReturns;
        if (pending is { Count: > 0 })
        {
            var redo = pending.Where(entry => entry.Sequence > file).OrderBy(entry => entry.Sequence).ToList();
            pending.Clear();
            changed = true;

            foreach (var item in redo.SelectMany(entry => entry.Items))
            {
                if (_world.TryReturnItem(player, item))
                {
                    placed.Add(item);
                    continue;
                }

                dbPlayer.HeldCraftItems ??= new List<string>();
                dbPlayer.HeldCraftItems.Add(item);
            }

            if (redo.Count > 0)
                _world.LogCraft(player, $"had {redo.Sum(entry => entry.Items.Count)} craft item(s) returned again after the character file predated them (file sequence {file}).");

            // The redone items are in the inventory only in memory, so they wait in a new entry until a file holds them.
            if (placed.Count > 0)
                RecordReturn(player, dbPlayer, placed);
        }

        var record = dbPlayer.ActiveCraftSession;
        if (record != null && record.DebitSequence > file)
        {
            // The file predates the debit, so it still holds the components, the enhancements and the blueprint charge or credits.
            dbPlayer.ActiveCraftSession = null;
            changed = true;
            _world.LogCraft(player, $"had an unfinished craft of '{record.Recipe}' discarded without a refund: the character file (sequence {file}) predates its debit (sequence {record.DebitSequence}).");
        }

        if (changed)
            _world.SaveRecord(dbPlayer);
        if (placed.Count > 0)
            _world.PersistCharacter(player);

        return SettleLeftover(player, dbPlayer, reason);
    }

    /// <summary>
    /// Settles a craft left over from a logout, death or restart, then gives back any held items that now fit. Call it whenever a
    /// crafting window opens; at login call <see cref="RecoverAtLogin"/>, which ends with this.
    /// </summary>
    public CraftSettlement SettleLeftover(uint player, Player dbPlayer, CraftSessionEndReason reason)
    {
        GrantPendingXp(player, dbPlayer);
        var settlement = Fail(player, dbPlayer, reason);
        if (!settlement.Settled)
            ReturnHeldItems(player, dbPlayer);

        return settlement;
    }

    /// <summary>
    /// Gives the player the XP a craft settlement entitled them to (<see cref="Player.PendingCraftXp"/>), once. Every settlement saves its XP
    /// there in the same record save that ends the craft, and then calls this, so the XP survives a stop between the settlement and the
    /// award, and a dead player's XP waits for a respawn or login. Does nothing while the player cannot receive XP. Returns whether any
    /// XP was given.
    /// <para>
    /// Each skill's XP is given and taken off the entitlement in the same player save (<see cref="ICraftSessionWorld.AwardPendingXp"/>),
    /// so a second call, a relog or a crash can neither give it twice nor lose it.
    /// </para>
    /// </summary>
    public bool GrantPendingXp(uint player, Player dbPlayer)
    {
        var pending = dbPlayer.PendingCraftXp;
        if (pending == null || pending.Count == 0 || !_world.CanReceiveXp(player))
            return false;

        // Each skill's XP is given and taken off the entitlement in one save, so a stop between them cannot lose or repeat it.
        var given = false;
        foreach (var (skill, xp) in pending.ToList())
            given |= _world.AwardPendingXp(player, dbPlayer, skill, xp);

        return given;
    }

    /// <summary>Gives back held items that fit the inventory now. Returns how many are still held.</summary>
    public int ReturnHeldItems(uint player, Player dbPlayer)
    {
        var held = dbPlayer.HeldCraftItems;
        if (held == null || held.Count == 0)
            return 0;

        DropExportedReturns(player, dbPlayer);
        var remaining = new List<string>();
        var given = new List<string>();
        foreach (var serialized in held)
        {
            if (_world.TryReturnItem(player, serialized))
                given.Add(serialized);
            else
                remaining.Add(serialized);
        }

        if (given.Count > 0)
        {
            // The items leave the record in the same save that lists them as a pending return, so a crash before the export loses none.
            held.Clear();
            held.AddRange(remaining);
            RecordReturn(player, dbPlayer, given);
            _world.SaveRecord(dbPlayer);
            _world.PersistCharacter(player);
        }

        if (held.Count > 0)
            _world.Message(player, HeldItemsMessage);

        return held.Count;
    }

    /// <summary>
    /// Lists items just put in the inventory as a pending return at the next craft file sequence. The caller saves the record, in the same
    /// save that takes the items off the record, and then exports the character.
    /// </summary>
    private PendingCraftReturn RecordReturn(uint player, Player dbPlayer, List<string> items)
    {
        var entry = new PendingCraftReturn
        {
            Sequence = _world.AdvanceFileSequence(player),
            Items = items
        };
        dbPlayer.PendingCraftReturns ??= new List<PendingCraftReturn>();
        dbPlayer.PendingCraftReturns.Add(entry);
        return entry;
    }

    /// <summary>Forgets pending returns an export has since put in the character file. The next save of the record keeps the change.</summary>
    private void DropExportedReturns(uint player, Player dbPlayer)
    {
        var pending = dbPlayer.PendingCraftReturns;
        if (pending == null || pending.Count == 0)
            return;

        var exported = _world.ExportedSequence(player);
        pending.RemoveAll(entry => entry.Sequence <= exported);
    }

    /// <summary>
    /// Writes the character file first when it may not yet hold the session's debit. Items only leave the record for the inventory after the
    /// file has lost them, so a file below a later return always still lacks what the return gives.
    /// </summary>
    private void FlushDebit(uint player, CraftSessionRecord record)
    {
        if (record.DebitSequence > _world.ExportedSequence(player))
            _world.FlushCharacter(player);
    }

    /// <summary>
    /// The recipe's base XP held to the modeled pace: scaled by the share the craft's actions earned in the time since it began
    /// (<see cref="CraftMinigameXp.PaceShare"/>), by the world's clock.
    /// </summary>
    private int PacedBaseXp(CraftSessionRecord record, RecipeDetail recipe, int rank, int actionsTaken)
    {
        var elapsedSeconds = (_world.UtcNow - record.StartedUtc).TotalSeconds;
        return CraftMinigameXp.ScaleBaseXp(
            CraftMinigameXp.BaseXp(recipe, rank),
            CraftMinigameXp.PaceShare(elapsedSeconds, actionsTaken));
    }

    private static void AddPendingXp(Player dbPlayer, SkillType skill, int xp)
    {
        dbPlayer.PendingCraftXp ??= new Dictionary<SkillType, int>();
        dbPlayer.PendingCraftXp[skill] = dbPlayer.PendingCraftXp.GetValueOrDefault(skill) + xp;
    }

    private static List<int> FilledSlots(CraftSessionRecord record)
    {
        var slots = new List<int>();
        for (var index = 0; index < record.Enhancements.Count; index++)
        {
            if (!string.IsNullOrEmpty(record.Enhancements[index]))
                slots.Add(index + 1);
        }

        return slots;
    }

    private static int GetRank(Player dbPlayer, RecipeDetail recipe) =>
        dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
}
