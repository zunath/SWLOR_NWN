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
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class EquipmentRecordReconciliationTests
{
    // The Item facade resolves the item service from a container.
    private FacadeContainer _itemFacades = null!;
    private IDisposable _itemFacadeScope = null!;

    [OneTimeSetUp]
    public void BuildItemFacades() => _itemFacades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeItemFacades() => _itemFacades.Dispose();

    [SetUp]
    public void InstallItemFacades() => _itemFacadeScope = _itemFacades.Install();

    [TearDown]
    public void RemoveItemFacades() => _itemFacadeScope.Dispose();

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

        corrected.Should().Be(EquipmentRecordStats.Defense | EquipmentRecordStats.FP | EquipmentRecordStats.Attack);
        record.Defenses[CombatDamageType.Physical].Should().Be(86);
        record.Defenses[CombatDamageType.Force].Should().Be(100);
        record.MaxFP.Should().Be(Stat.BaseFP + 18);
        record.MaxStamina.Should().Be(Stat.BaseSTM + 16);
        record.Attack.Should().Be(5);
    }

    [Test]
    public void NakedRecord_ReturnsToZeroAndTheBases()
    {
        var record = Record(physical: -32, force: -47, evasion: 7, maxFP: Stat.BaseFP - 18, maxStamina: Stat.BaseSTM + 3);
        record.Control[SkillType.Smithery] = -21;
        record.Craftsmanship[SkillType.Smithery] = -6;
        record.CPBonus[SkillType.Smithery] = -5;
        record.Resistances[ResistanceType.Fire] = -10;
        record.FPRegen = 2;
        record.STMRegen = -3;
        record.Attack = -20;
        record.Accuracy = 4;
        record.ForceAttack = -1;
        record.Stealth = 3;
        record.Detection = -2;
        record.TrapBonus = 1;
        record.TrapDisarm = -1;
        record.PoisonBonus = 2;
        record.Lockpicking = -4;

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
        record.Resistances[ResistanceType.Fire].Should().Be(0, "Resistance comes only from gear");
        new[]
        {
            record.FPRegen, record.STMRegen, record.Attack, record.Accuracy, record.ForceAttack, record.Stealth,
            record.Detection, record.TrapBonus, record.TrapDisarm, record.PoisonBonus, record.Lockpicking,
        }.Should().OnlyContain(amount => amount == 0, "each summed record comes only from gear");
    }

    // One item of each kind: a ring and an amulet with every summed stat, and a blade and creature item whose Accuracy
    // belongs to the weapon itself.
    private static EquipmentStatTotals SummedGearTotals()
    {
        var totals = new EquipmentStatTotals();
        void Add(BaseItem baseItem, ItemPropertyType type, int subtype, int amount) => totals.Add(type, subtype, amount, baseItem);
        Add(BaseItem.Ring, ItemPropertyType.FPRegen, -1, 2);
        Add(BaseItem.Ring, ItemPropertyType.STMRegen, -1, 3);
        Add(BaseItem.Ring, ItemPropertyType.Attack, -1, 5);
        Add(BaseItem.Amulet, ItemPropertyType.Attack, -1, 4);
        Add(BaseItem.Ring, ItemPropertyType.Accuracy, -1, 6);
        Add(BaseItem.Longsword, ItemPropertyType.Accuracy, -1, 9);
        Add(BaseItem.CreatureItem, ItemPropertyType.Accuracy, -1, 7);
        Add(BaseItem.Amulet, ItemPropertyType.ForceAttack, -1, 7);
        Add(BaseItem.Amulet, ItemPropertyType.Stealth, -1, 8);
        Add(BaseItem.Amulet, ItemPropertyType.Detection, -1, 1);
        Add(BaseItem.Amulet, ItemPropertyType.TrapBonus, -1, 2);
        Add(BaseItem.Amulet, ItemPropertyType.Disarm, -1, 3);
        Add(BaseItem.Amulet, ItemPropertyType.PoisonBonus, -1, 4);
        Add(BaseItem.Amulet, ItemPropertyType.Lockpicking, -1, 5);
        Add(BaseItem.Ring, ItemPropertyType.Resistance, (int)ResistanceType.Fire, Resistance.EncodeItemPropertyCostTableValue(10));
        Add(BaseItem.Amulet, ItemPropertyType.Resistance, (int)ResistanceType.Fire, Resistance.EncodeItemPropertyCostTableValue(5));
        Add(BaseItem.Amulet, ItemPropertyType.Resistance, (int)ResistanceType.Ice, Resistance.EncodeItemPropertyCostTableValue(-5));
        return totals;
    }

    [Test]
    public void Totals_CountSummedRecordsResistancesAndOnlyNonWeaponAccuracy()
    {
        var totals = SummedGearTotals();

        totals.GetTotal(EquipmentRecordStats.FPRegen).Should().Be(2);
        totals.GetTotal(EquipmentRecordStats.STMRegen).Should().Be(3);
        totals.GetTotal(EquipmentRecordStats.Attack).Should().Be(9);
        totals.GetTotal(EquipmentRecordStats.Accuracy).Should().Be(6, "a weapon's or creature item's Accuracy belongs to that weapon");
        totals.GetTotal(EquipmentRecordStats.ForceAttack).Should().Be(7);
        totals.GetTotal(EquipmentRecordStats.Stealth).Should().Be(8);
        totals.GetTotal(EquipmentRecordStats.Detection).Should().Be(1);
        totals.GetTotal(EquipmentRecordStats.TrapBonus).Should().Be(2);
        totals.GetTotal(EquipmentRecordStats.TrapDisarm).Should().Be(3);
        totals.GetTotal(EquipmentRecordStats.PoisonBonus).Should().Be(4);
        totals.GetTotal(EquipmentRecordStats.Lockpicking).Should().Be(5);
        totals.GetResistance(ResistanceType.Fire).Should().Be(15);
        totals.GetResistance(ResistanceType.Ice).Should().Be(-5, "a vulnerability decodes to a negative resistance");
        totals.GetResistance(ResistanceType.Poison).Should().Be(0);
    }

    [Test]
    public void DriftedSummedRecords_AreRebuiltFromTheWornGear()
    {
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);
        record.Attack = -11;
        record.Accuracy = 15;
        record.Stealth = -8;
        record.Resistances[ResistanceType.Fire] = 30;

        var corrected = Stat.ApplyEquipmentStatTotals(record, SummedGearTotals(), EquipmentRecordStats.All);

        corrected.Should().Be(EquipmentRecordStats.FPRegen | EquipmentRecordStats.STMRegen | EquipmentRecordStats.Attack |
                              EquipmentRecordStats.Accuracy | EquipmentRecordStats.ForceAttack | EquipmentRecordStats.Resistance |
                              EquipmentRecordStats.Stealth | EquipmentRecordStats.Detection | EquipmentRecordStats.TrapBonus |
                              EquipmentRecordStats.TrapDisarm | EquipmentRecordStats.PoisonBonus | EquipmentRecordStats.Lockpicking);
        record.FPRegen.Should().Be(2);
        record.STMRegen.Should().Be(3);
        record.Attack.Should().Be(9);
        record.Accuracy.Should().Be(6);
        record.ForceAttack.Should().Be(7);
        record.Stealth.Should().Be(8);
        record.Detection.Should().Be(1);
        record.TrapBonus.Should().Be(2);
        record.TrapDisarm.Should().Be(3);
        record.PoisonBonus.Should().Be(4);
        record.Lockpicking.Should().Be(5);
        record.Resistances[ResistanceType.Fire].Should().Be(15);
        record.Resistances[ResistanceType.Ice].Should().Be(-5);

        Stat.ApplyEquipmentStatTotals(record, SummedGearTotals(), EquipmentRecordStats.All)
            .Should().Be(EquipmentRecordStats.None, "a rebuilt record matches the gear");
    }

    [Test]
    public void LegacyRecordWithoutAResistanceMap_IsRebuiltInsteadOfThrowing()
    {
        var record = Record(physical: 0, force: 0, evasion: 0, maxFP: Stat.BaseFP, maxStamina: Stat.BaseSTM);
        record.Resistances = null!;

        Stat.ApplyEquipmentStatTotals(record, SummedGearTotals(), EquipmentRecordStats.Resistance)
            .Should().Be(EquipmentRecordStats.Resistance);
        record.Resistances[ResistanceType.Fire].Should().Be(15);
    }

    [Test]
    public void EveryRecordAnEquipHandlerKeeps_IsRebuilt()
    {
        // HP Bonus has its own rebuild (RefreshPlayerMaxHP) and Combat Readiness its own refresh; every other
        // property an equip handler adds to a player record must feed a rebuilt record.
        EquipmentStats.RegisterStatActions();
        EquipmentStats.HandledPropertyTypes.Should().Contain(ItemPropertyType.Lockpicking);
        var withOwnRebuild = new[] { ItemPropertyType.HPBonus, ItemPropertyType.CombatReadiness };
        foreach (var type in EquipmentStats.HandledPropertyTypes.Except(withOwnRebuild))
            EquipmentStatTotals.GetRecordStat(type).Should().NotBe(EquipmentRecordStats.None, $"{type} feeds a player record");
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
        record.Attack = 5;

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
    [TestCase(ItemPropertyType.Resistance, EquipmentRecordStats.Resistance)]
    [TestCase(ItemPropertyType.FPRegen, EquipmentRecordStats.FPRegen)]
    [TestCase(ItemPropertyType.STMRegen, EquipmentRecordStats.STMRegen)]
    [TestCase(ItemPropertyType.Accuracy, EquipmentRecordStats.Accuracy)]
    [TestCase(ItemPropertyType.ForceAttack, EquipmentRecordStats.ForceAttack)]
    [TestCase(ItemPropertyType.Stealth, EquipmentRecordStats.Stealth)]
    [TestCase(ItemPropertyType.Detection, EquipmentRecordStats.Detection)]
    [TestCase(ItemPropertyType.TrapBonus, EquipmentRecordStats.TrapBonus)]
    [TestCase(ItemPropertyType.Disarm, EquipmentRecordStats.TrapDisarm)]
    [TestCase(ItemPropertyType.PoisonBonus, EquipmentRecordStats.PoisonBonus)]
    [TestCase(ItemPropertyType.Lockpicking, EquipmentRecordStats.Lockpicking)]
    [TestCase(ItemPropertyType.HPBonus, EquipmentRecordStats.None)]
    [TestCase(ItemPropertyType.Attack, EquipmentRecordStats.Attack)]
    [TestCase(ItemPropertyType.CombatReadiness, EquipmentRecordStats.None)]
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
