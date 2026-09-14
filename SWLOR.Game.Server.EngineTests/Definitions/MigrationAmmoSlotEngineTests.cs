using System.Buffers.Binary;
using System.Text;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        /// <summary>
        /// Verifies that ammunition migration preserves the equipped slot after base normalization.
        /// </summary>
        [EngineTest("Ammunition migration preserves the equipped slot after base normalization", Category = "MigrationAmmoSlot")]
        public static Task MigrateEquippedLegacyAmmunition(EngineTestContext ctx) =>
            VerifyLegacyAmmoMigration(ctx, false, BaseItem.Bullet);

        /// <summary>
        /// Verifies that ammunition migration preserves displaced canonical ammunition.
        /// </summary>
        [EngineTest("Ammunition migration preserves displaced canonical ammunition", Category = "MigrationAmmoSlot")]
        public static Task MigrateIntoOccupiedAmmunitionSlot(EngineTestContext ctx) =>
            VerifyLegacyAmmoMigration(ctx, true, BaseItem.Bullet);

        /// <summary>
        /// Verifies that ammunition migration converts an unnormalized equipped arrow stack.
        /// </summary>
        [EngineTest("Ammunition migration converts an unnormalized equipped arrow stack", Category = "MigrationAmmoSlot")]
        public static Task MigrateUnnormalizedAmmunition(EngineTestContext ctx) =>
            VerifyLegacyAmmoMigration(ctx, false, BaseItem.Arrow);

        /// <summary>
        /// Verifies that ammunition migration stows saved ammunition when the weapon supplies bullets.
        /// </summary>
        [EngineTest("Ammunition migration stows saved ammunition when the weapon supplies bullets", Category = "MigrationAmmoSlot")]
        public static async Task PreserveAmmunitionAlongsideGeneratedBullets(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(creature, "blaster_bullets", InventorySlot.Bullets);
            var weapon = await ctx.EquipItemAsync(creature, "b_longsword", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                // Save the property on a melee fixture, then restore the old
                // ranged base in the GFF so native loading creates the conflict.
                AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), weapon);
                creature = Deserialize(ctx, WithLegacyAmmoSlot(ObjectPlugin.Serialize(creature), true));
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                var ammo = GetItemInSlot(InventorySlot.Arrows, creature);
                var quantity = GetItemStackSize(ammo);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
                ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped, "Fixture weapon reserves the canonical ammunition slot");
                ctx.Assert(GetIsObjectValid(ammo), "Fixture also carries real legacy ammunition");
                ctx.AssertEqual(true, (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", creature), "The saved stack is migrated");
                ctx.AssertEqual(BaseItem.Bullet, GetBaseItemType(ammo), "Saved ammunition is normalized");
                ctx.AssertEqual(creature, GetItemPossessor(ammo), "The saved stack remains owned by the creature");
                ctx.AssertEqual(quantity, GetItemStackSize(ammo), "The complete ammunition stack survives");
                ctx.Assert(native.m_pcItemRepository.GetItemInRepository(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(ammo).AsNWSItem()) != 0,
                    "Saved ammunition moves to inventory while the weapon supplies equipped ammunition");
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(creature));
                ctx.AssertEqual(false, (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", restored), "Retry preserves the stowed stack");
            });
        }

        /// <summary>
        /// Verifies that ammunition migration preserves unlimited-ammunition weapons and inventory stacks.
        /// </summary>
        [EngineTest("Ammunition migration preserves unlimited-ammunition weapons and inventory stacks", Category = "MigrationAmmoSlot")]
        public static async Task PreserveUnequippedNpcAmmunition(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var weapon = await ctx.EquipItemAsync(creature, "exchangepistol", InventorySlot.RightHand);
            var ammo = await CreateItemAsync(ctx, creature, "blaster_bullets", creature);
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ItemPlugin.SetBaseItemType(weapon, BaseItem.Pistol);
                ItemPlugin.SetBaseItemType(ammo, BaseItem.Arrow);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
                ctx.AssertEqual(1, native.m_bMagicalArrowsEquipped,
                    "Fixture weapon produces engine-owned ammunition");
                ctx.Assert(GetItemInSlot(InventorySlot.Arrows, creature) != ammo,
                    "Engine-owned ammunition is distinct from the saved inventory stack");
                var quantity = GetItemStackSize(ammo);
                ctx.AssertEqual(true, (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", creature),
                    "Legacy item bases are converted");
                ctx.AssertEqual(BaseItem.Bullet, GetBaseItemType(ammo), "Inventory ammunition is normalized");
                ctx.Assert(native.m_pInventory.GetItemInSlot(1u << (int)InventorySlot.Bullets)?.m_idSelf != ammo,
                    "Inventory ammunition is not silently equipped");
                ctx.AssertEqual(quantity, GetItemStackSize(ammo), "Inventory ammunition quantity is preserved");
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(creature));
                ctx.AssertEqual(false, (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", restored),
                    "Canonical inventory ammunition requires no equipment change on retry");
            });
        }

        /// <summary>
        /// Checks native ammunition slots and saved quantities before conversion, after conversion, and on retry.
        /// </summary>
        private static async Task VerifyLegacyAmmoMigration(EngineTestContext ctx, bool occupied, BaseItem initialBase)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(creature, "blaster_bullets", InventorySlot.Bullets);
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                // Current equip handlers reject the historical slot. Build that
                // saved state from a synthetic creature instead of relying on them.
                creature = Deserialize(ctx, WithLegacyAmmoSlot(ObjectPlugin.Serialize(creature)));
            });
            await ctx.WaitFrameAsync();
            var existingAmmo = OBJECT_INVALID;
            var existingQuantity = 0;
            if (occupied)
            {
                await ctx.ExecuteInCreatureContextAsync(creature, () =>
                    SetLocalString(GetItemInSlot(InventorySlot.Arrows, creature), "MIGRATION_FIXTURE_STACK", "legacy"));
                existingAmmo = await ctx.EquipItemAsync(creature, "blaster_bullets", InventorySlot.Bullets);
                existingQuantity = GetItemStackSize(existingAmmo);
            }
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                var ammo = GetItemInSlot(InventorySlot.Arrows, creature);
                ctx.Assert(GetIsObjectValid(ammo), "Fixture has saved legacy ammunition");
                // Acquisition normalization can change the base before migration
                // relocates ammunition out of its old equipment slot.
                ItemPlugin.SetBaseItemType(ammo, initialBase);
                var quantity = GetItemStackSize(ammo);
                var changed = (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", creature);
                ctx.Assert(changed, "Moving equipped ammunition must be persisted");
                ctx.AssertEqual(ammo, GetItemInSlot(InventorySlot.Bullets, creature), "Ammunition remains equipped in the canonical slot");
                ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.Arrows, creature)), "The old ammunition slot is cleared");
                ctx.AssertEqual(quantity, GetItemStackSize(ammo), "All ammunition is preserved");
                if (occupied)
                {
                    ctx.AssertEqual(creature, GetItemPossessor(existingAmmo), "Displaced ammunition remains in the inventory");
                    ctx.AssertEqual(existingQuantity, GetItemStackSize(existingAmmo), "Displaced ammunition quantity is preserved");
                }
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(creature));
                ctx.Assert(GetIsObjectValid(GetItemInSlot(InventorySlot.Bullets, restored)), "The saved creature retains equipped ammunition");
                ctx.AssertEqual(false, (bool)Invoke("PistolBaseItemMigration", "MigrateStoredObject", restored), "Retry makes no further slot change");
            });
        }

        /// <summary>
        /// Creates a saved legacy ammunition slot layout without applying current engine equip restrictions.
        /// </summary>
        private static string WithLegacyAmmoSlot(string serialized, bool restoreSlingWeapon = false)
        {
            var data = Convert.FromBase64String(serialized);
            uint Read(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
            var structs = (int)Read(8);
            var fields = (int)Read(16);
            var labels = (int)Read(24);
            var fieldIndices = (int)Read(40);
            var listIndices = (int)Read(48);
            int FindField(int node, string name)
            {
                var first = Read(structs + node * 12 + 4);
                var count = Read(structs + node * 12 + 8);
                for (var i = 0; i < count; i++)
                {
                    var index = count == 1 ? first : Read(fieldIndices + (int)first + i * 4);
                    var field = fields + (int)index * 12;
                    var label = Encoding.ASCII.GetString(data, labels + (int)Read(field + 4) * 16, 16).TrimEnd('\0');
                    if (label == name) return field;
                }
                throw new InvalidOperationException($"Fixture field {name} is missing.");
            }
            var list = listIndices + (int)Read(FindField(0, "Equip_ItemList") + 8);
            var foundAmmo = false;
            for (var i = 0; i < Read(list); i++)
            {
                var node = (int)Read(list + 4 + i * 4);
                if (restoreSlingWeapon && Read(structs + node * 12) == 1u << (int)InventorySlot.RightHand)
                    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(FindField(node, "BaseItem") + 8, 4), (uint)BaseItem.Sling);
                if (Read(structs + node * 12) != 1u << (int)InventorySlot.Bullets) continue;
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(structs + node * 12, 4), 1u << (int)InventorySlot.Arrows);
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(FindField(node, "BaseItem") + 8, 4), (uint)BaseItem.Arrow);
                foundAmmo = true;
            }
            if (foundAmmo) return Convert.ToBase64String(data);
            throw new InvalidOperationException("Fixture has no equipped ammunition.");
        }
    }
}
