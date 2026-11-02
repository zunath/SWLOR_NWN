using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Station windows keep slotted items when the player's inventory has no room: a removal leaves the item in its
    /// slot, and a closed window keeps what did not fit until it opens again.
    /// </summary>
    public static class StationReturnInventorySpaceEngineTests
    {
        private const string Ore = "raw_veldite";
        private const string Cpu = "d_as_cpu1_a";
        private const string Head = "d_hd_mpv1";
        private const string Dna = "beast_dna";
        private const string Hydrolase = "hydrolase_blue";

        [EngineTest("Refinery ores without inventory room stay listed after removal and closing", Category = "ItemClaimSpace")]
        public static async Task RefineryWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<RefineryViewModel>(fixture, GuiWindowType.Refinery, null);
                    ((List<string>)Get(model, "_inputItems")).Add(SerializeNewItem(ctx, Ore, 10));
                    ((List<int>)Get(model, "_inputStackSizes")).Add(10);
                    ((List<string>)Get(model, "_inputItemResrefs")).Add(Ore);
                    Call(model, "AddInputRow", Ore, 10);

                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    model.InputItemToggles[0] = true;
                    model.OnClickRemoveItems()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.AssertEqual(1, model.InputItemNames.Count, "A removed ore without room stays listed");
                    ctx.AssertEqual(Item.InventoryFullShortMessage, model.Instructions, "The refinery says the inventory is full");

                    model = ReopenWindow<RefineryViewModel>(fixture, GuiWindowType.Refinery, null);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.AssertEqual(1, model.InputItemNames.Count, "A closed refinery keeps and shows the ore that did not fit");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = (RefineryViewModel)Gui.GetPlayerWindow(player, GuiWindowType.Refinery).ViewModel;
                    model.InputItemToggles[0] = true;
                    model.OnClickRemoveItems()();
                    ctx.AssertEqual(0, model.InputItemNames.Count, "The ore leaves the refinery");
                    ctx.AssertEqual(10, CarriedStackTotal(player, Ore), "The ore returns to the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
            }
        }

        [EngineTest("Droid assembly parts without inventory room stay installed after removal, reset and closing", Category = "ItemClaimSpace")]
        public static async Task DroidAssemblyWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            fixture.Update(record => record.Skills[SkillType.Engineering].Rank = 40);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<DroidAssemblyViewModel>(fixture, GuiWindowType.DroidAssembly, null);
                    foreach (var resref in new[] { Cpu, Head })
                    {
                        var part = CreateItemOnObject(resref, player);
                        ctx.Assert(GetIsObjectValid(part), $"The player carries a '{resref}' part");
                        Call(model, "AddPart", Droid.LoadDroidPartItemPropertyDetails(part), part);
                    }
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_cpuItem")) && !string.IsNullOrEmpty((string)Get(model, "_headItem")),
                        "The CPU and head are installed");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = (DroidAssemblyViewModel)Gui.GetPlayerWindow(player, GuiWindowType.DroidAssembly).ViewModel;
                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);

                    model.OnClickHead()();
                    model.OnModalConfirmClick()();
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_headItem")), "A removed head without room stays installed");
                    ctx.AssertEqual(Item.InventoryFullMessage, model.Error, "The window says the inventory is full");

                    model.OnClickReset()();
                    model.OnModalConfirmClick()();
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_cpuItem")) && !string.IsNullOrEmpty((string)Get(model, "_headItem")),
                        "Reset keeps the parts without room installed");

                    model.OnWindowClosed()();
                    model = ReopenWindow<DroidAssemblyViewModel>(fixture, GuiWindowType.DroidAssembly, null);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_cpuItem")) && !string.IsNullOrEmpty((string)Get(model, "_headItem")),
                        "A closed assembly keeps the parts that did not fit");
                    ctx.Assert(model.HeadResref != "Blank", "The reopened assembly shows the kept head");
                    ctx.Assert(model.IsBuildInProgress, "The reopened assembly lets the kept parts be removed");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = (DroidAssemblyViewModel)Gui.GetPlayerWindow(player, GuiWindowType.DroidAssembly).ViewModel;
                    model.OnWindowClosed()();
                    ctx.Assert(string.IsNullOrEmpty((string)Get(model, "_cpuItem")) && string.IsNullOrEmpty((string)Get(model, "_headItem")),
                        "Closing with room returns every part");
                    ctx.AssertEqual(1, CountCarried(player, Cpu), "The CPU returns to the inventory");
                    ctx.AssertEqual(1, CountCarried(player, Head), "The head returns to the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
            }
        }

        [EngineTest("Incubator DNA and enzymes without inventory room stay in the incubator after removal and closing", Category = "ItemClaimSpace")]
        public static async Task IncubatorWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var payload = new IncubatorPayload("engine-claim-incubator/" + fixture.Id, string.Empty);
            var otherPayload = new IncubatorPayload("engine-claim-other-incubator/" + fixture.Id, string.Empty);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    Set(model, "_dnaItem", SerializeNewItem(ctx, Dna));
                    Set(model, "_hydrolaseItem", SerializeNewItem(ctx, Hydrolase));
                    Call(model, "RestoreHeldItems");

                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    model.OnClickHydrolase()();
                    model.OnModalConfirmClick()();
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_hydrolaseItem")), "A removed enzyme without room stays");

                    model.OnClickDNA()();
                    model.OnModalConfirmClick()();
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_dnaItem")), "The DNA stays while its enzyme does");

                    model.OnWindowClosed()();
                    model = ReopenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, payload);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_dnaItem")) && !string.IsNullOrEmpty((string)Get(model, "_hydrolaseItem")),
                        "A closed incubator keeps the items that did not fit");
                    ctx.Assert(model.DNAItemResref != "Blank" && model.HydrolaseItemResref != "Blank",
                        "The reopened incubator shows the kept items");

                    ReopenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, otherPayload);
                    ctx.Assert(!Gui.IsWindowOpen(player, GuiWindowType.Incubator), "Another incubator does not open while it would take the kept items");
                    ctx.Assert(!string.IsNullOrEmpty((string)Get(model, "_dnaItem")), "The first incubator keeps its items");
                    AssertCarriedIntact(ctx, player, carried);
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var model = OpenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, otherPayload);
                    ctx.Assert(string.IsNullOrEmpty((string)Get(model, "_dnaItem")) && string.IsNullOrEmpty((string)Get(model, "_hydrolaseItem")),
                        "Opening another incubator with room returns the kept items");
                    ctx.AssertEqual(1, CountCarried(player, Dna), "The kept DNA returns to the inventory");
                    ctx.AssertEqual(1, CountCarried(player, Hydrolase), "The kept enzyme returns to the inventory");

                    Set(model, "_dnaItem", SerializeNewItem(ctx, Dna));
                    Set(model, "_hydrolaseItem", SerializeNewItem(ctx, Hydrolase));
                    Call(model, "RestoreHeldItems");
                    model.OnClickDNA()();
                    model.OnModalConfirmClick()();
                    ctx.Assert(string.IsNullOrEmpty((string)Get(model, "_dnaItem")) && string.IsNullOrEmpty((string)Get(model, "_hydrolaseItem")),
                        "Removing the DNA with room returns everything");
                    ctx.AssertEqual(2, CarriedStackTotal(player, Dna), "The DNA returns to the inventory");
                    ctx.AssertEqual(2, CarriedStackTotal(player, Hydrolase), "The enzyme returns to the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
            }
        }

        [EngineTest("Station windows closed by the server settle their slotted items", Category = "ItemClaimSpace")]
        public static async Task ServerClosedStations(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            fixture.Update(record => record.Skills[SkillType.Engineering].Rank = 40);
            var incubatorPayload = new IncubatorPayload("engine-claim-server-close/" + fixture.Id, string.Empty);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    // Walking away from a station or changing areas closes its window from the server.
                    var droid = OpenWindow<DroidAssemblyViewModel>(fixture, GuiWindowType.DroidAssembly, null);
                    var cpu = CreateItemOnObject(Cpu, player);
                    Call(droid, "AddPart", Droid.LoadDroidPartItemPropertyDetails(cpu), cpu);

                    var incubator = OpenWindow<IncubatorViewModel>(fixture, GuiWindowType.Incubator, incubatorPayload);
                    Set(incubator, "_dnaItem", SerializeNewItem(ctx, Dna));
                    Call(incubator, "RestoreHeldItems");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var droid = (DroidAssemblyViewModel)Gui.GetPlayerWindow(player, GuiWindowType.DroidAssembly).ViewModel;
                    var incubator = (IncubatorViewModel)Gui.GetPlayerWindow(player, GuiWindowType.Incubator).ViewModel;
                    Gui.ClosePlayerWindow(player, GuiWindowType.DroidAssembly);
                    Gui.ClosePlayerWindow(player, GuiWindowType.Incubator);
                    ctx.Assert(string.IsNullOrEmpty((string)Get(droid, "_cpuItem")), "A server close returns the droid parts");
                    ctx.AssertEqual(1, CountCarried(player, Cpu), "The CPU reaches the inventory");
                    ctx.Assert(string.IsNullOrEmpty((string)Get(incubator, "_dnaItem")), "A server close returns the incubator items");
                    ctx.AssertEqual(1, CountCarried(player, Dna), "The DNA reaches the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
            }
        }
    }
}
