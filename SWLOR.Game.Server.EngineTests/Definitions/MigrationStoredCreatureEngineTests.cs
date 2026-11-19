using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        /// <summary>
        /// Verifies that saved equipment survives native unequipping without confusing identical items.
        /// </summary>
        [EngineTest("Saved equipment survives native unequipping without confusing identical items", Category = "MigrationStoredCreature")]
        public static async Task PreserveSavedEquipmentSlots(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.5f);
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            var spare = await CreateItemAsync(ctx, owner, "nw_wswls001", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetName(weapon, "Equipped fixture weapon");
                SetName(spare, "Stowed fixture weapon");
                var saved = ObjectPlugin.Serialize(owner);
                var document = Invoke("StoredObjectData", "ReadCreature", saved);
                // Reflection does not fill optional parameters: no temporary appearance, no replaced items.
                var prepared = (string)document.GetType().GetMethod("PrepareForNativeLoad")
                    .Invoke(document, new object[] { null, null });
                var loaded = Deserialize(ctx, prepared);
                var equipped = GetItemInSlot(InventorySlot.RightHand, loaded);
                ctx.Assert(GetIsObjectValid(equipped), "The fixture starts equipped");
                CreaturePlugin.RunUnequip(loaded, equipped);
                ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, loaded)), "Reproduce native unequipping");
                SetLocalInt(equipped, "MIGRATION_TEST_VALUE", 7);
                var migrated = (string)document.GetType().GetMethod("CopyMigratedInventory")
                    .Invoke(document, new object[] { ObjectPlugin.Serialize(loaded) });
                ctx.Assert(!Encoding.UTF8.GetString(Convert.FromBase64String(migrated)).Contains("MIGRATION_EQUIPMENT_"),
                    "Temporary tracking locals never enter persisted data");
                var restored = Deserialize(ctx, migrated);
                var restoredWeapon = GetItemInSlot(InventorySlot.RightHand, restored);
                ctx.AssertEqual("Equipped fixture weapon", GetName(restoredWeapon), "The original equipped copy returns to its saved slot");
                ctx.AssertEqual(7, GetLocalInt(restoredWeapon, "MIGRATION_TEST_VALUE"), "Migrated item changes survive slot restoration");
                var spareCount = 0;
                for (var item = GetFirstItemInInventory(restored); GetIsObjectValid(item); item = GetNextItemInInventory(restored))
                    if (GetName(item) == "Stowed fixture weapon") spareCount++;
                ctx.AssertEqual(1, spareCount, "The identical-resref spare remains in inventory");
            });
        }

        /// <summary>
        /// Verifies that stored inventory replacement does not restore removed equipment effects.
        /// </summary>
        [EngineTest("Stored inventory replacement does not restore removed equipment effects", Category = "MigrationStoredCreature")]
        public static async Task StoredEquipmentEffects(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.5f);
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            var originalMight = GetAbilityScore(owner, AbilityType.Might);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ApplyEffectToObject(DurationType.Permanent, EffectAbilityIncrease(AbilityType.Might, 2), owner);
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), weapon);
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(originalMight + 6, GetAbilityScore(owner, AbilityType.Might), "Both fixture effects apply");
            uint restored = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var document = Invoke("StoredObjectData", "ReadCreature", ObjectPlugin.Serialize(owner));
                for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                {
                    if (GetItemPropertyType(property) != ItemPropertyType.AbilityBonus) continue;
                    Invoke("MigrationObject", "RemoveProperty", weapon, property);
                    break;
                }
                var saved = (string)document.GetType().GetMethod("CopyMigratedInventory")
                    .Invoke(document, new object[] { ObjectPlugin.Serialize(owner) });
                restored = Deserialize(ctx, saved);
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(originalMight + 2, GetAbilityScore(restored, AbilityType.Might),
                "The standalone effect survives while the removed equipment bonus does not");
        }

    }
}
