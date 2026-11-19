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
