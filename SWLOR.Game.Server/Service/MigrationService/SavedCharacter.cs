using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Node = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Node;
using Field = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Field;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// A saved character file (.bic) and its player record, upgraded together outside the engine at boot.
/// Player migrations change both through this object on worker threads, so nothing here calls NWScript,
/// NWNX, the engine or the database: lookups come from <see cref="SavedCharacterData"/>.
/// </summary>
internal sealed class SavedCharacter
{
    /// <summary>Inventory grid of a character, in item cells: six pages of six rows, and no item spans two pages.</summary>
    internal const int InventoryWidth = 10;
    internal const int InventoryHeight = 36;
    internal const int InventoryPageHeight = 6;

    private readonly StoredObjectData _file;
    /// <summary>
    /// Items that had no room, held apart from the file until the upgrade ends. Later migrations still convert
    /// them, as they converted items the login-time path stowed in a recovery bag.
    /// </summary>
    private readonly Field _overflowList = StoredObjectData.MakeField(15, "OverflowList", children: new List<Node>());

    internal SavedCharacter(byte[] data, Player record)
    {
        _file = new StoredObjectData(data);
        if (_file.FileType != "BIC ")
            throw new InvalidDataException($"Unsupported character file type '{_file.FileType}'.");
        Record = record;
    }

    internal Node Root => _file.Root;

    public Player Record { get; }

    /// <summary>
    /// True while the running migration's effects on the record are not yet saved in it. A retried file whose
    /// record already holds them still gets the file changes, but must not change the record a second time.
    /// </summary>
    public bool AppliesRecordEffects { get; internal set; } = true;

    /// <summary>Items that had no room in the character's inventory, as overflow storage keeps them.</summary>
    public IReadOnlyList<SavedOverflowItem> Overflow => _overflowList.Children.Select(SavedOverflowItem.From).ToList();

    /// <summary>Messages for the player about changes they should know of, delivered at their next login.</summary>
    public List<string> Notices { get; } = new();

    public string Id => ReadString("UUID");

    public byte[] Serialize() => _file.SerializeBytes();

    public int ReadInteger(string name) => StoredObjectData.ReadInteger(Root, name);

    /// <summary>Writes a root scalar, keeping an existing field's GFF type or adding one of <paramref name="type"/>.</summary>
    public void WriteInteger(string name, int value, uint type = 5) => SavedGff.WriteScalar(Root, name, type, value);

    public string ReadString(string name)
    {
        var field = Root.Fields.SingleOrDefault(x => x.Name == name && x.Type == 10);
        return field == null ? string.Empty : Encoding.UTF8.GetString(field.Data.AsSpan(4));
    }

    // ---- Character -----------------------------------------------------------------------------------

    private static readonly string[] AbilityFields = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

    public int GetRawAbilityScore(AbilityType ability) => ReadInteger(AbilityFields[(int)ability]);

    public void SetRawAbilityScore(AbilityType ability, int value) => WriteInteger(AbilityFields[(int)ability], value, 0);

    /// <summary>Feats in the character's feat list, in saved order.</summary>
    public IReadOnlyList<FeatType> Feats =>
        SavedGff.List(Root, "FeatList", false)?.Children.Select(x => (FeatType)StoredObjectData.ReadInteger(x, "Feat")).ToList()
        ?? new List<FeatType>();

    public bool HasFeat(FeatType feat) => Feats.Contains(feat);

    /// <summary>NWNX RemoveFeat: removes the feat from the feat list; the levels that granted it keep their record.</summary>
    public void RemoveFeat(FeatType feat) =>
        SavedGff.List(Root, "FeatList", false)?.Children.RemoveAll(x => StoredObjectData.ReadInteger(x, "Feat") == (int)feat);

    public void ClearFeats() => SavedGff.List(Root, "FeatList", false)?.Children.Clear();

    /// <summary>NWNX AddFeat: adds a feat to the feat list only, once.</summary>
    public void AddFeat(FeatType feat)
    {
        if (!HasFeat(feat))
            SavedGff.List(Root, "FeatList", true).Children.Add(new Node { Type = 1, Fields = new List<Field> { SavedGff.Scalar(2, "Feat", (int)feat) } });
    }

    /// <summary>NWNX AddFeatByLevel: adds a feat to the feat list and to the given level's feats, each once.</summary>
    public void AddFeatByLevel(FeatType feat, int level)
    {
        if (!HasFeat(feat))
            SavedGff.List(Root, "FeatList", true).Children.Add(new Node { Type = 1, Fields = new List<Field> { SavedGff.Scalar(2, "Feat", (int)feat) } });
        var levels = SavedGff.List(Root, "LvlStatList", false)?.Children;
        if (levels == null || level < 1 || level > levels.Count)
            return;
        var levelFeats = SavedGff.List(levels[level - 1], "FeatList", true);
        if (levelFeats.Children.All(x => StoredObjectData.ReadInteger(x, "Feat") != (int)feat))
            levelFeats.Children.Add(new Node { Fields = new List<Field> { SavedGff.Scalar(2, "Feat", (int)feat) } });
    }

    /// <summary>NWNX SetSkillRank: the character's total rank in a native skill.</summary>
    public void SetSkillRank(int skill, int rank)
    {
        var skills = SavedGff.List(Root, "SkillList", false)?.Children;
        if (skills != null && skill >= 0 && skill < skills.Count)
            SavedGff.WriteScalar(skills[skill], "Rank", 0, rank);
    }

    /// <summary>
    /// NWNX SetBaseSavingThrow for all three saves: the saved bonus beside each class save. The engine adds it to
    /// the class save when it loads the character.
    /// </summary>
    public void SetBaseSavingThrows(int value)
    {
        WriteInteger("fortbonus", value, 3);
        WriteInteger("refbonus", value, 3);
        WriteInteger("willbonus", value, 3);
    }

    /// <summary>The class in a class row (1-based position, as GetClassByPosition counts).</summary>
    public ClassType GetClassByPosition(int position)
    {
        var rows = SavedGff.List(Root, "ClassList", false)?.Children;
        return rows == null || position < 1 || position > rows.Count
            ? ClassType.Invalid
            : (ClassType)StoredObjectData.ReadInteger(rows[position - 1], "Class");
    }

    /// <summary>NWNX SetClassByPosition (0-based): changes a class row and the levels taken in its old class.</summary>
    public void SetClassByPosition(int position, ClassType @class)
    {
        var rows = SavedGff.List(Root, "ClassList", false)?.Children;
        if (rows == null || position < 0 || position >= rows.Count)
            return;
        var previous = StoredObjectData.ReadInteger(rows[position], "Class");
        SavedGff.WriteScalar(rows[position], "Class", 5, (int)@class);
        foreach (var level in SavedGff.List(Root, "LvlStatList", false)?.Children ?? new List<Node>())
            if (StoredObjectData.ReadInteger(level, "LvlStatClass") == previous)
                SavedGff.WriteScalar(level, "LvlStatClass", 0, (int)@class);
    }

    /// <summary>
    /// Adds native levels of the first class row until the character has <paramref name="target"/>. A new level
    /// records its class, epic status and every fourth level's ability pick as the engine saves level-ups; the
    /// feat and skill choices a native level-up would also record are left empty, since SWLOR reads neither.
    /// </summary>
    public void AddNativeLevels(int target)
    {
        var rows = SavedGff.List(Root, "ClassList", false)?.Children;
        if (rows == null || rows.Count == 0)
            throw new InvalidDataException("The character has no class.");
        var levels = SavedGff.List(Root, "LvlStatList", true).Children;
        var @class = StoredObjectData.ReadInteger(rows[0], "Class");
        var gained = Math.Max(0, target - NativeLevelCount);
        for (var level = NativeLevelCount + 1; level <= target; level++)
        {
            var fields = new List<Field>();
            if (level % 4 == 0)
                fields.Add(SavedGff.Scalar(0, "LvlStatAbility", 0));
            fields.Add(SavedGff.Scalar(0, "LvlStatHitDie", 1));
            fields.Add(SavedGff.Scalar(0, "LvlStatClass", @class));
            fields.Add(SavedGff.Scalar(0, "EpicLevel", level > 20 ? 1 : 0));
            fields.Add(SavedGff.Scalar(2, "SkillPoints", 0));
            fields.Add(StoredObjectData.MakeField(15, "SkillList", children: Enumerable.Range(0, SkillCount)
                .Select(_ => new Node { Fields = new List<Field> { SavedGff.Scalar(0, "Rank", 0) } }).ToList()));
            fields.Add(StoredObjectData.MakeField(15, "FeatList", children: new List<Node>()));
            levels.Add(new Node { Fields = fields });
            SavedGff.WriteScalar(rows[0], "ClassLevel", 3, StoredObjectData.ReadInteger(rows[0], "ClassLevel") + 1);
            // A level-up adds the class hit die to the base HP and heals it, leaving the damage taken unchanged.
            var hitDie = SavedCharacterData.GetClassHitDie((ClassType)@class);
            WriteInteger("HitPoints", ReadInteger("HitPoints") + hitDie, 3);
            WriteInteger("CurrentHitPoints", ReadInteger("CurrentHitPoints") + hitDie, 3);
        }
        // Each level-up raises the challenge rating by one.
        SavedGff.WriteFloat(Root, "ChallengeRating", SavedGff.ReadFloat(Root, "ChallengeRating") + gained);
    }

    /// <summary>Native skills in a saved skill list.</summary>
    private const int SkillCount = 28;

    /// <summary>Sets a body part, saving the byte-sized and extended fields as the engine does.</summary>
    public void SetCreatureBodyPart(string label, int value) => StoredObjectData.WritePart(Root, label, value);

    /// <summary>Replaces every quickbar slot: the given slots hold feats, the rest are empty.</summary>
    public void SetQuickBar(IReadOnlyDictionary<int, FeatType> feats, int slots = 36)
    {
        var list = SavedGff.List(Root, "QBList", true);
        list.Children.Clear();
        for (var slot = 0; slot < slots; slot++)
        {
            var fields = new List<Field> { SavedGff.Scalar(0, "QBObjectType",
                feats.ContainsKey(slot) ? (int)SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlotType.Feat : 0) };
            if (feats.TryGetValue(slot, out var feat))
                fields.Add(SavedGff.Scalar(5, "QBINTParam1", (int)feat));
            list.Children.Add(new Node { Fields = fields });
        }
    }

    /// <summary>Points a feat quickbar slot at another feat, keeping the slot's other saved fields.</summary>
    public void SetQuickBarFeat(int slot, FeatType feat)
    {
        var slots = SavedGff.List(Root, "QBList", false)?.Children;
        if (slots != null && slot >= 0 && slot < slots.Count)
            SavedGff.WriteScalar(slots[slot], "QBINTParam1", 5, (int)feat);
    }

    /// <summary>The record's active beast, loaded for perk requirements that check it.</summary>
    public Beast ActiveBeast { get; internal set; }

    // ---- Quickbar item buttons -----------------------------------------------------------------------
    // An item button names a worn item by its slot bit (QBItemInvSlot), or a carried item by its inventory cell
    // (QBItemReposX/Y), inside a container at QBContReposX/Y (255 when carried directly).

    private IEnumerable<Node> ItemButtons() =>
        SavedGff.List(Root, "QBList", false)?.Children.Where(button =>
            StoredObjectData.ReadInteger(button, "QBObjectType") == (int)SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlotType.Item)
        ?? Enumerable.Empty<Node>();

    private IEnumerable<Node> QuickBarReferences(SavedItem item)
    {
        var buttons = ItemButtons();
        if (item.IsEquipped)
            return buttons.Where(button => (uint)StoredObjectData.ReadInteger(button, "QBItemInvSlot") == item.Node.Type);
        // Items waiting for overflow storage have no inventory position; their buttons were cleared when they left.
        if (ReferenceEquals((item.Container ?? item).List, _overflowList))
            return Enumerable.Empty<Node>();
        var (x, y) = (StoredObjectData.ReadInteger(item.Node, "Repos_PosX"), StoredObjectData.ReadInteger(item.Node, "Repos_Posy"));
        var (containerX, containerY) = item.Container == null
            ? (byte.MaxValue, byte.MaxValue)
            : (StoredObjectData.ReadInteger(item.Container.Node, "Repos_PosX"), StoredObjectData.ReadInteger(item.Container.Node, "Repos_Posy"));
        if (item.Container?.Container != null)
            return Enumerable.Empty<Node>();
        return buttons.Where(button => (uint)StoredObjectData.ReadInteger(button, "QBItemInvSlot") == uint.MaxValue &&
            StoredObjectData.ReadInteger(button, "QBItemReposX") == x && StoredObjectData.ReadInteger(button, "QBItemReposY") == y &&
            StoredObjectData.ReadInteger(button, "QBContReposX") == containerX && StoredObjectData.ReadInteger(button, "QBContReposY") == containerY);
    }

    // A weapon-set button also names a second item, worn or carried directly: QBSecItemInvSlot (its slot bit, or
    // 0xFFFFFFFF when carried) and QBSecItemReposX/Y.
    private IEnumerable<Node> SecondaryQuickBarReferences(SavedItem item)
    {
        var buttons = ItemButtons().Where(button => button.Fields.Any(field => field.Name == "QBSecItemInvSlot"));
        if (item.IsEquipped)
            return buttons.Where(button => (uint)StoredObjectData.ReadInteger(button, "QBSecItemInvSlot") == item.Node.Type);
        if (item.Container != null || ReferenceEquals(item.List, _overflowList))
            return Enumerable.Empty<Node>();
        var (x, y) = (StoredObjectData.ReadInteger(item.Node, "Repos_PosX"), StoredObjectData.ReadInteger(item.Node, "Repos_Posy"));
        return buttons.Where(button => (uint)StoredObjectData.ReadInteger(button, "QBSecItemInvSlot") == uint.MaxValue &&
            StoredObjectData.ReadInteger(button, "QBSecItemReposX") == x && StoredObjectData.ReadInteger(button, "QBSecItemReposY") == y);
    }

    private void ClearQuickBarReferences(SavedItem item)
    {
        foreach (var button in QuickBarReferences(item).ToList())
            ClearQuickBarButton(button);
        foreach (var button in SecondaryQuickBarReferences(item).ToList())
            RemoveSecondaryItem(button);
    }

    private static void PointSecondaryAtInventory(Node button, int x, int y)
    {
        SavedGff.WriteScalar(button, "QBSecItemInvSlot", 4, -1);
        SavedGff.WriteScalar(button, "QBSecItemReposX", 0, x);
        SavedGff.WriteScalar(button, "QBSecItemReposY", 0, y);
    }

    /// <summary>A weapon-set button whose second item is gone keeps its first item, as the engine leaves it.</summary>
    private static void RemoveSecondaryItem(Node button) =>
        button.Fields.RemoveAll(field => field.Name is "QBSecItemInvSlot" or "QBSecItemReposX" or "QBSecItemReposY");

    private static void PointQuickBarAtInventory(Node button, int x, int y, int containerX, int containerY)
    {
        SavedGff.WriteScalar(button, "QBItemInvSlot", 4, -1);
        SavedGff.WriteScalar(button, "QBItemReposX", 0, x);
        SavedGff.WriteScalar(button, "QBItemReposY", 0, y);
        SavedGff.WriteScalar(button, "QBContReposX", 0, containerX);
        SavedGff.WriteScalar(button, "QBContReposY", 0, containerY);
    }

    /// <summary>An empty quickbar button, as the engine leaves one whose item is gone.</summary>
    private static void ClearQuickBarButton(Node button)
    {
        button.Fields.Clear();
        button.Fields.Add(SavedGff.Scalar(0, "QBObjectType", 0));
    }

    /// <summary>The saved quickbar (QBList), one entry per slot.</summary>
    public IReadOnlyList<SavedQuickBarSlot> QuickBar =>
        SavedGff.List(Root, "QBList", false)?.Children.Select(slot => new SavedQuickBarSlot(
            (SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlotType)StoredObjectData.ReadInteger(slot, "QBObjectType"),
            StoredObjectData.ReadInteger(slot, "QBINTParam1"))).ToList() ?? new List<SavedQuickBarSlot>();

    // ---- Items ---------------------------------------------------------------------------------------

    public IEnumerable<SavedItem> Equipped
    {
        get
        {
            var list = SavedGff.List(Root, "Equip_ItemList", false);
            return list == null ? Enumerable.Empty<SavedItem>() : list.Children.ToArray().Select(node => new SavedItem(node, list, null));
        }
    }

    /// <summary>
    /// Items in the character's own inventory and those waiting for overflow storage, not counting container contents.
    /// </summary>
    public IEnumerable<SavedItem> Carried
    {
        get
        {
            var list = SavedGff.List(Root, "ItemList", false);
            var carried = list == null ? Enumerable.Empty<SavedItem>() : list.Children.ToArray().Select(node => new SavedItem(node, list, null));
            return carried.Concat(_overflowList.Children.ToArray().Select(node => new SavedItem(node, _overflowList, null)));
        }
    }

    /// <summary>Carried items and everything inside carried containers, outer items first.</summary>
    public IEnumerable<SavedItem> CarriedRecursive => Carried.SelectMany(Expand);

    private static IEnumerable<SavedItem> Expand(SavedItem item) => new[] { item }.Concat(item.Contents.SelectMany(Expand));

    public SavedItem GetEquipped(InventorySlot slot) => Equipped.FirstOrDefault(x => x.Slot == slot);

    /// <summary>Removes an item (and its contents) from the character, as destroying it does.</summary>
    public void Destroy(SavedItem item)
    {
        ClearQuickBarReferences(item);
        Detach(item);
    }

    private static void Detach(SavedItem item)
    {
        if (!item.List.Children.Remove(item.Node))
            throw new InvalidOperationException("The saved item is no longer in its inventory list.");
    }

    /// <summary>
    /// Moves an equipped item into the character's inventory, or into overflow storage when no inventory
    /// cell fits it. Returns false when it went to overflow storage. Quickbar buttons for the item follow it
    /// into the inventory, as unequipping does; a button for an item sent to overflow storage is cleared.
    /// </summary>
    public bool Unequip(SavedItem item)
    {
        if (!item.IsEquipped)
            throw new InvalidOperationException("Only equipped items can be unequipped.");
        var references = QuickBarReferences(item).ToList();
        var secondary = SecondaryQuickBarReferences(item).ToList();
        Detach(item);
        var kept = AddToInventory(item.Node);
        var (x, y) = (StoredObjectData.ReadInteger(item.Node, "Repos_PosX"), StoredObjectData.ReadInteger(item.Node, "Repos_Posy"));
        foreach (var button in references)
        {
            if (kept)
                PointQuickBarAtInventory(button, x, y, byte.MaxValue, byte.MaxValue);
            else
                ClearQuickBarButton(button);
        }
        foreach (var button in secondary)
        {
            if (kept)
                PointSecondaryAtInventory(button, x, y);
            else
                RemoveSecondaryItem(button);
        }
        return kept;
    }

    /// <summary>Moves a worn item to another, empty equipment slot, along with its quickbar buttons.</summary>
    public SavedItem MoveEquipped(SavedItem item, InventorySlot slot)
    {
        if (!item.IsEquipped)
            throw new InvalidOperationException("Only equipped items can change slots.");
        if (GetEquipped(slot) != null)
            throw new InvalidOperationException($"Equipment slot {slot} is occupied.");
        foreach (var button in QuickBarReferences(item).ToList())
            SavedGff.WriteScalar(button, "QBItemInvSlot", 4, 1 << (int)slot);
        foreach (var button in SecondaryQuickBarReferences(item).ToList())
            SavedGff.WriteScalar(button, "QBSecItemInvSlot", 4, 1 << (int)slot);
        item.Node.Type = 1u << (int)slot;
        return new SavedItem(item.Node, item.List, null);
    }

    /// <summary>The current position of an item node: equipped, carried, or inside a carried container.</summary>
    public SavedItem Locate(Node node) =>
        Equipped.Concat(CarriedRecursive).FirstOrDefault(item => ReferenceEquals(item.Node, node));

    /// <summary>Equips a carried item into an empty slot, along with its quickbar buttons.</summary>
    public SavedItem Equip(SavedItem item, InventorySlot slot)
    {
        if (GetEquipped(slot) != null)
            throw new InvalidOperationException($"Equipment slot {slot} is occupied.");
        foreach (var button in QuickBarReferences(item).ToList())
            SavedGff.WriteScalar(button, "QBItemInvSlot", 4, 1 << (int)slot);
        foreach (var button in SecondaryQuickBarReferences(item).ToList())
            SavedGff.WriteScalar(button, "QBSecItemInvSlot", 4, 1 << (int)slot);
        Detach(item);
        item.Node.Fields.RemoveAll(x => x.Name is "Repos_PosX" or "Repos_Posy");
        item.Node.Type = 1u << (int)slot;
        var list = SavedGff.List(Root, "Equip_ItemList", true);
        list.Children.Add(item.Node);
        return new SavedItem(item.Node, list, null);
    }

    /// <summary>
    /// Places an item node in the first inventory cell range that fits it, scanning rows top to bottom as the
    /// engine's repository does, or keeps it for overflow storage. Returns false when it went to overflow storage.
    /// </summary>
    public bool AddToInventory(Node item)
    {
        var list = SavedGff.List(Root, "ItemList", true);
        if (Place(list, InventoryWidth, InventoryHeight, item, InventoryPageHeight))
            return true;
        _overflowList.Children.Add(item);
        return false;
    }

    /// <summary>
    /// Places an item node in a container's inventory, or the character's inventory when the container is full,
    /// or overflow storage when that is full too. Returns false when it went to overflow storage.
    /// </summary>
    public bool AddToContainer(SavedItem container, Node item)
    {
        var list = SavedGff.List(container.Node, "ItemList", true);
        if (Place(list, SavedCharacterData.ContainerWidth, SavedCharacterData.ContainerHeight, item))
        {
            StoredObjectData.NumberListElements(list);
            return true;
        }
        return AddToInventory(item);
    }

    /// <summary>
    /// Saved-data <c>CreateItemOnObject</c> plus removal, as <see cref="ObsoleteItemMigration.ConvertItem"/> replaces an
    /// item: a new item from the blueprint with the old stack size and droid item identity goes to the old item's
    /// holder (the character's inventory for worn and carried items), then the old item is removed.
    /// </summary>
    public bool Replace(SavedItem item, string replacementResRef)
    {
        var replacement = StoredObjectData.ParseItem(SavedCharacterData.GetBlueprint(replacementResRef));
        var saved = new SavedItem(replacement, null, null) { StackSize = Math.Max(1, item.StackSize) };
        var droidItemId = item.GetLocalString("DROID_ITEM_ID");
        if (!string.IsNullOrWhiteSpace(droidItemId))
            saved.SetLocalString("DROID_ITEM_ID", droidItemId);
        // The engine marks items it places in a character's inventory as pickpocketable; the blueprint does not.
        if (item.Node.Fields.Any(field => field.Name == "Pickpocketable"))
            SavedGff.WriteScalar(replacement, "Pickpocketable", 0, StoredObjectData.ReadInteger(item.Node, "Pickpocketable"));
        var kept = item.Container == null ? AddToInventory(replacement) : AddToContainer(item.Container, replacement);
        // Removing a worn item takes its stats off the record, as unequipping it does.
        var worn = item.IsEquipped;
        if (worn)
            ApplyItemStats(item, false);
        Destroy(item);
        if (worn)
            RefreshCombatReadinessEquipment();
        if (item.Container != null)
            StoredObjectData.NumberListElements(item.List);
        return kept;
    }

    /// <summary>
    /// Places an item node in the first free cell range of an inventory grid, scanning rows top to bottom, where
    /// the item stays within one page of <paramref name="pageHeight"/> rows (the whole grid by default); false
    /// when none fits.
    /// </summary>
    internal static bool Place(Field list, int width, int height, Node item, int pageHeight = 0)
    {
        if (pageHeight <= 0)
            pageHeight = height;
        item.Fields.RemoveAll(x => x.Name is "Repos_PosX" or "Repos_Posy");
        var size = SavedCharacterData.GetInventorySize(StoredObjectData.ReadBaseItem(item));
        var occupied = new bool[width, height];
        foreach (var carried in list.Children)
        {
            var x = StoredObjectData.ReadInteger(carried, "Repos_PosX");
            var y = StoredObjectData.ReadInteger(carried, "Repos_Posy");
            var cells = SavedCharacterData.GetInventorySize(StoredObjectData.ReadBaseItem(carried));
            for (var dx = 0; dx < cells.Width; dx++)
            for (var dy = 0; dy < cells.Height; dy++)
                if (x + dx < width && y + dy < height)
                    occupied[x + dx, y + dy] = true;
        }
        for (var y = 0; y + size.Height <= height; y++)
        for (var x = 0; x + size.Width <= width; x++)
        {
            if (y % pageHeight + size.Height > pageHeight)
                continue;
            var fits = true;
            for (var dx = 0; dx < size.Width && fits; dx++)
            for (var dy = 0; dy < size.Height && fits; dy++)
                fits = !occupied[x + dx, y + dy];
            if (!fits) continue;
            item.Type = 0;
            item.Fields.Add(SavedGff.Scalar(2, "Repos_PosX", x));
            item.Fields.Add(SavedGff.Scalar(2, "Repos_Posy", y));
            list.Children.Add(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Replaces an item in a saved container outside the character (a droid's bag) with a new item node, which may
    /// use the cells the old item frees. False, with the container unchanged, when the new item still has no room.
    /// </summary>
    internal static bool ReplaceInContainer(SavedItem old, Node replacement)
    {
        var list = old.List;
        var index = list.Children.IndexOf(old.Node);
        list.Children.RemoveAt(index);
        if (!Place(list, SavedCharacterData.ContainerWidth, SavedCharacterData.ContainerHeight, replacement))
        {
            list.Children.Insert(index, old.Node);
            return false;
        }
        StoredObjectData.NumberListElements(list);
        return true;
    }

    // ---- Record effects ------------------------------------------------------------------------------

    /// <summary>Adds or removes every stat an equipped item grants, as <see cref="Feature.EquipmentStats.ApplyItemStats"/> does.</summary>
    public void ApplyItemStats(SavedItem item, bool isAdding)
    {
        if (!AppliesRecordEffects) return;
        foreach (var property in item.Properties)
            SavedEquipmentStats.Apply(this, item, property.Type, property.Subtype, property.CostValue, isAdding);
    }

    /// <summary>Adds or removes one amount of a stat for an equipped item, as <see cref="Feature.EquipmentStats.ApplyStatAmount"/> does.</summary>
    public void ApplyStatAmount(SavedItem item, ItemPropertyType type, int subtype, int amount, bool isAdding)
    {
        if (!AppliesRecordEffects || amount <= 0) return;
        SavedEquipmentStats.Apply(this, item, type, subtype, amount, isAdding);
    }

    /// <summary>Saved-data <see cref="Stat.RefreshCombatReadinessEquipment"/>: the record holds the equipped total.</summary>
    public void RefreshCombatReadinessEquipment()
    {
        if (!AppliesRecordEffects) return;
        Record.CombatReadiness = Equipped.Sum(item => item.Properties
            .Where(property => property.Type == ItemPropertyType.CombatReadiness)
            .Sum(property => property.CostValue));
    }

    /// <summary>
    /// Saved-data <see cref="Stat.AdjustPlayerMaxHP"/>: changes the record's maximum and spreads it over the
    /// character's native levels, at least one HP per level and at most 255.
    /// </summary>
    public void AdjustPlayerMaxHP(int adjustBy)
    {
        // A record that already holds this change still has its maximum written to the file's levels.
        if (AppliesRecordEffects)
            Record.MaxHP += adjustBy;
        ApplyMaxHitPointsToLevels();
    }

    /// <summary>
    /// Writes the record's maximum HP into the native level hit dice, as AdjustPlayerMaxHP does. A living character
    /// is saved at full HP for its new maximum (<see cref="SetSavedHitPointsToMaximum"/>).
    /// </summary>
    public void ApplyMaxHitPointsToLevels()
    {
        const int MaxHPPerLevel = 254;
        var levels = SavedGff.List(Root, "LvlStatList", false)?.Children ?? new List<Node>();
        var levelCount = NativeLevelCount;
        var hpToApply = Record.MaxHP;
        for (var level = 0; level < levelCount && level < levels.Count; level++)
        {
            hpToApply--;
            SavedGff.WriteScalar(levels[level], "LvlStatHitDie", 0, 1);
        }
        for (var level = 0; hpToApply > 0 && level < levelCount && level < levels.Count; level++)
        {
            if (hpToApply > MaxHPPerLevel)
            {
                SavedGff.WriteScalar(levels[level], "LvlStatHitDie", 0, 255);
                hpToApply -= MaxHPPerLevel;
            }
            else
            {
                SavedGff.WriteScalar(levels[level], "LvlStatHitDie", 0, hpToApply + 1);
                break;
            }
        }
        SetSavedHitPointsToMaximum();
    }

    /// <summary>The character's native level total across its class rows.</summary>
    public int NativeLevelCount =>
        (SavedGff.List(Root, "ClassList", false)?.Children ?? new List<Node>()).Take(3).Sum(row => StoredObjectData.ReadInteger(row, "ClassLevel"));

    /// <summary>
    /// The maximum HP the engine computes for the saved character: each native level adds its hit die plus the
    /// Vitality modifier, at least one, and an odd Vitality score adds one HP per two of those levels
    /// (<see cref="Native.VitalityHitPoints"/>).
    /// </summary>
    public int GetMaxHitPoints()
    {
        var vitality = SavedEquipmentStats.GetAbilityScore(this, AbilityType.Vitality);
        var modifier = (int)Math.Floor((vitality - 10) / 2.0);
        var dice = (SavedGff.List(Root, "LvlStatList", false)?.Children ?? new List<Node>())
            .Take(NativeLevelCount).Select(level => StoredObjectData.ReadInteger(level, "LvlStatHitDie")).ToList();
        var eligible = modifier < 0 ? dice.Count(die => die + modifier >= 1) : dice.Count;
        return dice.Sum(die => Math.Max(1, die + modifier)) + Native.VitalityHitPoints.CalculateOddScoreBonus(vitality, eligible);
    }

    /// <summary>Whether the character was saved dead: its saved current HP is 0 or less.</summary>
    public bool IsDead => ReadInteger("PregameCurrent") <= 0;

    /// <summary>
    /// The current HP a migration reads after it set the character to full HP: the maximum, or the saved current
    /// HP of a dead character, which SetCurrentHitPoints cannot revive.
    /// </summary>
    public int GetCurrentHitPoints() => IsDead ? ReadInteger("PregameCurrent") : GetMaxHitPoints();

    /// <summary>
    /// Saves a living character at full HP for its maximum: no damage taken (CurrentHitPoints equal to HitPoints),
    /// with the saved maximum and pregame HP the engine writes beside them. Login then sets the character's HP from
    /// its record (<see cref="Feature.PersistentHitPoints.LoadHP"/>), so the record holds the HP a migration keeps;
    /// a character loaded at full HP is never below it. A dead character keeps its saved HP.
    /// </summary>
    public void SetSavedHitPointsToMaximum()
    {
        if (IsDead)
            return;
        var maximum = GetMaxHitPoints();
        WriteInteger("CurrentHitPoints", ReadInteger("HitPoints"), 3);
        WriteInteger("MaxHitPoints", maximum, 3);
        WriteInteger("PregameCurrent", maximum, 3);
    }

    /// <summary>Saved-data SetCurrentHitPoints(GetMaxHitPoints()).</summary>
    public void RestoreFullHitPoints() => SetSavedHitPointsToMaximum();
}

/// <summary>A saved quickbar slot: its object type and first integer parameter (a feat for feat slots).</summary>
internal readonly record struct SavedQuickBarSlot(SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlotType Type, int IntParameter);

/// <summary>An item a saved-character upgrade could not keep in the character's inventory.</summary>
internal sealed class SavedOverflowItem
{
    /// <summary>The item as a standalone UTI, base64 encoded as <c>ObjectPlugin.Serialize</c> writes items.</summary>
    public string Data { get; init; }
    public string Name { get; init; }
    public string Resref { get; init; }
    public string Tag { get; init; }
    public int Quantity { get; init; }

    internal static SavedOverflowItem From(Node item)
    {
        var saved = new SavedItem(item, null, null);
        return new SavedOverflowItem
        {
            Data = StoredObjectData.ItemToUti(item),
            Name = saved.Name,
            Resref = saved.ResRef,
            Tag = saved.Tag,
            Quantity = Math.Max(1, saved.StackSize),
        };
    }
}
