using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ExamineItemEngineTests
    {
        [EngineTest("Item inspection native bindings render authored color tokens", Category = "ItemInspection", TimeoutSeconds = 30f)]
        public static async Task AuthoredColorsReachNativeLayouts(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            var template = Gui.GetWindowTemplate(GuiWindowType.ExamineItem);
            var playerWindow = template.CreatePlayerWindowAction();
            var model = (ExamineItemViewModel)playerWindow.ViewModel;
            model.Geometry = new GuiRectangle(40f, 45f, 430f, 560f);
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            ctx.Assert(!windows.ContainsKey(observer.Id), "headless fixture GUI registration must be isolated");
            windows.Add(observer.Id, new Dictionary<GuiWindowType, GuiPlayerWindow> { [GuiWindowType.ExamineItem] = playerWindow });
            var snapshots = new JArray();
            var license = string.Join("\n", new[]
            {
                ColorToken.Custom("Name: ", 99, 113, 68) + "<cÿÿÿ>Cinder</c>",
                ColorToken.Custom("Species: ", 99, 212, 63) + ColorToken.White("Charhound"),
                ColorToken.Custom("Sex: ", 99, 212, 63) + ColorToken.White("Male"),
                ColorToken.Custom("Weight: ", 99, 212, 63) + ColorToken.White("30kg"),
                ColorToken.Custom("Vaccinated: ", 99, 212, 63) + ColorToken.White("Yes"),
                ColorToken.Custom("License No. ", 99, 212, 63) + ColorToken.White("PVCL-A000")
            });
            var longDescription = ColorToken.Orange("Mixed colors: ") + ColorToken.Cyan(
                "A description that wraps across multiple lines while keeping the highlighted text in its original color.") +
                "\n\n" + ColorToken.Green("A second paragraph with a green sentence.") + "\n\n" +
                ColorToken.Pink(string.Join("\n", Enumerable.Range(1, 15).Select(index => $"Scroll line {index}")));
            var payloads = new[]
            {
                new ExamineItemPayload(ColorToken.Custom("Veles City Animal License", 99, 120, 32), license, string.Empty),
                new ExamineItemPayload(ColorToken.Orange("Colored item"), longDescription, ColorToken.Cyan("DMG: ") + "10"),
                new ExamineItemPayload("Unidentified item", "An ordinary unidentified description.", string.Empty) { IsIdentified = false }
            };

            try
            {
                foreach (var payload in payloads)
                {
                    await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                        Gui.TogglePlayerWindow(observer.Creature, GuiWindowType.ExamineItem, payload));
                    await ctx.WaitFrameAsync();
                    var token = playerWindow.WindowToken;
                    ctx.Assert(token > 0, "native inspection window must open");
                    var title = JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.WindowTitle)));
                    ctx.Assert(!title.Contains("<c"), "native title must not contain color tokens");
                    var binds = new JObject();
                    foreach (var name in new[] { nameof(model.WindowTitle), nameof(model.TitleColor), nameof(model.Weight), nameof(model.Geometry) })
                        binds[name] = JToken.Parse(JsonDump(NuiGetBind(observer.Creature, token, name)));

                    var layouts = new Dictionary<string, string>((Dictionary<string, string>)typeof(ExamineItemViewModel).BaseType!
                        .GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!);
                    var textLayouts = GetTextLayouts();
                    var description = JObject.Parse(textLayouts[ExamineItemViewModel.DescriptionElement]);
                    var drawItems = description.SelectTokens("$..draw_list[*]").ToList();
                    if (payload.IsIdentified)
                    {
                        ctx.Assert(drawItems.Count > 0, "colored descriptions must use explicit draw colors");
                        ctx.Assert(drawItems.All(item => !item["text"]!.Value<string>()!.Contains("<c")), "draw text must contain no color tokens");
                        ctx.Assert(drawItems.Any(item => item["color"]!["r"]!.Value<int>() != 255), "authored color must reach native JSON");
                        ctx.Assert(binds[nameof(model.TitleColor)]!["g"]!.Value<int>() != 255, "authored title color must reach native bind");
                    }
                    else
                        ctx.Assert(drawItems.Count == 0, "ordinary unidentified text must retain the native text widget");

                    var window = JObject.Parse(JsonDump(template.Window));
                    window["root"] = JObject.Parse(GuiLayoutComposer.Compose(template.PartialViewLayouts["%%WINDOW_MAIN%%"], layouts));
                    ctx.Assert(window.SelectTokens("$..id").Any(id => id.Value<string>() == ExamineItemViewModel.DescriptionElement),
                        "exported root must include the applied inspection body");
                    snapshots.Add(new JObject
                    {
                        ["title"] = title,
                        ["window"] = window,
                        ["binds"] = binds
                    });
                    await VerifyUnchangedRefreshes();
                    if (payload == payloads[1])
                    {
                        await VerifyChangedRefreshes(payload);
                        var changedLayouts = new Dictionary<string, string>(GetGroupLayouts());
                        var updatedWindow = JObject.Parse(JsonDump(template.Window));
                        updatedWindow["root"] = JObject.Parse(GuiLayoutComposer.Compose(template.PartialViewLayouts["%%WINDOW_MAIN%%"], changedLayouts));
                        snapshots.Add(new JObject { ["title"] = "Updated colored item", ["window"] = updatedWindow, ["binds"] = binds.DeepClone() });

                        var cachedDescription = GetTextLayouts()[ExamineItemViewModel.DescriptionElement];
                        var cachedProperties = GetTextLayouts()[ExamineItemViewModel.PropertiesElement];
                        Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                        await ctx.WaitFrameAsync();
                        await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                            Gui.TogglePlayerWindow(observer.Creature, GuiWindowType.ExamineItem, payload));
                        await ctx.WaitFrameAsync();
                        ctx.Assert(ReferenceEquals(cachedDescription, GetTextLayouts()[ExamineItemViewModel.DescriptionElement]) &&
                                   ReferenceEquals(cachedProperties, GetTextLayouts()[ExamineItemViewModel.PropertiesElement]),
                            "reopening identical text must reuse both rendered panels");
                        ctx.Assert(GetGroupLayouts().ContainsKey(ExamineItemViewModel.ContentElement), "reopening must restore the cached content");
                    }
                    Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                    await ctx.WaitFrameAsync();
                }
                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "item-inspection-native-states.json"), snapshots.ToString(Formatting.Indented));
                ctx.SetResultDetail("Real NUI windows verified authored colors, wrapping, unidentified text and cached reopening. Unchanged equip/perk/stat refreshes reused both panels with no redraw; changed descriptions/properties produced one redraw and reused unchanged panels. Native layouts and bindings exported for isolated client replay; no client input was simulated.");
            }
            finally
            {
                Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                await ctx.WaitFrameAsync();
                windows.Remove(observer.Id);
            }

            Dictionary<string, string> GetGroupLayouts() => (Dictionary<string, string>)typeof(ExamineItemViewModel).BaseType!
                .GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;

            Dictionary<string, string> GetTextLayouts() => (Dictionary<string, string>)typeof(ExamineItemViewModel)
                .GetField("_textLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;

            async Task VerifyUnchangedRefreshes()
            {
                var description = GetTextLayouts()[ExamineItemViewModel.DescriptionElement];
                var properties = GetTextLayouts()[ExamineItemViewModel.PropertiesElement];
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                {
                    var height = model.Geometry.Height;
                    model.Refresh(new EquipItemRefreshEvent());
                    model.Refresh(new UnequipItemRefreshEvent());
                    model.Refresh(new PerkAcquiredRefreshEvent(PerkType.Antitoxin));
                    model.Refresh(new PerkRefundedRefreshEvent(PerkType.Antitoxin));
                    for (var index = 0; index < 20; index++) model.Refresh(new StatAdjustmentRefreshEvent());
                    // Every applied root layout synchronously nudges native geometry once.
                    ctx.AssertEqual(height, model.Geometry.Height, "unchanged refreshes must not redraw the window");
                    ctx.Assert(ReferenceEquals(description, GetTextLayouts()[ExamineItemViewModel.DescriptionElement]) &&
                               ReferenceEquals(properties, GetTextLayouts()[ExamineItemViewModel.PropertiesElement]),
                        "unchanged refreshes must not rebuild either text panel");
                    ctx.AssertEqual("null", JsonDump(NuiGetBind(observer.Creature, playerWindow.WindowToken, nameof(model.Description))), "unused description bind");
                    ctx.AssertEqual("null", JsonDump(NuiGetBind(observer.Creature, playerWindow.WindowToken, nameof(model.ItemProperties))), "unused properties bind");
                });
                await ctx.WaitFrameAsync();
            }

            async Task VerifyChangedRefreshes(ExamineItemPayload payload)
            {
                for (var change = 0; change < 3; change++)
                {
                    var description = GetTextLayouts()[ExamineItemViewModel.DescriptionElement];
                    var properties = GetTextLayouts()[ExamineItemViewModel.PropertiesElement];
                    await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    {
                        if (change != 1) payload.Description = ColorToken.Green($"Updated description {change}.");
                        if (change != 0)
                        {
                            payload.ItemProperties = ColorToken.Cyan($"Updated property {change}.");
                            // Snapshot fixtures have no live item; seed the property value a live item refresh supplies.
                            typeof(ExamineItemViewModel).GetProperty(nameof(model.ItemProperties))!.SetValue(model, payload.ItemProperties);
                        }
                        var height = model.Geometry.Height;
                        model.Refresh(new StatAdjustmentRefreshEvent());
                        ctx.AssertEqual(height + 1f, model.Geometry.Height, "changed panels must share one root redraw");
                        if (change == 0) ctx.Assert(ReferenceEquals(properties, GetTextLayouts()[ExamineItemViewModel.PropertiesElement]), "description changes must reuse properties");
                        if (change == 1) ctx.Assert(ReferenceEquals(description, GetTextLayouts()[ExamineItemViewModel.DescriptionElement]), "property changes must reuse description");
                        ctx.Assert(GetGroupLayouts()[ExamineItemViewModel.ContentElement].Contains($"Updated description {(change == 1 ? 0 : change)}."), "new description must reach applied content");
                        if (change != 0) ctx.Assert(GetGroupLayouts()[ExamineItemViewModel.ContentElement].Contains($"Updated property {change}."), "new properties must reach applied content");
                    });
                    await ctx.WaitFrameAsync();
                }
            }
        }
    }
}
