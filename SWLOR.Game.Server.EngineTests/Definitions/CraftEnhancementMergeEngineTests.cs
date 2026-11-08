using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Two enhancements of the same kind merge into one property on the finished item. A crafted item is delivered as a copy made
    /// from its serialized form in the same script that applied the enhancements, so a merge that removed the old property and added
    /// the summed one left both in the copy: two "Combat Readiness 5" kits came out as "Combat Readiness 10" and "Combat Readiness 5".
    /// </summary>
    public static class CraftEnhancementMergeEngineTests
    {
        private static readonly (RecipeType Recipe, SkillType Skill, string Kit)[] Crafts =
        {
            (RecipeType.TitanBreastplate, SkillType.Smithery, "aen_recast1"),
            (RecipeType.TitanBreastplate, SkillType.Smithery, "aen_def_phy1"),
            (RecipeType.TitanKnife, SkillType.Smithery, "wen_atk1"),
            (RecipeType.ObeliskLarge, SkillType.Fabrication, "sten_sb1")
        };

        [EngineTest("Two enhancements of the same kind reach the finished item as one merged property", Category = "CraftEnhancement")]
        public static async Task TwoKitsMergeIntoOneProperty(EngineTestContext ctx)
        {
            foreach (var (recipe, skill, kit) in Crafts)
            {
                using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
                var player = fixture.Creature;
                var detail = Craft.GetRecipe(recipe);
                fixture.Update(record => record.Skills[skill].Rank = 30);

                var expected = new List<(ItemPropertyType Type, int SubType, int Amount)>();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    foreach (var (resref, quantity) in detail.Components)
                        CreateItemOnObject(resref, player, quantity);
                    CreateItemOnObject(kit, player);
                    CreateItemOnObject(kit, player);
                    expected.AddRange(Expected(detail, kit));
                });
                await ctx.WaitFrameAsync();
                ctx.Assert(expected.Count > 0, $"{recipe} + {kit}: the kit gives the recipe a property");

                CraftSession session = null;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var kits = Snapshot(player).Keys.Where(item => GetResRef(item) == kit).ToArray();
                    ctx.AssertEqual(2, kits.Length, $"{recipe} + {kit}: both kits are carried");
                    var start = CraftSessions.Start(player, new CraftSessionStartRequest(recipe, OBJECT_INVALID, kits));
                    ctx.Assert(start.Succeeded, $"{recipe} starts: {start.Message}");
                    session = start.Session;
                });
                await ctx.WaitFrameAsync();

                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    ctx.Assert(CraftSessions.Succeed(player, session, new MinigameMeter(100, 100), 10) != null, $"{recipe} succeeds");
                    AssertMerged(ctx, player, detail, kit, expected, "in the craft's script");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () => AssertMerged(ctx, player, detail, kit, expected, "a frame later"));
            }
        }

        /// <summary>What the template carries for each property the kit gives, plus the kit's amount twice.</summary>
        private static IEnumerable<(ItemPropertyType Type, int SubType, int Amount)> Expected(RecipeDetail detail, string kit)
        {
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            var template = CreateItemOnObject(detail.Resref, storage);
            var enhancement = CreateItemOnObject(kit, storage);
            var reading = CraftEnhancementReader.Read(enhancement, detail);
            var result = new List<(ItemPropertyType, int, int)>();
            foreach (var ip in reading.Properties)
            {
                var type = GetItemPropertyType(ip);
                var subType = GetItemPropertySubType(ip);
                var amount = GetItemPropertyCostTableValue(ip) * 2;
                foreach (var existing in Matching(template, type, subType))
                    amount += GetItemPropertyCostTableValue(existing);
                result.Add((type, subType, amount));
            }

            DestroyObject(template);
            DestroyObject(enhancement);
            return result;
        }

        private static void AssertMerged(EngineTestContext ctx, uint player, RecipeDetail detail, string kit,
            IReadOnlyList<(ItemPropertyType Type, int SubType, int Amount)> expected, string when)
        {
            var made = Snapshot(player).Keys.Where(item => GetResRef(item) == detail.Resref).ToList();
            ctx.Assert(made.Count > 0, $"{detail.Resref} + {kit} ({when}): the crafted item is carried");
            foreach (var item in made)
            {
                ctx.Assert(GetLocalBool(item, ItemService.PlayerProducedItemVariable),
                    $"{detail.Resref} + {kit} ({when}): the remade item keeps the player-made mark");
                var copy = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(item));
                foreach (var (target, label) in new[] { (item, "carried item"), (copy, "re-serialized copy") })
                {
                    foreach (var (type, subType, amount) in expected)
                    {
                        var matches = Matching(target, type, subType).ToList();
                        ctx.AssertEqual(1, matches.Count, $"{detail.Resref} + 2x {kit} ({when}, {label}): one {type} property");
                        if (matches.Count == 1)
                            ctx.AssertEqual(amount, GetItemPropertyCostTableValue(matches[0]),
                                $"{detail.Resref} + 2x {kit} ({when}, {label}): {type} holds the template and both kits");
                    }
                }

                DestroyObject(copy);
            }
        }

        private static IEnumerable<ItemProperty> Matching(uint item, ItemPropertyType type, int subType)
        {
            var result = new List<ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == type && (subType == -1 || GetItemPropertySubType(ip) == subType))
                    result.Add(ip);
            }

            return result;
        }
    }
}
