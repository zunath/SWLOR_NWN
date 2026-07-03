using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatAttackDelayTests
{
    [Test]
    public void CalculateAttackDelayMilliseconds_UsesSingleWeaponDelay()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 0, 0, 0);

        delay.Should().Be(3500);
        CombatFormula.CalculateEffectiveAttackDelay(delay).Should().Be(1750);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_DualWieldCountsDefaultDelayOnce()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 210, 0, 0);

        delay.Should().Be(5250);
        CombatFormula.CalculateEffectiveAttackDelay(delay).Should().Be(3500);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandReductionBeforeCombiningDualWieldDelay()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 210, 0, 30);

        delay.Should().Be(4200);
        CombatFormula.CalculateEffectiveAttackDelay(delay).Should().Be(2450);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_SubtractsDefaultDelayFromHigherAttackerDelay()
    {
        var attackerDelay = CombatFormula.BaseAttackDelayMilliseconds + 2500;

        var effectiveDelay = CombatFormula.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(2500);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_AllowsDelaysBelowDefaultAfterBaselineSubtraction()
    {
        var attackerDelay = CombatFormula.BaseAttackDelayMilliseconds + 1250;

        var effectiveDelay = CombatFormula.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(1250);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_FastestWeaponDelayCanBenefitFromHaste()
    {
        var unmodifiedDelay = CombatFormula.CalculateAttackDelayMilliseconds(220, 0, 0, 0);
        var hastenOneDelay = CombatFormula.CalculateAttackDelayMilliseconds(220, 0, 15, 0);
        var hastenTwoDelay = CombatFormula.CalculateAttackDelayMilliseconds(220, 0, 25, 0);

        CombatFormula.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().BeGreaterThan(CombatFormula.BaseAttackDelayMilliseconds);
        CombatFormula.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeGreaterThan(CombatFormula.MinimumAttackDelayMilliseconds);
        CombatFormula.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeGreaterThan(CombatFormula.MinimumAttackDelayMilliseconds);
        CombatFormula.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeLessThan(CombatFormula.CalculateEffectiveAttackDelay(unmodifiedDelay));
        CombatFormula.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeLessThan(CombatFormula.CalculateEffectiveAttackDelay(hastenOneDelay));
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_NegativeHasteIncreasesDelay()
    {
        var unmodifiedDelay = CombatFormula.CalculateAttackDelayMilliseconds(210, 0, 0, 0);
        var slowedDelay = CombatFormula.CalculateAttackDelayMilliseconds(210, 0, -10, 0);

        unmodifiedDelay.Should().Be(3500);
        slowedDelay.Should().Be(3850);
        CombatFormula.CalculateEffectiveAttackDelay(slowedDelay)
            .Should()
            .BeGreaterThan(CombatFormula.CalculateEffectiveAttackDelay(unmodifiedDelay));
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
            WeaponDelay.GetWeaponDelay(naturalWeaponType).Should().Be(24);
        }

        var unmodifiedDelay = CombatFormula.CalculateAttackDelayMilliseconds(240, 0, 0, 0);
        var hastenOneDelay = CombatFormula.CalculateAttackDelayMilliseconds(240, 0, 15, 0);
        var hastenTwoDelay = CombatFormula.CalculateAttackDelayMilliseconds(240, 0, 25, 0);

        CombatFormula.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().Be(2250);
        CombatFormula.CalculateEffectiveAttackDelay(hastenOneDelay).Should().Be(1650);
        CombatFormula.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().Be(1250);
    }

    [Test]
    public void LegacySlingPistolDelay_UsesPistolDelay()
    {
        WeaponDelay.GetWeaponDelay(BaseItem.Sling).Should().Be(25);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsReducedDualWieldDelayToAbsoluteMinimum()
    {
        var delay = CombatFormula.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        CombatFormula.CalculateEffectiveAttackDelay(delay).Should().Be(CombatFormula.MinimumAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultDelayWhenAttackerDelayIsSameOrLower()
    {
        var attackerDelays = new[]
        {
            0,
            CombatFormula.BaseAttackDelayMilliseconds - 1,
            CombatFormula.BaseAttackDelayMilliseconds
        };

        foreach (var attackerDelay in attackerDelays)
        {
            var effectiveDelay = CombatFormula.CalculateEffectiveAttackDelay(attackerDelay);

            effectiveDelay.Should().Be(CombatFormula.BaseAttackDelayMilliseconds);
        }
    }

    [Test]
    public void MinimumAttackDelay_SupportsMaxAttacksPerSwingWithoutOverflow()
    {
        CombatFormula.MinimumAttackDelayMilliseconds.Should().Be(584);
        (CombatFormula.BaseAttackDelayMilliseconds / (float)CombatFormula.MinimumAttackDelayMilliseconds)
            .Should()
            .BeLessThanOrEqualTo(CombatFormula.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttackSwingDelay_FloorsAtBaseDelay()
    {
        CombatFormula.CalculateAttackSwingDelay(584).Should().Be(CombatFormula.BaseAttackDelayMilliseconds);
        CombatFormula.CalculateAttackSwingDelay(CombatFormula.BaseAttackDelayMilliseconds).Should().Be(CombatFormula.BaseAttackDelayMilliseconds);
        CombatFormula.CalculateAttackSwingDelay(2500).Should().Be(2500);
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesOneAttackWhenDelayAtOrAboveSwingFloor()
    {
        foreach (var effectiveDelay in new[] { CombatFormula.BaseAttackDelayMilliseconds, 2500, 5000 })
        {
            var attacks = CombatFormula.CalculateAttacksPerSwing(effectiveDelay, 0f, out var attackDebt);

            attacks.Should().Be(1);
            attackDebt.Should().Be(0f);
        }
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesTwoAttacksWhenDelayIsHalfSwingFloor()
    {
        var attacks = CombatFormula.CalculateAttacksPerSwing(CombatFormula.BaseAttackDelayMilliseconds / 2, 0f, out var attackDebt);

        attacks.Should().Be(2);
        attackDebt.Should().BeApproximately(0f, 0.01f);
    }

    [Test]
    public void CalculateAttacksPerSwing_CarriesFractionalAttacksBetweenSwings()
    {
        // 1000ms delay = 1.75 attacks per 1750ms swing; long-run average must match.
        const int effectiveDelay = 1000;
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 100;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += CombatFormula.CalculateAttacksPerSwing(effectiveDelay, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (CombatFormula.BaseAttackDelayMilliseconds / (float)effectiveDelay);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    [Test]
    public void CalculateAttacksPerSwing_CapsAttacksAtMaxPerSwing()
    {
        var attacks = CombatFormula.CalculateAttacksPerSwing(CombatFormula.MinimumAttackDelayMilliseconds, 5f, out var attackDebt);

        attacks.Should().Be(CombatFormula.MaxAttacksPerSwing);
        attackDebt.Should().BeLessThanOrEqualTo(CombatFormula.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttacksPerSwing_MinimumDelayAveragesToMaxAttacksPerSwing()
    {
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 60;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += CombatFormula.CalculateAttacksPerSwing(CombatFormula.MinimumAttackDelayMilliseconds, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (CombatFormula.BaseAttackDelayMilliseconds / (float)CombatFormula.MinimumAttackDelayMilliseconds);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    [Test]
    public void ConsumeAttacksPerSwing_TracksDebtPerAttacker()
    {
        const uint attackerOne = 100;
        const uint attackerTwo = 200;
        const int effectiveDelay = 1000;

        CombatAttackTiming.ClearAttackSwingDebt(attackerOne);
        CombatAttackTiming.ClearAttackSwingDebt(attackerTwo);

        CombatAttackTiming.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(1);
        CombatAttackTiming.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(1);
        CombatAttackTiming.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(2);
        CombatAttackTiming.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(2);

        CombatAttackTiming.ClearAttackSwingDebt(attackerOne);
        CombatAttackTiming.ClearAttackSwingDebt(attackerTwo);
    }

    [Test]
    public void ClearAttackSwingDebt_ResetsStoredDebt()
    {
        const uint attacker = 300;
        const int effectiveDelay = 1000;

        CombatAttackTiming.ClearAttackSwingDebt(attacker);

        CombatAttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);
        CombatAttackTiming.ClearAttackSwingDebt(attacker);
        CombatAttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);

        CombatAttackTiming.ClearAttackSwingDebt(attacker);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultMinimumWhenNoDelayAttackIsQueued()
    {
        var attackerDelay = CombatFormula.BaseAttackDelayMilliseconds + 2000;

        var effectiveDelay = CombatFormula.CalculateEffectiveAttackDelay(attackerDelay, true);

        effectiveDelay.Should().Be(CombatFormula.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CanConsumeNextAbilityNoDelay_RequiresHostileAbility()
    {
        QueuedCombatActions.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true
        })
            .Should()
            .BeTrue();

        QueuedCombatActions.CanConsumeNextAbilityNoDelay(new AbilityDetail
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
            "StoredItemDataMigration.cs"));
        var weaponDelayMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(obj)");
        weaponDelayMigrationSource.Should().Contain("ItemPropertyType.Delay");
        weaponDelayMigrationSource.Should().Contain("WeaponDelay.GetWeaponDelay(baseItem)");
        weaponDelayMigrationSource.Should().Contain("BuildWeaponBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("StaffBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("[\"t_knife\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"t_shuriken\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"t_rifle\"] = 30");
        weaponDelayMigrationSource.Should().Contain("[\"t_twinblade\"] = 29");
        weaponDelayMigrationSource.Should().Contain("[\"byyskwarriorswor\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"sith_blade\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"wswss002\"] = 22");
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

    [Test]
    public void ModuleShieldItems_DoNotHaveDelayProperties()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"))
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "utc"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectShieldDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
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
    private static readonly IReadOnlySet<int> ShieldBaseItems = SWLOR.Game.Server.Service.Item.ShieldBaseItemTypes
        .Select(x => (int)x)
        .ToHashSet();

    private static IReadOnlyDictionary<int, int> BuildWeaponDelayCostByBaseItem()
    {
        var delays = new Dictionary<int, int>();
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibrobladeBaseItemTypes, 23);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.KatarBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.TwinBladeBaseItemTypes, 29);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.StaffBaseItemTypes, 27);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.RifleBaseItemTypes, 30);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.HeavyVibrobladeBaseItemTypes, 30);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.PistolBaseItemTypes, 25);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.LightsaberBaseItemTypes, 24);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SpearBaseItemTypes, 28);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.ThrowingWeaponBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SaberstaffBaseItemTypes, 29);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.CreatureBaseItemTypes, 24);

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
        InspectItemDelays(element, file, path, findings, InspectWeaponDelay);
    }

    private static void InspectShieldDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        InspectItemDelays(element, file, path, findings, InspectShieldDelay);
    }

    private static void InspectItemDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings,
        Action<int, JsonElement, string, ICollection<string>> inspectItemDelay)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryGetWrappedInt(element, "BaseItem", out var baseItem) &&
                    TryGetWrappedValue(element, "PropertiesList", out var propertiesList))
                {
                    inspectItemDelay(baseItem, propertiesList, $"{file}:{path}", findings);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "__struct_id")
                        continue;

                    InspectItemDelays(
                        property.Value,
                        file,
                        string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}",
                        findings,
                        inspectItemDelay);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectItemDelays(item, file, $"{path}[{index}]", findings, inspectItemDelay);
                    index++;
                }
                break;
        }
    }

    private static void InspectWeaponDelay(
        int baseItem,
        JsonElement propertiesList,
        string findingPath,
        ICollection<string> findings)
    {
        if (!WeaponDelayCostByBaseItem.TryGetValue(baseItem, out var expectedDelayCost))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (delayCosts.Count == 0)
        {
            findings.Add($"{findingPath} missing weapon Delay");
        }
        else if (delayCosts.Any(x => x != expectedDelayCost))
        {
            findings.Add($"{findingPath} weapon Delay [{string.Join(", ", delayCosts)}] should be {expectedDelayCost}");
        }
    }

    private static void InspectShieldDelay(
        int baseItem,
        JsonElement propertiesList,
        string findingPath,
        ICollection<string> findings)
    {
        if (!ShieldBaseItems.Contains(baseItem))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (delayCosts.Count > 0)
        {
            findings.Add($"{findingPath} shield Delay [{string.Join(", ", delayCosts)}] should be removed");
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
