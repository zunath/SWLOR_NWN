using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Civic Exchange (Player City Revamp, "Civic Exchange"): who may use it, what each Cantina level stocks, the 3 crates a week per
/// CD key, how a crate is rolled, and the long buff that replaces food and is replaced by it.
/// </summary>
public class CivicExchangeTests
{
    private const string CityId = "city-1";
    private const string OtherCityId = "city-2";
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private static CivicExchangeAccess Access(string home = CityId, int owedTaxes = 0, bool isDeleted = false, int? citizenDaysAgo = 30) =>
        CivicExchangeRules.GetAccess(home, CityId, isDeleted, owedTaxes, citizenDaysAgo.HasValue ? Now.AddDays(-citizenDaysAgo.Value) : null, Now);

    // Who may use it

    [Test]
    public void ACitizenInGoodStandingMayUseIt()
    {
        Access().Should().Be(CivicExchangeAccess.Allowed);
        CivicExchangeRules.DescribeAccess(CivicExchangeAccess.Allowed, 0).Should().BeEmpty();
    }

    [Test]
    public void ACitizenOwingTaxesIsRefused_WithTheAmountInTheMessage()
    {
        Access(owedTaxes: 1).Should().Be(CivicExchangeAccess.OwesTaxes);
        CivicExchangeRules.DescribeAccess(CivicExchangeAccess.OwesTaxes, 420).Should().Contain("420 credits");
    }

    [Test]
    public void ACitizenNeedsSevenDaysOfCitizenship()
    {
        Access(citizenDaysAgo: 6).Should().Be(CivicExchangeAccess.TooNew);
        Access(citizenDaysAgo: 7).Should().Be(CivicExchangeAccess.Allowed, "exactly 7 days counts");
        Access(citizenDaysAgo: null).Should().Be(CivicExchangeAccess.TooNew, "a citizen with no citizen-since time never qualifies");
        CivicExchangeRules.DescribeAccess(CivicExchangeAccess.TooNew, 0).Should().Contain("7 days");
    }

    [Test]
    public void VisitorsAlliesAndCitizensOfOtherCitiesAreRefused()
    {
        Access(home: string.Empty).Should().Be(CivicExchangeAccess.NotACitizen);
        Access(home: OtherCityId).Should().Be(CivicExchangeAccess.NotACitizen, "an allied citizen does not get the Exchange");
        Access(isDeleted: true).Should().Be(CivicExchangeAccess.NotACitizen);
        CivicExchangeRules.DescribeAccess(CivicExchangeAccess.NotACitizen, 0).Should().Contain("citizens");
    }

    [Test]
    public void TheExchangeUsesTheEdictCitizenRule()
    {
        // Same answer as the Edicts' helper for every combination, so the two never drift apart.
        foreach (var home in new[] { CityId, OtherCityId, string.Empty })
        foreach (var owed in new[] { 0, 5 })
        foreach (var days in new int?[] { null, 3, 7, 40 })
        {
            var since = days.HasValue ? Now.AddDays(-days.Value) : (DateTime?)null;
            var expected = SWLOR.Game.Server.Service.EdictService.EdictRules.IsEligibleCitizen(home, CityId, false, owed, since, Now);
            (CivicExchangeRules.GetAccess(home, CityId, false, owed, since, Now) == CivicExchangeAccess.Allowed).Should().Be(expected);
        }
    }

    // What each Cantina level stocks

    [Test]
    public void TheCatalogSellsFiveCratesAndOneLongBuff_AndNothingElse()
    {
        CivicExchangeCatalog.All.Should().HaveCount(6);
        CivicExchangeCatalog.All.Count(x => x.Kind == CivicExchangeItemKind.MaterialsCrate).Should().Be(5);
        CivicExchangeCatalog.All.Count(x => x.Kind == CivicExchangeItemKind.Buff).Should().Be(1);
        CivicExchangeCatalog.All.Select(x => x.Key).Should().OnlyHaveUniqueItems();
        CivicExchangeCatalog.All.Should().OnlyContain(x => x.Kind == CivicExchangeItemKind.MaterialsCrate || x.Kind == CivicExchangeItemKind.Buff,
            "no research time and no faster training");
        CivicExchangeCatalog.All.Select(x => x.Name + " " + x.Description).Should().NotContain(x => x.Contains("research", StringComparison.OrdinalIgnoreCase));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 4)]
    [TestCase(4, 5)]
    [TestCase(5, 6)]
    public void HigherCantinaLevelsUnlockMoreStock(int cantinaLevel, int itemCount)
    {
        CivicExchangeCatalog.GetStock(cantinaLevel).Should().HaveCount(itemCount);
    }

    [Test]
    public void ATierCrateOpensAtTheCantinaLevelOfThatTier_AndTheBuffAtLevelThree()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var crate = CivicExchangeCatalog.Get(CivicExchangeCatalog.GetCrateKey(tier));
            crate.MinCantinaLevel.Should().Be(tier);
            crate.Tier.Should().Be(tier);
            crate.Kind.Should().Be(CivicExchangeItemKind.MaterialsCrate);
        }

        CivicExchangeCatalog.Get("buff_civic_provisions").MinCantinaLevel.Should().Be(3);
        CivicExchangeCatalog.GetStock(1).Select(x => x.Key).Should().Equal("crate_t1");
        CivicExchangeCatalog.GetStock(3).Select(x => x.Key).Should().Contain("buff_civic_provisions").And.NotContain("crate_t4");
        CivicExchangeCatalog.GetStock(5).Should().Contain(CivicExchangeCatalog.All, "a level 5 Cantina stocks everything");
    }

    [Test]
    public void ACantinaBelowAnItemsLevelRefusesIt()
    {
        var tier3 = CivicExchangeCatalog.Get("crate_t3");
        CivicExchangeRules.GetRefusal(tier3, 2, 999999, 3).Should().Be(CivicExchangeRefusal.CantinaTooLow);
        CivicExchangeRules.GetRefusal(tier3, 3, 999999, 3).Should().Be(CivicExchangeRefusal.None);

        var buff = CivicExchangeCatalog.Get("buff_civic_provisions");
        CivicExchangeRules.GetRefusal(buff, 2, 999999, 3).Should().Be(CivicExchangeRefusal.CantinaTooLow);
        CivicExchangeRules.GetRefusal(buff, 3, 999999, 3).Should().Be(CivicExchangeRefusal.None);
        CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.CantinaTooLow, tier3).Should().Contain("level 3");
    }

    [Test]
    public void PricesRiseWithTheTier_AndNothingIsFree()
    {
        var prices = Enumerable.Range(1, 5).Select(tier => CivicExchangeCatalog.Get(CivicExchangeCatalog.GetCrateKey(tier)).PriceXp).ToList();
        prices.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        CivicExchangeCatalog.All.Should().OnlyContain(x => x.PriceXp > 0);
        prices.Max().Should().BeLessThan(10000, "a crate costs less than the Training Store's 10,000 XP perk refund tome");
    }

    [Test]
    public void ABuyerNeedsTheAvailableXp_AndTheXpIsCheckedAfterTheLimits()
    {
        var crate = CivicExchangeCatalog.Get("crate_t1");
        CivicExchangeRules.GetRefusal(crate, 1, crate.PriceXp - 1, 3).Should().Be(CivicExchangeRefusal.NotEnoughXp);
        CivicExchangeRules.GetRefusal(crate, 1, crate.PriceXp, 3).Should().Be(CivicExchangeRefusal.None);
        CivicExchangeRules.GetRefusal(crate, 1, 0, 0).Should().Be(CivicExchangeRefusal.WeeklyLimit, "the weekly limit is the first thing to fix");
        CivicExchangeRules.GetRefusal(null, 5, 999999, 3).Should().Be(CivicExchangeRefusal.UnknownItem);
        CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.NotEnoughXp, crate).Should().Contain("1,000");
        CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.None, crate).Should().BeEmpty();
    }

    // Three crates a week per CD key

    [Test]
    public void ACdKeyBuysThreeCratesAWeek()
    {
        var record = new CivicExchangeAllowance("cd-key");
        CivicExchangeRules.GetCratesRemaining(null, Now).Should().Be(3, "a CD key with no record has the full limit");
        CivicExchangeRules.GetCratesRemaining(record, Now).Should().Be(3);

        for (var bought = 1; bought <= 3; bought++)
        {
            CivicExchangeRules.RecordCrate(record, Now);
            CivicExchangeRules.GetCratesRemaining(record, Now).Should().Be(3 - bought);
        }

        var crate = CivicExchangeCatalog.Get("crate_t1");
        CivicExchangeRules.GetRefusal(crate, 5, 999999, CivicExchangeRules.GetCratesRemaining(record, Now))
            .Should().Be(CivicExchangeRefusal.WeeklyLimit);
        CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.WeeklyLimit, crate).Should().Contain("3 crates").And.Contain("Monday");
    }

    [Test]
    public void TheLimitCountsEveryTierTogether_AndDoesNotLimitTheBuff()
    {
        var record = new CivicExchangeAllowance("cd-key");
        CivicExchangeRules.RecordCrate(record, Now);
        CivicExchangeRules.RecordCrate(record, Now);
        CivicExchangeRules.RecordCrate(record, Now);
        var remaining = CivicExchangeRules.GetCratesRemaining(record, Now);

        foreach (var tier in Enumerable.Range(1, 5))
        {
            CivicExchangeRules.GetRefusal(CivicExchangeCatalog.Get(CivicExchangeCatalog.GetCrateKey(tier)), 5, 999999, remaining)
                .Should().Be(CivicExchangeRefusal.WeeklyLimit);
        }

        CivicExchangeRules.GetRefusal(CivicExchangeCatalog.Get("buff_civic_provisions"), 5, 999999, remaining)
            .Should().Be(CivicExchangeRefusal.None);
    }

    [Test]
    public void TheCountStartsOverAtTheWeeklyBoundary_MondayMidnightUtc()
    {
        var friday = new DateTime(2026, 11, 6, 20, 0, 0, DateTimeKind.Utc);
        var record = new CivicExchangeAllowance("cd-key");
        for (var i = 0; i < 3; i++)
            CivicExchangeRules.RecordCrate(record, friday);

        CivicExchangeRules.GetCratesRemaining(record, friday).Should().Be(0);
        CivicExchangeRules.GetCratesRemaining(record, new DateTime(2026, 11, 8, 23, 59, 59, DateTimeKind.Utc)).Should().Be(0, "still the same week on Sunday night");
        CivicExchangeRules.GetCratesRemaining(record, new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc)).Should().Be(3, "Monday 00:00 UTC is a new week");

        CivicExchangeRules.RecordCrate(record, new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc));
        CivicExchangeRules.GetCratesRemaining(record, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc)).Should().Be(2, "the new week counts from one");
        record.WeekStartUtc.Should().Be(CityWeek.GetWeekStart(new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void TheLimitBelongsToTheCdKey_NotTheCharacterOrTheCity()
    {
        // Two characters on one key share a record; another key has its own; a character with no key falls back to its own id.
        CivicExchangeRules.GetAllowanceKey("cd-key", "player-a").Should().Be(CivicExchangeRules.GetAllowanceKey("cd-key", "player-b"));
        CivicExchangeRules.GetAllowanceKey("cd-key", "player-a").Should().NotBe(CivicExchangeRules.GetAllowanceKey("other-key", "player-a"));
        CivicExchangeRules.GetAllowanceKey("", "player-a").Should().Be("player:player-a");
        CivicExchangeRules.GetAllowanceKey(" ", "player-b").Should().Be("player:player-b");
        CivicExchangeRules.GetAllowanceKey("  cd-key ", "p").Should().Be("cd-key", "stray spaces do not make a second key");

        new CivicExchangeAllowance("cd-key").Id.Should().Be("cd-key");
        typeof(CivicExchangeAllowance).GetProperties().Select(x => x.Name).Should().NotContain(new[] { "CityId", "PlayerId" });
    }

    // What a crate holds

    [Test]
    public void ACrateIsWorthUnderTwentyMinutesOfHarvesting()
    {
        CivicExchangeCatalog.CrateUnits.Should().Be(30);
        CivicExchangeRules.GetCrateHarvestMinutes().Should().BeLessThan(CivicExchangeCatalog.MaxCrateHarvestMinutes);
        CivicExchangeRules.GetCrateHarvestMinutes().Should().Be(10, "30 units at the baseline's 20 seconds a unit");
        CivicExchangeCatalog.MaxCrateHarvestMinutes.Should().Be(20);
        CivicExchangeCatalog.WeeklyCrateLimit.Should().Be(3);
    }

    [Test]
    public void ACrateRollsThirtyUnitsFromItsTiersHarvestTables()
    {
        var rolled = new List<string>();
        var picked = new List<int>();
        var contents = CivicExchangeRules.PlanCrate(
            3,
            count =>
            {
                var index = picked.Count % count;
                picked.Add(index);
                return index;
            },
            table =>
            {
                rolled.Add(table);
                return table == "HERBS_3" ? "herb_c" : "raw_plagionite";
            });

        rolled.Should().HaveCount(30);
        rolled.Distinct().Should().BeEquivalentTo(CivicExchangeCatalog.GetCrateTables(3));
        contents.Values.Sum().Should().Be(30);
        contents.Keys.Should().BeEquivalentTo("herb_c", "raw_plagionite");
        picked.Should().OnlyContain(x => x >= 0 && x < 4);
    }

    [Test]
    public void ATierWithNoCrateRollsNothing()
    {
        CivicExchangeRules.PlanCrate(0, _ => 0, _ => "x").Should().BeEmpty();
        CivicExchangeRules.PlanCrate(6, _ => 0, _ => "x").Should().BeEmpty();
    }

    [Test]
    public void EveryCrateTableIsARealHarvestTable_OfItsTier_WithRealItems()
    {
        var tables = typeof(ILootTableDefinition).Assembly.GetTypes()
            .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .SelectMany(type => ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables())
            .GroupBy(pair => pair.Key)
            .ToDictionary(group => group.Key, group => group.First().Value);
        var seen = new HashSet<string>();

        for (var tier = 1; tier <= 5; tier++)
        {
            var names = CivicExchangeCatalog.GetCrateTables(tier);
            names.Should().HaveCount(4, $"tier {tier}: ore, wood, fiber and herbs");
            foreach (var name in names)
            {
                tables.Should().ContainKey(name, $"tier {tier} crate table {name}");
                tables[name].Should().NotBeEmpty();
                seen.Add(name).Should().BeTrue($"{name} belongs to one tier only");

                foreach (var item in tables[name])
                {
                    File.Exists(Path.Combine(RepositoryRoot, "Module", "uti", $"{item.Resref}.uti.json"))
                        .Should().BeTrue($"{name} rolls {item.Resref}, which must be a real item");
                }
            }
        }
    }

    // The long buff

    [Test]
    public void TheBuffReplacesFood_AndFoodReplacesTheBuff()
    {
        var buff = new CivicProvisionsStatusEffect();
        var food = new FoodStatusEffect();

        buff.LessPowerfulEffectTypes.Should().Contain(typeof(FoodStatusEffect), "buying the buff removes the food buff");
        food.LessPowerfulEffectTypes.Should().Contain(typeof(CivicProvisionsStatusEffect), "eating removes the buff");

        // Neither may call the other more powerful, which would refuse the newer buff instead of replacing the older one.
        buff.MorePowerfulEffectTypes.Should().NotContain(typeof(FoodStatusEffect));
        food.MorePowerfulEffectTypes.Should().NotContain(typeof(CivicProvisionsStatusEffect));

        // A clone and a registry-made food keep the rule, since the engine builds foods both ways.
        food.Clone().LessPowerfulEffectTypes.Should().Contain(typeof(CivicProvisionsStatusEffect));
        new FoodStatusEffect(new SWLOR.Game.Server.Service.StatusEffectService.FoodEffectData { HP = 50 })
            .LessPowerfulEffectTypes.Should().Contain(typeof(CivicProvisionsStatusEffect));
    }

    [Test]
    public void TheBuffStaysOutOfTheFoodHitPointMachinery()
    {
        // Food keeps its HP in Player.TemporaryFoodHP and strips it when no FoodStatusEffect is found; the buff holds none.
        var buff = new CivicProvisionsStatusEffect();
        buff.StatGroup.Stats.Where(x => x.Value != 0).Select(x => x.Key).Should().BeEquivalentTo(new[]
        {
            SWLOR.Game.Server.Service.StatService.StatType.MaxFP,
            SWLOR.Game.Server.Service.StatService.StatType.MaxStamina,
            SWLOR.Game.Server.Service.StatService.StatType.PhysicalDefense,
            SWLOR.Game.Server.Service.StatService.StatType.ForceDefense
        }, "the buff changes only those four stats, and none of them is HP");
        buff.GetType().BaseType.Should().Be(typeof(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectBase));
        buff.Name.Should().Be("Civic Provisions");
        buff.StackingType.Should().Be(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectStackType.Disabled, "buying it again restarts it");
    }

    [Test]
    public void TheBuffIsLongAndItsStatsComeFromTheCatalog()
    {
        CivicExchangeCatalog.BuffDurationSeconds.Should().Be(7200);
        CivicExchangeCatalog.BuffDurationSeconds.Should().BeGreaterThan(2700, "longer than the best food's 45 minutes");

        var stats = new CivicProvisionsStatusEffect().StatGroup.Stats;
        stats[SWLOR.Game.Server.Service.StatService.StatType.MaxFP].Should().Be(CivicExchangeCatalog.BuffMaxFP);
        stats[SWLOR.Game.Server.Service.StatService.StatType.MaxStamina].Should().Be(CivicExchangeCatalog.BuffMaxStamina);
        stats[SWLOR.Game.Server.Service.StatService.StatType.PhysicalDefense].Should().Be(CivicExchangeCatalog.BuffPhysicalDefense);
        stats[SWLOR.Game.Server.Service.StatService.StatType.ForceDefense].Should().Be(CivicExchangeCatalog.BuffForceDefense);
    }

    [Test]
    public void TheBuffIsNoStrongerThanTheBestFoodBuff()
    {
        // Every food blueprint's food bonuses, summed by kind, from the module itself.
        var foods = ReadFoodBonuses();
        foods.Should().NotBeEmpty();

        var buff = new Dictionary<FoodItemPropertySubType, int>
        {
            [FoodItemPropertySubType.FP] = CivicExchangeCatalog.BuffMaxFP,
            [FoodItemPropertySubType.STM] = CivicExchangeCatalog.BuffMaxStamina,
            [FoodItemPropertySubType.DefensePhysical] = CivicExchangeCatalog.BuffPhysicalDefense,
            [FoodItemPropertySubType.DefenseForce] = CivicExchangeCatalog.BuffForceDefense
        };

        foreach (var (kind, value) in buff)
        {
            foods.Max(food => food.GetValueOrDefault(kind)).Should().BeGreaterThanOrEqualTo(value, $"no food gives less {kind} than the buff");
        }

        var buffTotal = buff.Values.Sum();
        var bestFoodTotal = foods.Max(food => food
            .Where(pair => pair.Key != FoodItemPropertySubType.Duration && pair.Key != FoodItemPropertySubType.XPBonus)
            .Sum(pair => pair.Value));
        buffTotal.Should().BeLessThanOrEqualTo(bestFoodTotal, "the whole buff is smaller than the strongest food");
    }

    [Test]
    public void FoodCanStillBeEatenUnderTheBuff()
    {
        // Food refuses to be eaten only while a FoodStatusEffect is on the character; the buff is a different effect type.
        var consumables = ReadServer("Feature", "ItemDefinition", "ConsumableItemDefinition.cs");
        consumables.Should().Contain("StatusEffect.HasStatusEffect(user, typeof(FoodStatusEffect))");
        consumables.Should().NotContain("CivicProvisionsStatusEffect", "the food validation never blocks on the buff");
        typeof(CivicProvisionsStatusEffect).IsSubclassOf(typeof(FoodStatusEffect)).Should().BeFalse();
    }

    // Text and wiring

    [Test]
    public void AllPlayerFacingTextIsPlainAscii()
    {
        var text = CivicExchangeCatalog.All.SelectMany(x => new[] { x.Name, x.Description })
            .Concat(new[]
            {
                CivicExchangeCatalog.BuffSummary,
                CivicExchangeRules.OpenResponse,
                CivicExchangeRules.NotACitizenMessage,
                CivicExchangeRules.TooNewMessage,
                CivicExchangeRules.DescribeAccess(CivicExchangeAccess.OwesTaxes, 10),
                CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.WeeklyLimit, null),
                CivicExchangeRules.DescribeRefusal(CivicExchangeRefusal.UnknownItem, null)
            });

        text.Should().OnlyContain(x => x.All(c => c >= ' ' && c <= '~'));
    }

    [Test]
    public void TheExchangeIsWiredIntoTheBartenderAndTheTrainingStoreWindow()
    {
        var dialog = ReadServer("Feature", "DialogDefinition", "CityStaffDialog.cs");
        dialog.Should().Contain("if (role == CityStaffRole.Bartender)").And.Contain("AddCivicExchange(page, cityId);");
        dialog.Should().Contain("dbPlayer.CitizenPropertyId != cityId", "only the Cantina's own citizens see the response");
        dialog.Should().Contain("CivicExchange.GetAccessFailure(");
        dialog.Should().Contain("Gui.TogglePlayerWindow(Player, GuiWindowType.TrainingStore, new CivicExchangePayload(cityId), Owner);");

        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "TrainingStoreViewModel.cs");
        viewModel.Should().Contain("initialPayload is CivicExchangePayload exchange ? exchange.CityId : string.Empty",
            "the mode is reset every time the window opens");
        viewModel.Should().Contain("CivicExchange.TryBuy(Player, cityId, item.Key)");
        viewModel.Should().Contain("new(\"iIT_BOOK_244\", \"refund_tome\", 10000)", "the Training Store keeps its stock");

        ReadServer("Feature", "GuiDefinition", "TrainingStoreDefinition.cs").Should().Contain(".BindTitle(model => model.WindowTitle)");

        var service = ReadServer("Service", "CivicExchange.cs");
        service.Should().Contain("CivicExchangeRules.GetAccess(");
        service.Should().Contain("TransferJournal.Withdraw(player, entry, out var overflowByItem)");
        service.Should().Contain("StatusEffect.ApplyStatusEffect<CivicProvisionsStatusEffect>(player, player, CivicExchangeCatalog.BuffDurationSeconds)");
    }

    [Test]
    public void ACratePurchasePaysAndGivesTheMaterialsInOneJournaledWithdrawal()
    {
        var service = ReadServer("Service", "CivicExchange.cs");
        var crate = Method(service, "private static string BuyCrate(");
        crate.Should().NotContain("dbPlayer.UnallocatedXP", "the journal handler takes the XP");
        crate.Should().NotContain("FarmItems.Give(", "the journal gives the materials");

        // The handler takes the XP with its receipt, then the week's allowance; the withdrawal gives the materials after both.
        var apply = Method(service, "private static void ApplyCrate(TransferEntry entry)");
        var order = new[]
        {
            "dbPlayer.UnallocatedXP -= ",
            "DB.Set(dbPlayer);",
            "DB.Set(allowance);"
        };
        var last = -1;
        foreach (var line in order)
        {
            var index = apply.IndexOf(line, StringComparison.Ordinal);
            index.Should().BeGreaterThan(last, $"'{line}' comes after the line before it");
            last = index;
        }
    }

    private static List<Dictionary<FoodItemPropertySubType, int>> ReadFoodBonuses()
    {
        var foods = new List<Dictionary<FoodItemPropertySubType, int>>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!document.RootElement.TryGetProperty("PropertiesList", out var list))
                continue;

            var bonuses = list.GetProperty("value").EnumerateArray()
                .Where(property => Value(property, "PropertyName") == 106)
                .GroupBy(property => (FoodItemPropertySubType)Value(property, "Subtype"))
                .ToDictionary(group => group.Key, group => group.Sum(property => Value(property, "CostValue")));
            if (bonuses.Count > 0)
                foods.Add(bonuses);
        }

        return foods;
    }

    private static int Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"'{signature}' exists");
        var next = source.IndexOf("\n        private static ", start + signature.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
