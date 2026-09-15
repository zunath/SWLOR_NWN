using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class LightsaberWorkbenchEngineTests
    {
        [EngineTest("Workbench selection survives close and reset without removing inventory inputs", Category = "LightsaberWorkbench")]
        public static async Task SelectionDoesNotConsumeInputs(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var kit = CreateKit(owner);
                var token = CreateItemOnObject("wpn_sub_token", owner);
                var model = Bind(owner);
                ctx.Assert((bool)Call(model, "SelectEnhancement", 0, kit), "An owned valid kit is selected");
                ctx.Assert(!(bool)Call(model, "SelectEnhancement", 1, kit), "One physical kit cannot fill two slots");
                Set(model, "_submissionItem", token);
                Set(model, "_submissionId", GetObjectUUID(token));
                ctx.Assert((bool)Call(model, "ValidateSelectedInputs"), "Both inputs are valid before construction");
                model.OnWindowClosed()();
                ctx.AssertEqual(owner, GetItemPossessor(kit), "Closing retains the kit");
                ctx.AssertEqual(owner, GetItemPossessor(token), "Closing retains the submission token");
                ctx.Assert((bool)Call(model, "SelectEnhancement", 0, kit), "Kit can be selected again");
                Call(model, "Initialize", new object[] { null });
                ctx.AssertEqual(owner, GetItemPossessor(kit), "Reset without close retains the kit");
                ctx.AssertEqual(owner, GetItemPossessor(token), "Reset without close retains the token");
                var restored = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(owner));
                ctx.Track(restored);
                ctx.AssertEqual(Item.GetInventoryItemCount(owner), Item.GetInventoryItemCount(restored), "A character snapshot retains every selected input");
                Call(model, "SelectEnhancement", 0, kit);
                var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
                ctx.Assert(ObjectPlugin.AcquireItem(storage, kit), "Fixture moves the selected kit out of the inventory");
                ctx.Track(kit);
                ctx.Assert(!(bool)Call(model, "ValidateSelectedInputs"), "Traded or moved inputs cannot be used");
            });
        }

        [EngineTest("Workbench failed output transfer preserves inputs and successful retry consumes them once", Category = "LightsaberWorkbench")]
        public static async Task ConstructionConsumesOnlyAfterAcquisition(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var playerId = GetObjectUUID(owner);
                var player = new Player(playerId);
                player.Currencies[CurrencyType.KyberToken] = 1;
                DB.Set(player);
                try
                {
                    var kit = CreateKit(owner);
                    var token = CreateItemOnObject("wpn_sub_token", owner);
                    var model = Bind(owner);
                    Call(model, "SelectEnhancement", 0, kit);
                    Set(model, "_submissionItem", token);
                    Set(model, "_submissionId", GetObjectUUID(token));
                    var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature().m_pcItemRepository;
                    var width = repository.m_nWidth;
                    var height = repository.m_nHeight;
                    var boundary = repository.m_nBoundary;
                    var scalable = repository.m_bScalable;
                    try
                    {
                        repository.m_nWidth = 0;
                        repository.m_nHeight = 0;
                        repository.m_nBoundary = 0;
                        repository.m_bScalable = 0;
                        Call(model, "ConstructSaber");
                        ctx.AssertEqual(1, Currency.GetCurrency(owner, CurrencyType.KyberToken), "A failed transfer keeps currency");
                        ctx.AssertEqual(owner, GetItemPossessor(kit), "A failed transfer keeps the enhancement");
                        ctx.AssertEqual(owner, GetItemPossessor(token), "A failed transfer keeps the submission token");
                    }
                    finally
                    {
                        repository.m_nWidth = width;
                        repository.m_nHeight = height;
                        repository.m_nBoundary = boundary;
                        repository.m_bScalable = scalable;
                    }
                    Call(model, "ConstructSaber");
                    ctx.AssertEqual(0, Currency.GetCurrency(owner, CurrencyType.KyberToken), "Successful retry consumes one Kyber Token");
                    ctx.AssertEqual(OBJECT_INVALID, ((uint[])Get(model, "_enhancementItems"))[0], "Consumed enhancement selection is cleared");
                    ctx.AssertEqual(OBJECT_INVALID, (uint)Get(model, "_submissionItem"), "Consumed submission selection is cleared");
                    var count = Item.GetInventoryItemCount(owner);
                    Call(model, "ConstructSaber");
                    ctx.AssertEqual(count, Item.GetInventoryItemCount(owner), "Retry without currency cannot create a second saber");
                }
                finally { DB.Delete<Player>(playerId); }
            });
        }

        private static uint CreateKit(uint owner)
        {
            var kit = CreateItemOnObject("nw_it_medkit001", owner);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.Accuracy, 5), kit);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.EnhancementLevel, 0, 1), kit);
            return kit;
        }

        private static LightsaberWorkbenchViewModel Bind(uint owner)
        {
            var model = new LightsaberWorkbenchViewModel();
            typeof(GuiViewModelBase<LightsaberWorkbenchViewModel, GuiPayloadBase>)
                .GetProperty("Player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, owner);
            Call(model, "Initialize", new object[] { null });
            return model;
        }

        private static object Call(LightsaberWorkbenchViewModel model, string method, params object[] args) =>
            typeof(LightsaberWorkbenchViewModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, args);
        private static object Get(LightsaberWorkbenchViewModel model, string field) =>
            typeof(LightsaberWorkbenchViewModel).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
        private static void Set(LightsaberWorkbenchViewModel model, string field, object value) =>
            typeof(LightsaberWorkbenchViewModel).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, value);
    }
}
