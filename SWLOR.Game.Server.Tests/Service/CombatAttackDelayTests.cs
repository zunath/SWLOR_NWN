using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatAttackDelayTests
{
    [Test]
    public void CalculateAttackDelayMilliseconds_UsesSingleWeaponDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 0, 0, 0);

        delay.Should().Be(3500);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(1750);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_DualWieldCountsDefaultDelayOnce()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 0);

        delay.Should().Be(5250);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(3500);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandReductionBeforeCombiningDualWieldDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 30);

        delay.Should().Be(4200);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(2450);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_SubtractsDefaultDelayFromHigherAttackerDelay()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2500;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(2500);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsPostBaselineDelayToDefaultMinimum()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 1250;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_FastestWeaponDelayCanBenefitFromHaste()
    {
        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 0, 0);
        var hastenOneDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 15, 0);
        var hastenTwoDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 25, 0);

        Combat.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeLessThan(Combat.CalculateEffectiveAttackDelay(hastenOneDelay));
    }

    [Test]
    public void NaturalWeaponDelay_UsesFastestWeaponDelayAndBenefitsFromHaste()
    {
        var naturalWeaponTypes = new[]
        {
            BaseItem.CreatureSlashWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreatureSlashPierceWeapon
        };

        foreach (var naturalWeaponType in naturalWeaponTypes)
        {
            WeaponDelay.GetWeaponDelay(naturalWeaponType).Should().Be(29);
        }

        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 0, 0);
        var hastenOneDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 15, 0);
        var hastenTwoDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 25, 0);

        Combat.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().Be(3083);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().Be(2359);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().Be(1875);
    }

    [Test]
    public void LegacySlingPistolDelay_UsesPistolDelay()
    {
        WeaponDelay.GetWeaponDelay(BaseItem.Sling).Should().Be(31);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsReducedDualWieldDelayToDefaultMinimum()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultDelayWhenAttackerDelayIsSameOrLower()
    {
        var attackerDelays = new[]
        {
            0,
            Combat.BaseAttackDelayMilliseconds - 1,
            Combat.BaseAttackDelayMilliseconds
        };

        foreach (var attackerDelay in attackerDelays)
        {
            var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

            effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
        }
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultMinimumWhenNoDelayAttackIsQueued()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2000;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay, true);

        effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CanConsumeNextAbilityNoDelay_RequiresHostileAbility()
    {
        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true
        })
            .Should()
            .BeTrue();

        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = false
        })
            .Should()
            .BeFalse();
    }

    [Test]
    public void WeaponDelayMigration_CoversLivePlayerInventoryAndSerializedItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_14_MigrateResistanceItemProperties.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_31_MigrateResistanceItemProperties.cs"));
        var weaponDelayMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateSerializedObject");
        weaponDelayMigrationSource.Should().Contain("ItemPropertyType.Delay");
        weaponDelayMigrationSource.Should().Contain("WeaponDelay.GetWeaponDelay(baseItem)");
        weaponDelayMigrationSource.Should().Contain("BuildWeaponBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("StaffBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("[\"t_knife\"] = 25");
        weaponDelayMigrationSource.Should().Contain("[\"t_shuriken\"] = 25");
        weaponDelayMigrationSource.Should().Contain("[\"t_rifle\"] = 41");
        weaponDelayMigrationSource.Should().Contain("[\"t_twinblade\"] = 39");
        weaponDelayMigrationSource.Should().Contain("GetHasInventory(obj)");
        weaponDelayMigrationSource.Should().Contain("GetItemInSlot((InventorySlot)index, creature)");
    }

    [Test]
    public void ModuleWeaponDelayProperties_AreNormalized()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectWeaponDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
        }

        findings.Should().BeEmpty(string.Join("\n", findings.Take(25)));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }

    private static readonly IReadOnlyDictionary<int, int> WeaponDelayCostByBaseItem = BuildWeaponDelayCostByBaseItem();

    private static IReadOnlyDictionary<int, int> BuildWeaponDelayCostByBaseItem()
    {
        var delays = new Dictionary<int, int>();
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibrobladeBaseItemTypes, 27);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.KatarBaseItemTypes, 25);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.TwinBladeBaseItemTypes, 39);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes, 25);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.StaffBaseItemTypes, 35);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.RifleBaseItemTypes, 41);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.HeavyVibrobladeBaseItemTypes, 41);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.PistolBaseItemTypes, 31);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.LightsaberBaseItemTypes, 28);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SpearBaseItemTypes, 37);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.ThrowingWeaponBaseItemTypes, 25);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SaberstaffBaseItemTypes, 39);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.CreatureBaseItemTypes, 29);

        return delays;
    }

    private static void AddWeaponDelays(
        Dictionary<int, int> delays,
        IEnumerable<BaseItem> baseItems,
        int delayCost)
    {
        foreach (var baseItem in baseItems)
            delays[(int)baseItem] = delayCost;
    }

    private static void InspectWeaponDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryGetWrappedInt(element, "BaseItem", out var baseItem) &&
                    WeaponDelayCostByBaseItem.TryGetValue(baseItem, out var expectedDelayCost) &&
                    TryGetWrappedValue(element, "PropertiesList", out var propertiesList))
                {
                    var delayCosts = GetDelayCostValues(propertiesList).ToList();
                    if (delayCosts.Count == 0)
                    {
                        findings.Add($"{file}:{path} missing weapon Delay");
                    }
                    else if (delayCosts.Any(x => x != expectedDelayCost))
                    {
                        findings.Add($"{file}:{path} weapon Delay [{string.Join(", ", delayCosts)}] should be {expectedDelayCost}");
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "__struct_id")
                        continue;

                    InspectWeaponDelays(
                        property.Value,
                        file,
                        string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}",
                        findings);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectWeaponDelays(item, file, $"{path}[{index}]", findings);
                    index++;
                }
                break;
        }
    }

    private static IEnumerable<int> GetDelayCostValues(JsonElement propertiesList)
    {
        if (propertiesList.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var property in propertiesList.EnumerateArray())
        {
            if (TryGetWrappedInt(property, "PropertyName", out var propertyName) &&
                propertyName == 98 &&
                TryGetWrappedInt(property, "CostTable", out var costTable) &&
                costTable == 52 &&
                TryGetWrappedInt(property, "CostValue", out var costValue))
            {
                yield return costValue;
            }
        }
    }

    private static bool TryGetWrappedValue(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var wrapper) ||
            wrapper.ValueKind != JsonValueKind.Object ||
            !wrapper.TryGetProperty("value", out value))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetWrappedInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return TryGetWrappedValue(element, propertyName, out var wrapperValue) &&
               wrapperValue.ValueKind == JsonValueKind.Number &&
               wrapperValue.TryGetInt32(out value);
    }
}
