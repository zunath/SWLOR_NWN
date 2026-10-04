#nullable enable

using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatReadinessEquipmentTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionCalculationWithControlledEquipment()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Service", "Stat.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.ValueText is "GetCombatReadinessPercent" or "GetEquippedCombatReadiness");
        // Execute the production slot traversal, property filtering, source combination,
        // and cap. Only native equipment queries and other stat sources are substituted.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Service.StatService;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;

            public static class ReadinessHarness
            {
                private const int NumberOfInventorySlots = 18;
                private const int MaximumCombatReadinessPercent = {{Stat.MaximumCombatReadinessPercent}};
                private static readonly uint[] Slots = new uint[NumberOfInventorySlots];
                private static readonly Dictionary<uint, List<(ItemPropertyType Type, int Amount)>> Properties = new();
                private static int Adjustment;
                private static bool IsPlayer;
                private static readonly Player Npc = new();
                public readonly record struct ItemProperty(uint Item, int Index);
                public sealed class Player { public int CombatReadiness; }
                public static class DB
                {
                    public static Player Saved = new();
                    public static int Reads;
                    public static T Get<T>(string id) { Reads++; return (T)(object)Saved; }
                }
                private static string GetObjectUUID(uint creature) => "test-player";
                private static bool GetIsObjectValid(uint obj) => obj != 0;
                private static bool GetIsPC(uint creature) => IsPlayer;
                private static bool GetIsDM(uint creature) => false;
                private static bool GetIsDMPossessed(uint creature) => false;
                private static int GetStatAdjustment(uint creature, StatType type) => Adjustment;
                private static Player GetNPCStats(uint creature) => Npc;
                private static uint GetItemInSlot(InventorySlot slot, uint creature) => Slots[(int)slot];
                private static ItemProperty GetFirstItemProperty(uint item) => new(item, 0);
                private static ItemProperty GetNextItemProperty(uint item)
                {
                    var index = Iteration[item] + 1;
                    Iteration[item] = index;
                    return new(item, index);
                }
                private static readonly Dictionary<uint, int> Iteration = new();
                private static bool GetIsItemPropertyValid(ItemProperty ip)
                {
                    Iteration[ip.Item] = ip.Index;
                    return ip.Index < Properties[ip.Item].Count;
                }
                private static ItemPropertyType GetItemPropertyType(ItemProperty ip) => Properties[ip.Item][ip.Index].Type;
                private static int GetItemPropertyCostTableValue(ItemProperty ip) => Properties[ip.Item][ip.Index].Amount;

                {{string.Join(Environment.NewLine, methods)}}

                public static void Reset(bool player, int adjustment, int saved, int npc)
                {
                    Array.Clear(Slots);
                    Properties.Clear();
                    Iteration.Clear();
                    IsPlayer = player;
                    Adjustment = adjustment;
                    DB.Saved.CombatReadiness = saved;
                    DB.Reads = 0;
                    Npc.CombatReadiness = npc;
                }
                public static void Equip(int slot, int[] bonuses)
                {
                    if (bonuses.Length == 0) { Slots[slot] = 0; return; }
                    var item = (uint)slot + 2;
                    Slots[slot] = item;
                    Properties[item] = new() { (ItemPropertyType.HPBonus, 100) };
                    foreach (var amount in bonuses) Properties[item].Add((ItemPropertyType.CombatReadiness, amount));
                }
                public static int SavedReads() => DB.Reads;
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(Stat).Assembly.Location)
            .Append(typeof(InventorySlot).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CombatReadinessEquipmentHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("ReadinessHarness")!;
    }

    [SetUp]
    public void Reset() => Call("Reset", true, 0, -25, 0);

    [TestCase(InventorySlot.Head)]
    [TestCase(InventorySlot.Chest)]
    [TestCase(InventorySlot.Cloak)]
    [TestCase(InventorySlot.Boots)]
    [TestCase(InventorySlot.Arms)]
    public void EveryReportedArmorSlot_AddsItsPropertiesDespiteAStaleSavedTotal(InventorySlot slot)
    {
        Equip(slot, 2, 3);
        Read().Should().Be(5);
        Call("SavedReads").Should().Be(0);
    }

    [Test]
    public void WornArmorBonuses_StackAcrossSlotsAndCapOnlyAfterOtherSources()
    {
        Call("Reset", true, 2, 10, 0);
        foreach (var slot in new[] { InventorySlot.Head, InventorySlot.Chest, InventorySlot.Cloak,
                     InventorySlot.Boots, InventorySlot.Arms })
            Equip(slot, 2);
        Read().Should().Be(12);
        Equip(InventorySlot.Chest, 7);
        Read().Should().Be(15);
        Call("Reset", true, -5, 0, 0);
        Equip(InventorySlot.Chest, 20);
        Read().Should().Be(15, "negative stat adjustments apply before the final cap");
    }

    [Test]
    public void ReportedEnhancedArmor_ReachesTheCapWithoutAHelmet()
    {
        // Transcendent Tunic, Eclipse Cloak, Supreme Boots, and Supreme Gloves
        // each display Combat Readiness 10 on the player's enhanced items.
        var slots = new[] { InventorySlot.Chest, InventorySlot.Cloak,
            InventorySlot.Boots, InventorySlot.Arms };
        foreach (var slot in slots)
        {
            Equip(slot, 10);
            Read().Should().Be(10, "each reported piece must work independently");
            Equip(slot);
        }

        foreach (var slot in slots)
            Equip(slot, 10);
        Call("GetEquippedCombatReadiness", 1u).Should().Be(40);
        Read().Should().Be(15, "the four armor bonuses reach the effective cap without a helmet");

        Equip(InventorySlot.Head, 5);
        Call("GetEquippedCombatReadiness", 1u).Should().Be(45);
        Read().Should().Be(15);
        Equip(InventorySlot.Head);
        Read().Should().Be(15, "removing the helmet must not disable the other armor bonuses");
        Call("SavedReads").Should().Be(0);
    }

    [Test]
    public void SwappingUnequippingAndPropertyChanges_UseTheCurrentEquipment()
    {
        Equip(InventorySlot.Head, 5);
        Equip(InventorySlot.Chest, 4);
        Read().Should().Be(9);
        Equip(InventorySlot.Chest, 1);
        Read().Should().Be(6);
        Equip(InventorySlot.Head);
        Read().Should().Be(1);
        Equip(InventorySlot.Chest, 3, 2);
        Read().Should().Be(5);
        Equip(InventorySlot.Chest);
        Read().Should().Be(0, "stale persisted bonuses must not survive unequipping");
    }

    [Test]
    public void NpcSkinBudgetAndStatAdjustments_KeepTheirExistingCalculation()
    {
        Call("Reset", false, 4, 99, 7);
        Equip(InventorySlot.Head, 5);
        Read().Should().Be(11, "NPC equipment is already represented in its stat skin budget");
        Call("Reset", false, -10, 99, 7);
        Read().Should().Be(0);
    }

    private int Read() => (int)Call("GetCombatReadinessPercent", 1u)!;
    private void Equip(InventorySlot slot, params int[] amounts) => Call("Equip", (int)slot, amounts);
    private object? Call(string method, params object[] arguments) =>
        _harness.GetMethod(method)!.Invoke(null, arguments);
}
