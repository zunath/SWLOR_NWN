using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class ConnectedHelmetEngineTests
{
    private static string ProbeDirectory => Environment.GetEnvironmentVariable("SWLOR_HELMET_PROBE_DIRECTORY");
    private static uint _player = OBJECT_INVALID;

    [NWNEventHandler(ScriptName.OnModuleNuiEvent)]
    public static void ObserveEditorEvent()
    {
        if (string.IsNullOrWhiteSpace(ProbeDirectory) || NuiGetEventPlayer() != _player)
            return;
        var detail = $"NUI {NuiGetEventType()} {NuiGetEventElement()} {NuiGetEventArrayIndex()}";
        DelayCommand(0.1f, () => Snapshot(detail));
    }

    [EngineTest("Connected helmet editor reproduction", Category = "ConnectedHelmet", TimeoutSeconds = 3600f)]
    public static async Task RunConnectedEditor(EngineTestContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ProbeDirectory))
            ctx.Skip("Requires an explicitly configured disposable connected-client probe.");
        Directory.CreateDirectory(ProbeDirectory);
        File.WriteAllText(Path.Combine(ProbeDirectory, "ready.json"), "{\"waitingForClient\":true}");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetFirstPC()), 1800f, "a real client to enter the test server");
        _player = GetFirstPC();
        await ctx.DelaySecondsAsync(5f);
        await ctx.ExecuteInCreatureContextAsync(_player, () => JumpToLocation(ctx.GetArenaLocation()));
        await ctx.DelaySecondsAsync(3f);
        var helmet = await ctx.EquipItemAsync(_player, "advent_helmet", InventorySlot.Head);
        await Setup(AppearanceType.Human, Gender.Female);
        var request = Path.Combine(ProbeDirectory, "request.json");
        while (GetIsObjectValid(_player))
        {
            if (File.Exists(request))
            {
                var action = JObject.Parse(File.ReadAllText(request));
                File.Delete(request);
                var operation = (string)action["op"];
                if (operation == "stop") break;
                if (operation == "setup")
                    await Setup((AppearanceType)(int)action["race"], (Gender)(int)action["gender"]);
                if (operation == "camera")
                    AssignCommand(_player, () => SetCameraFacing(90f, (float)action["distance"], (float)action["pitch"]));
                if (operation == "editor")
                {
                    var editor = (AppearanceEditorViewModel)Gui.GetPlayerWindow(_player, GuiWindowType.AppearanceEditor).ViewModel;
                    editor.Geometry = new GuiRectangle(0, 0, 600, 680);
                    editor.OnSelectEquipment()();
                    editor.SelectedItemTypeIndex = 1;
                    await ctx.DelaySecondsAsync(1f);
                }
                Snapshot("request:" + (string)action["id"]);
                File.WriteAllText(Path.Combine(ProbeDirectory, "response.json"), action.ToString(Formatting.None));
            }
            await ctx.DelaySecondsAsync(0.1f);
        }
        _player = OBJECT_INVALID;
        ctx.SetResultDetail("Connected editor probe completed; rendering results are recorded separately.");

        async Task Setup(AppearanceType race, Gender gender)
        {
            await ctx.ExecuteInCreatureContextAsync(_player, () =>
            {
                foreach (var type in Enum.GetValues<GuiWindowType>())
                    Gui.ClosePlayerWindow(_player, type);
                var record = DB.Get<Player>(GetObjectUUID(_player));
                record.RebuildComplete = true;
                record.Settings.ShowHelmet = true;
                DB.Set(record);
                ArmorDisplay.Apply(_player, false, true);
                SetCreatureAppearanceType(_player, race);
                SetGender(_player, gender);
                RacialAppearanceRegistry.TryGet(race, out var definition);
                SetCreatureBodyPart(CreaturePart.Head,
                    (gender == Gender.Female ? definition.FemaleHeads : definition.MaleHeads).First(), _player);
                EquippedItemAppearance.Set(helmet, ItemAppearanceType.SimpleModel, -1, race == AppearanceType.Droid ? 0 : 63);
                ArmorDisplay.Apply(_player, true, true);
                SetFacing(270f);
                AssignCommand(_player, () => SetCameraFacing(90f, 4f, 65f));
                Gui.TogglePlayerWindow(_player, GuiWindowType.AppearanceEditor, new AppearanceEditorPayload(_player));
            });
            await ctx.DelaySecondsAsync(3f);
            var editor = (AppearanceEditorViewModel)Gui.GetPlayerWindow(_player, GuiWindowType.AppearanceEditor).ViewModel;
            editor.OnSelectEquipment()();
            editor.SelectedItemTypeIndex = 1;
            await ctx.DelaySecondsAsync(1f);
            Snapshot("setup");
        }
    }

    private static void Snapshot(string reason)
    {
        if (!GetIsObjectValid(_player)) return;
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(_player);
        var helmet = GetItemInSlot(InventorySlot.Head, _player);
        var editor = (AppearanceEditorViewModel)Gui.GetPlayerWindow(_player, GuiWindowType.AppearanceEditor).ViewModel;
        var state = new
        {
            reason, time = DateTime.UtcNow, player = _player, helmet,
            race = GetAppearanceType(_player).ToString(), gender = GetGender(_player).ToString(),
            model = GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, -1),
            head = native.m_cAppearance.m_nHeadVariation, canonicalHead = native.m_pStats.m_nHeadVariation,
            headItem = native.m_cAppearance.m_oidHeadItem, hidden = GetHiddenWhenEquipped(helmet),
            marker = GetLocalInt(helmet, "HELMET_RENDER_HEAD_HIDDEN"), shown = HelmetModelRenderer.IsShownByOwner(helmet),
            owner = GetItemPossessor(helmet), equipmentPage = editor.IsEquipmentSelected,
            itemType = editor.SelectedItemTypeIndex, editor.Geometry,
            observers = NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Select(player => new
            {
                id = player.m_nPlayerID,
                head = player.GetLastUpdateObject(_player)?.m_cAppearance.m_nHeadVariation,
                item = player.GetLastUpdateObject(_player)?.m_cAppearance.m_oidHeadItem
            }).ToArray()
        };
        File.AppendAllText(Path.Combine(ProbeDirectory, "trace.jsonl"), JsonConvert.SerializeObject(state) + Environment.NewLine);
    }
}
