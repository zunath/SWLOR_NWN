using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Every droid enhancement recipe produces supported part properties", Category = "Droid", TimeoutSeconds = 90f)]
        public static async Task DroidEnhancementProperties(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var recipes = new DroidEnhancementRecipes().BuildRecipes();
            ctx.Assert(recipes.Count > 0, "Droid enhancement recipes exist");
            foreach (var recipe in recipes.Values)
            {
                var item = await CreateItemAsync(ctx, owner, recipe.Resref, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var count = 0;
                    for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(property) != ItemPropertyType.DroidEnhancement)
                            continue;
                        count++;
                        var properties = Craft.BuildItemPropertiesForEnhancement((EnhancementSubType)GetItemPropertySubType(property), GetItemPropertyCostTableValue(property)).ToList();
                        ctx.Assert(properties.Count > 0, $"{recipe.Resref} creates part bonuses");
                        foreach (var bonus in properties)
                        {
                            ctx.Assert(GetIsItemPropertyValid(bonus), $"{recipe.Resref} bonus is constructible");
                            ctx.AssertEqual(ItemPropertyType.DroidStat, GetItemPropertyType(bonus), $"{recipe.Resref} targets droid parts");
                            var subtype = (DroidStatSubType)GetItemPropertySubType(bonus);
                            ctx.Assert(Enum.IsDefined(subtype) && subtype != DroidStatSubType.Invalid, $"{recipe.Resref} stat is supported by assembly and spawning");
                        }
                    }
                    ctx.Assert(count > 0, $"{recipe.Resref} has enhancement properties");
                });
            }
        }

        [EngineTest("Droid spawn applies controller HP skills resistances and AI feats at every tier", Category = "Droid", TimeoutSeconds = 60f)]
        public static async Task DroidSpawnBudgets(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            for (var tier = 1; tier <= 5; tier++)
            {
                var cpu = await CreateItemAsync(ctx, owner, $"d_bl_cpu{tier}_m", owner);
                var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
                var droid = OBJECT_INVALID;
                DroidItemPropertyDetails expected = null;
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    for (var property = GetFirstItemProperty(cpu); GetIsItemPropertyValid(property); property = GetNextItemProperty(cpu))
                        if (GetItemPropertyType(property) == ItemPropertyType.DroidStat)
                            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat,
                                GetItemPropertySubType(property), GetItemPropertyCostTableValue(property)), controller);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, 15), controller);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.VIT, 14), controller);
                    Droid.SaveInstructions(controller, new ConstructedDroid
                    {
                        ActivePerks = new List<DroidPerk> { new(PerkType.MedKit, 1) },
                        LearnedPerks = new List<DroidPerk> { new(PerkType.MedKit, 1) },
                        SerializedCPU = ObjectPlugin.Serialize(cpu)
                    });
                    expected = Droid.LoadDroidItemPropertyDetails(controller);
                    Droid.SpawnDroid(owner, controller);
                    droid = Droid.GetDroid(owner);
                    ctx.Track(droid);
                });
                await ctx.DelaySecondsAsync(0.5f);
                ctx.Assert(GetIsObjectValid(droid), "Droid spawned");
                ctx.AssertEqual(24, GetAbilityScore(droid, AbilityType.Vitality), "CPU and part Vitality bonuses are combined");
                ctx.AssertEqual(40, GetHitDice(droid), "Native levels are independent of the CPU's gameplay level");
                ctx.AssertEqual(expected.HP + 280, GetMaxHitPoints(droid), $"Tier {tier} retains native Vitality HP in addition to controller HP");
                ctx.AssertEqual(expected.HP + 280, GetCurrentHitPoints(droid), "Droid spawns at its full final HP budget");
                ctx.AssertEqual(expected.Level, Stat.GetNPCStats(droid).Level, "NPC level comes from CPU");
                ctx.AssertEqual(expected.MGT, GetAbilityScore(droid, AbilityType.Might), "Raw CPU attribute");
                ctx.AssertEqual(expected.Skills[SkillType.Armor], Skill.GetCreatureSkillRank(droid, SkillType.Armor), "Armor equipment skill");
                ctx.AssertEqual(expected.Skills[SkillType.Vibroblade], Skill.GetCreatureSkillRank(droid, SkillType.Vibroblade), "Weapon equipment skill");
                ctx.AssertEqual(15, Stat.GetNPCStats(droid).Resistances[ResistanceType.Fire], "Resistance reaches combat skin");
                ctx.Assert(GetHasFeat(FeatType.MedKit1, droid), "Active instruction grants AI feat");
                ctx.AssertEqual(1, Perk.GetPerkLevel(droid, PerkType.MedKit), "Instruction rank reaches ability execution");
                ctx.AssertEqual(AIProfileType.DroidCompanion, NPCAI.GetProfileType(droid), "Droid companion AI is installed");
                await ctx.ExecuteInCreatureContextAsync(owner, () => DestroyObject(droid));
                await ctx.WaitFrameAsync();
            }
        }

        [EngineTest("Droid Vitality HP survives equipment bonuses below the native HP floor", Category = "Droid", TimeoutSeconds = 60f)]
        public static async Task DroidHitPointEquipment(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var cpu = await CreateItemAsync(ctx, owner, "d_bl_cpu5_m", owner);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            var droid = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                for (var property = GetFirstItemProperty(cpu); GetIsItemPropertyValid(property); property = GetNextItemProperty(cpu))
                    if (GetItemPropertyType(property) == ItemPropertyType.DroidStat)
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat,
                            GetItemPropertySubType(property), GetItemPropertyCostTableValue(property)), controller);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.VIT, 14), controller);
                Droid.SaveConstructedDroid(controller, new ConstructedDroid());
                Droid.SpawnDroid(owner, controller);
                droid = Droid.GetDroid(owner);
                ctx.Track(droid);
            });
            // SpawnDroid schedules a full heal after four seconds; let it finish
            // before wounding the fixture to test equipment's current-HP behavior.
            await ctx.DelaySecondsAsync(4.5f);
            ctx.SuppressNPCNaturalRegen(droid);
            ctx.AssertEqual(350, GetMaxHitPoints(droid), "70 controller HP plus 280 Vitality HP");
            await ctx.ExecuteInCreatureContextAsync(droid, () => ObjectPlugin.SetCurrentHitPoints(droid, 300));

            var equipment = new List<(uint Item, InventorySlot Slot, int Bonus)>();
            var expectedHP = 350;
            foreach (var (resref, slot, bonus) in new[]
                     {
                         ("advent_ring", InventorySlot.RightRing, 45),
                         ("advent_ring", InventorySlot.LeftRing, 80),
                         ("advent_necklace", InventorySlot.Neck, 80)
                     })
            {
                var item = await CreateItemAsync(ctx, droid, resref, droid);
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ClearProperties(item);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, bonus), item);
                    AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(GetRacialType(droid)), item);
                    ctx.AssertEqual(string.Empty, Item.CanEquip(droid, item), "HP equipment is usable by the droid");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ClearAllActions();
                    ActionEquipItem(item, slot);
                });
                await ctx.WaitUntilAsync(() => GetItemInSlot(slot, droid) == item, 5f, "HP equipment to be worn");
                expectedHP += bonus;
                ctx.AssertEqual(expectedHP, GetMaxHitPoints(droid), "Every equipment HP bonus applies immediately");
                ctx.AssertEqual(300, GetCurrentHitPoints(droid), "Equipping HP does not heal the droid");
                equipment.Add((item, slot, bonus));
            }
            ctx.AssertEqual(555, GetMaxHitPoints(droid), "Reported 275 base HP retains the missing 280 Vitality HP");

            equipment.Reverse();
            foreach (var (item, slot, bonus) in equipment)
            {
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ClearAllActions();
                    ActionUnequipItem(item);
                });
                await ctx.WaitUntilAsync(() => GetItemInSlot(slot, droid) != item, 5f, "HP equipment to be removed");
                expectedHP -= bonus;
                ctx.AssertEqual(expectedHP, GetMaxHitPoints(droid), "Unequipping removes only the equipment HP bonus");
                ctx.AssertEqual(300, GetCurrentHitPoints(droid), "Unequipping preserves wounds below the new maximum");
            }
            await ctx.ExecuteInCreatureContextAsync(droid, () => Stat.LoadNPCStats(droid));
            ctx.AssertEqual(350, GetMaxHitPoints(droid), "Reloading the skin preserves Vitality HP without doubling it");
            ctx.AssertEqual(350, GetCurrentHitPoints(droid), "Reload restores the full final HP budget");
        }
    }
}
