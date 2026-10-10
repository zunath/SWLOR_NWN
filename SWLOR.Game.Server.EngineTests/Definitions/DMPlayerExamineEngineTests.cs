using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DMPlayerExamineEngineTests
    {
        /// <summary>
        /// Exercises real server NUI state with registered headless player fixtures. Exported
        /// bindings can also be rendered by an isolated offline client; no client input is simulated.
        /// </summary>
        [EngineTest("DM examine native bindings retain filters, plain names and modal state", Category = "DMPlayerExamine", TimeoutSeconds = 45f)]
        public static async Task NativeBindingsSurviveTabAndModalChanges(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            CreaturePlugin.SetClassByPosition(target.Creature, 0, ClassType.ForceSensitive);
            var record = DB.Get<Player>(target.Id);
            record.UnallocatedXP = 12000;
            record.UnknownDisplayName = "Grumpy old man";
            DB.Set(record);
            PlayerName.SetKnownName(observer.Creature, target.Creature, "Examine Test Character");
            var coloredName = PlayerName.GetDisplayName(observer.Creature, target.Creature);
            var plainName = UtilPlugin.StripColors(coloredName);
            ctx.Assert(coloredName != plainName, "fixture must exercise an inline-colored descriptor");

            var template = Gui.GetWindowTemplate(GuiWindowType.DMPlayerExamine);
            var playerWindow = template.CreatePlayerWindowAction();
            var model = (DMPlayerExamineViewModel)playerWindow.ViewModel;
            model.Geometry = new GuiRectangle(35f, 20f, 640f, 660f);
            // Headless fixtures do not pass through OnModuleEnter. Register this window so
            // native watch events use the same event routing as a connected player's UI.
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            ctx.Assert(!windows.ContainsKey(observer.Id), "fixture GUI registration must be isolated");
            windows.Add(observer.Id, new Dictionary<GuiWindowType, GuiPlayerWindow>
            {
                [GuiWindowType.DMPlayerExamine] = playerWindow
            });
            var token = 0;
            var snapshots = new List<object>();
            try
            {
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    Gui.TogglePlayerWindow(observer.Creature, GuiWindowType.DMPlayerExamine,
                        new DMPlayerExaminePayload(target.Creature)));
                token = playerWindow.WindowToken;
                ctx.Assert(token > 0, "native NUI window must be created for the registered player fixture");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(plainName, JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.Name))), "plain native title");
                ctx.AssertEqual("12,000", JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.RPXP))), "target RP XP");
                ctx.AssertEqual("Force Sensitive", model.CharacterType, "native Force Sensitive class");
                ctx.Assert(model.FP != "0 / 0", "Force Sensitive FP must be displayed");
                Capture("details", 0);

                await InPlayerContext(() => model.TopTabId = 2);
                AssertPerkInputs(0, 2, 1, "");
                Capture("perks-default", 2);
                await InPlayerContext(() =>
                {
                    model.SelectedPerkSortOrderId = 1;
                    model.SelectedPerkStatusId = 0;
                    model.PerkSearchText = "analyzer";
                });
                ctx.Assert(model.PerkNames.Count > 0, "search must find real target perk definitions");
                var filteredPerks = model.PerkNames.ToArray();
                await InPlayerContext(() => model.TopTabId = 0);
                await InPlayerContext(() => model.TopTabId = 2);
                AssertPerkInputs(0, 1, 0, "analyzer");
                ctx.Assert(filteredPerks.SequenceEqual(model.PerkNames), "tab return must retain filtered rows");
                Capture("perks-return", 2);

                await InPlayerContext(() => model.TopTabId = 1);
                await InPlayerContext(() => model.SkillSearchText = "first");
                await InPlayerContext(() => model.TopTabId = 0);
                await InPlayerContext(() => model.TopTabId = 1);
                ctx.AssertEqual("first", JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.SkillSearchText))), "skill search after tab return");
                Capture("skills-return", 1);

                AssignCommand(target.Creature, () => ApplyEffectToObject(DurationType.Temporary,
                    EffectAbilityIncrease(AbilityType.Might, 2), target.Creature, 60f));
                await ctx.WaitFrameAsync();
                await InPlayerContext(() => model.BottomTabId = 0);
                ctx.Assert(model.EffectSources.Contains(plainName), "effect source must use the plain observer-resolved name");
                ctx.Assert(model.EffectSources.All(name => name == UtilPlugin.StripColors(name)), "all effect sources must be plain text");
                Capture("effects", 3);

                await InPlayerContext(() => model.BottomTabId = 1);
                model.OnClickNewNote()();
                typeof(DMPlayerExamineViewModel).GetField("_selectedIndex", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(model, 0);
                model.ActiveNoteName = "Engine verification note";
                model.ActiveNoteDetail = "Unsaved note text survives modal cancellation.\n\n" +
                    "This longer engine verification note checks that the editor uses the available " +
                    "space and wraps its contents naturally. The title and author remain above the " +
                    "note, with the save action below it.\n\n" +
                    "Widening the window should give these paragraphs more room. Reducing the " +
                    "window height should shorten the editor while keeping the note readable and " +
                    "editable. The note list stays in its sidebar.\n\n" +
                    "Cancelling deletion must preserve all of this unsaved text.";
                await InPlayerContext(model.OnClickDeleteNote());
                await InPlayerContext(model.OnModalCancelClick());
                ctx.AssertEqual("Engine verification note", JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.ActiveNoteName))), "note title after modal cancellation");
                ctx.AssertEqual(model.ActiveNoteDetail, JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.ActiveNoteDetail))), "unsaved note text after modal cancellation");
                Capture("notes-restored", 4);
                await InPlayerContext(() => model.Geometry = new GuiRectangle(35f, 20f, 920f, 660f));
                await ctx.WaitUntilAsync(() => AppliedNotesWidth() == 840f, 2f, "Notes layout to grow with native geometry");
                ctx.AssertEqual(920f, JsonGetFloat(JsonObjectGet(NuiGetBind(observer.Creature, token, nameof(model.Geometry)), "w")), "widened native geometry");
                Capture("notes-wide", 4);
                await InPlayerContext(() => model.Geometry = new GuiRectangle(35f, 20f, 640f, 500f));
                await ctx.WaitUntilAsync(() => AppliedNotesWidth() == 560f, 2f, "Notes layout to shrink with native geometry");
                ctx.AssertEqual(500f, JsonGetFloat(JsonObjectGet(NuiGetBind(observer.Creature, token, nameof(model.Geometry)), "h")), "shorter native geometry");
                ctx.AssertEqual(model.ActiveNoteDetail, JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.ActiveNoteDetail))), "note text after resizing");
                Capture("notes-compact", 4);
                await InPlayerContext(() => model.Geometry = new GuiRectangle(35f, 20f, 640f, 660f));

                CreaturePlugin.SetClassByPosition(target.Creature, 0, ClassType.Standard);
                record = DB.Get<Player>(target.Id);
                record.CharacterType = Enumeration.CharacterType.Standard;
                DB.Set(record);
                await InPlayerContext(() => model.TopTabId = 0);
                ctx.AssertEqual("Standard", model.CharacterType, "native Standard class");
                ctx.AssertEqual("0 / 0", JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.FP))), "Standard FP");
                Capture("details-standard", 0);

                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "dm-examine-native-states.json"),
                    JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true }));
            }
            finally
            {
                Gui.ClosePlayerWindow(observer.Creature, GuiWindowType.DMPlayerExamine);
                await ctx.WaitFrameAsync();
                windows.Remove(observer.Id);
                foreach (var note in DB.Search(new DBQuery<PlayerNote>()
                             .AddFieldSearch(nameof(PlayerNote.PlayerId), target.Id, false).AddPaging(50, 0)))
                    DB.Delete<PlayerNote>(note.Id);
                foreach (var names in DB.Search(new DBQuery<PlayerKnownName>()
                             .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false).AddPaging(50, 0)))
                    DB.Delete<PlayerKnownName>(names.Id);
                await ctx.WaitFrameAsync();
            }

            async Task InPlayerContext(Action action)
            {
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, action);
                await ctx.WaitFrameAsync();
            }

            void AssertPerkInputs(int category, int sort, int status, string search)
            {
                ctx.AssertEqual(category, JsonGetInt(NuiGetBind(observer.Creature, token, nameof(model.SelectedPerkCategoryId))), "perk category");
                ctx.AssertEqual(sort, JsonGetInt(NuiGetBind(observer.Creature, token, nameof(model.SelectedPerkSortOrderId))), "perk sort");
                ctx.AssertEqual(status, JsonGetInt(NuiGetBind(observer.Creature, token, nameof(model.SelectedPerkStatusId))), "perk status");
                ctx.AssertEqual(search, JsonGetString(NuiGetBind(observer.Creature, token, nameof(model.PerkSearchText))), "perk search");
            }

            void Capture(string name, int tab)
            {
                var binds = new Dictionary<string, JsonElement>();
                foreach (var watched in new[] { false, true })
                {
                    for (var index = 0; ; index++)
                    {
                        var bind = NuiGetNthBind(observer.Creature, token, watched, index);
                        if (string.IsNullOrEmpty(bind)) break;
                        using var json = JsonDocument.Parse(JsonDump(NuiGetBind(observer.Creature, token, bind)));
                        binds[bind] = json.RootElement.Clone();
                    }
                }
                using var partial = JsonDocument.Parse(AppliedPartial());
                snapshots.Add(new { name, tab, binds, partial = partial.RootElement.Clone() });
            }

            string AppliedPartial() => ((Dictionary<string, string>)typeof(DMPlayerExamineViewModel).BaseType!
                .GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!)
                [DMPlayerExamineViewModel.PartialView];

            float AppliedNotesWidth()
            {
                using var partial = JsonDocument.Parse(AppliedPartial());
                return partial.RootElement.GetProperty("children")[0].GetProperty("width").GetSingle();
            }
        }
    }
}
