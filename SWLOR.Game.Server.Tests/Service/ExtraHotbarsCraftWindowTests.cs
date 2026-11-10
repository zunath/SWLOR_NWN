using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production equipment-slot use against a model of the player's hands. A craft holds the player's gear, so a
/// slot changes nothing while a crafting window is open, whether its click would equip the set or put it away.
/// </summary>
public class ExtraHotbarsCraftWindowTests
{
    private const uint Crafter = 1;
    private const uint Saber = 11;
    private const uint Shield = 12;
    private const uint Rifle = 13;
    private const string Refusal = "Items cannot be equipped while crafting.";

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionEquipmentSlotUse()
    {
        const string hotbars = "Service/ExtraHotbars.cs";
        _harness = Compile($$"""
            public enum InventorySlot { RightHand, LeftHand, Invalid }
            public enum BaseItem { Weapon, CompanionPistol, CompanionPowerGlove }
            public class ExtraHotbarEquipItem { public string ItemName = "an item"; }
            public class ExtraHotbarSlot { public ExtraHotbarEquipItem MainHand = new(), OffHand; }
            public static class ColorToken { public static string Red(string text) => text; }

            public static class CraftMinigameRouting
            {
                public static bool WindowOpen;
                public static bool IsAnyCraftWindowOpen(uint player) => WindowOpen;
            }

            public static class Hands
            {
                public static readonly Dictionary<InventorySlot, uint> Held = new();
                public static uint Slotted, SlottedOffHand;
                public static int PutAway, Equipped;
                public static uint In(InventorySlot slot) => Held.GetValueOrDefault(slot, OBJECT_INVALID);
            }

            public static class PlayerEquipmentStorage
            {
                public static bool TryUnequip(uint player, uint item, InventorySlot slot, bool allowRecoveryBag = true)
                {
                    Hands.Held.Remove(slot);
                    Hands.PutAway++;
                    return true;
                }
            }

            public static class ExtraHotbars
            {
                private static (uint MainHand, uint OffHand) FindEquipItems(uint player, ExtraHotbarSlot slot) =>
                    (Hands.Slotted, Hands.SlottedOffHand);
                private static uint GetItemInSlot(InventorySlot slot, uint creature) => Hands.In(slot);
                private static uint GetItemPossessor(uint item) => OBJECT_INVALID;
                private static BaseItem GetBaseItemType(uint item) => BaseItem.Weapon;
                private static bool CanPairHands(uint mainHand, uint offHand) => true;
                private static bool CanHoldInMainHand(uint item) => true;
                private static string GetEquipError(uint player, uint item) => string.Empty;
                private static uint GetAttackTarget(uint creature) => OBJECT_INVALID;
                private static bool GetIsDead(uint creature) => false;
                private static void ClearAllActions() { }
                private static void ActionAttack(uint target) { }
                private static void ActionEquipItem(uint item, InventorySlot slot) { Hands.Held[slot] = item; Hands.Equipped++; }
                private static void RefreshStatusSoon(uint player) { }

                {{Method(hotbars, "EquipHands")}}
                {{Method(hotbars, "UnequipHands")}}
                {{Method(hotbars, "GetHandSlot")}}
                {{Method(hotbars, "IsHandCompanion")}}
                {{Method(hotbars, "PutAwayHandItem")}}
                {{Method(hotbars, "PlanHands")}}

                public static void Use(uint player) => EquipHands(player, new ExtraHotbarSlot
                {
                    OffHand = Hands.SlottedOffHand == OBJECT_INVALID ? null : new ExtraHotbarEquipItem()
                });
            }

            public static class Driver
            {
                public static void Reset(uint mainHand, uint offHand, uint slotted, uint slottedOffHand, bool craftWindowOpen)
                {
                    Hands.Held.Clear();
                    Hands.Held[InventorySlot.RightHand] = mainHand;
                    Hands.Held[InventorySlot.LeftHand] = offHand;
                    Hands.Slotted = slotted;
                    Hands.SlottedOffHand = slottedOffHand;
                    Hands.PutAway = Hands.Equipped = 0;
                    CraftMinigameRouting.WindowOpen = craftWindowOpen;
                }

                public static uint MainHand() => Hands.In(InventorySlot.RightHand);
                public static uint OffHand() => Hands.In(InventorySlot.LeftHand);
                public static int PutAway() => Hands.PutAway;
                public static int Equipped() => Hands.Equipped;
            }
            """);
    }

    [SetUp]
    public void AddCrafter()
    {
        _harness.Reset();
        _harness.AddPlayer(Crafter);
    }

    [Test]
    public void SetAlreadyInHand_StaysInHandWhileACraftWindowIsOpen()
    {
        UseSlot(mainHand: Saber, offHand: Shield, slotted: Saber, slottedOffHand: Shield, craftWindowOpen: true);

        ExpectHandsUnchanged(Saber, Shield);
        _harness.Messages.Should().ContainSingle().Which.Message.Should().Be(Refusal);
    }

    [Test]
    public void DifferentSet_IsNotEquippedWhileACraftWindowIsOpen()
    {
        UseSlot(mainHand: Saber, offHand: Shield, slotted: Rifle, slottedOffHand: Invalid, craftWindowOpen: true);

        ExpectHandsUnchanged(Saber, Shield);
        _harness.Messages.Should().ContainSingle().Which.Message.Should().Be(Refusal);
    }

    [Test]
    public void SetAlreadyInHand_IsPutAwayWithNoCraftWindowOpen()
    {
        UseSlot(mainHand: Saber, offHand: Shield, slotted: Saber, slottedOffHand: Shield, craftWindowOpen: false);

        _harness.Invoke<uint>("Driver", "MainHand").Should().Be(Invalid);
        _harness.Invoke<uint>("Driver", "OffHand").Should().Be(Invalid);
        _harness.Invoke<int>("Driver", "PutAway").Should().Be(2);
        _harness.Messages.Should().BeEmpty();
    }

    private void UseSlot(uint mainHand, uint offHand, uint slotted, uint slottedOffHand, bool craftWindowOpen)
    {
        _harness.Invoke("Driver", "Reset", mainHand, offHand, slotted, slottedOffHand, craftWindowOpen);
        _harness.Invoke("ExtraHotbars", "Use", Crafter);
    }

    private void ExpectHandsUnchanged(uint mainHand, uint offHand)
    {
        _harness.Invoke<uint>("Driver", "MainHand").Should().Be(mainHand);
        _harness.Invoke<uint>("Driver", "OffHand").Should().Be(offHand);
        _harness.Invoke<int>("Driver", "PutAway").Should().Be(0);
        _harness.Invoke<int>("Driver", "Equipped").Should().Be(0);
    }
}
