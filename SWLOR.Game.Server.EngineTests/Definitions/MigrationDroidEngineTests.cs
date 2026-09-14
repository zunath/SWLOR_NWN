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

        [EngineTest("Droid migration preserves new resistances across serialization and retries", Category = "Droid")]
        public static async Task DroidResistanceRetry(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var part = await CreateItemAsync(ctx, owner, "d_hd_mpv1", owner);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, 15), part);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceIce, 10), part);
                Droid.SaveConstructedDroid(controller, new ConstructedDroid { SerializedHead = ObjectPlugin.Serialize(part) });
                for (var pass = 0; pass < 3; pass++)
                {
                    controller = MigrateSerialized(ctx, controller);
                    var saved = Droid.LoadConstructedDroid(controller);
                    var migratedPart = Deserialize(ctx, saved.SerializedHead);
                    ctx.AssertEqual(15, PropertyValue(migratedPart, ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire), "Fire resistance survives retry");
                    ctx.AssertEqual(10, PropertyValue(migratedPart, ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceIce), "Ice resistance survives retry");
                    ctx.AssertEqual(-1, PropertyValue(migratedPart, ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroblade), "Fire resistance must not become weapon skill");
                    ctx.AssertEqual(-1, PropertyValue(migratedPart, ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol), "Ice resistance must not become weapon skill");
                }
            });
        }

        [EngineTest("Droid migration repairs invalid AI ranks and retains learned instructions", Category = "Droid")]
        public static async Task DroidInstructionMigration(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidInstruction, (int)PerkType.MedKit, 2), controller);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, 3), controller);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AISlots, 3), controller);
                Droid.SaveConstructedDroid(controller, new ConstructedDroid
                {
                    ActivePerks = new List<DroidPerk> { null, new(PerkType.MedKit, 1), new(PerkType.Provoke, 2), new(PerkType.DualWield, 1) }
                });
                for (var pass = 0; pass < 2; pass++)
                {
                    controller = MigrateSerialized(ctx, controller);
                    var droid = Droid.LoadConstructedDroid(controller);
                    ctx.AssertEqual(1, droid.ActivePerks.Count, "Only the affordable highest Med Kit rank remains active");
                    ctx.AssertEqual(2, droid.ActivePerks.Single().Level, "Retained active rank");
                    ctx.AssertEqual(PerkType.MedKit, droid.ActivePerks.Single().Perk, "Retained active ability");
                    ctx.Assert(droid.LearnedPerks.Any(perk => perk.Perk == PerkType.Provoke && perk.Level == 2), "Over-budget instructions remain learned");
                    ctx.AssertEqual(2, PropertyValue(controller, ItemPropertyType.DroidInstruction, (int)PerkType.MedKit), "Inspection property matches JSON");
                    ctx.AssertEqual(2, Droid.LoadDroidItemPropertyDetails(controller).Perks[PerkType.MedKit], "Runtime reads the migrated loadout regardless of property order");
                }
                var cleared = Droid.LoadConstructedDroid(controller);
                cleared.ActivePerks.Clear();
                Droid.SaveInstructions(controller, cleared);
                ctx.AssertEqual(0, Droid.LoadDroidItemPropertyDetails(controller).Perks.Count, "A deactivated instruction is unavailable immediately");
            });
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
                ctx.AssertEqual(expected.HP, GetMaxHitPoints(droid), $"Tier {tier} final HP budget includes native Vitality once");
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
    }
}
