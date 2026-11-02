using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class EquipmentRecordReconciliationTests
{
    // Eight pieces granting 86 Physical, 100 Force, 18 FP and 16 STM, with retired Poison Defense on the boots.
    private static readonly (ItemPropertyType Type, int Subtype, int Amount)[] WornGear =
    {
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 13), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 24), (ItemPropertyType.FP, 0, 10),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 9), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 20), (ItemPropertyType.FP, 0, 8),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 11), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 9),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 10), (ItemPropertyType.Stamina, 0, 8),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 11), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 9),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 11), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 9),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 10),
        (ItemPropertyType.Defense, (int)CombatDamageType.Poison, 30), (ItemPropertyType.Stamina, 0, 8),
        (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 11), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 9),
        (ItemPropertyType.HPBonus, 0, 45), (ItemPropertyType.Attack, 0, 5),
    };

    private static EquipmentStatTotals Totals(params (ItemPropertyType Type, int Subtype, int Amount)[] properties)
    {
        var totals = new EquipmentStatTotals();
        foreach (var (type, subtype, amount) in properties)
            totals.Add(type, subtype, amount);
        return totals;
    }

    private static Player Record(int physical, int force, int evasion, int maxFP, int maxStamina)
    {
        var record = new Player("test-player")
        {
            Evasion = evasion,
            MaxFP = maxFP,
            MaxStamina = maxStamina,
        };
        record.Defenses[CombatDamageType.Physical] = physical;
        record.Defenses[CombatDamageType.Force] = force;
        return record;
    }

    [Test]
    public void Totals_CountDefenseTypesAndSkipRetiredDefense()
    {
        var totals = Totals(WornGear);

        totals.GetDefense(CombatDamageType.Physical).Should().Be(86);
        totals.GetDefense(CombatDamageType.Force).Should().Be(100);
        totals.GetDefense(CombatDamageType.Poison).Should().Be(0, "retired Poison Defense never counted on the wearer");
        totals.FP.Should().Be(18);
        totals.Stamina.Should().Be(16);
        totals.Evasion.Should().Be(0);
    }

    [Test]
    public void DriftedRecord_IsRebuiltFromTheWornGear()
    {
        var record = Record(physical: 86 - 32, force: 100 - 47, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM + 16);

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(WornGear), EquipmentRecordStats.All);

        corrected.Should().Be(EquipmentRecordStats.Defense | EquipmentRecordStats.FP);
        record.Defenses[CombatDamageType.Physical].Should().Be(86);
        record.Defenses[CombatDamageType.Force].Should().Be(100);
        record.MaxFP.Should().Be(Stat.BaseFP + 18);
        record.MaxStamina.Should().Be(Stat.BaseSTM + 16);
    }

    [Test]
    public void NakedRecord_ReturnsToZeroAndTheBases()
    {
        var record = Record(physical: -32, force: -47, evasion: 7, maxFP: Stat.BaseFP - 18, maxStamina: Stat.BaseSTM + 3);
        record.Control[SkillType.Smithery] = -21;
        record.Craftsmanship[SkillType.Smithery] = -6;
        record.CPBonus[SkillType.Smithery] = -5;

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(), EquipmentRecordStats.All);

        corrected.Should().Be(EquipmentRecordStats.All);
        foreach (var type in Combat.GetDefenseDamageTypes())
            record.Defenses[type].Should().Be(0, $"{type} Defense comes only from gear");
        record.Evasion.Should().Be(0);
        record.MaxFP.Should().Be(Stat.BaseFP);
        record.MaxStamina.Should().Be(Stat.BaseSTM);
        record.Control[SkillType.Smithery].Should().Be(0, "Control comes only from gear");
        record.Craftsmanship[SkillType.Smithery].Should().Be(0, "Craftsmanship comes only from gear");
        record.CPBonus[SkillType.Smithery].Should().Be(0, "CP Bonus comes only from gear");
    }

    // Smithery gear with a Fabrication piece, as iprp_crafttype.2da subtypes: 1 Smithery, 3 Fabrication.
    private static readonly (ItemPropertyType Type, int Subtype, int Amount)[] CraftingGear =
    {
        (ItemPropertyType.Control, 1, 4), (ItemPropertyType.Craftsmanship, 1, 2), (ItemPropertyType.CPBonus, 1, 5),
        (ItemPropertyType.Control, 1, 3), (ItemPropertyType.Craftsmanship, 1, 1), (ItemPropertyType.CPBonus, 1, 1),
        (ItemPropertyType.Control, 3, 5),
        // An unknown craft subtype feeds no skill, as equipping skips it.
        (ItemPropertyType.Craftsmanship, 0, 9),
    };

    [Test]
    public void Totals_CountControlCraftsmanshipAndCPBonusPerCraftSkill()
    {
        var totals = Totals(CraftingGear);

        totals.GetControl(SkillType.Smithery).Should().Be(7);
        totals.GetCraftsmanship(SkillType.Smithery).Should().Be(3);
        totals.GetCPBonus(SkillType.Smithery).Should().Be(6);
        totals.GetControl(SkillType.Fabrication).Should().Be(5);
        totals.GetCraftsmanship(SkillType.Fabrication).Should().Be(0);
        totals.GetControl(SkillType.Engineering).Should().Be(0);
        totals.GetControl(SkillType.Agriculture).Should().Be(0);
    }

    [TestCase(1, SkillType.Smithery)]
    [TestCase(2, SkillType.Engineering)]
    [TestCase(3, SkillType.Fabrication)]
    [TestCase(4, SkillType.Agriculture)]
    [TestCase(0, SkillType.Invalid)]
    [TestCase(5, SkillType.Invalid)]
    [TestCase(-1, SkillType.Invalid)]
    public void CraftSubtypes_MapToTheCraftTypeTable(int subtype, SkillType expected)
    {
        EquipmentStatTotals.GetCraftSkill(subtype).Should().Be(expected);
    }

    [Test]
    public void NegativeCraftingRecords_AreRebuiltFromTheWornGear()
    {
        // The reported state: one helmet's Smithery Control, Craftsmanship and CP Bonus subtracted without being added.
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);
        record.Control[SkillType.Smithery] = -21;
        record.Craftsmanship[SkillType.Smithery] = -6;
        record.CPBonus[SkillType.Smithery] = -5;
        record.Control[SkillType.Engineering] = 4;

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(CraftingGear), EquipmentRecordStats.All);

        corrected.Should().Be(EquipmentRecordStats.Control | EquipmentRecordStats.Craftsmanship | EquipmentRecordStats.CPBonus);
        record.Control[SkillType.Smithery].Should().Be(7);
        record.Craftsmanship[SkillType.Smithery].Should().Be(3);
        record.CPBonus[SkillType.Smithery].Should().Be(6);
        record.Control[SkillType.Fabrication].Should().Be(5);
        record.Control[SkillType.Engineering].Should().Be(0, "no worn gear grants Engineering Control");
        record.Craftsmanship.Should().NotContainKey(SkillType.Invalid);

        Stat.ApplyEquipmentStatTotals(record, Totals(CraftingGear), EquipmentRecordStats.All)
            .Should().Be(EquipmentRecordStats.None, "a rebuilt record matches the gear");
    }

    [Test]
    public void MissingCraftingEntries_ReadAsZeroAndAreNotCorrected()
    {
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);

        Stat.ApplyEquipmentStatTotals(record, Totals(),
                EquipmentRecordStats.Control | EquipmentRecordStats.Craftsmanship | EquipmentRecordStats.CPBonus)
            .Should().Be(EquipmentRecordStats.None);
        record.Control.Should().BeEmpty();
    }

    [Test]
    public void LegacyRecordWithoutCraftingMaps_IsRebuiltInsteadOfThrowing()
    {
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);
        record.Control = null!;
        record.Craftsmanship = null!;
        record.CPBonus = null!;

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(CraftingGear), EquipmentRecordStats.All);

        corrected.Should().Be(EquipmentRecordStats.Control | EquipmentRecordStats.Craftsmanship | EquipmentRecordStats.CPBonus);
        record.Control[SkillType.Smithery].Should().Be(7);
        record.Craftsmanship[SkillType.Smithery].Should().Be(3);
        record.CPBonus[SkillType.Smithery].Should().Be(6);
    }

    [Test]
    public void LegacyRecordWithoutADefenseMap_IsRebuiltInsteadOfThrowing()
    {
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);
        record.Defenses = null!;

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(WornGear), EquipmentRecordStats.All);

        corrected.Should().HaveFlag(EquipmentRecordStats.Defense);
        record.Defenses[CombatDamageType.Physical].Should().Be(86);
        record.Defenses[CombatDamageType.Force].Should().Be(100);
    }

    [Test]
    public void MatchingRecord_IsNotCorrected()
    {
        var record = Record(physical: 86, force: 100, evasion: 0, maxFP: Stat.BaseFP + 18, maxStamina: Stat.BaseSTM + 16);

        Stat.ApplyEquipmentStatTotals(record, Totals(WornGear), EquipmentRecordStats.All)
            .Should().Be(EquipmentRecordStats.None);
    }

    [Test]
    public void OnlyTheSelectedRecordsAreRebuilt()
    {
        var record = Record(physical: 0, force: 0, evasion: 5, maxFP: 0, maxStamina: 0);

        var corrected = Stat.ApplyEquipmentStatTotals(record, Totals(WornGear), EquipmentRecordStats.Stamina);

        corrected.Should().Be(EquipmentRecordStats.Stamina);
        record.MaxStamina.Should().Be(Stat.BaseSTM + 16);
        record.Defenses[CombatDamageType.Physical].Should().Be(0);
        record.Evasion.Should().Be(5);
        record.MaxFP.Should().Be(0);
    }

    [Test]
    public void EquipChangeEquivalence_RepeatedRebuildsMatchTheRunningSum()
    {
        // A clean running sum after equipping each piece in turn equals the rebuilt totals at every step.
        var record = Record(0, 0, 0, Stat.BaseFP, Stat.BaseSTM);
        var worn = new List<(ItemPropertyType, int, int)>();
        foreach (var property in WornGear)
        {
            worn.Add(property);
            Stat.ApplyEquipmentStatTotals(record, Totals(worn.ToArray()), EquipmentRecordStats.All);
            Stat.ApplyEquipmentStatTotals(record, Totals(worn.ToArray()), EquipmentRecordStats.All)
                .Should().Be(EquipmentRecordStats.None, "a second rebuild of the same gear changes nothing");
        }

        record.Defenses[CombatDamageType.Physical].Should().Be(86);
        record.Defenses[CombatDamageType.Force].Should().Be(100);
    }

    [TestCase(ItemPropertyType.Defense, EquipmentRecordStats.Defense)]
    [TestCase(ItemPropertyType.Evasion, EquipmentRecordStats.Evasion)]
    [TestCase(ItemPropertyType.FP, EquipmentRecordStats.FP)]
    [TestCase(ItemPropertyType.Stamina, EquipmentRecordStats.Stamina)]
    [TestCase(ItemPropertyType.Control, EquipmentRecordStats.Control)]
    [TestCase(ItemPropertyType.Craftsmanship, EquipmentRecordStats.Craftsmanship)]
    [TestCase(ItemPropertyType.CPBonus, EquipmentRecordStats.CPBonus)]
    [TestCase(ItemPropertyType.HPBonus, EquipmentRecordStats.None)]
    [TestCase(ItemPropertyType.Attack, EquipmentRecordStats.None)]
    public void ItemProperties_SelectTheRecordsTheyFeed(ItemPropertyType type, EquipmentRecordStats expected)
    {
        EquipmentStatTotals.GetRecordStat(type).Should().Be(expected);
    }

    [Test]
    public void Reconciliation_IsWiredToNativeAfterEventsAndLogin()
    {
        var scripts = typeof(EquipmentStats).GetMethod(nameof(EquipmentStats.RefreshEquipmentRecordsAfterEquipmentChange))!
            .GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).ToArray();
        scripts.Should().BeEquivalentTo([ScriptName.OnItemEquipValidateAfter, ScriptName.OnItemUnequipAfter]);

        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        var migration = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Service", "Migration.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "AfterCharacterInitialization");
        migration.Body!.Statements.Select(statement => statement.ToString())
            .Should().Contain("Stat.RefreshPlayerEquipmentStats(player);");
    }
}
