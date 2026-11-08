using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ExtraHotbarSetupEngineTests
    {
        /// <summary>
        /// After a game window resize the client dropped the setup window's values, and its slot slider reported its
        /// minimum back, which saved every bar at one slot. A report that arrives before the window has settled
        /// must not change a bar, the settled window must show the saved values, and the player's own changes after
        /// that must still apply.
        /// </summary>
        [EngineTest("Hotbar setup ignores control defaults reported while it opens", Category = "ExtraHotbars", TimeoutSeconds = 45f)]
        public static async Task SetupIgnoresDefaultsReportedWhileOpening(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var record = DB.Get<Player>(player.Id);
            var bars = ExtraHotbars.GetBars(record);
            bars[0].SlotCount = 8;
            bars[0].Layout = ExtraHotbarLayout.Stacked;
            DB.Set(record);

            // Headless fixtures skip OnModuleEnter, so their windows are registered here.
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(SWLOR.Game.Server.Domain.UI.GuiService)
                .GetField("_playerWindows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Gui)!;
            ctx.Assert(!windows.ContainsKey(player.Id), "fixture GUI registration must be isolated");
            var playerWindows = new Dictionary<GuiWindowType, GuiPlayerWindow>();
            foreach (var type in new[] { GuiWindowType.ExtraHotbarSetup, GuiWindowType.ExtraHotbar1,
                         GuiWindowType.ExtraHotbar2, GuiWindowType.ExtraHotbar3 })
            {
                var template = Gui.GetWindowTemplate(type);
                playerWindows[type] = template.CreatePlayerWindowAction();
                var geometry = template.InitialGeometry;
                playerWindows[type].ViewModel.Geometry =
                    new GuiRectangle(geometry.X, geometry.Y, geometry.Width, geometry.Height);
            }
            windows.Add(player.Id, playerWindows);

            var setupWindow = playerWindows[GuiWindowType.ExtraHotbarSetup];
            var model = (ExtraHotbarSetupViewModel)setupWindow.ViewModel;
            var slotCount = ExtraHotbarSetupViewModel.SlotCountProperty(0);
            var layout = ExtraHotbarSetupViewModel.LayoutProperty(0);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                {
                    Gui.TogglePlayerWindow(player.Creature, GuiWindowType.ExtraHotbarSetup);

                    // What the client reports when the slider is built without its value.
                    NuiSetBind(player.Creature, setupWindow.WindowToken, slotCount, JsonInt(1));
                    model.UpdatePropertyFromClient(slotCount);
                    NuiSetBind(player.Creature, setupWindow.WindowToken, layout, JsonInt((int)ExtraHotbarLayout.Horizontal));
                    model.UpdatePropertyFromClient(layout);
                });
                var token = setupWindow.WindowToken;
                ctx.Assert(token > 0, "the setup window must open for the registered player fixture");

                AssertSavedBar(8, ExtraHotbarLayout.Stacked, "after the opening reports");

                await ctx.WaitFrameAsync();
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(8, JsonGetInt(NuiGetBind(player.Creature, token, slotCount)), "slider value once settled");
                ctx.AssertEqual((int)ExtraHotbarLayout.Stacked, JsonGetInt(NuiGetBind(player.Creature, token, layout)),
                    "layout value once settled");

                await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                {
                    NuiSetBind(player.Creature, token, slotCount, JsonInt(5));
                    model.UpdatePropertyFromClient(slotCount);
                });
                AssertSavedBar(5, ExtraHotbarLayout.Stacked, "after the player moves the slider");
            }
            finally
            {
                Gui.ClosePlayerWindow(player.Creature, GuiWindowType.ExtraHotbarSetup);
                await ctx.WaitFrameAsync();
                windows.Remove(player.Id);
            }

            void AssertSavedBar(int expectedSlots, ExtraHotbarLayout expectedLayout, string when)
            {
                var bar = ExtraHotbars.GetBars(DB.Get<Player>(player.Id))[0];
                ctx.AssertEqual(expectedSlots, bar.SlotCount, $"saved slot count {when}");
                ctx.AssertEqual(expectedLayout, bar.Layout, $"saved layout {when}");
            }
        }
    }
}
