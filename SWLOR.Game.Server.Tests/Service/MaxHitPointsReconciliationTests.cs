#nullable enable

using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class MaxHitPointsReconciliationTests
{
    // The reported character: 40 native levels and Vitality 18, so each level adds its hit die plus 4.
    private const int NativeLevels = 40;
    private const int VitalityModifier = 4;
    // The record the report implies: 200 HP naked at the one-HP-per-level floor, 315 after 245 gear HP.
    private const int DriftedRecordMaxHP = -90;

    private Type _harness = null!;
    private string _root = null!;

    [OneTimeSetUp]
    public void CompileProductionCalculationWithControlledEngine()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        _root = root!.FullName;
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root,
                "SWLOR.Game.Server", "Service", "Stat.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.ValueText is "AdjustPlayerMaxHP" or "RefreshPlayerMaxHP" or "GetEquippedHPBonus");
        var equipmentMethods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root,
                "SWLOR.Game.Server", "Feature", "EquipmentStats.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.ValueText is "ApplyHPBonus" or "RefreshMaxHPAfterEquipmentChange");
        // Execute the real HP application, before handler, after handler, and reconciliation.
        // Substitute only the native level and equipment queries and the database.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Core;
            using SWLOR.Game.Server.Core.Bioware;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using Stat = HitPointHarness;

            public static class HitPointHarness
            {
                private const int NumberOfInventorySlots = 18;
                public const int BaseHP = {{Stat.BaseHP}};
                private const uint OBJECT_SELF = 1;
                private static readonly uint[] Slots = new uint[NumberOfInventorySlots];
                private static readonly Dictionary<uint, List<(ItemPropertyType Type, int Amount)>> Properties = new();
                private static readonly Dictionary<uint, int> Iteration = new();
                private static readonly int[] HitDice = new int[{{NativeLevels}}];
                private static int CurrentHP;
                private static bool IsPlayer;
                private static bool IsDm;
                private static bool IsPossessed;
                public readonly record struct ItemProperty(uint Item, int Index);
                public sealed class Player { public int MaxHP; public int TemporaryFoodHP; }
                public static class DB
                {
                    public static Player? Saved = new();
                    public static int Writes;
                    public static T Get<T>(string id) => (T)(object)Saved!;
                    public static void Set(Player player) { Saved = player; Writes++; }
                }
                public static class CreaturePlugin
                {
                    public static void SetMaxHitPointsByLevel(uint creature, int level, int value) => HitDice[level - 1] = value;
                }
                public static class ObjectPlugin
                {
                    public static int GetCurrentHitPoints(uint creature) => CurrentHP;
                }
                public static class TemporaryHitPointEffects
                {
                    public static void ScheduleLimitCheck(uint creature) { }
                }
                public static class BiowareXP2
                {
                    public static void IPSafeAddItemProperty(uint item, ItemProperty ip, float duration,
                        AddItemPropertyPolicy policy, bool ignoreDuration, bool ignoreSubtype) { }
                }
                public static class EventsPlugin
                {
                    public static uint Item;
                    public static string GetEventData(string key) => key == "ITEM" ? Item.ToString() : "";
                }
                private static uint StringToObject(string id) => uint.Parse(id);
                public sealed class PlayerStatusRefreshEvent
                {
                    public enum StatType { HP }
                    public PlayerStatusRefreshEvent(StatType type) { }
                }
                public static class Gui
                {
                    public static int Refreshes;
                    public static void PublishRefreshEvent(uint player, PlayerStatusRefreshEvent payload) => Refreshes++;
                }
                public static void SetNPCMaxHitPoints(uint creature, int maxHP) { }
                private static ItemProperty ItemPropertyCustom(ItemPropertyType type, int subtype, int amount) => default;
                private static string GetObjectUUID(uint creature) => "test-player";
                private static bool GetIsObjectValid(uint obj) => obj != 0;
                private static bool GetIsPC(uint creature) => IsPlayer;
                private static bool GetIsDM(uint creature) => IsDm;
                private static bool GetIsDMPossessed(uint creature) => IsPossessed;
                private static int GetLevelByPosition(int position, uint creature) => position == 1 ? HitDice.Length : 0;
                private static void SetCurrentHitPoints(uint creature, int hp) => CurrentHP = hp;
                private static uint GetItemInSlot(InventorySlot slot, uint creature) => Slots[(int)slot];
                private static ItemProperty GetFirstItemProperty(uint item) => new(item, 0);
                private static ItemProperty GetNextItemProperty(uint item)
                {
                    var index = Iteration[item] + 1;
                    Iteration[item] = index;
                    return new(item, index);
                }
                private static bool GetIsItemPropertyValid(ItemProperty ip)
                {
                    Iteration[ip.Item] = ip.Index;
                    return Properties.TryGetValue(ip.Item, out var list) && ip.Index < list.Count;
                }
                private static ItemPropertyType GetItemPropertyType(ItemProperty ip) => Properties[ip.Item][ip.Index].Type;
                private static int GetItemPropertyCostTableValue(ItemProperty ip) => Properties[ip.Item][ip.Index].Amount;

                // The engine's maximum: each level's hit die plus the Vitality modifier, at least one.
                public static int GetMaxHitPoints(uint creature)
                {
                    var total = 0;
                    foreach (var die in HitDice) total += Math.Max(1, die + {{VitalityModifier}});
                    return total;
                }

                {{string.Join(Environment.NewLine, methods)}}
                {{string.Join(Environment.NewLine, equipmentMethods)}}

                public static void Reset(int recordMaxHP, int foodHP)
                {
                    Array.Clear(Slots);
                    Properties.Clear();
                    Iteration.Clear();
                    IsPlayer = true;
                    IsDm = IsPossessed = false;
                    DB.Saved = new Player { MaxHP = recordMaxHP, TemporaryFoodHP = foodHP };
                    // The saved character's hit dice were written from this record.
                    AdjustPlayerMaxHP(DB.Saved, OBJECT_SELF, 0);
                    CurrentHP = GetMaxHitPoints(OBJECT_SELF);
                    DB.Writes = 0;
                    Gui.Refreshes = 0;
                }
                public static void SetSlot(int slot, int hp)
                {
                    if (hp == 0) { Slots[slot] = 0; return; }
                    var item = (uint)slot + 2;
                    Slots[slot] = item;
                    Properties[item] = new() { (ItemPropertyType.HPBonus, hp) };
                }
                public static void BeforeChange(int slot, bool adding)
                {
                    var item = (uint)slot + 2;
                    if (!Properties.ContainsKey(item)) Properties[item] = new() { (ItemPropertyType.HPBonus, 0) };
                    ApplyHPBonus(OBJECT_SELF, item, new ItemProperty(item, 0), adding);
                }
                public static void AfterChange(int slot)
                {
                    EventsPlugin.Item = (uint)slot + 2;
                    RefreshMaxHPAfterEquipmentChange();
                }
                public static void SetProperty(int slot, ItemPropertyType type, int amount)
                {
                    var item = (uint)slot + 2;
                    Slots[slot] = item;
                    Properties[item] = new() { (type, amount) };
                }
                public static void SetHP(int slot, int hp) => Properties[(uint)slot + 2] = new() { (ItemPropertyType.HPBonus, hp) };
                public static void SetStaff(bool player, bool dm, bool possessed) { IsPlayer = player; IsDm = dm; IsPossessed = possessed; }
                public static void ClearRecord() => DB.Saved = null;
                public static int RecordMaxHP() => DB.Saved!.MaxHP;
                public static int Writes() => DB.Writes;
                public static int Refreshes() => Gui.Refreshes;
                public static int MaxHP() => GetMaxHitPoints(OBJECT_SELF);
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(Stat).Assembly.Location)
            .Append(typeof(InventorySlot).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("MaxHitPointsReconciliationHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("HitPointHarness")!;
    }

    [SetUp]
    public void Reset() => Call("Reset", Stat.BaseHP, 0);

    [Test]
    public void ReportedDriftedRecord_HidesTheFirstGearHPBehindTheLevelFloor()
    {
        // The report as it happened: equip deltas alone, on a record 160 HP below the base.
        Call("Reset", DriftedRecordMaxHP, 0);
        MaxHP().Should().Be(200, "18 Vitality and 200 max HP naked");
        Call("SetSlot", (int)InventorySlot.Chest, 125);
        Call("BeforeChange", (int)InventorySlot.Chest, true);
        MaxHP().Should().Be(200, "the 125 HP piece left HP at 200/200");
        Call("SetSlot", (int)InventorySlot.Cloak, 120);
        Call("BeforeChange", (int)InventorySlot.Cloak, true);
        MaxHP().Should().Be(315, "the 120 HP piece raised HP by 115 to 315");
    }

    [Test]
    public void ReportedDriftedRecord_IsRepairedByTheCompletedEquip()
    {
        Call("Reset", DriftedRecordMaxHP, 0);
        Equip(InventorySlot.Chest, 125);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP + 125);
        MaxHP().Should().Be(355, "the 125 HP piece adds all of its HP to the 230 HP naked maximum");
        Call("Refreshes").Should().Be(1, "the status window shows the corrected maximum");
        Equip(InventorySlot.Cloak, 120);
        MaxHP().Should().Be(475);
        Equip(InventorySlot.Cloak, 0);
        Equip(InventorySlot.Chest, 0);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP);
        MaxHP().Should().Be(230);
    }

    [TestCase(DriftedRecordMaxHP)]
    [TestCase(0)]
    [TestCase(900)]
    public void LoginReconciliation_RepairsADriftedRecordWithoutEquipEvents(int record)
    {
        Call("Reset", record, 0);
        Call("SetSlot", (int)InventorySlot.Chest, 125);
        Call("SetSlot", (int)InventorySlot.Cloak, 120);

        // Saved gear loads without equip events, so the login refresh repairs the record itself.
        Call("RefreshPlayerMaxHP", 1u);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP + 245);
        MaxHP().Should().Be(475);
        Call("Writes").Should().Be(1);
        Call("RefreshPlayerMaxHP", 1u);
        Call("Writes").Should().Be(1, "repeated reconciliation must not write unchanged data");
    }

    [Test]
    public void ActiveFoodHP_StaysInTheRecord()
    {
        Call("Reset", Stat.BaseHP + 30, 30);
        Equip(InventorySlot.Chest, 125);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP + 125 + 30);
        Call("Writes").Should().Be(1, "only the equip delta is written; reconciliation finds nothing to change");
        Call("Refreshes").Should().Be(0);
    }

    [Test]
    public void CanceledUnequip_DoesNotLeaveTheRecordShort()
    {
        Equip(InventorySlot.Chest, 125);
        // The before handler removes the HP, then the native unequip fails and the item stays worn.
        Call("BeforeChange", (int)InventorySlot.Chest, false);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP);
        Call("AfterChange", (int)InventorySlot.Chest);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP + 125);
        Equip(InventorySlot.Chest, 0);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP);
    }

    [Test]
    public void PropertyChangedWhileWorn_IsCountedAtItsCurrentValue()
    {
        Equip(InventorySlot.Chest, 125);
        Call("SetHP", (int)InventorySlot.Chest, 160);
        Equip(InventorySlot.Chest, 0);
        Call("RecordMaxHP").Should().Be(Stat.BaseHP, "removing the larger value must not push the record below the base");
    }

    [Test]
    public void ChangesWithoutHPBonuses_LeaveTheRecordAlone()
    {
        // Engine fixtures and other equipment changes keep whatever record they had.
        Call("Reset", 1000, 0);
        Call("SetProperty", (int)InventorySlot.RightHand, ItemPropertyType.Attack, 5);
        Call("AfterChange", (int)InventorySlot.RightHand);
        Call("RecordMaxHP").Should().Be(1000);
        Call("Writes").Should().Be(0);
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Reconciliation_SkipsNpcsAndStaff(bool player, bool dm, bool possessed)
    {
        Call("Reset", DriftedRecordMaxHP, 0);
        Call("SetSlot", (int)InventorySlot.Chest, 125);
        Call("SetStaff", player, dm, possessed);
        Call("RefreshPlayerMaxHP", 1u);
        Call("Writes").Should().Be(0);
        Call("RecordMaxHP").Should().Be(DriftedRecordMaxHP);
    }

    [Test]
    public void MissingRecordAndInvalidCreature_AreNotWritten()
    {
        Call("RefreshPlayerMaxHP", 0u);
        Call("ClearRecord");
        Call("RefreshPlayerMaxHP", 1u);
        Call("Writes").Should().Be(0);
    }

    [Test]
    public void Reconciliation_IsWiredToNativeAfterEventsAndLogin()
    {
        var scripts = typeof(EquipmentStats).GetMethod(nameof(EquipmentStats.RefreshMaxHPAfterEquipmentChange))!
            .GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).ToArray();
        scripts.Should().BeEquivalentTo([ScriptName.OnItemEquipValidateAfter, ScriptName.OnItemUnequipAfter]);

        var migration = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root,
                "SWLOR.Game.Server", "Service", "Migration.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "AfterCharacterInitialization");
        migration.Body!.Statements.Select(statement => statement.ToString()).Should().Contain("Stat.RefreshPlayerMaxHP(player);");
    }

    private int MaxHP() => (int)Call("MaxHP")!;
    private void Equip(InventorySlot slot, int hp)
    {
        if (hp > 0)
        {
            Call("SetSlot", (int)slot, hp);
            Call("BeforeChange", (int)slot, true);
        }
        else
        {
            Call("BeforeChange", (int)slot, false);
            Call("SetSlot", (int)slot, 0);
        }
        Call("AfterChange", (int)slot);
    }
    private object? Call(string method, params object[] arguments) =>
        _harness.GetMethod(method)!.Invoke(null, arguments);
}
