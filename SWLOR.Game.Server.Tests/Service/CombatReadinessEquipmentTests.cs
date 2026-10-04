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

namespace SWLOR.Game.Server.Tests.Service;

public class CombatReadinessEquipmentTests
{
    private Type _harness = null!;
    private string _root = null!;

    [OneTimeSetUp]
    public void CompileProductionCalculationWithControlledEquipment()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        _root = root!.FullName;
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Service", "Stat.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.ValueText is "GetCombatReadinessPercent" or "GetEquippedCombatReadiness"
                or "RefreshCombatReadinessEquipment" or "AdjustCombatReadiness");
        var equipmentMethods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root,
                "SWLOR.Game.Server", "Feature", "EquipmentStats.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.ValueText is "ApplyCombatReadiness" or "RefreshCombatReadinessAfterEquipmentChange");
        // Execute the real cache reconciliation, before/after handlers, and read path.
        // Substitute only the native equipment/actor queries, database, and other stat sources.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Core;
            using SWLOR.Game.Server.Service.StatService;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using Stat = ReadinessHarness;

            public static class ReadinessHarness
            {
                private const int NumberOfInventorySlots = 18;
                private const int MaximumCombatReadinessPercent = {{Stat.MaximumCombatReadinessPercent}};
                private static readonly uint[] Slots = new uint[NumberOfInventorySlots];
                private static readonly Dictionary<uint, List<(ItemPropertyType Type, int Amount)>> Properties = new();
                private static int Adjustment;
                private static bool IsPlayer;
                private static bool IsDm;
                private static bool IsPossessed;
                private static int SlotQueries;
                private static int PropertyQueries;
                private static int NpcDelta;
                private const uint OBJECT_SELF = 1;
                private static readonly Player Npc = new();
                public readonly record struct ItemProperty(uint Item, int Index);
                public sealed class Player { public int CombatReadiness; }
                public static class DB
                {
                    public static Player Saved = new();
                    public static int Reads;
                    public static int Writes;
                    public static T Get<T>(string id) { Reads++; return (T)(object)Saved; }
                    public static void Set(Player player) { Saved = player; Writes++; }
                }
                private static string GetObjectUUID(uint creature) => "test-player";
                private static bool GetIsObjectValid(uint obj) => obj != 0;
                private static bool GetIsPC(uint creature) => IsPlayer;
                private static bool GetIsDM(uint creature) => IsDm;
                private static bool GetIsDMPossessed(uint creature) => IsPossessed;
                private static int GetStatAdjustment(uint creature, StatType type) => Adjustment;
                private static Player GetNPCStats(uint creature) => Npc;
                private static uint GetItemInSlot(InventorySlot slot, uint creature) { SlotQueries++; return Slots[(int)slot]; }
                private static ItemProperty GetFirstItemProperty(uint item) { PropertyQueries++; return new(item, 0); }
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
                private static void ReapplyNPCStat(uint creature, ItemPropertyType type, int amount, bool isAdding)
                    => NpcDelta += isAdding ? amount : -amount;

                {{string.Join(Environment.NewLine, methods)}}
                {{string.Join(Environment.NewLine, equipmentMethods)}}

                public static void Reset(bool player, int adjustment, int saved, int npc)
                {
                    Array.Clear(Slots);
                    Properties.Clear();
                    Iteration.Clear();
                    IsPlayer = player;
                    IsDm = IsPossessed = false;
                    Adjustment = adjustment;
                    DB.Saved = new Player();
                    DB.Saved.CombatReadiness = saved;
                    DB.Reads = 0;
                    DB.Writes = SlotQueries = PropertyQueries = NpcDelta = 0;
                    Npc.CombatReadiness = npc;
                }
                public static void SetSlot(int slot, int[] bonuses)
                {
                    if (bonuses.Length == 0) { Slots[slot] = 0; return; }
                    var item = (uint)slot + 2;
                    Slots[slot] = item;
                    Properties[item] = new() { (ItemPropertyType.HPBonus, 100) };
                    foreach (var amount in bonuses) Properties[item].Add((ItemPropertyType.CombatReadiness, amount));
                }
                public static int SavedTotal() => DB.Saved.CombatReadiness;
                public static void SetSavedTotal(int total) => DB.Saved.CombatReadiness = total;
                public static void ClearRecord() => DB.Saved = null;
                public static void SetStaff(bool dm, bool possessed) { IsDm = dm; IsPossessed = possessed; }
                public static int Writes() => DB.Writes;
                public static int SlotsRead() => SlotQueries;
                public static int PropertiesRead() => PropertyQueries;
                public static int NpcChange() => NpcDelta;
                public static void BeforeChange(int amount, bool adding)
                {
                    Properties[100] = new() { (ItemPropertyType.CombatReadiness, amount) };
                    ApplyCombatReadiness(1, 100, new ItemProperty(100, 0), adding);
                }
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
    public void CompletedEquip_RepairsTheCacheForEveryReportedArmorSlot(InventorySlot slot)
    {
        Equip(slot, 2, 3);
        Read().Should().Be(5);
        Call("SavedTotal").Should().Be(5);
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
        Call("SavedTotal").Should().Be(40);
        Read().Should().Be(15, "the four armor bonuses reach the effective cap without a helmet");

        Equip(InventorySlot.Head, 5);
        Call("SavedTotal").Should().Be(45);
        Read().Should().Be(15);
        Equip(InventorySlot.Head);
        Read().Should().Be(15, "removing the helmet must not disable the other armor bonuses");
        Call("SavedTotal").Should().Be(40);
    }

    [Test]
    public void CompletedSwapsAndUnequips_ReplaceTheCachedContribution()
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

    [TestCase(0)]
    [TestCase(-25)]
    [TestCase(99)]
    public void LoginReconciliation_RepairsStaleSavedTotalsWithoutEquipEvents(int saved)
    {
        Call("Reset", true, 5, saved, 0);
        foreach (var slot in new[] { InventorySlot.Chest, InventorySlot.Cloak,
                     InventorySlot.Boots, InventorySlot.Arms })
            Call("SetSlot", (int)slot, new[] { 10 });

        // NWNX RunEquip events do not fire when a saved character's gear is loaded.
        // The login refresh therefore has to repair the persisted contribution itself.
        Call("RefreshCombatReadinessEquipment", 1u);
        Call("SavedTotal").Should().Be(40);
        Call("Writes").Should().Be(1);
        Read().Should().Be(15);
        Call("RefreshCombatReadinessEquipment", 1u);
        Call("Writes").Should().Be(1, "repeated reconciliation must not write unchanged data");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FailedOrCanceledEquipmentAttempts_DoNotChangeTheCache(bool adding)
    {
        Equip(InventorySlot.Head, 5);
        var writes = Call("Writes");
        Call("BeforeChange", 10, adding);
        Call("SavedTotal").Should().Be(5, "validation is not a completed equipment change");

        // Native operation failed: slots are unchanged when the AFTER handler runs.
        Call("RefreshCombatReadinessAfterEquipmentChange");
        Call("SavedTotal").Should().Be(5);
        Call("Writes").Should().Be(writes);
    }

    [Test]
    public void RepeatedAfterEventsAndSlotMoves_DoNotDoubleCountEquipment()
    {
        Equip(InventorySlot.RightRing, 4);
        Call("SetSlot", (int)InventorySlot.RightRing, Array.Empty<int>());
        Call("SetSlot", (int)InventorySlot.LeftRing, new[] { 4 });
        Call("RefreshCombatReadinessAfterEquipmentChange");
        Call("RefreshCombatReadinessAfterEquipmentChange");
        Call("SavedTotal").Should().Be(4);
        Call("Writes").Should().Be(1);
        Equip(InventorySlot.LeftRing);
        Call("SavedTotal").Should().Be(0);
    }

    [Test]
    public void RebuildResetFollowedByUnequips_DoesNotSubtractFromAnAlreadyResetTotal()
    {
        Equip(InventorySlot.Head, 5);
        Equip(InventorySlot.Chest, 10);
        Call("SetSavedTotal", 0);
        Call("BeforeChange", 5, false);
        Equip(InventorySlot.Head);
        Call("SavedTotal").Should().Be(10);
        Equip(InventorySlot.Chest);
        Call("SavedTotal").Should().Be(0);
    }

    [Test]
    public void AbilityReads_DoNotEnumerateEquipmentOrWriteToTheDatabase()
    {
        Call("Reset", true, 5, 0, 0);
        Equip(InventorySlot.Head, 5);
        var slots = Call("SlotsRead");
        var properties = Call("PropertiesRead");
        var writes = Call("Writes");
        for (var index = 0; index < 1000; index++)
            Read().Should().Be(10);
        Call("SlotsRead").Should().Be(slots);
        Call("PropertiesRead").Should().Be(properties);
        Call("Writes").Should().Be(writes);
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Reconciliation_SkipsNpcsAndStaff(bool player, bool dm, bool possessed)
    {
        Call("Reset", player, 0, 99, 7);
        Call("SetStaff", dm, possessed);
        Call("RefreshCombatReadinessEquipment", 1u);
        Call("SlotsRead").Should().Be(0);
        Call("Writes").Should().Be(0);
        Call("SavedTotal").Should().Be(99);
    }

    [Test]
    public void MissingRecordAndInvalidCreature_AreNotWrittenOrScanned()
    {
        Call("RefreshCombatReadinessEquipment", 0u);
        Call("ClearRecord");
        Call("RefreshCombatReadinessEquipment", 1u);
        Call("SlotsRead").Should().Be(0);
        Call("Writes").Should().Be(0);
        Read().Should().Be(0);
    }

    [Test]
    public void NpcBeforeHandlers_StillApplyAndRemoveSkinBonuses()
    {
        Call("Reset", false, 0, 0, 0);
        Call("BeforeChange", 7, true);
        Call("NpcChange").Should().Be(7);
        Call("BeforeChange", 7, false);
        Call("NpcChange").Should().Be(0);
        Call("Writes").Should().Be(0);
    }

    [Test]
    public void Reconciliation_IsWiredToNativeAfterEventsAndAfterAllLoginMigrations()
    {
        var scripts = typeof(EquipmentStats).GetMethod(nameof(EquipmentStats.RefreshCombatReadinessAfterEquipmentChange))!
            .GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).ToArray();
        scripts.Should().BeEquivalentTo([ScriptName.OnItemEquipValidateAfter, ScriptName.OnItemUnequipAfter]);
        var registration = File.ReadAllText(Path.Combine(_root, "SWLOR.Game.Server", "Feature", "EventRegistration.cs"));
        registration.Should().Contain("SubscribeEvent(\"NWNX_ON_ITEM_EQUIP_AFTER\", ScriptName.OnItemEquipValidateAfter)");
        registration.Should().Contain("SubscribeEvent(\"NWNX_ON_ITEM_UNEQUIP_AFTER\", ScriptName.OnItemUnequipAfter)");

        var migration = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_root,
                "SWLOR.Game.Server", "Service", "Migration.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "RunPlayerMigrations");
        migration.AttributeLists.ToString().Should().Contain("ScriptName.OnCharacterInitAfter");
        migration.Body!.Statements.Last().ToString().Should().Be("Stat.RefreshCombatReadinessEquipment(player);");
        migration.Body.Statements[^2].ToString().Should().Be("Perk.RestorePlayerFeats(player);");
        migration.Body.Statements.OfType<ForEachStatementSyntax>().Should().ContainSingle();
    }

    private int Read() => (int)Call("GetCombatReadinessPercent", 1u)!;
    private void Equip(InventorySlot slot, params int[] amounts)
    {
        Call("SetSlot", (int)slot, amounts);
        Call("RefreshCombatReadinessAfterEquipmentChange");
    }
    private object? Call(string method, params object[] arguments) =>
        _harness.GetMethod(method)!.Invoke(null, arguments);
}
