using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    /// <summary>
    /// Retired ship deeds and modules (plan section 15): the saved-data conversion must load in the engine as the new
    /// item a blueprint makes, keeping crafted bonuses and locals and losing the retired perk stamp, and the live rule
    /// (login and acquire) must produce the same item.
    /// </summary>
    [EngineTest("Retired ship deeds and modules load as their new items, the same from saved data and live", Category = SavedDataSweepCategory, TimeoutSeconds = 120f)]
    public static async Task SavedSpaceItemsMatchTheLiveConversion(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(owner, LegacySpaceItemBlueprints.Load);
        var failures = new List<string>();
        foreach (var resref in new[] { "com_laser_2", "shld_boost_b", "hvy_armor_3", "cap_minlas", "turbolas2", "sdeed_hound", "sdeed_freighter", "capdeed_sthran" })
        {
            LegacySpaceItemCatalog.TryGetReplacement(resref, out var replacement);
            var legacy = await CreateItemAsync(ctx, owner, resref, owner);
            var expected = await CreateItemAsync(ctx, owner, replacement, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                void Check(bool condition, string message)
                {
                    if (!condition) failures.Add($"{resref}: {message}");
                }

                var deed = resref.StartsWith("sdeed") || resref.StartsWith("capdeed");
                var bonusType = deed ? ItemPropertyType.StarshipBonus : ItemPropertyType.ModuleBonus;
                AddItemProperty(DurationType.Permanent, deed ? ItemPropertyCustom(bonusType, 46, 7) : ItemPropertyCustom(bonusType, -1, 4), legacy);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, 118, 3), legacy);
                SetLocalInt(legacy, "CRAFTED_QUALITY", 27);

                var tally = new LegacySpaceItemTally();
                Check(LegacySpaceItemConversion.Migrate(ObjectPlugin.Serialize(legacy), out var migrated, tally), "the saved-data rule converts it");
                var saved = Deserialize(ctx, migrated);
                Check(GetResRef(saved) == replacement && GetTag(saved) == replacement, $"loads as {GetResRef(saved)} tagged {GetTag(saved)}");
                Check(GetName(saved) == GetName(expected), $"is named {GetName(saved)}, not {GetName(expected)}");
                Check(GetBaseItemType(saved) == GetBaseItemType(expected) &&
                      GetItemAppearance(saved, ItemAppearanceType.SimpleModel, 0) == GetItemAppearance(expected, ItemAppearanceType.SimpleModel, 0),
                    "has the new item's base item and model");
                var required = Item.GetRequiredSkills(saved);
                var expectedRequired = Item.GetRequiredSkills(expected);
                Check((required == null && expectedRequired == null) ||
                      (required != null && expectedRequired != null && required.OrderBy(x => x.Key).SequenceEqual(expectedRequired.OrderBy(x => x.Key))),
                    "has the new item's Requires Skill rank");
                Check(!GetItemHasItemProperty(saved, ItemPropertyType.UseLimitationPerk), "loses the retired perk stamp");
                Check(GetItemHasItemProperty(saved, bonusType), "keeps its crafted bonus");
                Check(GetLocalInt(saved, "CRAFTED_QUALITY") == 27, "keeps its locals");
                var savedDescription = DescribeLoadedObject(ctx, saved);
                Invoke("MigrationObject", "DestroyTemporaryObject", saved);

                Check(EquipmentRequirementCompatibility.Normalize(legacy), "the live rule converts it");
                var live = OBJECT_INVALID;
                for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                    if (GetResRef(item) == replacement && GetLocalInt(item, "CRAFTED_QUALITY") == 27)
                        live = item;
                Check(GetIsObjectValid(live), "the live rule leaves the new item in its holder's inventory");
                if (GetIsObjectValid(live))
                {
                    var liveDescription = DescribeLoadedObject(ctx, live);
                    Check(liveDescription == savedDescription, $"the live and saved-data results differ:\n  live:  {liveDescription}\n  saved: {savedDescription}");
                    DestroyObject(live);
                }
                DestroyObject(expected);
            });
            await ctx.WaitFrameAsync();
        }

        var configuration = await CreateItemAsync(ctx, owner, "config_fig2", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var tally = new LegacySpaceItemTally();
            LegacySpaceItemConversion.Migrate(ObjectPlugin.Serialize(configuration), out _, tally);
            ctx.Assert(tally.RootIsConfigurationModule && tally.Credits == 400, "a saved configuration module is refunded at its value");
            ctx.Assert(EquipmentRequirementCompatibility.Normalize(configuration), "the live rule removes a configuration module");
        });
        await ctx.WaitFrameAsync();
        ctx.Assert(!GetIsObjectValid(configuration), "the configuration module is gone");
        ctx.Assert(failures.Count == 0, $"{failures.Count} differences:\n" + string.Join("\n", failures));
    }
}
