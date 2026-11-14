using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.NWScript.Enum;

using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class ExtraHotbarsTests
{
    // The Gui facade resolves the GUI service from a container.
    private FacadeContainer _guiFacades = null!;
    private IDisposable _guiFacadeScope = null!;

    [OneTimeSetUp]
    public void BuildGuiFacades() => _guiFacades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeGuiFacades() => _guiFacades.Dispose();

    [SetUp]
    public void InstallGuiFacades() => _guiFacadeScope = _guiFacades.Install();

    [TearDown]
    public void RemoveGuiFacades() => _guiFacadeScope.Dispose();

    [Test]
    public void GetBars_PlayerSavedBeforeHotbars_GetsEveryBarWithEverySlot()
    {
        var player = JsonConvert.DeserializeObject<Player>("{}")!;

        var bars = ExtraHotbars.GetBars(player);

        bars.Should().HaveCount(ExtraHotbarsService.BarCount);
        bars.Should().OnlyContain(bar => bar.Slots.Count == ExtraHotbarsService.MaxSlots && !bar.IsVisible);
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

        bars[0].SlotCount.Should().Be(ExtraHotbarsService.MaxSlots);
        bars[0].Slots[0].Feat.Should().Be(FeatType.Provoke1);
        bars[0].Slots.Should().HaveCount(ExtraHotbarsService.MaxSlots);
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
    private const uint Pistol = 5;

    private static bool CanPair(uint main, uint off) =>
        main == NoItem || off == NoItem || (main == Pistol ? off == Shield : main != Rifle);

    private static (uint MainHand, uint OffHand) Pick(uint mainHand, uint offHand, uint item)
    {
        bool CanMain(uint candidate) => candidate != Shield;
        bool CanOff(uint candidate) => candidate is not (Rifle or Pistol);

        return ExtraHotbars.PickHandItem(mainHand, offHand, item, CanMain(item), CanOff(item), CanPair);
    }

    [Test]
    public void PlanHands_ReplacesBothHandsOfASet()
    {
        ExtraHotbars.PlanHands(Saber, SecondSaber, Saber, Shield, CanPair).Should().Be((Saber, Shield));
        ExtraHotbars.PlanHands(Rifle, NoItem, Saber, SecondSaber, CanPair).Should().Be((Saber, SecondSaber));
    }

    [Test]
    public void PlanHands_LeavesAnUnsetHandAloneWhenItStillFits()
    {
        ExtraHotbars.PlanHands(Saber, Shield, SecondSaber, NoItem, CanPair).Should().Be((SecondSaber, Shield));
        ExtraHotbars.PlanHands(Saber, SecondSaber, NoItem, Shield, CanPair).Should().Be((Saber, Shield));
    }

    [Test]
    public void PlanHands_EmptiesAnUnsetHandThatNoLongerFits()
    {
        ExtraHotbars.PlanHands(Saber, SecondSaber, Rifle, NoItem, CanPair).Should().Be((Rifle, NoItem));
        ExtraHotbars.PlanHands(Saber, SecondSaber, Pistol, NoItem, CanPair).Should().Be((Pistol, NoItem));
        ExtraHotbars.PlanHands(Rifle, NoItem, NoItem, Shield, CanPair).Should().Be((NoItem, Shield));
    }

    [Test]
    public void PlanHands_ItemMovingToTheOtherHandLeavesItsOldHand()
    {
        ExtraHotbars.PlanHands(Saber, SecondSaber, SecondSaber, NoItem, CanPair).Should().Be((SecondSaber, NoItem));
        ExtraHotbars.PlanHands(Saber, SecondSaber, NoItem, Saber, CanPair).Should().Be((NoItem, Saber));
    }

    [Test]
    public void PickHandItem_PistolKeepsAShieldButNotASecondWeapon()
    {
        Pick(Pistol, NoItem, Shield).Should().Be((Pistol, Shield));
        Pick(NoItem, Shield, Pistol).Should().Be((Pistol, Shield));
        Pick(Saber, Shield, Pistol).Should().Be((Pistol, Shield));
        Pick(NoItem, SecondSaber, Pistol).Should().Be((Pistol, NoItem));
        Pick(Pistol, NoItem, Saber).Should().Be((Saber, NoItem));
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

    // Hotbar 1 as set up at 1280x720: 12 horizontal slots with the setup title bar.
    private static readonly GuiRectangle _bar1 = new(793f, 566f, 482f, 96f);

    [Test]
    public void FindClearSpot_BarTouchingAnotherBar_StaysPut()
    {
        var spot = ExtraHotbarViewModel.FindClearSpot(793f, 470f, 482f, 96f, new[] { _bar1 }, 1280f, 720f);

        spot.Should().Be((793f, 470f));
    }

    [Test]
    public void FindClearSpot_BarDraggedOnePixelOntoAnother_MovesFlushAgainstIt()
    {
        var spot = ExtraHotbarViewModel.FindClearSpot(793f, 471f, 482f, 96f, new[] { _bar1 }, 1280f, 720f);

        spot.Should().Be((793f, 470f));
    }

    [Test]
    public void FindClearSpot_TitleBarCoversTheBarStackedAbove_UpperBarMovesUp()
    {
        // The default stack touches while locked, so bar 1's title bar covers the bottom of bar 2 in setup.
        var spot = ExtraHotbarViewModel.FindClearSpot(793f, 502f, 482f, 96f, new[] { _bar1 }, 1280f, 720f);

        spot.Should().Be((793f, 470f));
    }

    [Test]
    public void FindClearSpot_NoRoomOnTheNearSide_UsesTheNextNearestSide()
    {
        var topBar = new GuiRectangle(793f, 0f, 482f, 96f);

        var spot = ExtraHotbarViewModel.FindClearSpot(793f, 10f, 482f, 96f, new[] { topBar }, 1280f, 720f);

        spot.Should().Be((793f, 96f));
    }

    [Test]
    public void FindClearSpot_PushedOffOneBarOntoAnother_ClearsBoth()
    {
        var bar2 = new GuiRectangle(793f, 470f, 482f, 96f);

        var spot = ExtraHotbarViewModel.FindClearSpot(793f, 520f, 482f, 96f, new[] { _bar1, bar2 }, 1280f, 720f);

        spot.Should().Be((793f, 374f));
    }
    // The default stack at 1280x720, as the live client showed it: bar 1 above the chat bar, bar 2 on top of it.
    private const float BarWidth = 484f;
    private const float BarHeight = 66f;

    [Test]
    public void FollowScreen_DefaultStackWhenWindowShrinks_StaysInTheBottomRightCorner()
    {
        var bar1 = ExtraHotbarViewModel.FollowScreen(792f, 598f, BarWidth, BarHeight, BarWidth, BarHeight,
            1280f, 720f, 800f, 600f);
        var bar2 = ExtraHotbarViewModel.FollowScreen(792f, 500f, BarWidth, BarHeight, BarWidth, BarHeight,
            1280f, 720f, 800f, 600f);

        // Same gaps to the right and bottom edges, so bar 1 stays above the chat bar and clear of bar 2.
        bar1.Should().Be((312f, 478f));
        bar2.Should().Be((312f, 380f));
    }

    [Test]
    public void FollowScreen_WindowGrowsBack_ReturnsToTheChosenSpot()
    {
        var shrunk = ExtraHotbarViewModel.FollowScreen(792f, 598f, BarWidth, BarHeight, BarWidth, BarHeight,
            1280f, 720f, 800f, 600f);
        var grown = ExtraHotbarViewModel.FollowScreen(shrunk.X, shrunk.Y, BarWidth, BarHeight, BarWidth, BarHeight,
            800f, 600f, 1280f, 720f);

        grown.Should().Be((792f, 598f));
    }

    [Test]
    public void FollowScreen_BarNearTheTopLeft_KeepsItsSpot()
    {
        var spot = ExtraHotbarViewModel.FollowScreen(20f, 40f, 66f, 66f, 66f, 66f, 2560f, 1440f, 1280f, 720f);

        spot.Should().Be((20f, 40f));
    }

    [Test]
    public void FollowScreen_BarCentredOverTheQuickbar_StaysCentred()
    {
        // Centred at x 1280 on a 2560-wide screen.
        var spot = ExtraHotbarViewModel.FollowScreen(1038f, 1300f, BarWidth, BarHeight, BarWidth, BarHeight,
            2560f, 1440f, 1280f, 720f);

        spot.X.Should().Be(398f);
        (spot.X + BarWidth / 2f).Should().Be(640f);
        // Its gap to the bottom edge is kept too.
        spot.Y.Should().Be(580f);
    }

    [Test]
    public void FollowScreen_UiScaleGrows_KeepsTheGapToTheFarEdge()
    {
        var spot = ExtraHotbarViewModel.FollowScreen(792f, 598f, BarWidth, BarHeight, BarWidth * 2f, BarHeight * 2f,
            1280f, 720f, 1280f, 720f);

        spot.Should().Be((1280f - 4f - BarWidth * 2f, 720f - 56f - BarHeight * 2f));
    }

    [Test]
    public void FollowScreen_ScreenNarrowerThanTheBar_KeepsTheBarOnScreen()
    {
        var spot = ExtraHotbarViewModel.FollowScreen(792f, 598f, BarWidth, BarHeight, BarWidth, BarHeight,
            1280f, 720f, 400f, 300f);

        spot.Should().Be((0f, 178f));
    }
}
