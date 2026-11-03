using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PazaakCardItemTests
{
    private const int CardBaseItem = 401;

    [Test]
    public void EveryCardHasItsOwnBlueprint()
    {
        var root = FindRepositoryRoot();
        PazaakCards.All.Select(card => card.Resref).Should().OnlyHaveUniqueItems();
        PazaakCards.All.Select(card => card.Model).Should().OnlyHaveUniqueItems();

        foreach (var card in PazaakCards.All)
        {
            card.Resref.Length.Should().BeLessThanOrEqualTo(16, card.Resref);
            var path = Path.Combine(root.FullName, "Module", "uti", $"{card.Resref}.uti.json");
            File.Exists(path).Should().BeTrue($"{card.Resref} needs a blueprint");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var uti = document.RootElement;
            uti.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(CardBaseItem, card.Resref);
            uti.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(card.Model, card.Resref);
            uti.GetProperty("Tag").GetProperty("value").GetString().Should().Be(PazaakCards.CardItemTag, card.Resref);
            uti.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(card.Resref);
            uti.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()
                .Should().Be($"Pazaak Card: {card.Name}");

            var cardType = uti.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Single(var => var.GetProperty("Name").GetProperty("value").GetString() == PazaakCards.CardTypeVariable)
                .GetProperty("Value").GetProperty("value").GetInt32();
            cardType.Should().Be((int)card.Type, $"{card.Resref} must add its own card");
        }
    }

    [Test]
    public void EveryCardHasAnInventoryIcon()
    {
        var itemFolder = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_item");
        if (!Directory.Exists(itemFolder))
            Assert.Ignore("SWLOR_Haks/sw_item is not checked out.");

        foreach (var card in PazaakCards.All)
        {
            File.Exists(Path.Combine(itemFolder, $"iit_card_{card.Model:000}.tga"))
                .Should().BeTrue($"{card.Resref} uses card model {card.Model}");
        }
    }

    [Test]
    public void LootTablesDropEveryCardWithSpecialCardsRarest()
    {
        var tables = new PazaakLootTableDefinition().BuildLootTables();

        foreach (var name in new[] { PazaakLootTableDefinition.PazaakCards, PazaakLootTableDefinition.BossPazaakCards })
        {
            var table = tables[name];
            table.IsRare.Should().BeTrue();
            table.Select(item => item.Resref).Should().BeEquivalentTo(PazaakCards.All.Select(card => card.Resref));
        }

        var weights = tables[PazaakLootTableDefinition.PazaakCards]
            .ToDictionary(item => item.Resref, item => item.Weight);
        weights["pzk_plus3"].Should().BeGreaterThan(weights["pzk_pm3"]);
        weights["pzk_pm3"].Should().BeGreaterThan(weights["pzk_double"]);
        weights["pzk_double"].Should().BeGreaterThan(weights["pzk_tiebreak"]);
    }

    // Rare find bonuses: none, Treasure Hunter I-III.
    [TestCase(0, 1, 5)]
    [TestCase(10, 7, 10)]
    [TestCase(20, 13, 15)]
    [TestCase(30, 20, 20)]
    public void DropChanceMatchesSongDiscsAndTopsOutAtTwentyPercent(int rareBonus, int regularChance, int bossChance)
    {
        var tables = new PazaakLootTableDefinition().BuildLootTables();

        (PazaakLootTableDefinition.PazaakCardChance + tables[PazaakLootTableDefinition.PazaakCards].GetRareChanceBonus(rareBonus))
            .Should().Be(regularChance);
        (PazaakLootTableDefinition.BossPazaakCardChance + tables[PazaakLootTableDefinition.BossPazaakCards].GetRareChanceBonus(rareBonus))
            .Should().Be(bossChance);
    }

    [Test]
    public void CreaturesThatDropSongDiscsAlsoDropPazaakCards()
    {
        var failures = new List<string>();
        var carriers = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot().FullName, "Module", "utc"), "*.utc.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var lootTables = document.RootElement.TryGetProperty("VarTable", out var varTable)
                ? varTable.GetProperty("value").EnumerateArray()
                    .Select(var => (Name: var.GetProperty("Name").GetProperty("value").GetString()!,
                        Value: var.GetProperty("Value").GetProperty("value").ToString()))
                    .Where(var => var.Name.StartsWith("LOOT_TABLE_"))
                    .ToList()
                : new List<(string Name, string Value)>();
            var resref = Path.GetFileName(path).Split('.')[0];
            var songs = lootTables.Where(var => var.Value.StartsWith("MUSIC_DISCS")).ToList();
            var cards = lootTables.Where(var => var.Value.StartsWith("PAZAAK_CARDS")).ToList();

            if (songs.Count == 0)
            {
                if (cards.Count > 0)
                    failures.Add($"{resref} drops Pazaak cards without song discs");
                continue;
            }

            carriers++;
            var isBoss = songs[0].Value.StartsWith($"{MusicLootTableDefinition.BossMusicDiscs},");
            var expected = isBoss
                ? $"{PazaakLootTableDefinition.BossPazaakCards},{PazaakLootTableDefinition.BossPazaakCardChance},1"
                : $"{PazaakLootTableDefinition.PazaakCards},{PazaakLootTableDefinition.PazaakCardChance},1";
            if (cards.Count != 1 || cards[0].Value != expected)
                failures.Add($"{resref} should carry exactly '{expected}'");

            // Loot stops reading at the first missing LOOT_TABLE_N.
            var expectedNames = Enumerable.Range(1, lootTables.Count).Select(index => $"LOOT_TABLE_{index}");
            if (!lootTables.Select(var => var.Name).ToHashSet().SetEquals(expectedNames))
                failures.Add($"{resref} loot tables are not numbered 1 to {lootTables.Count} without gaps");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
        carriers.Should().BeGreaterThan(0);
    }

    [Test]
    public void DefaultDeckIsFreeAndOtherCardsNeedCollectedCopies()
    {
        var none = new Dictionary<PazaakCardType, int>();
        PazaakCards.IsValidSideDeck(PazaakCards.DefaultSideDeck.ToList(), none).Should().BeTrue();

        var twoPlusFives = PazaakCards.DefaultSideDeck.Where(card => card != PazaakCardType.Plus1)
            .Append(PazaakCardType.Plus5).ToList();
        PazaakCards.IsValidSideDeck(twoPlusFives, none).Should().BeFalse("only one +5 is free");
        PazaakCards.IsValidSideDeck(twoPlusFives, new Dictionary<PazaakCardType, int> { [PazaakCardType.Plus5] = 1 })
            .Should().BeTrue();

        var withTiebreaker = PazaakCards.DefaultSideDeck.Skip(1).Append(PazaakCardType.Tiebreaker).ToList();
        PazaakCards.IsValidSideDeck(withTiebreaker, none).Should().BeFalse("special cards are never free");
        PazaakCards.IsValidSideDeck(withTiebreaker, new Dictionary<PazaakCardType, int> { [PazaakCardType.Tiebreaker] = 1 })
            .Should().BeTrue();

        PazaakCards.GetOwnedCopies(PazaakCardType.Plus6, none).Should().Be(0);
        PazaakCards.GetOwnedCopies(PazaakCardType.Minus2, new Dictionary<PazaakCardType, int> { [PazaakCardType.Minus2] = 2 })
            .Should().Be(3);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
