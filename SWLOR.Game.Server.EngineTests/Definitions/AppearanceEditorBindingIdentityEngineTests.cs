using SWLOR.Game.Server.Domain.Character;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Records every binding the appearance editor publishes (name, order and the JSON the client receives) while the
    /// editor opens and through a fixed run of colour operations, and compares the transcript with the one captured
    /// before the per-channel colour bindings stopped being declared properties (P5-16e).
    /// </summary>
    public static class AppearanceEditorBindingIdentityEngineTests
    {
        private const string Resource = "AppearanceEditor/BindingIdentity.txt";
        private const string CapturePath = "/nwn/home/appearance-binding-identity.txt";

        [EngineTest("Appearance editor publishes the same bindings and values as before the split", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static async Task BindingTranscriptMatchesFixture(EngineTestContext ctx)
        {
            var civilian = await AppearanceEditorEngineTests.SpawnCivilianAsync(ctx);
            var transcript = new StringBuilder();
            await AppearanceEditorEngineTests.RunAssignedAsync(ctx, civilian, () =>
            {
                var editor = AppearanceEditorEngineTests.BindWithoutClient(civilian);
                var converter = new GuiPropertyConverter();
                var cache = (IDictionary)editor.GetType().BaseType!
                    .GetField("_propertyValues", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
                var phase = "init";
                var sequence = 0;

                string Json(object value) => value == null
                    ? "null"
                    : JToken.Parse(JsonDump(converter.ToJson(value))).ToString(Newtonsoft.Json.Formatting.None);

                object CachedValue(object key)
                {
                    var detail = cache[key]!;
                    return detail.GetType().GetProperty("Value")!.GetValue(detail);
                }

                void Record(object sender, PropertyChangedEventArgs args)
                {
                    var value = cache.Contains(args.PropertyName) ? CachedValue(args.PropertyName) : null;
                    transcript.Append(phase).Append(' ').Append(++sequence).Append(' ').Append(args.PropertyName)
                        .Append(' ').Append(Json(value)).Append('\n');
                }

                // Binding already published the opening state; list it in the order the cache holds it.
                foreach (DictionaryEntry entry in cache)
                    transcript.Append("init-state ").Append(entry.Key).Append(' ').Append(Json(CachedValue(entry.Key))).Append('\n');

                editor.PropertyChanged += Record;
                void Step(string name, Action action)
                {
                    phase = name;
                    sequence = 0;
                    action();
                }

                Step("select-equipment", () => editor.OnSelectEquipment()());
                Step("global-cloth1", () => editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Cloth1)());
                Step("palette-35", () => editor.OnClickColorPalette(35)());
                Step("chest-leather2", () => editor.OnClickColorTarget(AppearanceColorTarget.Chest, AppearanceArmorColor.Leather2)());
                Step("palette-77", () => editor.OnClickColorPalette(77)());
                Step("global-leather2", () => editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Leather2)());
                Step("custom-rgb", () => AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(17, 83, 209)));
                Step("custom-red-text", () =>
                {
                    AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "40");
                    AppearanceEditorEngineTests.InvokePrivate(editor, "CommitCustomTintComponents");
                });
                Step("part-custom-rgb", () =>
                {
                    editor.OnClickColorTarget(AppearanceColorTarget.LeftForearm, AppearanceArmorColor.Leather2)();
                    AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(200, 17, 91));
                });
                Step("global-preset-clears-custom", () =>
                {
                    editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Leather2)();
                    editor.OnClickColorPalette(66)();
                });
                Step("robe-metal2", () => editor.OnClickColorTarget(AppearanceColorTarget.Robe, AppearanceArmorColor.Metal2)());
                Step("palette-12", () => editor.OnClickColorPalette(12)());
                Step("clear-color", () => editor.OnClickClearColor(AppearanceColorTarget.Robe, AppearanceArmorColor.Metal2)());
                Step("revert-chest", () => AppearanceEditorEngineTests.InvokePrivate(editor, "ResetArmorColorToInheritance",
                    AppearanceColorTarget.Chest, AppearanceArmorColor.Leather2));
                Step("copy-right", () => editor.OnClickCopyToRight()());
                Step("select-appearance", () => editor.OnSelectAppearance()());
                editor.PropertyChanged -= Record;
            });

            var actual = transcript.ToString();
            using var stream = typeof(AppearanceEditorBindingIdentityEngineTests).Assembly.GetManifestResourceStream(Resource);
            if (stream == null)
            {
                if (Directory.Exists("/nwn/home"))
                    File.WriteAllText(CapturePath, actual);
                ctx.Assert(false, $"No fixture is embedded; the transcript ({actual.Length} chars) was written to {CapturePath}.");
                return;
            }
            var expected = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            if (expected != actual)
            {
                if (Directory.Exists("/nwn/home"))
                    File.WriteAllText(CapturePath, actual);
                var expectedLines = expected.Split('\n');
                var actualLines = actual.Split('\n');
                var index = 0;
                while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
                    index++;
                ctx.Assert(false, $"Binding transcript differs at line {index + 1}: expected '{(index < expectedLines.Length ? expectedLines[index] : "<end>")}' " +
                                  $"but got '{(index < actualLines.Length ? actualLines[index] : "<end>")}'.");
                return;
            }
            ctx.SetResultDetail($"{actual.Split('\n').Length - 1} lines, identical to the captured transcript.");
        }
    }
}
