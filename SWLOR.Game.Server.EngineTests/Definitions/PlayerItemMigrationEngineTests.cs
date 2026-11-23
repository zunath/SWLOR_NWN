using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Hum cleanup preserves a remaining equipped saber", Category = "PlayerItemMigration")]
        public static async Task RemainingSaberRetainsHum(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var saber = await ctx.EquipItemAsync(owner, "valcl4", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.AssertEqual(BaseItem.Lightsaber, GetBaseItemType(saber), "Fixture holds a lightsaber");
                for (var index = 0; index < 2; index++)
                    ApplyEffectToObject(DurationType.Permanent,
                        TagEffect(EffectVisualEffect(SWLOR.NWN.API.NWScript.Enum.VisualEffect.VisualEffect.LightsaberHum), "LIGHTSABER_HUM"), owner);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var remove = typeof(Feature.LightsaberAudio).GetMethod("RemoveHum", BindingFlags.Static | BindingFlags.NonPublic);
                remove.Invoke(null, new object[] { owner, OBJECT_INVALID });
                ctx.AssertEqual(1, CountSaberHum(owner), "The remaining equipped saber keeps one hum");
                ctx.AssertEqual(saber, GetItemInSlot(InventorySlot.RightHand, owner), "The event still exposes the removed saber in its old slot");
                ctx.Assert((bool)remove.Invoke(null, new object[] { owner, saber }), "The unequip event removes stale hum synchronously");
                ctx.AssertEqual(0, CountSaberHum(owner), "Removing the last saber clears every hum before saving");
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(owner));
                ctx.AssertEqual(0, CountSaberHum(restored), "Event-time export cannot restore stopped audio");
            });
        }

        private static int CountSaberHum(uint obj)
        {
            var creature = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj).AsNWSCreature();
            var count = 0;
            foreach (var effect in creature.m_appliedEffects)
                if (effect.m_sCustomTag.ToString() == "LIGHTSABER_HUM") count++;
            return count;
        }

        [EngineTest("Stopped lightsaber hum is absent from the character checkpoint", Category = "PlayerItemMigration")]
        public static async Task UnequippedSaberAudioIsSavedImmediately(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ApplyEffectToObject(DurationType.Permanent,
                    TagEffect(EffectVisualEffect(SWLOR.NWN.API.NWScript.Enum.VisualEffect.VisualEffect.LightsaberHum), "LIGHTSABER_HUM"), owner);
                ApplyEffectToObject(DurationType.Permanent,
                    TagEffect(EffectVisualEffect(SWLOR.NWN.API.NWScript.Enum.VisualEffect.VisualEffect.LightsaberHum), "LIGHTSABER_HUM"), owner);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Assert(CountSaberHum(owner) >= 2, "The fixture starts with duplicate looping saber audio");
                typeof(Feature.LightsaberAudio).GetMethod("RemoveHum", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner, OBJECT_INVALID });
                ctx.AssertEqual(0, CountSaberHum(owner), "Stopping saber audio removes every stale effect immediately");
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(owner));
                ctx.AssertEqual(0, CountSaberHum(restored), "A same-script save and reload cannot restore unequipped saber audio");
            });
        }

        [EngineTest("Full inventory retains unequipped gear in a recovery bag", Category = "PlayerItemMigration")]
        public static Task CrowdedInventoryRecoversEquipment(EngineTestContext ctx) => VerifyCrowdedInventory(ctx, false);

        [EngineTest("Full inventory uses an existing bag for unequipped gear", Category = "PlayerItemMigration")]
        public static Task CrowdedInventoryUsesExistingBag(EngineTestContext ctx) => VerifyCrowdedInventory(ctx, true);

        private static async Task VerifyCrowdedInventory(EngineTestContext ctx, bool existingBag)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.3f);
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
                native.m_pcItemRepository.m_nWidth = 2;
                native.m_pcItemRepository.m_nHeight = 3;
                native.m_pcItemRepository.m_nBoundary = 6;
                native.m_pcItemRepository.m_bScalable = 0;
                var carried = CreateItemOnObject(existingBag ? "bag_b" : "nw_maletatcivout", owner);
                ctx.AssertEqual(owner, GetItemPossessor(carried), "The fixture fills its owned inventory");
                ctx.Assert((bool)Invoke("PlayerEquipmentStorage", "TryUnequip", owner, weapon, InventorySlot.RightHand),
                    "The live equipment rules can reserve space for the unequipped weapon");
                ctx.Assert(GetItemInSlot(InventorySlot.RightHand, owner) != weapon, "The equipment slot is cleared");
                ctx.AssertEqual(owner, GetItemPossessor(weapon), "The weapon stays owned by the character");
                var bag = GetItemPossessor(weapon, true);
                ctx.AssertEqual("bag_b", GetResRef(bag), "The weapon is stored in a carried bag");
                ctx.AssertEqual(owner, GetItemPossessor(bag), "The bag stays in the character inventory");
                ctx.AssertEqual(owner, GetItemPossessor(carried), "The original carried item is retained");
                if (existingBag)
                    ctx.AssertEqual(carried, bag, "An existing bag is reused without awarding another");
                else
                    ctx.AssertEqual(bag, GetItemPossessor(carried, true), "The displaced carried item shares the recovery bag");
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(owner));
                ctx.AssertEqual(existingBag ? 2 : 3, Item.GetInventoryItemCount(restored), "The saved file retains all carried items and the recovered weapon");
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(owner, GetItemPossessor(weapon), "A later engine update must not drop the weapon");
        }

    }
}
