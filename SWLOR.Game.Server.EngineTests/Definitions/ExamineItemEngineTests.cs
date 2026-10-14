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
            model.Geometry = new GuiRectangle(40f, 45f, 440f, 560f);
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
                    await Open(payload);
                    var token = playerWindow.WindowToken;
                    ctx.Assert(token > 0, "native inspection window must open");
                    var title = JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.WindowTitle)));
                    ctx.Assert(!title.Contains("<c"), "native title must not contain color tokens");
                    var binds = new JObject();
                    foreach (var name in new[] { nameof(model.WindowTitle), nameof(model.TitleColor), nameof(model.Geometry) })
                        binds[name] = JToken.Parse(JsonDump(NuiGetBind(observer.Creature, token, name)));

                    var content = JObject.Parse(GetContentLayout());
                    var drawItems = content.SelectTokens("$..draw_list[*]").ToList();
                    ctx.Assert(drawItems.Count > 0, "inspection text must use explicit draw colors");
                    ctx.Assert(drawItems.All(item => !item["text"]!.Value<string>()!.Contains("<c")), "draw text must contain no color tokens");
                    if (payload.IsIdentified)
                    {
                        ctx.Assert(drawItems.Any(item => item["color"]!["r"]!.Value<int>() is not (255 or 226 or 169)), "authored color must reach native JSON");
                        ctx.Assert(binds[nameof(model.TitleColor)]!["g"]!.Value<int>() != 255, "authored title color must reach native bind");
                    }
                    else
                    {
                        ctx.Assert(HasText(content, "Unidentified"), "unidentified items must say so");
                        ctx.Assert(!HasText(content, "Properties"), "unidentified items must not list properties");
                    }

                    snapshots.Add(new JObject { ["title"] = title, ["window"] = ExportWindow(), ["binds"] = binds });
                    await VerifyUnchangedRefreshes();
                    if (payload == payloads[1])
                    {
                        await VerifyChangedRefreshes(payload);
                        snapshots.Add(new JObject { ["title"] = "Updated colored item", ["window"] = ExportWindow(), ["binds"] = binds.DeepClone() });

                        var cachedLayout = GetContentLayout();
                        Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                        await ctx.WaitFrameAsync();
                        await Open(payload);
                        ctx.Assert(ReferenceEquals(cachedLayout, GetContentLayout()), "reopening identical content must reuse the rendered layout");
                        ctx.Assert(GetGroupLayouts().ContainsKey(ExamineItemViewModel.ContentElement), "reopening must restore the cached content");
                    }
                    Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                    await ctx.WaitFrameAsync();
                }

                await VerifyLiveItems();
                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "item-inspection-native-states.json"), snapshots.ToString(Formatting.Indented));
                ctx.SetResultDetail("Real NUI windows verified authored colors, icons, unidentified masking, live charges/quantity and cached reopening. Unchanged equip/perk/stat refreshes caused no redraw; changed text produced one redraw.");
            }
            finally
            {
                Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                await ctx.WaitFrameAsync();
                windows.Remove(observer.Id);
            }

            async Task Open(ExamineItemPayload payload)
            {
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    Gui.TogglePlayerWindow(observer.Creature, GuiWindowType.ExamineItem, payload));
                await ctx.WaitFrameAsync();
            }

            JObject ExportWindow()
            {
                var window = JObject.Parse(JsonDump(template.Window));
                window["root"] = JObject.Parse(GuiLayoutComposer.Compose(template.PartialViewLayouts["%%WINDOW_MAIN%%"], GetGroupLayouts()));
                return window;
            }

            string FactValue(string label)
            {
                var texts = JObject.Parse(GetContentLayout()).SelectTokens("$..draw_list[*].text").Select(token => token.Value<string>()).ToList();
                var index = texts.IndexOf(label);
                return index >= 0 && index + 1 < texts.Count ? texts[index + 1] : null;
            }

            List<string> IconLayers() => JObject.Parse(GetContentLayout()).SelectTokens("$..draw_list[*].image")
                .Select(token => token.Value<string>()).ToList();

            static bool HasText(JObject layout, string text) =>
                layout.SelectTokens("$..draw_list[*].text").Any(token => token.Value<string>()!.Contains(text));

            Dictionary<string, string> GetGroupLayouts() => (Dictionary<string, string>)typeof(ExamineItemViewModel).BaseType!
                .GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;

            string GetContentLayout() => (string)typeof(ExamineItemViewModel)
                .GetField("_contentLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;

            async Task VerifyUnchangedRefreshes()
            {
                var layout = GetContentLayout();
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
                    ctx.Assert(ReferenceEquals(layout, GetContentLayout()), "unchanged refreshes must not rebuild the content");
                });
                await ctx.WaitFrameAsync();
            }

            async Task VerifyChangedRefreshes(ExamineItemPayload payload)
            {
                for (var change = 0; change < 3; change++)
                {
                    await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    {
                        payload.Description = ColorToken.Green($"Updated description {change}.");
                        var height = model.Geometry.Height;
                        model.Refresh(new StatAdjustmentRefreshEvent());
                        ctx.AssertEqual(height + 1f, model.Geometry.Height, "changed content must redraw once");
                        ctx.Assert(GetGroupLayouts()[ExamineItemViewModel.ContentElement].Contains($"Updated description {change}."), "new description must reach applied content");
                    });
                    await ctx.WaitFrameAsync();
                }
            }

            async Task VerifyLiveItems()
            {
                uint extractor = OBJECT_INVALID, cores = OBJECT_INVALID, saber = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                {
                    extractor = CreateItemOnObject("dna_extractor_5", observer.Creature);
                    SetItemCharges(extractor, 3);
                    cores = CreateItemOnObject("power_core50", observer.Creature, 25);
                    saber = CreateItemOnObject("lightsaber_zhar", observer.Creature);
                    SetIdentified(saber, false);
                });
                try
                {
                    await Open(new ExamineItemPayload(extractor, trackLiveItem: true));
                    var layout = GetContentLayout();
                    ctx.AssertEqual(1, IconLayers().Count, "a simple item must draw its one icon");
                    ctx.AssertEqual("3", FactValue("Charges: "), "extractor must show its remaining charges");
                    ctx.Assert(layout.Contains("Unique Power"), "extractor must list its properties");
                    await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    {
                        SetItemCharges(extractor, 2);
                        model.Refresh(new StatAdjustmentRefreshEvent());
                        ctx.AssertEqual("2", FactValue("Charges: "), "a refresh must show the live charge count");
                    });
                    Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                    await ctx.WaitFrameAsync();

                    await Open(new ExamineItemPayload(cores, trackLiveItem: true));
                    ctx.AssertEqual("25", FactValue("Quantity: "), "stacks must show their quantity");
                    Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                    await ctx.WaitFrameAsync();

                    await Open(new ExamineItemPayload(saber, trackLiveItem: true));
                    var title = JsonGetString(NuiGetBind(observer.Creature, playerWindow.WindowToken, nameof(model.WindowTitle)));
                    ctx.AssertEqual("Lightsaber (Unidentified)", title, "unidentified items must not reveal their name");
                    ctx.AssertEqual(1, IconLayers().Count, "unidentified items still show their icon");
                    ctx.Assert(!GetContentLayout().Contains("Properties"), "unidentified items must not list properties");
                    Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.ExamineItem);
                    await ctx.WaitFrameAsync();
                }
                finally
                {
                    DestroyObject(extractor);
                    DestroyObject(cores);
                    DestroyObject(saber);
                }
            }
        }
    }
}
