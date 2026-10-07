using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class StoreInventoryTests
{
    private static readonly StoreWeaponExpectation[] PlanetaryGeneralStores =
    {
        new("cz220_merchant", "CZ-220 Merchant", 2),
        new("veles_gen_merch", "Veles Colony General Store", 4),
        new("moncala_gen_merc", "Mon Cala General Store", 6),
        new("korr_general", "Korriban General Store", 6),
        new("hutlar_gen_merch", "Hutlar General Store", 8),
        new("dat_gen_merch", "Dathomir General Store", 9),
        new("tat_gen_merch", "Tatooine General Store", 9),
    };

    private static readonly string[] NormalWeaponBases =
    {
        "knife",
        "greatsword",
        "longsword",
        "katar",
        "staff",
        "twinblade",
        "spear",
        "pistol",
        "rifle",
        "shuriken",
    };

    private static readonly string[] NormalTierPrefixes =
    {
        "b",
        "fld",
        "tit",
        "vet",
        "del",
        "prm",
        "proto",
        "asc",
        "oph",
    };

    private static readonly string[] ElectrobladeTiers =
    {
        "electroblade_1",
        "fld_electroblade",
        "electroblade_2",
        "vet_electroblade",
        "electroblade_3",
        "prm_electroblade",
        "electroblade_4",
        "asc_electroblade",
        "electroblade_5",
    };

    private static readonly string[] TwinElectrobladeTiers =
    {
        "twin_elec_1",
        "fld_twinelec",
        "twin_elec_2",
        "vet_twinelec",
        "twin_elec_3",
        "prm_twinelec",
        "twin_elec_4",
        "asc_twinelec",
        "twin_elec_5",
    };

    [Test]
    public void PlanetaryGeneralStoreBlueprints_CarryCumulativeWeaponTiers()
    {
        var root = FindRepositoryRoot();

        foreach (var store in PlanetaryGeneralStores)
        {
            var items = ReadStoreBlueprintItems(root, store.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedWeapons = BuildExpectedWeapons(store.WeaponTierCount).ToArray();

            items.Should().Contain(expectedWeapons, $"{store.Name} should sell all weapon tiers up to its planetary tier");
        }
    }

    [Test]
    public void PlacedPlanetaryGeneralStores_MatchBlueprintInventory()
    {
        var root = FindRepositoryRoot();
        var expectedItemsByStore = PlanetaryGeneralStores.ToDictionary(
            store => store.Resref,
            store => ReadStoreBlueprintItems(root, store.Resref).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        var foundStores = PlanetaryGeneralStores.ToDictionary(
            store => store.Resref,
            _ => 0,
            StringComparer.OrdinalIgnoreCase);
        var findings = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!TryGetWrappedArray(document.RootElement, "StoreList", out var stores))
                continue;

            var storeIndex = 0;
            foreach (var store in stores.EnumerateArray())
            {
                var storeResref = GetWrappedString(store, "ResRef");
                if (!expectedItemsByStore.TryGetValue(storeResref, out var expectedItems))
                {
                    storeIndex++;
                    continue;
                }

                foundStores[storeResref]++;
                var placedItems = ReadPlacedStoreItems(store).ToArray();

                // Compare inventory as multisets, not sequences: the toolset reshuffles store panes
                // on every save, so pane/item order is not stable, but the set of items sold must match.
                var missing = expectedItems.Where(item => !placedItems.Contains(item, StringComparer.OrdinalIgnoreCase)).ToArray();
                var extra = placedItems.Where(item => !expectedItems.Contains(item, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (missing.Length > 0 || extra.Length > 0 || placedItems.Length != expectedItems.Length)
                {
                    findings.Add(
                        $"{Path.GetRelativePath(root.FullName, file)} store[{storeIndex}] {storeResref} does not match its UTM blueprint. " +
                        $"Missing: {string.Join(", ", missing)}. Extra: {string.Join(", ", extra)}.");
                }

                storeIndex++;
            }
        }

        foundStores.Should().OnlyContain(entry => entry.Value > 0, "each planetary general store should have at least one placed instance");
        findings.Should().BeEmpty(string.Join("\n", findings));
    }

    [Test]
    public void PlacedStoreItems_UseBlueprintEquipmentRequirements()
    {
        var root = FindRepositoryRoot();
        var requirementsByResref = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var findings = new List<string>();
        var checkedItems = 0;

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!TryGetWrappedArray(document.RootElement, "StoreList", out var stores))
                continue;

            foreach (var store in stores.EnumerateArray())
            foreach (var item in ReadStoreItemData(store))
            {
                var resref = GetWrappedString(item, "TemplateResRef");
                var blueprintFile = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
                if (string.IsNullOrWhiteSpace(resref) || !File.Exists(blueprintFile))
                    continue;

                if (!requirementsByResref.TryGetValue(resref, out var expected))
                {
                    using var blueprint = JsonDocument.Parse(File.ReadAllText(blueprintFile));
                    expected = ReadEquipmentRequirements(blueprint.RootElement);
                    requirementsByResref.Add(resref, expected);
                }

                checkedItems++;
                if (!ReadEquipmentRequirements(item).SequenceEqual(expected))
                    findings.Add($"{Path.GetFileName(file)} / {GetWrappedString(store, "ResRef")} / {resref}");
            }
        }

        checkedItems.Should().BeGreaterThan(5000, "the audit must cover the placed store corpus");
        findings.Should().BeEmpty("store copies must not retain retired perk IDs after their blueprints change: " + string.Join(", ", findings));
    }

    [Test]
    public void GuildStoreBlueprintItems_UseValidSkillRequirements()
    {
        var root = FindRepositoryRoot();
        var checkedRequirements = 0;
        foreach (var storeFile in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utm"), "gp_*.utm.json"))
        {
            using var store = JsonDocument.Parse(File.ReadAllText(storeFile));
            foreach (var resref in ReadStoreItems(store.RootElement, item => GetWrappedString(item, "InventoryRes")))
            {
                var blueprintFile = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
                if (!File.Exists(blueprintFile))
                    continue;

                using var blueprint = JsonDocument.Parse(File.ReadAllText(blueprintFile));
                if (!TryGetWrappedArray(blueprint.RootElement, "PropertiesList", out var properties))
                    continue;

                foreach (var property in properties.EnumerateArray())
                {
                    var type = (ItemPropertyType)GetWrappedInt(property, "PropertyName");
                    type.Should().NotBe(ItemPropertyType.UseLimitationPerk, $"guild item {resref} must not reference retired weapon/armor perks");
                    if (type != ItemPropertyType.RequiresSkill)
                        continue;

                    checkedRequirements++;
                    var skill = (SkillType)GetWrappedInt(property, "Subtype");
                    Enum.IsDefined(skill).Should().BeTrue($"guild item {resref} must require an existing skill");
                    skill.Should().NotBe(SkillType.Invalid, $"guild item {resref} must have a usable requirement");
                    GetWrappedInt(property, "CostValue").Should().BeInRange(0, 50, $"guild item {resref} must be usable within the skill cap");
                }
            }
        }

        checkedRequirements.Should().BeGreaterThan(100);
    }

    [TestCase(1, 0)]
    [TestCase(2, 10)]
    [TestCase(3, 20)]
    [TestCase(4, 30)]
    [TestCase(5, 40)]
    public void HunterRifles_RequireRifleSkillInsteadOfRetiredPerks(int tier, int requiredRank)
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"h_rifle_{tier}.uti.json")));
        var properties = blueprint.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().ToArray();
        properties.Should().NotContain(property => GetWrappedInt(property, "PropertyName") == (int)ItemPropertyType.UseLimitationPerk);
        var requirement = properties.Single(property => GetWrappedInt(property, "PropertyName") == (int)ItemPropertyType.RequiresSkill);
        GetWrappedInt(requirement, "Subtype").Should().Be((int)SkillType.Rifle);
        GetWrappedInt(requirement, "CostValue").Should().Be(requiredRank);
    }

    private static IEnumerable<JsonElement> ReadStoreItemData(JsonElement store)
    {
        if (!TryGetWrappedArray(store, "StoreList", out var panes))
            yield break;
        foreach (var pane in panes.EnumerateArray())
        {
            if (!TryGetWrappedArray(pane, "ItemList", out var items))
                continue;
            foreach (var item in items.EnumerateArray())
                yield return item;
        }
    }

    private static string[] ReadEquipmentRequirements(JsonElement item)
    {
        if (!TryGetWrappedArray(item, "PropertiesList", out var properties))
            return Array.Empty<string>();

        return properties.EnumerateArray()
            .Where(property => (ItemPropertyType)GetWrappedInt(property, "PropertyName") is
                ItemPropertyType.UseLimitationPerk or ItemPropertyType.RequiresSkill or ItemPropertyType.RequiresStat)
            .Select(property => string.Join("/", new[] { "PropertyName", "Subtype", "CostTable", "CostValue", "Param1", "Param1Value" }
                .Select(field => GetWrappedInt(property, field))))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetWrappedInt(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetProperty("value").GetInt32();

    private static IEnumerable<string> BuildExpectedWeapons(int tierCount)
    {
        foreach (var weaponBase in NormalWeaponBases)
        {
            foreach (var prefix in NormalTierPrefixes.Take(tierCount))
            {
                var normalizedPrefix = prefix == "proto" && weaponBase == "longsword"
                    ? "pro"
                    : prefix;

                yield return $"{normalizedPrefix}_{weaponBase}";
            }
        }

        foreach (var resref in ElectrobladeTiers.Take(tierCount))
        {
            yield return resref;
        }

        foreach (var resref in TwinElectrobladeTiers.Take(tierCount))
        {
            yield return resref;
        }
    }

    private static IReadOnlyList<string> ReadStoreBlueprintItems(DirectoryInfo root, string storeResref)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utm",
            $"{storeResref}.utm.json")));

        return ReadStoreItems(document.RootElement, item => GetWrappedString(item, "InventoryRes")).ToArray();
    }

    private static IEnumerable<string> ReadPlacedStoreItems(JsonElement store)
    {
        return ReadStoreItems(store, item =>
        {
            var resref = GetWrappedString(item, "TemplateResRef");
            return string.IsNullOrWhiteSpace(resref)
                ? GetWrappedString(item, "Tag")
                : resref;
        });
    }

    private static IEnumerable<string> ReadStoreItems(JsonElement store, Func<JsonElement, string> readResref)
    {
        if (!TryGetWrappedArray(store, "StoreList", out var panes))
            yield break;

        foreach (var pane in panes.EnumerateArray())
        {
            if (!TryGetWrappedArray(pane, "ItemList", out var items))
                continue;

            foreach (var item in items.EnumerateArray())
            {
                var resref = readResref(item);
                if (!string.IsNullOrWhiteSpace(resref))
                    yield return resref;
            }
        }
    }

    private static bool TryGetWrappedArray(JsonElement element, string propertyName, out JsonElement array)
    {
        if (element.TryGetProperty(propertyName, out var wrapper) &&
            wrapper.TryGetProperty("value", out array) &&
            array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static string GetWrappedString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var wrapper) &&
               wrapper.TryGetProperty("value", out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }

    private sealed record StoreWeaponExpectation(string Resref, string Name, int WeaponTierCount);
}
