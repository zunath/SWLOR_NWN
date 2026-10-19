using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class MusicCollectionTests
{
    private const int SongKeyItemBase = 3000;
    private static IReadOnlyList<MusicSong> Songs => MusicSongCatalog.Songs;

    [Test]
    public void Catalog_TracksEverySongAsAMusicKeyItem()
    {
        Songs.Select(song => song.KeyItem).Should().OnlyHaveUniqueItems();
        Songs.Select(song => song.TrackId).Should().OnlyHaveUniqueItems();
        Songs.Should().OnlyContain(song => song.Category != MusicCategoryType.Invalid);
        Songs.Should().OnlyContain(song => song.Acquisition != MusicAcquisitionType.Invalid);
        Songs.Should().OnlyContain(song => (int)song.KeyItem == SongKeyItemBase + song.TrackId,
            "song key item IDs are derived from their ambientmusic.2da row");

        var musicKeyItems = Enum.GetValues<KeyItemType>()
            .Where(type => GetKeyItem(type).Category == KeyItemCategoryType.Music)
            .ToHashSet();
        musicKeyItems.Should().BeEquivalentTo(Songs.Select(song => song.KeyItem),
            "every Music key item must be a playable song and vice versa");
        Songs.Select(song => GetKeyItem(song.KeyItem).Name).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Catalog_CoversEveryNamedAmbientMusicTrack()
    {
        var namedRows = ReadNamedAmbientMusicRows();

        namedRows.Should().NotBeEmpty();
        Songs.Select(song => song.TrackId).Should().BeEquivalentTo(namedRows,
            "every ambientmusic.2da track except the silent row 0 is collectible");
    }

    [Test]
    public void Acquisition_StartsWithTwoSongsAndSellsAHandful()
    {
        Songs.Count(song => song.Acquisition == MusicAcquisitionType.Starter).Should().Be(2);
        Songs.Count(song => song.Acquisition == MusicAcquisitionType.Store).Should().BeInRange(5, 15);
        Songs.Count(song => song.Acquisition == MusicAcquisitionType.BossDrop).Should().BeGreaterThan(0);
        Songs.Count(song => song.Acquisition == MusicAcquisitionType.Loot).Should().BeGreaterThan(0);
        Songs.Count(song => song.Acquisition == MusicAcquisitionType.Crafted).Should().Be(50);
    }

    [Test]
    public void CraftedSongs_AreEngineeringRecipesInTheirMusicCategory()
    {
        var recipes = new MusicRecipes().BuildRecipes().Values.ToList();
        var categories = new Dictionary<MusicCategoryType, RecipeCategoryType>
        {
            [MusicCategoryType.Cantina] = RecipeCategoryType.CantinaMusicDiscs,
            [MusicCategoryType.Battle] = RecipeCategoryType.BattleMusicDiscs,
            [MusicCategoryType.Theme] = RecipeCategoryType.ThemeMusicDiscs,
            [MusicCategoryType.Location] = RecipeCategoryType.LocationMusicDiscs,
            [MusicCategoryType.Ambient] = RecipeCategoryType.AmbientMusicDiscs,
        };

        recipes.Should().OnlyContain(recipe => recipe.Skill == SkillType.Engineering);
        recipes.Select(recipe => recipe.Resref).Should().OnlyHaveUniqueItems();
        recipes.Should().ContainSingle(recipe => recipe.Resref == "music_player" &&
                                                 recipe.Category == RecipeCategoryType.MusicPlayers);

        var discRecipes = recipes.Where(recipe => recipe.Resref != "music_player").ToList();
        discRecipes.Select(recipe => recipe.Resref).Should().BeEquivalentTo(DiscsFor(MusicAcquisitionType.Crafted),
            "only crafted songs have recipes");
        foreach (var song in Songs.Where(song => song.Acquisition == MusicAcquisitionType.Crafted))
        {
            var recipe = discRecipes.Single(x => x.Resref == $"song_{song.TrackId}");
            recipe.Category.Should().Be(categories[song.Category], $"song_{song.TrackId} is a {song.Category} song");
        }

        discRecipes.Select(recipe => recipe.Level).Should().BeEquivalentTo(Enumerable.Range(1, 50),
            "crafted discs are spread one per Engineering level");
    }

    [Test]
    public void SongDiscs_GrantTheirSongKeyItem()
    {
        var root = FindRepositoryRoot();

        foreach (var song in Songs)
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"song_{song.TrackId}.uti.json");
            if (song.Acquisition == MusicAcquisitionType.Starter)
            {
                File.Exists(path).Should().BeFalse($"starter song {song.KeyItem} is granted directly, not as a disc");
                continue;
            }

            File.Exists(path).Should().BeTrue($"{song.KeyItem} needs a song disc blueprint");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var item = document.RootElement;
            item.GetProperty("Tag").GetProperty("value").GetString().Should().Be("KEY_ITEM");
            var keyItemId = item.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Single(var => var.GetProperty("Name").GetProperty("value").GetString() == "KEY_ITEM_ID")
                .GetProperty("Value").GetProperty("value").GetInt32();
            keyItemId.Should().Be((int)song.KeyItem, $"song_{song.TrackId} must unlock its own song");
        }
    }

    [Test]
    public void LootTables_DropOnlyTheirOwnSongs()
    {
        var tables = new MusicLootTableDefinition().BuildLootTables();
        var regular = tables[MusicLootTableDefinition.MusicDiscs];
        var boss = tables[MusicLootTableDefinition.BossMusicDiscs];

        regular.IsRare.Should().BeTrue();
        boss.IsRare.Should().BeTrue();
        regular.Select(item => item.Resref).Should().BeEquivalentTo(DiscsFor(MusicAcquisitionType.Loot));
        boss.Select(item => item.Resref).Should().BeEquivalentTo(DiscsFor(MusicAcquisitionType.BossDrop));
    }

    // Rare find bonuses: none, Treasure Hunter I-III, and the beast-only 5/10/15 bonuses.
    [TestCase(0, 1, 5)]
    [TestCase(10, 7, 10)]
    [TestCase(20, 13, 15)]
    [TestCase(30, 20, 20)]
    [TestCase(5, 4, 7)]
    [TestCase(15, 10, 12)]
    public void SongDiscDropChance_TopsOutAtTwentyPercent(int rareBonus, int regularChance, int bossChance)
    {
        var tables = new MusicLootTableDefinition().BuildLootTables();

        (MusicLootTableDefinition.MusicDiscChance + tables[MusicLootTableDefinition.MusicDiscs].GetRareChanceBonus(rareBonus))
            .Should().Be(regularChance);
        (MusicLootTableDefinition.BossMusicDiscChance + tables[MusicLootTableDefinition.BossMusicDiscs].GetRareChanceBonus(rareBonus))
            .Should().Be(bossChance);
    }

    [Test]
    public void RareTablesWithoutAScale_KeepTheFullRareBonus()
    {
        new LootTable { IsRare = true }.GetRareChanceBonus(LootTable.FullRareChanceBonus)
            .Should().Be(LootTable.FullRareChanceBonus);
    }

    [Test]
    public void SongDiscTables_AreAddedAlongsideExistingCreatureLoot()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        var regular = 0;
        var boss = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utc"), "*.utc.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var lootTables = document.RootElement.TryGetProperty("VarTable", out var varTable)
                ? varTable.GetProperty("value").EnumerateArray()
                    .Select(var => (Name: var.GetProperty("Name").GetProperty("value").GetString()!,
                        Value: var.GetProperty("Value").GetProperty("value").ToString()))
                    .Where(var => var.Name.StartsWith("LOOT_TABLE_"))
                    .ToList()
                : new List<(string Name, string Value)>();
            var musicTables = lootTables.Where(var => var.Value.StartsWith("MUSIC_DISCS")).ToList();
            if (musicTables.Count == 0)
                continue;

            var resref = Path.GetFileName(path).Split('.')[0];
            if (musicTables.Count > 1)
                failures.Add($"{resref} carries more than one song table");
            if (lootTables.Count == musicTables.Count)
                failures.Add($"{resref} drops only songs; song tables must sit beside existing loot");
            if (resref.StartsWith("cp_"))
                failures.Add($"{resref} is a capstone creature");

            // Loot stops reading at the first missing LOOT_TABLE_N, so a gap would skip the song table.
            var expectedNames = Enumerable.Range(1, lootTables.Count).Select(index => $"LOOT_TABLE_{index}");
            if (!lootTables.Select(var => var.Name).ToHashSet().SetEquals(expectedNames))
                failures.Add($"{resref} loot tables are not numbered 1 to {lootTables.Count} without gaps");
            if (lootTables.Any(var => string.IsNullOrWhiteSpace(var.Value)))
                failures.Add($"{resref} loot tables contain blank values");

            if (musicTables[0].Value.StartsWith($"{MusicLootTableDefinition.BossMusicDiscs},"))
            {
                boss++;
                if (musicTables[0].Value != $"{MusicLootTableDefinition.BossMusicDiscs},{MusicLootTableDefinition.BossMusicDiscChance},1")
                    failures.Add($"{resref} boss song table is '{musicTables[0].Value}'");
            }
            else
            {
                regular++;
                if (musicTables[0].Value != $"{MusicLootTableDefinition.MusicDiscs},{MusicLootTableDefinition.MusicDiscChance},1")
                    failures.Add($"{resref} song table is '{musicTables[0].Value}'");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
        regular.Should().BeGreaterThan(0);
        boss.Should().BeGreaterThan(0);
    }

    [Test]
    public void GalaxyBar_SellsOnlyTheStoreSongs()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root.FullName, "Module", "utm", "night_galaxybar.utm.json")));
        var sold = document.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray()
            .SelectMany(panel => panel.TryGetProperty("ItemList", out var items)
                ? items.GetProperty("value").EnumerateArray()
                : Enumerable.Empty<JsonElement>())
            .Select(item => item.GetProperty("InventoryRes").GetProperty("value").GetString())
            .ToHashSet();

        sold.Should().NotContain("music_player", "the Portable Music Player is crafted only");
        sold.Should().Contain(DiscsFor(MusicAcquisitionType.Store));
        sold.Should().NotContain(DiscsFor(MusicAcquisitionType.Loot)
                .Concat(DiscsFor(MusicAcquisitionType.BossDrop))
                .Concat(DiscsFor(MusicAcquisitionType.Crafted)),
            "dropped songs are not sold");
    }

    private static IEnumerable<string> DiscsFor(MusicAcquisitionType acquisition) =>
        Songs.Where(song => song.Acquisition == acquisition).Select(song => $"song_{song.TrackId}");

    private static KeyItemAttribute GetKeyItem(KeyItemType type) =>
        typeof(KeyItemType).GetField(type.ToString())!.GetCustomAttribute<KeyItemAttribute>()!;

    private static HashSet<int> ReadNamedAmbientMusicRows()
    {
        var root = FindRepositoryRoot();
        var lines = File.ReadAllLines(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "ambientmusic.2da"));
        var header = Tokenize(lines[2]);
        var descriptionIndex = header.IndexOf("Description") + 1;
        var displayNameIndex = header.IndexOf("DisplayName") + 1;
        var rows = new HashSet<int>();

        foreach (var line in lines.Skip(3).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var columns = Tokenize(line);
            var row = int.Parse(columns[0]);
            var description = columns.ElementAtOrDefault(descriptionIndex) ?? "****";
            var displayName = columns.ElementAtOrDefault(displayNameIndex) ?? "****";
            if (row != 0 && (description != "****" || displayName != "****"))
                rows.Add(row);
        }

        return rows;
    }

    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var c in line)
        {
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());
        return tokens;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
