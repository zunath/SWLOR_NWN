using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Retired appearances load in the engine from the saved-data conversion through repeated save and reload",
        Category = "RetiredAppearances", TimeoutSeconds = 120f)]
    public static async Task RetiredAppearancesLoadInTheEngine(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var armor = await CreateItemAsync(ctx, owner, "dan_heavy_armor", owner);
        var helmet = await CreateItemAsync(ctx, owner, "001", owner);
        var cloak = await CreateItemAsync(ctx, owner, "absdef_l5", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 160);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck, 299);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder, 255);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep, 18);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep, 17);
            ItemPlugin.SetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0, 297);
            ItemPlugin.SetItemAppearance(cloak, ItemAppearanceType.SimpleModel, 0, 33);
            SetLocalString(armor, "RETIRED_OWNER", "armor");

            (uint Item, string Name, System.Func<uint, string> Read, string Expected)[] cases =
            {
                (armor, "armor", item => string.Join(",",
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe),
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck),
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder),
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep),
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep)), "0,0,0,0,17"),
                (helmet, "helmet", item => GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString(), "1"),
                (cloak, "cloak", item => GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString(), "1"),
            };
            foreach (var (item, name, read, expected) in cases)
            {
                var resref = GetResRef(item);
                ctx.Assert(EnzymeBaseItemMigration.MigrateSavedObject(ObjectPlugin.Serialize(item), out var data), $"{name} saved conversion runs");
                for (var pass = 0; pass < 3; pass++)
                {
                    var restored = Deserialize(ctx, data);
                    ctx.AssertEqual(expected, read(restored), $"{name} parts after reload {pass}");
                    ctx.Assert(GetResRef(restored) == resref && GetItemStackSize(restored) == 1, $"{name} identity and quantity");
                    ctx.Assert(!EnzymeBaseItemMigration.MigrateSavedObject(data, out _), $"{name} retry is idempotent");
                    if (item == armor)
                        ctx.Assert(GetLocalString(restored, "RETIRED_OWNER") == "armor", "armor keeps its locals");
                    data = ObjectPlugin.Serialize(restored);
                    DestroyObject(restored);
                }
            }
        });

        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            SetCreatureAppearanceType(creature, AppearanceType.Human);
            SetGender(creature, Gender.Female);
            SetCreatureBodyPart(CreaturePart.Head, 258, creature);
            SetCreatureBodyPart(CreaturePart.LeftBicep, 18, creature);
            SetCreatureBodyPart(CreaturePart.RightBicep, 2, creature);
            RacialAppearanceRegistry.TryGet(AppearanceType.Human, out var human);
            ctx.Assert(EnzymeBaseItemMigration.MigrateSavedObject(ObjectPlugin.Serialize(creature), out var data), "creature saved conversion runs");
            for (var pass = 0; pass < 3; pass++)
            {
                var restored = Deserialize(ctx, data);
                ObjectPlugin.AddToArea(restored, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(0f, 0f)));
                ctx.AssertEqual(human.FemaleHeads[0], GetCreatureBodyPart(CreaturePart.Head, restored), $"retired head after reload {pass}");
                ctx.AssertEqual(1, GetCreatureBodyPart(CreaturePart.LeftBicep, restored), $"retired bicep after reload {pass}");
                ctx.AssertEqual(2, GetCreatureBodyPart(CreaturePart.RightBicep, restored), $"kept bicep after reload {pass}");
                ctx.Assert(GetAppearanceType(restored) == AppearanceType.Human && GetGender(restored) == Gender.Female, "body identity");
                ctx.Assert(!EnzymeBaseItemMigration.MigrateSavedObject(data, out _), "creature retry is idempotent");
                data = ObjectPlugin.Serialize(restored);
                DestroyObject(restored);
            }
        });
        ctx.SetResultDetail("Armor, helmet, cloak and a creature's head and body parts load on surviving appearances, keep identity, quantity and locals, and stay put across three reloads.");
    }
}
