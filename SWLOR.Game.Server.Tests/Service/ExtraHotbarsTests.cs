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
    public void BarSize_GrowsAlongItsDirectionOnly()
    {
        var horizontal = ExtraHotbarViewModel.GetWidth(12, false);
        var vertical = ExtraHotbarViewModel.GetWidth(12, true);

        horizontal.Should().BeGreaterThan(vertical);
        ExtraHotbarViewModel.GetHeight(12, true, false).Should().Be(horizontal);
        ExtraHotbarViewModel.GetHeight(4, false, true)
            .Should().BeGreaterThan(ExtraHotbarViewModel.GetHeight(4, false, false));
    }
}
