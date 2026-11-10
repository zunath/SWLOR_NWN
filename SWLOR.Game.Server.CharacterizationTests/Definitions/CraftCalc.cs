using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// The crafting calculations under test, called the way the server holds them. The parity run compiles these tests against
/// master, where <c>Craft</c> is one static class, and against the refactor branch, where the static <c>Craft</c> forwards to
/// a <c>CraftService</c> instance that needs the server's service container. Looking the instance class up by name lets one
/// source file do both: the branch calls the instance (built without neighbours (every constructor argument null), which these calculations do not use) and
/// master calls the static class.
/// </summary>
internal static class CraftCalc
{
    private static readonly Assembly ServerAssembly = typeof(RecipeDetail).Assembly;

    private static readonly Type InstanceType =
        ServerAssembly.GetType("SWLOR.Game.Server.Domain.Crafting.CraftService");

    // Master's static class; the branch has none once its facade is removed.
    private static readonly Type StaticType = ServerAssembly.GetType("SWLOR.Game.Server.Service.Craft");

    private static readonly object Instance = InstanceType == null
        ? null
        : Build(InstanceType);

    // Every constructor argument is null except the skill service, whose experience tables the calculations read.
    private static object Build(Type type) =>
        Activator.CreateInstance(type, type.GetConstructors()[0].GetParameters()
            .Select(parameter => parameter.ParameterType.Name == "ISkillService"
                ? Build(ServerAssembly.GetType("SWLOR.Game.Server.Domain.Character.SkillService"))
                : null)
            .ToArray());

    private static object Call(string name, params object[] arguments)
    {
        var method = (Instance == null ? StaticType : InstanceType).GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        return method!.Invoke(Instance, arguments);
    }

    public static Dictionary<RecipeType, RecipeDetail> GetAllRecipes() =>
        (Dictionary<RecipeType, RecipeDetail>)Call("GetAllRecipes");

    public static RecipeLevelDetail GetRecipeLevelDetail(int level) =>
        (RecipeLevelDetail)Call("GetRecipeLevelDetail", level);

    public static int GetRequiredSkillRankForRecipe(RecipeDetail recipe) =>
        (int)Call("GetRequiredSkillRankForRecipe", recipe);

    public static int GetBaseRecipeXP(RecipeDetail recipe, int skillRank) =>
        (int)Call("GetBaseRecipeXP", recipe, skillRank);

    public static int CalculateBlueprintResearchCreditCost(RecipeType recipe, int blueprintLevel, int reductionBonus) =>
        (int)Call("CalculateBlueprintResearchCreditCost", recipe, blueprintLevel, reductionBonus);

    public static int CalculateBlueprintResearchSeconds(RecipeType recipe, int blueprintLevel, int reductionBonus) =>
        (int)Call("CalculateBlueprintResearchSeconds", recipe, blueprintLevel, reductionBonus);

    public static bool IsWeaponDamageEnhancement(EnhancementSubType subType) =>
        (bool)Call("IsWeaponDamageEnhancement", subType);

    public static bool TryGetWeaponDamageTypeForEnhancement(
        EnhancementSubType subType, CombatDamageType explicitDamageType, out CombatDamageType damageType)
    {
        var arguments = new object[] { subType, explicitDamageType, CombatDamageType.Invalid };
        var result = (bool)Call("TryGetWeaponDamageTypeForEnhancement", arguments);
        damageType = (CombatDamageType)arguments[2];
        return result;
    }
}
