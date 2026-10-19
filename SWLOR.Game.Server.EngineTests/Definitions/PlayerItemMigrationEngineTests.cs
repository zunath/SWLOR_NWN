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

        /// <summary>
        /// A character whose inventory has no room keeps its unequipped gear: the upgrade moves it to overflow storage
        /// and the saved character holds exactly the items it carried before.
        /// </summary>
        [EngineTest("Full inventory sends unequipped gear to overflow storage", Category = "PlayerItemMigration", TimeoutSeconds = 120)]
        public static async Task CrowdedInventoryOverflowsEquipment(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var owner = fixture.Creature;
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            var fillers = 0;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // A 2x3 outfit fills six cells; sixty of them cover the 10x36 inventory a character file holds.
                for (; fillers < 60; fillers++)
                {
                    var carried = CreateItemOnObject("nw_maletatcivout", owner);
                    if (!GetIsObjectValid(carried) || GetItemPossessor(carried) != owner)
                        break;
                }
            });
            ctx.AssertEqual(60, fillers, "The fixture fills its whole inventory");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var carriedBefore = Item.GetInventoryItemCount(owner);
                var worn = Enumerable.Range(0, NumberOfInventorySlots)
                    .Select(slot => (InventorySlot)slot)
                    .Where(slot => slot != InventorySlot.CreatureArmor && slot != InventorySlot.CreatureBite &&
                                   slot != InventorySlot.CreatureLeft && slot != InventorySlot.CreatureRight)
                    .Count(slot => GetIsObjectValid(GetItemInSlot(slot, owner)));
                var upgraded = SavedCharacterMigration.Run(owner, DB.Get<Player>(fixture.Id), "_6_RecalculateStats");
                ctx.AssertEqual(worn, upgraded.Overflow.Count, "Every worn item that fits nowhere goes to overflow storage");
                ctx.Assert(upgraded.Overflow.Any(item => item.ResRef == "nw_wswls001"), "Overflow storage keeps the unequipped weapon");
                var character = SavedCharacterMigration.Load(ctx, upgraded);
                ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, character)), "The equipment slot is cleared");
                ctx.AssertEqual(carriedBefore, Item.GetInventoryItemCount(character), "The saved character keeps every item it carried");
                ctx.AssertEqual(0, SavedCharacterMigration.FindItems(character, "nw_wswls001").Count,
                    "The weapon is not duplicated into the character file");
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(owner, GetItemPossessor(weapon), "The live fixture is not changed by the saved upgrade");
        }

        [EngineTest("Sequential saber property conversion survives engine updates", Category = "PlayerItemMigration")]
        public static async Task SequentialSaberPropertiesSurviveUpdates(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            // Only DM-built sabers are recalibrated; NPC-only blueprints such as valcl4 keep their authored stats.
            var item = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(item, BaseItem.Lightsaber);
                ClearProperties(item);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, 20), item);
                AddLegacyProperty(item, ItemPropertyType.UseLimitationPerk, 18, 33, 5);
            });
            await ctx.WaitFrameAsync();
            uint migrated = OBJECT_INVALID;
            string expected = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Assert((bool)Invoke("LegacySaberMigration", "IsLegacySaber", item), "The fixture is a DM-built legacy saber");
                var upgraded = SavedCharacterMigration.Run(owner, new Player(GetObjectUUID(owner)),
                    "_13_UpdateItemRequirements", "_14_MigrateResistanceItemProperties", "_15_RemoveObsoleteCombatInstructionDiscs");
                migrated = SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, upgraded), "b_longsword");
                ctx.AssertEqual(21, PropertyValue(migrated, ItemPropertyType.DMG), "Tier 5 saber damage is present");
                ctx.AssertEqual(40, PropertyValue(migrated, ItemPropertyType.RequiresSkill), "Tier 5 skill requirement is present");
                expected = DescribeProperties(migrated);
                ctx.Log("Immediate saber properties: " + expected);
            });
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Log("Settled saber properties: " + DescribeProperties(migrated));
                ctx.AssertEqual(expected, DescribeProperties(migrated), "Later engine updates must retain every replacement property");
            });
        }

        /// <summary>Checks the saved character excludes the items and properties the original player migration removed.</summary>
        [EngineTest("Legacy player items are removed from the saved character", Category = "PlayerItemMigration")]
        public static async Task LegacyPlayerItemsAreSavedImmediately(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await CreateItemAsync(ctx, owner, "xp_tome_1", owner);
            await CreateItemAsync(ctx, owner, "refund_tome", owner);
            var retained = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), retained));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var wornBefore = Enumerable.Range(0, NumberOfInventorySlots).Count(slot => GetIsObjectValid(GetItemInSlot((InventorySlot)slot, owner)));
                var upgraded = SavedCharacterMigration.Run(owner, new Player(GetObjectUUID(owner)), "_1_LegacyPlayerMigration");
                var restored = SavedCharacterMigration.Load(ctx, upgraded);
                // The migration also takes the character's worn gear off, into the inventory.
                var carried = SavedCharacterMigration.Carried(restored);
                ctx.AssertEqual(1 + wornBefore, carried.Count, "The saved file excludes both retired items and carries the retained item and the unequipped gear");
                ctx.AssertEqual(0, carried.Count(item => GetResRef(item) is "xp_tome_1" or "refund_tome"), "The retired items are gone");
                var item = SavedCharacterMigration.FindItem(restored, "b_longsword");
                ctx.Assert(GetIsObjectValid(item), "The retained item survives");
                ctx.Assert(!GetIsItemPropertyValid(GetFirstItemProperty(item)), "No wiped property survives the save");
            });
        }

        /// <summary>
        /// The login migration wiped every item its creature inventory loop returned, and that loop also returns the items
        /// inside carried bags. The saved-data migration does the same.
        /// </summary>
        [EngineTest("Legacy migration wipes items inside carried bags, which the login migration's inventory loop reached", Category = "PlayerItemMigration")]
        public static async Task LegacyMigrationReachesBagContents(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            var nested = await CreateItemAsync(ctx, owner, "b_longsword", bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), nested);
                SetLocalString(nested, "LEGACY_FIXTURE", "nested");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var reached = false;
                for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                    reached |= item == nested;
                ctx.Assert(reached, "The engine's inventory loop on a creature returns the item inside its bag");

                var upgraded = SavedCharacterMigration.Run(owner, new Player(GetObjectUUID(owner)), "_1_LegacyPlayerMigration");
                var restoredBag = SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, upgraded), "bag_sm");
                var restored = GetFirstItemInInventory(restoredBag);
                ctx.AssertEqual("b_longsword", GetResRef(restored), "The item stays in its bag");
                ctx.Assert(!GetIsItemPropertyValid(GetFirstItemProperty(restored)), "Its properties are wiped, as the login migration did");
                ctx.AssertEqual(string.Empty, GetLocalString(restored, "LEGACY_FIXTURE"), "Its locals are wiped, as the login migration did");
            });
        }

        /// <summary>Checks historical damage conversion neither keeps the old properties nor increments damage twice.</summary>
        [EngineTest("Historical weapon damage conversion survives save and retry", Category = "PlayerItemMigration")]
        public static async Task HistoricalWeaponDamageSaveAndRetry(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            var item = await CreateItemAsync(ctx, owner, "tit_rifle", bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(item);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, 12), item);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var upgraded = SavedCharacterMigration.Run(owner, new Player(GetObjectUUID(owner)), "_8_UpgradeWeapons");
                var restoredBag = SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, upgraded), "bag_sm");
                var restored = GetFirstItemInInventory(restoredBag);
                ctx.AssertEqual(15, PropertyValue(restored, ItemPropertyType.DMG), "The historical +3 adjustment reaches weapons in bags and is saved");
                var saved = DescribeProperties(restored);
                var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_8_UpgradeWeapons");
                var retriedItem = GetFirstItemInInventory(SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, retried), "bag_sm"));
                ctx.AssertEqual(saved, DescribeProperties(retriedItem), "Retry after a later failure must not add damage again");
            });
        }
    }
}
