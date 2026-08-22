using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class ShieldDeflectionTests
{
    [Test]
    public void ShieldDeflectionChance_UsesShieldItemPropertyAndPerkBonusesOnly()
    {
        var root = FindRepositoryRoot();
        var statSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Stat.cs"));

        // Shield Deflection is perk/item-driven only. There is no inherent baseline: the Bulwark package
        // caps at +35 total per the combat-upgrade deflection budget, so adding a flat +10 for every shield
        // pushed Bulwark rank 3 to 45%. Deflection now comes solely from the shield item property and the
        // ShieldDeflection stat (Bulwark), mirroring how weapon deflection has no inherent base.
        statSource.Should().NotContain("InherentShieldDeflectionChance");
        statSource.Should().Contain("var chance = GetShieldDeflectionItemPropertyBonusNative(shield) +");
        statSource.Should().Contain("var chance = GetShieldDeflectionItemPropertyBonus(shield) +");
        statSource.Should().Contain("GetStatAdjustment(creature.m_idSelf, StatType.ShieldDeflection)");
        statSource.Should().Contain("GetStatAdjustment(creature, StatType.ShieldDeflection)");
        statSource.Should().Contain("ItemPropertyType.ShieldDeflection");
        statSource.Should().Contain("return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);");
    }

    [Test]
    public void ShieldDeflectionItemProperty_IsDeclaredForShields()
    {
        var root = FindRepositoryRoot();
        var itemPropertyTypeSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.NWN.API",
            "NWScript",
            "Enum",
            "Item",
            "ItemPropertyType.cs"));
        itemPropertyTypeSource.Should().Contain("ShieldDeflection = 135");

        var itemPropDefRows = Read2da(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "itempropdef.2da"));
        itemPropDefRows[135]["Name"].Should().Be("16859987");
        itemPropDefRows[135]["Label"].Should().Be("ShieldDeflection");
        itemPropDefRows[135]["CostTableResRef"].Should().Be("45");
        itemPropDefRows[135]["GameStrRef"].Should().Be("16859987");
        itemPropDefRows[135]["Description"].Should().Be("16859987");

        var itemPropRows = Read2da(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "itemprops.2da"));
        itemPropRows[135]["6_Arm_Shld"].Should().Be("1");
        itemPropRows[135]["0_Melee"].Should().Be("****");
        itemPropRows[135]["1_Ranged"].Should().Be("****");
        itemPropRows[135]["16_Misc"].Should().Be("****");
        itemPropRows[135]["StringRef"].Should().Be("16859987");
        itemPropRows[135]["Label"].Should().Be("ShieldDeflection");

        ReadTlkText(root, 82771).Should().Be("Shield Deflection");
    }

    [Test]
    public void ShieldDeflectionEnhancementSubType_BuildsShieldDeflectionItemProperty()
    {
        var root = FindRepositoryRoot();
        var enhancementSubTypeSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "CraftService",
            "EnhancementSubType.cs"));
        enhancementSubTypeSource.Should().Contain("[EnhancementSubType(\"Shield Deflection\")]");
        enhancementSubTypeSource.Should().Contain("ShieldDeflection = 127");

        var craftSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Craft.cs"));
        craftSource.Should().Contain("case EnhancementSubType.ShieldDeflection");
        craftSource.Should().Contain("ItemPropertyCustom(ItemPropertyType.ShieldDeflection, -1, amount)");

        var enhanceArmorRows = Read2da(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "iprp_enhancearm.2da"));
        enhanceArmorRows[127]["Name"].Should().Be("16859987");
        enhanceArmorRows[127]["Label"].Should().Be("ShieldDeflection");
        enhanceArmorRows[127]["Cost"].Should().Be("0");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(string file)
    {
        var lines = File.ReadAllLines(file)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static string ReadTlkText(DirectoryInfo root, int id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_tlk",
            "sw_tlk.tlk.json")));

        return document
            .RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .First(element => element.GetProperty("id").GetInt32() == id)
            .GetProperty("text")
            .GetString()!;
    }
}
