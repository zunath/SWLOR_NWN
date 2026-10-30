using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class ExtraHotbarsTests
{
    [Test]
    public void GetBars_PlayerSavedBeforeHotbars_GetsEveryBarWithEverySlot()
    {
        var player = JsonConvert.DeserializeObject<Player>("{}")!;

        var bars = ExtraHotbars.GetBars(player);

        bars.Should().HaveCount(ExtraHotbars.BarCount);
        bars.Should().OnlyContain(bar => bar.Slots.Count == ExtraHotbars.MaxSlots && !bar.IsVisible);
        bars.SelectMany(bar => bar.Slots).Should().OnlyContain(slot => slot.Type == ExtraHotbarSlotType.Empty);
    }

    [Test]
    public void GetBars_KeepsAssignedSlotsAndClampsSlotCount()
    {
        var player = new Player
        {
            ExtraHotbars = new List<ExtraHotbar>
            {
                new()
                {
                    IsVisible = true,
                    SlotCount = 40,
                    Slots = new List<ExtraHotbarSlot>
                    {
                        new() { Type = ExtraHotbarSlotType.Ability, Feat = FeatType.Provoke1 }
                    }
                }
            }
        };

        var bars = ExtraHotbars.GetBars(player);

        bars[0].SlotCount.Should().Be(ExtraHotbars.MaxSlots);
        bars[0].Slots[0].Feat.Should().Be(FeatType.Provoke1);
        bars[0].Slots.Should().HaveCount(ExtraHotbars.MaxSlots);
    }

    [Test]
    public void SavedSlots_RoundTripThroughJson()
    {
        var player = new Player();
        var slots = ExtraHotbars.GetBars(player)[1].Slots;
        slots[2] = new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Item, ItemResref = "stim_a", ItemName = "Stim A" };
        slots[3] = new ExtraHotbarSlot { Type = ExtraHotbarSlotType.Command, Command = "/sit" };

        var loaded = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

        loaded.ExtraHotbars[1].Slots[2].ItemResref.Should().Be("stim_a");
        loaded.ExtraHotbars[1].Slots[3].Command.Should().Be("/sit");
    }

    [Test]
    public void ActionModeSlot_RoundTripsThroughJson()
    {
        var player = new Player();
        ExtraHotbars.GetBars(player)[0].Slots[5] = new ExtraHotbarSlot
        {
            Type = ExtraHotbarSlotType.ActionMode,
            ActionMode = ActionMode.Stealth
        };

        var loaded = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

        loaded.ExtraHotbars[0].Slots[5].Type.Should().Be(ExtraHotbarSlotType.ActionMode);
        loaded.ExtraHotbars[0].Slots[5].ActionMode.Should().Be(ActionMode.Stealth);
    }

    [Test]
    public void EquipmentSlot_RoundTripsBothHandsThroughJson()
    {
        var player = new Player();
        ExtraHotbars.GetBars(player)[2].Slots[0] = new ExtraHotbarSlot
        {
            Type = ExtraHotbarSlotType.Equipment,
            MainHand = new ExtraHotbarEquipItem { ItemId = "saber-1", ItemResref = "lightsaber", ItemName = "Lightsaber" },
            OffHand = new ExtraHotbarEquipItem { ItemId = "saber-2", ItemResref = "lightsaber", ItemName = "Lightsaber" }
        };
        ExtraHotbars.GetBars(player)[2].Slots[1] = new ExtraHotbarSlot
        {
            Type = ExtraHotbarSlotType.Equipment,
            MainHand = new ExtraHotbarEquipItem { ItemId = "rifle-1", ItemResref = "rifle", ItemName = "Rifle" }
        };

        var loaded = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

        var pair = loaded.ExtraHotbars[2].Slots[0];
        pair.Type.Should().Be(ExtraHotbarSlotType.Equipment);
        pair.MainHand.ItemId.Should().Be("saber-1");
        pair.OffHand.ItemId.Should().Be("saber-2");
        loaded.ExtraHotbars[2].Slots[1].OffHand.Should().BeNull();
    }

    private const uint NoItem = SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID;
    private const uint Saber = 1;
    private const uint SecondSaber = 2;
    private const uint Shield = 3;
    private const uint Rifle = 4;

    private static (uint MainHand, uint OffHand) Pick(uint mainHand, uint offHand, uint item)
    {
        bool CanMain(uint candidate) => candidate != Shield;
        bool CanOff(uint candidate) => candidate != Rifle;
        bool FillsBoth(uint candidate) => candidate == Rifle;

        return ExtraHotbars.PickHandItem(mainHand, offHand, item,
            CanMain(item), CanOff(item), FillsBoth(item), mainHand != NoItem && FillsBoth(mainHand));
    }

    [Test]
    public void PickHandItem_FirstWeaponIsTheMainHand()
    {
        Pick(NoItem, NoItem, Saber).Should().Be((Saber, NoItem));
    }

    [Test]
    public void PickHandItem_SecondOneHandedWeaponIsTheOffHand()
    {
        Pick(Saber, NoItem, SecondSaber).Should().Be((Saber, SecondSaber));
    }

    [Test]
    public void PickHandItem_ShieldAlwaysGoesToTheOffHand()
    {
        Pick(NoItem, NoItem, Shield).Should().Be((NoItem, Shield));
        Pick(Saber, NoItem, Shield).Should().Be((Saber, Shield));
    }

    [Test]
    public void PickHandItem_TwoHandedWeaponDropsTheOffHand()
    {
        Pick(NoItem, Shield, Rifle).Should().Be((Rifle, NoItem));
        Pick(Rifle, NoItem, Shield).Should().Be((NoItem, Shield));
        Pick(Rifle, NoItem, Saber).Should().Be((Saber, NoItem));
    }

    [Test]
    public void PickHandItem_ClickingAChosenItemTakesItOut()
    {
        Pick(Saber, SecondSaber, Saber).Should().Be((NoItem, SecondSaber));
        Pick(Saber, SecondSaber, SecondSaber).Should().Be((Saber, NoItem));
    }

    [TestCase(ActionMode.Detect)]
    [TestCase(ActionMode.Stealth)]
    public void OfferedActionModes_HaveANameAndIcon(ActionMode mode)
    {
        ExtraHotbars.GetActionModeName(mode).Should().EndWith(" Mode");
        ExtraHotbars.GetActionModeIcon(mode).Should().StartWith("ife_");
    }

    [Test]
    public void BarSize_FollowsTheLayout()
    {
        var horizontal = ExtraHotbarViewModel.GetWidth(12, ExtraHotbarLayout.Horizontal);
        var oneSlot = ExtraHotbarViewModel.GetWidth(1, ExtraHotbarLayout.Horizontal);
        var slotStep = ExtraHotbarViewModel.GetWidth(2, ExtraHotbarLayout.Horizontal) - oneSlot;

        ExtraHotbarViewModel.GetWidth(12, ExtraHotbarLayout.Vertical).Should().Be(oneSlot);
        ExtraHotbarViewModel.GetHeight(12, ExtraHotbarLayout.Vertical, false).Should().Be(horizontal);

        // Stacked uses rows of six, so twelve slots are two rows of six.
        ExtraHotbarViewModel.GetWidth(12, ExtraHotbarLayout.Stacked)
            .Should().Be(ExtraHotbarViewModel.GetWidth(6, ExtraHotbarLayout.Horizontal));
        ExtraHotbarViewModel.GetHeight(12, ExtraHotbarLayout.Stacked, false)
            .Should().Be(ExtraHotbarViewModel.GetHeight(1, ExtraHotbarLayout.Horizontal, false) + slotStep);
        ExtraHotbarViewModel.GetHeight(4, ExtraHotbarLayout.Stacked, false)
            .Should().Be(ExtraHotbarViewModel.GetHeight(4, ExtraHotbarLayout.Horizontal, false));

        ExtraHotbarViewModel.GetHeight(4, ExtraHotbarLayout.Horizontal, true)
            .Should().BeGreaterThan(ExtraHotbarViewModel.GetHeight(4, ExtraHotbarLayout.Horizontal, false));
    }
}
