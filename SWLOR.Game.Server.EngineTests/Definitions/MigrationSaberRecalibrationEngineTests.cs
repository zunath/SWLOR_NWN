using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Legacy Valor saber recalibrates once and preserves identity", Category = "SaberRecalibration")]
        public static async Task ValorSaberRecalibration(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var saber = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            string savedData = null;
            string savedMigratedData = null;
            string identity = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(saber, BaseItem.Lightsaber);
                SetName(saber, "Valor Custom");
                SetLocalInt(saber, "LIGHTSABER_UPGRADE_COUNT", 1);
                SetLocalInt(saber, "KEEP_LOCAL", 17);
                var originalProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var property = GetFirstItemProperty(saber); GetIsItemPropertyValid(property); property = GetNextItemProperty(saber))
                    originalProperties.Add(property);
                AddLegacyProperty(saber, ItemPropertyType.DMG, (int)CombatDamageType.Physical, 34, 32);
                AddLegacyProperty(saber, ItemPropertyType.DMG, (int)CombatDamageType.Force, 34, 3);
                AddLegacyProperty(saber, ItemPropertyType.DMG, (int)CombatDamageType.Electrical, 34, 3);
                AddLegacyProperty(saber, ItemPropertyType.AccuracyBonus, 0, 2, 10);
                foreach (var property in originalProperties)
                    Invoke("MigrationObject", "RemoveProperty", saber, property);
                AddItemProperty(DurationType.Permanent, ItemPropertyOnHitCastSpell(OnHitCastSpellType.BESTOW_CURSE, 1), saber);
                AddItemProperty(DurationType.Permanent, ItemPropertyLight(LightBrightness.Bright, LightColor.Blue), saber);
                ItemPlugin.SetItemAppearance(saber, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Bottom, 2, false);
                ItemPlugin.SetItemAppearance(saber, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Middle, 1, false);
                ItemPlugin.SetItemAppearance(saber, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Top, 3, false);
                identity = GetObjectUUID(saber);
                ctx.Assert(!string.IsNullOrWhiteSpace(identity), "Valor has a saved native identity");
                savedData = ObjectPlugin.Serialize(saber);
                DestroyObject(saber);
            });
            await ctx.DelaySecondsAsync(0.3f);

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var (changed, migratedData) = MigrateStoredData(savedData);
                ctx.Assert(changed, "Valor requires saber recalibration");
                ctx.Assert(ContainsIdentity(migratedData, identity), "Recalibration preserves the saved identity");
                var migrated = Deserialize(ctx, migratedData);
                ctx.AssertEqual(identity, GetObjectUUID(migrated), "Reloaded Valor retains its identity");
                AssertSaberProfile(ctx, migrated, BaseItem.Lightsaber, tier: 6, damage: 29, accuracy: 10, skill: 50, delay: 24);
                ctx.AssertEqual("Valor Custom", GetName(migrated), "Custom name survives recalibration");
                ctx.AssertEqual(17, GetLocalInt(migrated, "KEEP_LOCAL"), "Unrelated local survives recalibration");
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.WeaponDamageType),
                    "Separate legacy damage types are removed in favor of the bounded single profile");
                ctx.Assert(HasProperty(migrated, ItemPropertyType.Light), "The custom light property survives recalibration");
                ctx.AssertEqual(2, GetItemAppearance(migrated, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Bottom), "Custom saber model survives recalibration");
                ctx.AssertEqual(1, GetItemAppearance(migrated, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Middle), "Custom saber middle model survives recalibration");
                ctx.AssertEqual(3, GetItemAppearance(migrated, ItemAppearanceType.WeaponModel,
                    (int)AppearanceWeapon.Top), "Custom saber top model survives recalibration");
                ctx.AssertEqual((int)OnHitCastSpellType.BESTOW_CURSE,
                    FindPropertySubtype(migrated, ItemPropertyType.OnHitCastSpell),
                    "Unrelated on-hit property survives recalibration");

                savedMigratedData = ObjectPlugin.Serialize(migrated);
                DestroyObject(migrated);
            });
            await ctx.DelaySecondsAsync(0.3f);

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var (changed, retriedData) = MigrateStoredData(savedMigratedData);
                ctx.Assert(!changed, "A recalibrated Valor does not migrate again");
                ctx.AssertEqual(savedMigratedData, retriedData, "Retry leaves the serialized Valor unchanged");
                ctx.Assert(ContainsIdentity(retriedData, identity), "Retry retains Valor's saved identity");
                var retried = Deserialize(ctx, retriedData);
                AssertSaberProfile(ctx, retried, BaseItem.Lightsaber, tier: 6, damage: 29, accuracy: 10, skill: 50, delay: 24);
                ctx.AssertEqual(identity, GetObjectUUID(retried), "Retry reload retains Valor's identity");
                ctx.AssertEqual("Valor Custom", GetName(retried), "Retry preserves custom name");
                ctx.AssertEqual(17, GetLocalInt(retried, "KEEP_LOCAL"), "Retry preserves unrelated local");
            });
        }

        [EngineTest("Previously stripped Chiro saber restores only its evidenced tier delta", Category = "SaberRecalibration")]
        public static async Task PreviouslyStrippedChiroSaber(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var saber = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(saber, BaseItem.Lightsaber);
                RemovePropertiesImmediately(saber);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 21), saber);
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)ItemPropertyAttackDelay.Delay240), saber);
                SetLocalInt(saber, "SABER_TIER", 5);
                SetLocalInt(saber, "LIGHTSABER_UPGRADE_COUNT", 1);
                saber = Deserialize(ctx, ObjectPlugin.Serialize(saber));
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrated = MigrateSerialized(ctx, saber);
                AssertSaberProfile(ctx, migrated, BaseItem.Lightsaber, tier: 6, damage: 24, accuracy: -1, skill: 50, delay: 24);
                var retried = MigrateSerialized(ctx, migrated);
                AssertSaberProfile(ctx, retried, BaseItem.Lightsaber, tier: 6, damage: 24, accuracy: -1, skill: 50, delay: 24);
            });
        }

        [EngineTest("Legacy saberstaff Chiro recalibration uses staff baseline and bounded bonuses", Category = "SaberRecalibration")]
        public static async Task SaberstaffChiroRecalibration(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var saber = await CreateItemAsync(ctx, owner, "b_staff", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(saber, BaseItem.Saberstaff);
                RemovePropertiesImmediately(saber);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 100), saber);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 5), saber);
                SetLocalInt(saber, "LIGHTSABER_UPGRADE_COUNT", 1);
                saber = Deserialize(ctx, ObjectPlugin.Serialize(saber));
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrated = MigrateSerialized(ctx, saber);
                AssertSaberProfile(ctx, migrated, BaseItem.Saberstaff, tier: 6, damage: 33, accuracy: 5, skill: 50, delay: 24);
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.WeaponDamageType),
                    "Separate legacy damage types are removed in favor of the single saber profile");
            });
        }

        [EngineTest("Saved legacy saber damage bonus survives native loading", Category = "SaberRecalibration")]
        public static async Task FlatDamageBonusConversion(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var saber = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(saber, BaseItem.Lightsaber);
                var originalProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var property = GetFirstItemProperty(saber); GetIsItemPropertyValid(property); property = GetNextItemProperty(saber))
                    originalProperties.Add(property);
                AddLegacyProperty(saber, ItemPropertyType.DamageBonus, 0, 4, FindFlatFiveDamageBonusRow());
                foreach (var property in originalProperties)
                    Invoke("MigrationObject", "RemoveProperty", saber, property);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 21), saber);
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)ItemPropertyAttackDelay.Delay240), saber);
                ctx.Log($"Damage bonus fixture before load: base {GetBaseItemType(saber)}, properties {DescribeProperties(saber)}");
                saber = Deserialize(ctx, ObjectPlugin.Serialize(saber));
                ctx.Log($"Damage bonus fixture after load: base {GetBaseItemType(saber)}, marker {GetLocalInt(saber, SWLOR.Game.Server.Native.LegacyItemProperties.ConvertedVariable)}, properties {DescribeProperties(saber)}");
                ctx.AssertEqual(26, Item.GetDMG(saber),
                    "Native loading retains the old damage bonus in the current DMG representation");
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrated = MigrateSerialized(ctx, saber);
                AssertSaberProfile(ctx, migrated, BaseItem.Lightsaber, tier: 5, damage: 26, accuracy: -1, skill: 40, delay: 24);
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.DamageBonus),
                    "The obsolete dice property is replaced by flat DMG");
            });
        }

        [EngineTest("Crafted and workbench saber blueprints stay outside legacy recalibration", Category = "SaberRecalibration")]
        public static async Task CraftableSabersRemainUnchanged(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            foreach (var resref in new[] { "ls_custom", "ss_custom", "saber_train_1" })
            {
                var item = await CreateItemAsync(ctx, owner, resref, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var before = DescribeProperties(item);
                    var originalTier = GetLocalInt(item, "SABER_TIER");
                    var changed = (bool)Invoke("LegacySaberMigration", "IsLegacySaber", item);
                    ctx.AssertEqual(false, changed, $"{resref} is excluded from legacy recalibration");
                    var args = new object[] { item, 0 };
                    var recalibrated = (bool)Invoke("LegacySaberMigration", "MigrateStoredObject", args);
                    ctx.AssertEqual(false, recalibrated, $"{resref} is not altered by legacy recalibration");
                    ctx.AssertEqual(originalTier, GetLocalInt(item, "SABER_TIER"), $"{resref} retains its authored tier");
                    ctx.AssertEqual(before, DescribeProperties(item), $"{resref} retains its authored properties");
                });
                await ctx.WaitFrameAsync();
            }
        }

        private static void AssertSaberProfile(
            EngineTestContext ctx,
            uint item,
            BaseItem expectedBaseItem,
            int tier,
            int damage,
            int accuracy,
            int skill,
            int delay)
        {
            ctx.AssertEqual(expectedBaseItem, GetBaseItemType(item), "Saber base item type");
            ctx.AssertEqual(tier, GetLocalInt(item, "SABER_TIER"), "Recalibrated saber tier");
            ctx.AssertEqual(damage, PropertyValue(item, ItemPropertyType.DMG), "Recalibrated damage");
            ctx.AssertEqual(accuracy, PropertyValue(item, ItemPropertyType.Accuracy), "Recalibrated accuracy");
            ctx.AssertEqual(skill, PropertyValue(item, ItemPropertyType.RequiresSkill), "Required skill rank");
            ctx.AssertEqual(delay, PropertyValue(item, ItemPropertyType.Delay), "Attack delay");
        }

        private static int FindPropertySubtype(uint item, ItemPropertyType type)
        {
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                if (GetItemPropertyType(property) == type)
                    return GetItemPropertySubType(property);

            return -1;
        }

        private static int FindFlatFiveDamageBonusRow()
        {
            for (var row = 0; row < Get2DARowCount("iprp_damagecost"); row++)
            {
                if (Get2DAString("iprp_damagecost", "NumDice", row) != "0" ||
                    Get2DAString("iprp_damagecost", "Die", row) != "5")
                    continue;

                return row;
            }

            throw new System.InvalidOperationException("iprp_damagecost has no flat 5 damage row.");
        }

        private static bool HasProperty(uint item, ItemPropertyType type)
        {
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                if (GetItemPropertyType(property) == type)
                    return true;

            return false;
        }

        private static void RemovePropertiesImmediately(uint item)
        {
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                properties.Add(property);

            foreach (var property in properties)
                Invoke("MigrationObject", "RemoveProperty", item, property);
        }
    }
}
