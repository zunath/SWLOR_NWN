using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// An in-memory stand-in for the game engine. Items are strings: a component stack is "resref xN", an enhancement is the
/// name it was given. Every engine call is also written to <see cref="Events"/> so a test can check order.
/// </summary>
internal sealed class FakeCraftSessionWorld : ICraftSessionWorld
{
    public const uint PlayerHandle = 1;
    public const uint FirstEnhancement = 100;
    public const uint SecondEnhancement = 101;
    public const uint Blueprint = 200;
    private uint _nextItem = 1000;

    public Dictionary<RecipeType, RecipeDetail> Recipes { get; } = new();
    public Dictionary<string, int> Inventory { get; } = new();
    public Dictionary<uint, string> Enhancements { get; } = new();
    public bool MeetsRequirements { get; set; } = true;
    public string BlueprintProblem { get; set; } = string.Empty;
    public BlueprintDetail BlueprintAfterCharge { get; set; } = new() { Recipe = RecipeType.Invalid, Level = 2, LicensedRuns = 4 };

    /// <summary>The number of returned items the inventory can still take.</summary>
    public int Room { get; set; } = int.MaxValue;

    public List<string> Events { get; } = new();
    public List<string> Returned { get; } = new();

    /// <summary>The finished items the player carries, by their serialized form.</summary>
    public List<string> Crafted { get; } = new();
    public List<string> Messages { get; } = new();

    /// <summary>The skills whose station work clip was started, in order. Kept apart from <see cref="Events"/> so ordering tests stay as they are.</summary>
    public List<SkillType> StationAnimations { get; } = new();
    public List<(SkillType Skill, int Xp)> XpGiven { get; } = new();
    public List<string> AppliedEnhancements { get; } = new();
    public List<(int Seconds, int Charges)> FoodBonuses { get; } = new();
    public List<int> VendorValues { get; } = new();
    public int BlueprintBonusCalls { get; private set; }
    public int SaveCount { get; private set; }

    /// <summary>How often the character file was asked to be written, and the saves the record had had by then.</summary>
    public List<int> PersistedAtSave { get; } = new();

    /// <summary>The clock the lifecycle reads. It only moves when a test advances it.</summary>
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Advance(double seconds) => UtcNow = UtcNow.AddSeconds(seconds);

    /// <summary>Moves the clock as far as a craft of <paramref name="actions"/> actions takes at the modeled pace.</summary>
    public void AdvanceByActions(int actions) => Advance(actions * CraftMinigameXp.ActionSeconds);

    public RecipeDetail GetRecipe(RecipeType recipe) => Recipes[recipe];

    public bool CanCraft(uint player, RecipeType recipe) => MeetsRequirements;

    public string ValidateBlueprint(uint player, uint blueprintItem, RecipeType recipe) => BlueprintProblem;

    public BlueprintDetail ChargeBlueprint(uint player, uint blueprintItem)
    {
        Events.Add("charge-blueprint");
        return BlueprintAfterCharge;
    }

    public bool TryTakeComponents(uint player, RecipeDetail recipe, List<string> taken)
    {
        foreach (var (resref, quantity) in recipe.Components)
        {
            if (!Inventory.TryGetValue(resref, out var have) || have < quantity)
                return false;
        }

        foreach (var (resref, quantity) in recipe.Components)
        {
            Inventory[resref] -= quantity;
            taken.Add($"{resref} x{quantity}");
        }

        Events.Add("take-components");
        return true;
    }

    public bool HasEnhancement(uint player, uint item) => Enhancements.ContainsKey(item);

    public string TakeEnhancement(uint player, uint item)
    {
        Enhancements.Remove(item, out var serialized);
        Events.Add($"take-enhancement:{serialized}");
        return serialized;
    }

    public bool TryReturnItem(uint player, string serialized)
    {
        if (Returned.Count >= Room)
            return false;

        Returned.Add(serialized);
        return true;
    }

    public void BeginStationWork(uint player) => Events.Add("begin-work");

    public void PlayStationWork(uint player, SkillType skill) => StationAnimations.Add(skill);

    public void EndStationWork(uint player) => Events.Add("end-work");

    public uint CreateFinishedItem(RecipeDetail recipe)
    {
        Events.Add($"create:{recipe.Resref}");
        var item = _nextItem++;
        _craftedNames[item] = $"crafted {recipe.Resref}";
        Crafted.Add(_craftedNames[item]);
        return item;
    }

    private readonly Dictionary<uint, string> _craftedNames = new();

    /// <summary>Resrefs whose template does not stack, so a multi-unit recipe's extra units are made as copies.</summary>
    public HashSet<string> NonStackingResrefs { get; } = new();

    /// <summary>How many of the finished units fit the inventory; the rest are held.</summary>
    public int OutputRoom { get; set; } = int.MaxValue;

    /// <summary>True to model an inventory with no room for any unit of the output.</summary>
    public bool OutputLost { get; set; }

    public CraftedUnits FinishUnits(uint player, uint item, RecipeDetail recipe)
    {
        Events.Add("finish");
        var result = new CraftedUnits(new List<string>(), new List<string>());
        var units = NonStackingResrefs.Contains(recipe.Resref) ? System.Math.Max(1, recipe.Quantity) : 1;
        var original = _craftedNames[item];
        Crafted.Remove(original);

        for (var unit = 0; unit < units; unit++)
        {
            var made = item;
            if (unit > 0)
            {
                Events.Add($"copy:{recipe.Resref}");
                made = _nextItem++;
                _craftedNames[made] = $"{original} copy {unit}";
            }

            if (!OutputLost && result.Delivered.Count < OutputRoom)
            {
                result.Delivered.Add(_craftedNames[made]);
                Crafted.Add(_craftedNames[made]);
            }
            else
            {
                result.Held.Add(original);
            }
        }

        return result;
    }

    public void ApplyVendorValue(uint item, int addGoldPiece) => VendorValues.Add(addGoldPiece);

    public void ApplyEnhancement(uint item, string serializedEnhancement, RecipeDetail recipe)
    {
        Events.Add("enhance");
        AppliedEnhancements.Add(serializedEnhancement);
    }

    public void ApplyFoodBonus(uint item, int durationSeconds, int bonusCharges) =>
        FoodBonuses.Add((durationSeconds, bonusCharges));

    public void ApplyBlueprintBonuses(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint) =>
        BlueprintBonusCalls++;

    /// <summary>False while the player is dead: no skill XP can be given.</summary>
    public bool PlayerAlive { get; set; } = true;

    /// <summary>True stops the server at the moment the lifecycle asks whether XP can be given: the record is saved, the award never runs.</summary>
    public bool StopBeforeXpGrant { get; set; }

    public bool CanReceiveXp(uint player)
    {
        if (StopBeforeXpGrant)
            throw new ServerStoppedException();

        return PlayerAlive;
    }

    /// <summary>What the saved record held at each save: whether a craft was still active and the XP entitlement it carried.</summary>
    public List<(bool SessionActive, Dictionary<SkillType, int> PendingXp)> SavedRecords { get; } = new();

    /// <summary>For each XP award, the entitlement the last saved record still held when the award ran.</summary>
    public List<Dictionary<SkillType, int>> SavedPendingAtXp { get; } = new();

    public bool AwardPendingXp(uint player, Player dbPlayer, SkillType skill, int xp)
    {
        Events.Add("xp");
        SavedPendingAtXp.Add(SavedRecords.Count > 0 ? SavedRecords[^1].PendingXp : new Dictionary<SkillType, int>());
        XpGiven.Add((skill, xp));

        // The award and the removal are one save.
        dbPlayer.PendingCraftXp?.Remove(skill);
        SaveRecord(dbPlayer);
        return true;
    }

    public void SaveRecord(Player dbPlayer)
    {
        SaveCount++;
        Events.Add("save");
        SavedRecords.Add((dbPlayer.ActiveCraftSession != null, new Dictionary<SkillType, int>(dbPlayer.PendingCraftXp ?? new Dictionary<SkillType, int>())));
    }

    public void PersistCharacter(uint player)
    {
        PersistedAtSave.Add(SaveCount);
        ExportQueued = true;
    }

    public string SerializeItem(uint item) => _craftedNames[item];

    /// <summary>The craft file sequence in memory: what the next character file written will carry.</summary>
    public int Sequence { get; private set; }

    /// <summary>The craft file sequence in the character file on disk.</summary>
    public int FileSequenceOnDisk { get; private set; }

    private int _exported;

    public int FileSequence(uint player) => Sequence;

    public int AdvanceFileSequence(uint player) => ++Sequence;

    public int ExportedSequence(uint player) => _exported;

    // Before any simulated reload the login sees the sequence held in memory, as a login straight after a clean export would.
    private int? _loaded;
    public int LoadedSequence(uint player) => _loaded ?? Sequence;

    /// <summary>True from a persist request until the delayed export runs (<see cref="RunQueuedExport"/>) or a flush writes the file.</summary>
    public bool ExportQueued { get; private set; }

    public int Flushes { get; private set; }

    public void FlushCharacter(uint player)
    {
        Flushes++;
        WriteFile();
    }

    /// <summary>The export a persist request queues, once its delay has passed.</summary>
    public void RunQueuedExport()
    {
        if (ExportQueued)
            WriteFile();
    }

    private Dictionary<string, int> _fileInventory = new();
    private List<string> _fileReturned = new();
    private List<string> _fileCrafted = new();

    private void WriteFile()
    {
        ExportQueued = false;
        FileSequenceOnDisk = Sequence;
        _exported = Sequence;
        _fileInventory = new Dictionary<string, int>(Inventory);
        _fileReturned = new List<string>(Returned);
        _fileCrafted = new List<string>(Crafted);
    }

    /// <summary>
    /// A process crash and the login after it: the inventory, the returned and finished items and the sequence come back from the last
    /// character file written. Nothing queued runs. The player record is not touched, as it was saved at once.
    /// </summary>
    public void CrashAndReload()
    {
        ExportQueued = false;
        Inventory.Clear();
        foreach (var (resref, quantity) in _fileInventory)
            Inventory[resref] = quantity;
        Returned.Clear();
        Returned.AddRange(_fileReturned);
        Crafted.Clear();
        Crafted.AddRange(_fileCrafted);
        Sequence = FileSequenceOnDisk;
        _exported = FileSequenceOnDisk;
        _loaded = FileSequenceOnDisk;
    }

    /// <summary>Writes the character file as it stands, as a logout or an unrelated export does.</summary>
    public void ExportNow() => WriteFile();

    public void Message(uint player, string text) => Messages.Add(text);

    public void LogCraft(uint player, string text)
    {
    }
}
