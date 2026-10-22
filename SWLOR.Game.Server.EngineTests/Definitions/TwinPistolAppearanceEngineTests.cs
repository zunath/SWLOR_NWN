using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolAppearanceEngineTests
{
    private static readonly AppearanceWeapon[] Parts = { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top };

    [EngineTest("Twin Pistol off hand mirrors the owned pistol through creation, both editor hands and re-equip", Category = "TwinPistolAppearance", TimeoutSeconds = 60f)]
    public static async Task OffHandMirrorsOwnedPistol(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 0);
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        AssertMirrored(ctx, main, spare, "A new pair");

        var template = Gui.GetWindowTemplate(GuiWindowType.AppearanceEditor);
        var playerWindow = template.CreatePlayerWindowAction();
        var editor = (AppearanceEditorViewModel)playerWindow.ViewModel;
        editor.Geometry = new GuiRectangle(25f, 20f, 590f, 660f);
        var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
            .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        ctx.Assert(!windows.ContainsKey(player.Id), "fixture GUI registration must be isolated");
        windows.Add(player.Id, new Dictionary<GuiWindowType, GuiPlayerWindow>
        {
            [GuiWindowType.AppearanceEditor] = playerWindow
        });
        var edits = 0;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                Gui.TogglePlayerWindow(actor, GuiWindowType.AppearanceEditor, new AppearanceEditorPayload(actor)));
            await ctx.WaitFrameAsync();
            ctx.Assert(playerWindow.WindowToken > 0, "the actual production NUI window opens");
            foreach (var (itemType, hand) in new[] { (4, "Off Hand"), (3, "Main Hand") })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    editor.OnSelectEquipment()();
                    editor.SelectedItemTypeIndex = itemType;
                    ctx.Assert(editor.HasItemEquipped, $"{hand} offers the equipped Twin Pistols for customization");
                    SelectMiddleCategory();
                    for (var step = 0; step < 3; step++)
                    {
                        var before = Snapshot(main);
                        editor.OnNextPart()();
                        ctx.Assert(before != Snapshot(main), $"{hand} edit {step + 1} changes the owned Twin Pistols item");
                        ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), $"{hand} edit keeps the owned item equipped");
                        ctx.AssertEqual(spare, GetItemInSlot(InventorySlot.LeftHand, actor), $"{hand} edit keeps the same generated off hand");
                        AssertMirrored(ctx, main, spare, $"{hand} edit {step + 1}");
                        edits++;
                    }
                });
            }
        }
        finally
        {
            Gui.ClosePlayerWindow(actor, GuiWindowType.AppearanceEditor);
            await ctx.WaitFrameAsync();
            windows.Remove(player.Id);
        }

        var customized = Snapshot(main);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            ActionUnequipItem(main);
        });
        await TwinPistolEquipmentEngineTests.WaitForRemovedPair(ctx, actor, main, spare);
        await TwinPistolEquipmentEngineTests.EquipExisting(ctx, actor, main, InventorySlot.RightHand);
        var rebuilt = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        ctx.Assert(rebuilt != spare, "Re-equipping generates a new off hand");
        ctx.AssertEqual(customized, Snapshot(main), "Unequipping keeps the owned item's customization");
        AssertMirrored(ctx, main, rebuilt, "A re-equipped customized pair");
        TwinPistolEquipmentEngineTests.AssertOwnedItems(ctx, actor, main, 1);
        ctx.SetResultDetail($"{edits} production editor edits through the Off Hand and Main Hand choices changed one owned Twin Pistols item; its generated off hand matched every model part and color, and a re-equip rebuilt a matching off hand ({customized}).");

        void SelectMiddleCategory()
        {
            var nativeEvent = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetModule().m_cCurrentNuiEvent;
            var previous = (nativeEvent.m_player, nativeEvent.m_token, nativeEvent.m_event,
                nativeEvent.m_element, nativeEvent.m_array_index);
            try
            {
                // Supply only this synchronous public handler's native event context.
                nativeEvent.m_player = actor;
                nativeEvent.m_token = playerWindow.WindowToken;
                nativeEvent.m_event = "click";
                nativeEvent.m_element = "ae_part_category";
                nativeEvent.m_array_index = 1;
                editor.OnSelectPartCategory()();
                ctx.AssertEqual(1, editor.SelectedPartCategoryIndex, "the public category action selects Middle");
            }
            finally
            {
                (nativeEvent.m_player, nativeEvent.m_token, nativeEvent.m_event,
                    nativeEvent.m_element, nativeEvent.m_array_index) = previous;
            }
        }
    }

    private static string Snapshot(uint item)
    {
        var values = new List<string>();
        foreach (var part in Parts)
            values.Add($"{part}={GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)part)}/" +
                       $"{GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)part)}");
        return string.Join(",", values);
    }

    private static void AssertMirrored(EngineTestContext ctx, uint main, uint spare, string label) =>
        ctx.AssertEqual(Snapshot(main), Snapshot(spare), $"{label}: the off hand shows the owned pistol's model parts and colors");
}
