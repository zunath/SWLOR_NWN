using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// Unit tests of the crafting instance: the engine is a recording fake, the database an in-memory one and the neighbouring
/// services scripted stand-ins, so no server is needed.
/// </summary>
[TestFixture]
public class CraftServiceTests
{
    private const uint PlayerObject = 1001;

    private PlayerEngine _engine = null!;
    private FakeEventsPlugin _events = null!;
    private FakeItemPropertyPlugin _itemProperties = null!;
    private readonly List<IDisposable> _scopes = new();
    private InMemoryDBService _db = null!;
    private ScriptedProxy _cacheScript = null!;
    private ScriptedProxy _itemScript = null!;
    private ScriptedProxy _propertyScript = null!;
    private CraftService _service = null!;
    private FacadeContainer _facades = null!;

    [OneTimeSetUp]
    public void LoadTheSkills()
    {
        // The resistance encoding is reached through the Resistance facade, which needs a container.
        _facades = new FacadeContainer();
        using (_facades.Install())
        using (new PlayerEngine())
        using (EventsPluginRuntime.Override(new FakeEventsPlugin()))
        {
            Skill.CacheData();
            Skill.CacheXPChartData();
        }
    }

    [OneTimeTearDown]
    public void DisposeTheFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _engine = new PlayerEngine();
        _events = new FakeEventsPlugin();
        _itemProperties = new FakeItemPropertyPlugin();
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _scopes.Add(ItemPropertyPluginRuntime.Override(_itemProperties));
        _db = new InMemoryDBService();
        var cache = ScriptedProxy.Create<ICacheService>(out _cacheScript);
        var item = ScriptedProxy.Create<IItemService>(out _itemScript);
        var property = ScriptedProxy.Create<IPropertyService>(out _propertyScript);
        _service = new CraftService(cache, _db, item, property);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        _engine.Dispose();
    }

    private RecipeDetail AddRecipe(
        RecipeType type,
        SkillType skill = SkillType.Fabrication,
        RecipeCategoryType category = RecipeCategoryType.Uncategorized,
        RecipeEnhancementType enhancement = RecipeEnhancementType.None,
        int level = 10,
        bool active = true)
    {
        var recipe = new RecipeDetail
        {
            Skill = skill,
            Category = category,
            EnhancementType = enhancement,
            Level = level,
            IsActive = active,
            Resref = "test_item"
        };
        _service.GetAllRecipes()[type] = recipe;
        return recipe;
    }

    private static T Only<T>(IEnumerable<T> values) => values.Should().ContainSingle().Subject;

    // ---- the caches

    [Test]
    public void A_new_instance_knows_no_recipes()
    {
        _service.GetAllRecipes().Should().BeEmpty();
        _service.GetAllResearchableRecipes().Should().BeEmpty();
        _service.GetAllCategories().Should().BeEmpty();
    }

    [Test]
    public void Each_instance_keeps_its_own_recipes()
    {
        AddRecipe(RecipeType.VenomCoating1);

        var other = new CraftService(
            ScriptedProxy.Create<ICacheService>(out _),
            _db,
            ScriptedProxy.Create<IItemService>(out _),
            ScriptedProxy.Create<IPropertyService>(out _));

        other.RecipeExists(RecipeType.VenomCoating1).Should().BeFalse();
        _service.RecipeExists(RecipeType.VenomCoating1).Should().BeTrue();
    }

    [Test]
    public void The_instance_owns_the_objects_the_minigames_keep()
    {
        var other = new CraftService(
            ScriptedProxy.Create<ICacheService>(out _),
            _db,
            ScriptedProxy.Create<IItemService>(out _),
            ScriptedProxy.Create<IPropertyService>(out _));

        _service.SessionWorld.Should().NotBeNull().And.BeSameAs(_service.SessionWorld).And.NotBeSameAs(other.SessionWorld);
        _service.QuickCraftWorld.Should().NotBeNull().And.BeSameAs(_service.QuickCraftWorld).And.NotBeSameAs(other.QuickCraftWorld);
    }

    [Test]
    public void CacheData_loads_the_categories_the_recipes_and_the_enhancement_sub_types()
    {
        _service.CacheData();

        _service.GetAllRecipes().Should().NotBeEmpty();
        _service.GetAllRecipes().Keys.Should().Contain(RecipeType.VenomCoating1);
        _service.GetAllCategories().Should().NotBeEmpty();
        _service.GetAllCategories().Values.Should().OnlyContain(category => category.IsActive);
        _service.GetEnhancementSubType(EnhancementSubType.DefensePhysical).Name.Should().Be("Defense - Physical");
    }

    [Test]
    public void CacheData_marks_researchable_recipes_and_lists_them_by_skill_and_category()
    {
        _service.CacheData();

        var researchable = _service.GetAllResearchableRecipes();

        researchable.Should().NotBeEmpty();
        researchable.Values.Should().OnlyContain(recipe =>
            recipe.EnhancementType == RecipeEnhancementType.Weapon ||
            recipe.EnhancementType == RecipeEnhancementType.Armor ||
            recipe.EnhancementType == RecipeEnhancementType.Food);
        foreach (var recipe in researchable.Values.Take(25))
        {
            _service.GetAllResearchableRecipesBySkill(recipe.Skill).Values.Should().Contain(recipe);
            _service.GetResearchableRecipesBySkillAndCategory(recipe.Skill, recipe.Category).Values.Should().Contain(recipe);
        }
    }

    [Test]
    public void CacheData_lists_every_recipe_under_its_skill_and_category()
    {
        _service.CacheData();

        foreach (var (type, recipe) in _service.GetAllRecipes().Take(50))
        {
            _service.GetAllRecipesBySkill(recipe.Skill).Should().ContainKey(type);
            _service.GetRecipesBySkillAndCategory(recipe.Skill, recipe.Category).Should().ContainKey(type);
        }
    }

    [Test]
    public void CacheData_remembers_the_components_of_the_active_recipes()
    {
        _service.CacheData();
        var recipe = _service.GetAllRecipes().Values.First(r => r.IsActive && r.Components.Count > 0);
        var resref = recipe.Components.Keys.First();

        _engine.Engine.SetReturn("GetResRef", resref);

        _service.IsItemComponent(OBJECT_INVALID_FOR_TESTS).Should().BeTrue();
    }

    private const uint OBJECT_INVALID_FOR_TESTS = 2002;

    [Test]
    public void IsItemComponent_is_false_for_a_resref_no_recipe_uses()
    {
        _engine.Engine.SetReturn("GetResRef", "not_a_component");

        _service.IsItemComponent(OBJECT_INVALID_FOR_TESTS).Should().BeFalse();
    }

    [Test]
    public void IsResearchableRecipe_is_the_private_rule_the_simulator_reads_by_name()
    {
        var method = typeof(CraftService).GetMethod("IsResearchableRecipe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull("SWLOR.CraftSim's ServerResearchRules finds the rule by this name");

        foreach (var type in Enum.GetValues<RecipeEnhancementType>())
        {
            var expected = type is RecipeEnhancementType.Weapon or RecipeEnhancementType.Armor or RecipeEnhancementType.Food;
            method!.Invoke(_service, new object[] { new RecipeDetail { EnhancementType = type } }).Should().Be(expected, type.ToString());
        }
    }

    // ---- lookups

    [Test]
    public void GetRecipe_returns_the_registered_recipe_and_fails_for_an_unknown_one()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1);

        _service.GetRecipe(RecipeType.VenomCoating1).Should().BeSameAs(recipe);
        var get = () => _service.GetRecipe(RecipeType.Invalid);
        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetAllRecipes_hands_out_the_cache_itself()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1);

        _service.GetAllRecipes().Should().ContainKey(RecipeType.VenomCoating1).WhoseValue.Should().BeSameAs(recipe);
    }

    [Test]
    public void GetRecipesBySkillAndCategory_is_empty_for_an_unknown_skill_or_category()
    {
        _service.GetRecipesBySkillAndCategory(SkillType.Fabrication, RecipeCategoryType.Uncategorized).Should().BeEmpty();
        _service.GetResearchableRecipesBySkillAndCategory(SkillType.Fabrication, RecipeCategoryType.Uncategorized).Should().BeEmpty();
        _service.GetRecipeCategoriesBySkill(SkillType.Fabrication).Should().BeEmpty();
    }

    [Test]
    public void GetRecipeLevelDetail_reads_the_level_chart()
    {
        var detail = _service.GetRecipeLevelDetail(1);

        detail.Should().NotBeNull();
        detail.Should().BeEquivalentTo(new RecipeLevelChart().GetByLevel(1));
    }

    // ---- skill rank and XP

    [TestCase(40, 37)]
    [TestCase(10, 7)]
    [TestCase(3, 0)]
    [TestCase(0, 0)]
    public void GetRequiredSkillRankForRecipe_unlocks_three_ranks_below_the_recipe_level(int level, int expected)
    {
        _service.GetRequiredSkillRankForRecipe(new RecipeDetail { Level = level }).Should().Be(expected);
    }

    [Test]
    public void GetBaseRecipeXP_uses_the_delta_curve_without_a_practice_limit()
    {
        var recipe = new RecipeDetail { Level = 12 };

        for (var rank = 0; rank <= 30; rank += 5)
            _service.GetBaseRecipeXP(recipe, rank).Should().Be(Skill.GetDeltaXP(recipe.Level - rank));
    }

    [Test]
    public void GetBaseRecipeXP_uses_the_practice_curve_with_a_practice_limit()
    {
        var recipe = new RecipeDetail { Level = 12, PracticeRankLimit = 15 };

        for (var rank = 0; rank <= 20; rank += 5)
            _service.GetBaseRecipeXP(recipe, rank).Should().Be(Skill.GetPracticeXP(recipe.Level, rank, 15));
    }

    // ---- requirements

    private sealed class FixedRequirement : IRecipeRequirement
    {
        private readonly string _failure;

        public FixedRequirement(string text, string failure)
        {
            RequirementText = text;
            _failure = failure;
        }

        public string RequirementText { get; }

        public string CheckRequirements(uint player) => _failure;
    }

    private void GivePlayerRank(SkillType skill, int rank)
    {
        var player = new Player { Id = PlayerEngine.PlayerId };
        player.Skills[skill] = new PlayerSkill { Rank = rank };
        _db.With(player);
    }

    [Test]
    public void CanPlayerCraftRecipe_is_true_when_the_rank_is_high_enough_and_every_requirement_passes()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1, level: 10);
        recipe.Requirements.Add(new FixedRequirement("ok", string.Empty));
        GivePlayerRank(recipe.Skill, 7);

        _service.CanPlayerCraftRecipe(PlayerObject, RecipeType.VenomCoating1).Should().BeTrue();
    }

    [Test]
    public void CanPlayerCraftRecipe_is_false_when_the_rank_is_too_low()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 10);
        GivePlayerRank(SkillType.Fabrication, 6);

        _service.CanPlayerCraftRecipe(PlayerObject, RecipeType.VenomCoating1).Should().BeFalse();
    }

    [Test]
    public void CanPlayerCraftRecipe_is_false_when_a_requirement_fails()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1, level: 3);
        recipe.Requirements.Add(new FixedRequirement("ok", string.Empty));
        recipe.Requirements.Add(new FixedRequirement("no", "Missing."));

        _service.CanPlayerCraftRecipe(PlayerObject, RecipeType.VenomCoating1).Should().BeFalse();
    }

    [Test]
    public void CanPlayerCraftRecipe_skips_the_rank_check_for_a_recipe_anyone_may_craft()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 3);

        _service.CanPlayerCraftRecipe(PlayerObject, RecipeType.VenomCoating1).Should().BeTrue();
        _db.Reads.Should().BeEmpty();
    }

    // ---- the recipe window

    [Test]
    public void BuildRecipeDetail_lists_the_sections_of_a_recipe()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1, level: 3);
        recipe.Components["comp_a"] = 2;
        recipe.EffectLines.Add("Poisons the target.");
        recipe.PracticeRankLimit = 15;
        recipe.Requirements.Add(new FixedRequirement("Needs luck.", "Missing."));
        _cacheScript.On("GetItemNameByResref", args => "Component A");
        _itemScript.On("BuildItemPropertyList", args => new SWLOR.Game.Server.Service.GuiService.GuiBindingList<string> { "+1 Stuff" });

        var (lines, colors) = _service.BuildRecipeDetail(PlayerObject, RecipeType.VenomCoating1, null);

        lines.Should().ContainInOrder(
            "[COMPONENTS]", "2x Component A", string.Empty,
            "[REQUIREMENTS]", $"{Skill.GetSkillDetails(SkillType.Fabrication).Name} rank 0", $"Trains {Skill.GetSkillDetails(SkillType.Fabrication).Name} up to rank 15", "Needs luck.", string.Empty,
            "[EFFECT]", "Poisons the target.", string.Empty,
            "[PROPERTIES]", "+1 Stuff", string.Empty);
        lines.Should().NotContain("[BLUEPRINT]");
        colors.Count.Should().Be(lines.Count);
        _engine.Engine.CallsTo("CreateItemOnObject").Should().ContainSingle();
        _engine.Engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void BuildRecipeDetail_adds_the_blueprint_section_for_a_blueprint()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 3);
        _cacheScript.On("GetItemNameByResref", args => "x");
        _itemScript.On("BuildItemPropertyList", args => new SWLOR.Game.Server.Service.GuiService.GuiBindingList<string>());
        var blueprint = new BlueprintDetail
        {
            Recipe = RecipeType.VenomCoating1,
            Level = 2,
            LicensedRuns = 3,
            ItemBonuses = 1,
            CreditReduction = 10,
            EnhancementSlots = 2
        };

        var (lines, _) = _service.BuildRecipeDetail(PlayerObject, RecipeType.VenomCoating1, blueprint);

        lines.Should().ContainInOrder("[BLUEPRINT]", "Licensed Runs x3", "Stat Bonus x1", "Price Reduced -10%", "Bonus Enhancements x2");
    }

    [Test]
    public void BuildRecipeDetail_says_no_bonuses_for_a_level_zero_blueprint()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 3);
        _cacheScript.On("GetItemNameByResref", args => "x");
        _itemScript.On("BuildItemPropertyList", args => new SWLOR.Game.Server.Service.GuiService.GuiBindingList<string>());

        var (lines, _) = _service.BuildRecipeDetail(PlayerObject, RecipeType.VenomCoating1, new BlueprintDetail { Recipe = RecipeType.VenomCoating1 });

        lines.Should().ContainInOrder("[BLUEPRINT]", "No Bonuses");
    }

    [Test]
    public void BuildRecipeDetail_colours_a_failed_requirement_red()
    {
        var recipe = AddRecipe(RecipeType.VenomCoating1, level: 3);
        recipe.Requirements.Add(new FixedRequirement("Needs luck.", "Missing."));
        _cacheScript.On("GetItemNameByResref", args => "x");
        _itemScript.On("BuildItemPropertyList", args => new SWLOR.Game.Server.Service.GuiService.GuiBindingList<string>());

        var (lines, colors) = _service.BuildRecipeDetail(PlayerObject, RecipeType.VenomCoating1, null);

        var index = lines.ToList().IndexOf("Needs luck.");
        colors[index].Should().BeEquivalentTo(SWLOR.Game.Server.Service.GuiService.Component.GuiColor.Red);
    }

    // ---- items

    [Test]
    public void IsItemRecipe_compares_the_tag_ignoring_case()
    {
        _engine.Engine.SetReturn("GetTag", "RECIPE");
        _service.IsItemRecipe(PlayerObject).Should().BeTrue();

        _engine.Engine.SetReturn("GetTag", "other");
        _service.IsItemRecipe(PlayerObject).Should().BeFalse();
    }

    [Test]
    public void IsItemEnhancement_is_true_when_the_item_carries_an_enhancement_property()
    {
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.Accuracy, ItemPropertyType.FoodEnhancement);

        _service.IsItemEnhancement(PlayerObject).Should().BeTrue();
    }

    [Test]
    public void IsItemEnhancement_is_false_for_an_item_without_one()
    {
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.Accuracy);

        _service.IsItemEnhancement(PlayerObject).Should().BeFalse();
    }

    // ---- enhancement item properties

    [Test]
    public void BuildItemPropertyForEnhancement_builds_the_property_of_each_supported_sub_type()
    {
        var supported = 0;
        foreach (var subType in Enum.GetValues<EnhancementSubType>().Where(value => value != EnhancementSubType.Invalid))
        {
            _engine.Engine.ClearCalls();
            try
            {
                _service.BuildItemPropertyForEnhancement(subType, 3);
            }
            catch (Exception exception) when (exception.Message == "Unsupported enhancement type.")
            {
                continue;
            }

            supported++;
            _engine.Engine.Calls.Should().Contain(call => call.Member.StartsWith("ItemProperty"), $"{subType} builds a property");
        }

        supported.Should().BeGreaterThan(30);
    }

    [Test]
    public void BuildItemPropertyForEnhancement_rejects_the_invalid_sub_type()
    {
        var build = () => _service.BuildItemPropertyForEnhancement(EnhancementSubType.Invalid, 1);

        build.Should().Throw<Exception>().WithMessage("Unsupported enhancement type.");
    }

    [Test]
    public void BuildItemPropertyForEnhancement_defense_uses_the_damage_type_as_the_sub_type()
    {
        _service.BuildItemPropertyForEnhancement(EnhancementSubType.DefenseForce, 4);

        var call = _engine.Engine.CallsTo("ItemPropertyCustom").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(ItemPropertyType.Defense);
        call.Arguments[1].Should().Be((int)CombatDamageType.Force);
        call.Arguments[2].Should().Be(4);
    }

    [Test]
    public void BuildItemPropertyForEnhancement_resistance_encodes_the_amount()
    {
        _service.BuildItemPropertyForEnhancement(EnhancementSubType.ResistanceFire, 5);

        var call = _engine.Engine.CallsTo("ItemPropertyCustom").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(ItemPropertyType.Resistance);
        call.Arguments[1].Should().Be((int)ResistanceType.Fire);
        call.Arguments[2].Should().Be(Resistance.EncodeItemPropertyCostTableValue(5));
    }

    [Test]
    public void BuildItemPropertiesForEnhancement_adds_a_damage_type_property_for_a_non_physical_weapon_damage_type()
    {
        var properties = _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2, CombatDamageType.Fire).ToList();

        properties.Should().HaveCount(2);
        var calls = _engine.Engine.CallsTo("ItemPropertyCustom");
        calls[^1].Arguments[0].Should().Be(ItemPropertyType.WeaponDamageType);
        calls[^1].Arguments[1].Should().Be((int)CombatDamageType.Fire);
    }

    [Test]
    public void BuildItemPropertiesForEnhancement_adds_nothing_extra_for_physical_damage_or_another_sub_type()
    {
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2, CombatDamageType.Physical).Should().HaveCount(1);
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.HP, 2, CombatDamageType.Fire).Should().HaveCount(1);
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2).Should().HaveCount(1);
    }

    [Test]
    public void IsWeaponDamageEnhancement_is_true_only_for_DMG()
    {
        foreach (var subType in Enum.GetValues<EnhancementSubType>())
            _service.IsWeaponDamageEnhancement(subType).Should().Be(subType == EnhancementSubType.DMG);
    }

    [TestCase(EnhancementSubType.HP, CombatDamageType.Fire, false, CombatDamageType.Invalid)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Force, true, CombatDamageType.Physical)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Fire, true, CombatDamageType.Fire)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Invalid, true, CombatDamageType.Physical)]
    public void TryGetWeaponDamageTypeForEnhancement_resolves_the_damage_type(
        EnhancementSubType subType,
        CombatDamageType explicitType,
        bool expectedResult,
        CombatDamageType expectedType)
    {
        _service.TryGetWeaponDamageTypeForEnhancement(subType, explicitType, out var resolved).Should().Be(expectedResult);

        resolved.Should().Be(expectedType);
    }

    // ---- merging crafted properties

    [Test]
    public void MergeCraftedItemProperty_adds_a_property_of_a_new_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void MergeCraftedItemProperty_sums_properties_of_the_same_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        var properties = new List<ItemProperty> { EngineStructures.Create<ItemProperty>() };

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void MergeCraftedItemProperty_never_keeps_a_force_weapon_damage_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Force);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().BeEmpty();
    }

    [Test]
    public void MergeCraftedItemProperty_replaces_the_weapon_damage_type_when_the_roll_does_not_keep_the_old_one()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Fire);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void ApplyCraftedItemProperty_ignores_a_force_weapon_damage_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Force);

        _service.ApplyCraftedItemProperty(PlayerObject, EngineStructures.Create<ItemProperty>());

        _engine.Engine.CountOf("RemoveItemProperty").Should().Be(0);
    }

    [Test]
    public void ApplyCraftedItemProperty_removes_the_matching_properties_before_adding_the_summed_one()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);

        _service.ApplyCraftedItemProperty(PlayerObject, EngineStructures.Create<ItemProperty>());

        _engine.Engine.CountOf("RemoveItemProperty").Should().Be(2);
    }

    // ---- blueprints

    [Test]
    public void GetBlueprintDetails_reads_the_recipe_the_flags_and_every_blueprint_property()
    {
        _engine.Engine.SetReturnHandler("GetLocalInt", args => (string)args[1]! == "BLUEPRINT_RECIPE_ID" ? (int)RecipeType.VenomCoating1 : 0);
        _engine.Engine.SetReturnHandler("GetLocalBool", args => (string)args[1]! == "BLUEPRINT_RANDOM_ENHANCEMENT_SLOT_GRANTED");
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, true, true, true, true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType",
            ItemPropertyType.Blueprint, ItemPropertyType.Blueprint, ItemPropertyType.Blueprint,
            ItemPropertyType.Blueprint, ItemPropertyType.Blueprint, ItemPropertyType.Blueprint);
        _engine.Engine.SetReturnSequence("GetItemPropertySubType",
            (int)BlueprintSubType.Level, (int)BlueprintSubType.LicensedRuns, (int)BlueprintSubType.ItemBonuses,
            (int)BlueprintSubType.CreditReduction, (int)BlueprintSubType.TimeReduction, (int)BlueprintSubType.EnhancementSlots);
        _engine.Engine.SetReturnSequence("GetItemPropertyCostTableValue", 4, 5, 6, 7, 8, 9);

        var detail = _service.GetBlueprintDetails(PlayerObject);

        detail.Recipe.Should().Be(RecipeType.VenomCoating1);
        detail.RandomEnhancementSlotGranted.Should().BeTrue();
        detail.Level.Should().Be(4);
        detail.LicensedRuns.Should().Be(5);
        detail.ItemBonuses.Should().Be(6);
        detail.CreditReduction.Should().Be(7);
        detail.TimeReduction.Should().Be(8);
        detail.EnhancementSlots.Should().Be(9);
        detail.GuaranteedBonuses.Should().BeEmpty();
    }

    [Test]
    public void GetBlueprintDetails_turns_an_enhancement_property_into_a_guaranteed_bonus()
    {
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.ArmorEnhancement);
        _engine.Engine.SetReturnSequence("GetItemPropertySubType", (int)EnhancementSubType.HP);
        _engine.Engine.SetReturnSequence("GetItemPropertyCostTableValue", 5);

        var detail = _service.GetBlueprintDetails(PlayerObject);

        detail.GuaranteedBonuses.Should().HaveCount(1);
        detail.Recipe.Should().Be(RecipeType.Invalid);
    }

    [Test]
    public void GetBlueprintDetails_pairs_a_weapon_damage_enhancement_with_the_damage_type_property_after_it()
    {
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.WeaponEnhancement, ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturnSequence("GetItemPropertySubType", (int)EnhancementSubType.DMG, (int)CombatDamageType.Fire);
        _engine.Engine.SetReturnSequence("GetItemPropertyCostTableValue", 2, 0);

        var detail = _service.GetBlueprintDetails(PlayerObject);

        detail.GuaranteedBonuses.Should().HaveCount(2);
    }

    [Test]
    public void SetBlueprintDetails_destroys_a_blueprint_with_no_licensed_runs()
    {
        _service.SetBlueprintDetails(PlayerObject, new BlueprintDetail { LicensedRuns = 0 });

        _engine.Engine.CountOf("DestroyObject").Should().Be(1);
        _engine.Engine.CountOf("SetLocalInt").Should().Be(0);
    }

    [Test]
    public void SetBlueprintDetails_stores_the_recipe_and_every_blueprint_property()
    {
        _service.SetBlueprintDetails(PlayerObject, new BlueprintDetail
        {
            Recipe = RecipeType.VenomCoating1,
            Level = 2,
            LicensedRuns = 3,
            RandomEnhancementSlotGranted = true
        });

        var setInt = _engine.Engine.CallsTo("SetLocalInt").Should().ContainSingle().Subject;
        setInt.Arguments[1].Should().Be("BLUEPRINT_RECIPE_ID");
        setInt.Arguments[2].Should().Be((int)RecipeType.VenomCoating1);
        _engine.Engine.CallsTo("ItemPropertyCustom").Select(call => call.Arguments[1]).Should().Equal(
            (int)BlueprintSubType.Level,
            (int)BlueprintSubType.LicensedRuns,
            (int)BlueprintSubType.ItemBonuses,
            (int)BlueprintSubType.CreditReduction,
            (int)BlueprintSubType.TimeReduction,
            (int)BlueprintSubType.EnhancementSlots);
        _engine.Engine.CountOf("DestroyObject").Should().Be(0);
    }

    // ---- research and crafting costs

    [Test]
    public void CalculateBlueprintResearchCreditCost_follows_the_formula()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 25).ResearchCostModifier = 0.5f;
        var tier = 3;
        var price = 200 * (Math.Pow(tier, 2.2f) * Math.Pow(4, 2f));
        price += price * 0.5f;
        price -= 0.1f * price;

        _service.CalculateBlueprintResearchCreditCost(RecipeType.VenomCoating1, 4, 10).Should().Be((int)price);
    }

    [Test]
    public void CalculateBlueprintResearchSeconds_uses_the_same_formula_as_the_research_credit_cost()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 25);

        _service.CalculateBlueprintResearchSeconds(RecipeType.VenomCoating1, 4, 10)
            .Should().Be(_service.CalculateBlueprintResearchCreditCost(RecipeType.VenomCoating1, 4, 10));
    }

    [Test]
    public void The_research_tier_of_a_recipe_stops_at_five()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 200);
        AddRecipe(RecipeType.VenomCoating2, level: 40);

        _service.CalculateBlueprintResearchCreditCost(RecipeType.VenomCoating1, 1, 0)
            .Should().Be(_service.CalculateBlueprintResearchCreditCost(RecipeType.VenomCoating2, 1, 0));
    }

    [Test]
    public void CalculateBlueprintCraftCreditCost_reads_the_blueprint_and_applies_its_credit_reduction()
    {
        AddRecipe(RecipeType.VenomCoating1, level: 25);
        _engine.Engine.SetReturnHandler("GetLocalInt", args => (string)args[1]! == "BLUEPRINT_RECIPE_ID" ? (int)RecipeType.VenomCoating1 : 0);
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);
        _engine.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.Blueprint, ItemPropertyType.Blueprint);
        _engine.Engine.SetReturnSequence("GetItemPropertySubType", (int)BlueprintSubType.Level, (int)BlueprintSubType.CreditReduction);
        _engine.Engine.SetReturnSequence("GetItemPropertyCostTableValue", 4, 20);

        var tier = 3;
        var price = 80 * (Math.Pow(tier, 2.2f) * Math.Pow(4, 2f));
        price -= 20 * 0.01f * price;

        _service.CalculateBlueprintCraftCreditCost(PlayerObject).Should().Be((int)price);
    }

    // ---- handlers

    [Test]
    public void OnRemoveProperty_does_nothing_when_the_property_has_no_research_job()
    {
        _events.SetReturn("GetEventData", "prop-1");

        _service.OnRemoveProperty();

        _db.Queries.Should().ContainSingle();
    }
}
