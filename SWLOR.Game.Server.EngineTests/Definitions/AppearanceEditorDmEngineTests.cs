using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectVisualTransform = SWLOR.NWN.API.NWScript.Enum.ObjectVisualTransform;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class AppearanceEditorDmEngineTests
    {
        [EngineTest("DM appearance editor exposes NPC scales and exports native NUI state", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static async Task DmCanEditNpcScaleThroughSettings(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx);
            var npc = ctx.SpawnCreature("civilian", 4f);
            await ctx.DelaySecondsAsync(1f);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var client = server.GetClientObjectByObjectId(dm.Creature);
            var playerInfo = server.GetNetLayer().GetPlayerInfo(client.m_nPlayerID);
            ctx.Assert(playerInfo != null && playerInfo.Pointer != IntPtr.Zero, "fixture has a native net layer slot");
            var originalDmPrivileges = playerInfo.m_bGameMasterPrivileges;
            var originalPlayerDmLogin = playerInfo.m_bGameMasterIsPlayerLogin;
            var nativeDm = server.GetCreatureByGameObjectID(dm.Creature);
            var nativeNpc = server.GetCreatureByGameObjectID(npc);
            var originalCharacterType = client.m_nCharacterType;
            var originalDmFile = nativeDm.m_pStats.m_bIsDMCharacterFile;
            var originalAssociate = nativeNpc.m_nAssociateType;
            var originalMaster = nativeNpc.m_oidMaster;
            var snapshots = new JArray();
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            ctx.Assert(!windows.ContainsKey(dm.Id), "DM fixture has no existing GUI registration");
            var template = Gui.GetWindowTemplate(GuiWindowType.AppearanceEditor);
            ctx.AssertEqual(0, template.LayoutFindings.Count, "Appearance editor has no boot layout warnings");
            var window = template.CreatePlayerWindowAction();
            var windowTarget = dm.Creature;
            window.ViewModel.Geometry = new GuiRectangle(0, 0, 590f, 740f);
            windows.Add(dm.Id, new Dictionary<GuiWindowType, GuiPlayerWindow>
            {
                [GuiWindowType.AppearanceEditor] = window
            });
            try
            {
                // NWNX's CharacterType::DM is 2. These fields install an actual native DM
                // identity on this disposable headless client, rather than mocking GetIsDM.
                client.m_nCharacterType = 2;
                nativeDm.m_pStats.m_bIsDMCharacterFile = 1;
                playerInfo.m_bGameMasterPrivileges = 1;
                playerInfo.m_bGameMasterIsPlayerLogin = 0;
                ctx.Assert(GetIsDM(dm.Creature), "native GetIsDM recognizes the fixture");
                var npcId = GetObjectUUID(npc);
                ctx.Assert(!DB.Exists<Player>(npcId), "NPC has no player record before editing");
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                {
                    Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.AppearanceEditor, new AppearanceEditorPayload(npc));
                });
                await ctx.WaitFrameAsync();
                var editor = (AppearanceEditorViewModel)window.ViewModel;
                ctx.Assert(window.WindowToken > 0, "DM opens a real native NUI window");
                ctx.Assert(editor.IsSettingsVisible, "DM sees the Settings tab for an NPC");
                ctx.Assert(!editor.IsPlayerSettingsVisible, "NPC does not expose player preferences");
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, editor.OnSelectSettings());
                await ctx.WaitFrameAsync();
                ctx.Assert(editor.IsSettingsSelected, "DM selects the Settings tab");
                Capture(snapshots, dm.Creature, window, template, "dm-npc-settings");
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                {
                    SetObjectVisualTransform(npc, ObjectVisualTransform.Scale, 1f);
                    HelmetModelRenderer.SetHeadScale(npc, 1f);
                    editor.OnIncreaseHeadScale()();
                    editor.OnDecreaseAppearanceScale()();
                    editor.OnClickSaveSettings()();
                });
                ctx.Assert(Math.Abs(HelmetModelRenderer.GetHeadScale(npc) - 1.01f) < 0.0001f, "head button changes the NPC");
                ctx.Assert(Math.Abs(GetObjectVisualTransform(npc, ObjectVisualTransform.Scale) - 0.99f) < 0.0001f,
                    "height button changes the NPC");
                ctx.Assert(!DB.Exists<Player>(npcId), "NPC edits and Save do not create player data");
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () => Gui.ClosePlayerWindow(dm.Creature, GuiWindowType.AppearanceEditor));

                // Install the native possession identity without moving a creature with a
                // synthetic client between areas. The chat command must route to the DM's cache.
                nativeNpc.m_nAssociateType = 7; // NWNX AssociateType::DMPossess.
                nativeNpc.m_oidMaster = dm.Creature;
                client.SetGameObject(nativeNpc);
                windowTarget = npc;
                ctx.Assert(GetIsDMPossessed(npc), "native GetIsDMPossessed recognizes the NPC");
                await ctx.ExecuteInCreatureContextAsync(npc, () =>
                    new CharacterChatCommand().BuildChatCommands()["customize"].DoAction(npc, OBJECT_INVALID, null));
                await ctx.WaitFrameAsync();
                ctx.Assert(window.WindowToken > 0, "possessed NPC opens the real editor through /customize");
                ctx.Assert(editor.IsSettingsVisible, "possessed NPC has the Settings tab");
                await ctx.ExecuteInCreatureContextAsync(npc, editor.OnSelectSettings());
                await ctx.WaitFrameAsync();
                Capture(snapshots, npc, window, template, "possessed-npc-settings");
                await ctx.ExecuteInCreatureContextAsync(npc, () =>
                {
                    editor.OnDecreaseHeadScale()();
                    editor.OnIncreaseAppearanceScale()();
                    editor.OnClickSaveSettings()();
                    editor.OnCloseWindow()();
                });
                ctx.Assert(Math.Abs(HelmetModelRenderer.GetHeadScale(npc) - 1f) < 0.0001f, "possessed NPC head adjustment survives close");
                ctx.Assert(Math.Abs(GetObjectVisualTransform(npc, ObjectVisualTransform.Scale) - 1f) < 0.0001f,
                    "possessed NPC height adjustment survives close");
                ctx.Assert(!DB.Exists<Player>(npcId), "possessed NPC still has no player data");
                await ctx.ExecuteInCreatureContextAsync(npc, () => Gui.ClosePlayerWindow(dm.Creature, GuiWindowType.AppearanceEditor, npc));

                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "appearance-editor-native-states.json"), snapshots.ToString());
                ctx.SetResultDetail("Native DM and possession predicates, /customize routing, NUI tab selection, both scale handlers, close and no NPC player record; exported real NUI layout and binds for isolated client capture.");
            }
            finally
            {
                if (window.WindowToken > 0)
                    NuiDestroy(windowTarget, window.WindowToken);
                client.SetGameObject(nativeDm);
                nativeNpc.m_nAssociateType = originalAssociate;
                nativeNpc.m_oidMaster = originalMaster;
                client.m_nCharacterType = originalCharacterType;
                nativeDm.m_pStats.m_bIsDMCharacterFile = originalDmFile;
                playerInfo.m_bGameMasterPrivileges = originalDmPrivileges;
                playerInfo.m_bGameMasterIsPlayerLogin = originalPlayerDmLogin;
                windows.Remove(dm.Id);
            }
        }

        private static void Capture(JArray snapshots, uint target, GuiPlayerWindow window, GuiConstructedWindow template, string name)
        {
            var baseType = window.ViewModel.GetType().BaseType;
            var rootPartial = (string)baseType.GetField("_rootPartial", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window.ViewModel);
            var groups = (Dictionary<string, string>)baseType.GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window.ViewModel);
            var binds = new JObject();
            foreach (var watched in new[] { false, true })
            {
                for (var index = 0; ; index++)
                {
                    var bind = NuiGetNthBind(target, window.WindowToken, watched, index);
                    if (string.IsNullOrEmpty(bind)) break;
                    binds[bind] = JToken.Parse(JsonDump(NuiGetBind(target, window.WindowToken, bind)));
                }
            }
            snapshots.Add(new JObject
            {
                ["name"] = name,
                ["window"] = JObject.Parse(JsonDump(template.Window)),
                ["root"] = JObject.Parse(GuiLayoutComposer.Compose(template.PartialViewLayouts[rootPartial], groups)),
                ["binds"] = binds
            });
        }
    }
}
