using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The checks behind a minigame's coverage test. Each returns one message per problem, or an empty list.
    /// </summary>
    public static class RecipeParameterAudit
    {
        private const double Tolerance = 1e-9;
        private static readonly Regex KeyPattern = new Regex("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.Compiled);

        /// <summary>Checks that a profile is well formed before it generates anything.</summary>
        public static IReadOnlyList<string> ValidateProfile(IRecipeParameterProfile profile)
        {
            var problems = new List<string>();

            if (string.IsNullOrEmpty(profile.Key) || !KeyPattern.IsMatch(profile.Key))
                problems.Add($"key '{profile.Key}' must be letters and digits starting with a letter");

            var definitions = profile.Definitions;
            if (definitions == null || definitions.Count == 0)
            {
                problems.Add("it declares no parameters");
                return problems;
            }

            foreach (var duplicate in definitions.GroupBy(d => d.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                problems.Add($"parameter name '{duplicate.Key}' is declared twice");
            }

            var headers = definitions.Select(d => d.Header).Append(RecipeParameterBibleColumns.OverrideHeader(profile)).ToList();
            foreach (var duplicate in headers.GroupBy(h => h, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            {
                problems.Add($"Bible header '{duplicate.Key}' is used twice");
            }

            var parametersType = profile.ParametersType;
            if (!typeof(RecipeMinigameParameters).IsAssignableFrom(parametersType) || parametersType.IsAbstract)
            {
                problems.Add($"{parametersType.Name} must be a concrete RecipeMinigameParameters");
                return problems;
            }

            var constructors = parametersType.GetConstructors();
            var matching = constructors.Any(constructor => ConstructorMatches(constructor.GetParameters(), definitions));
            if (!matching)
            {
                problems.Add(
                    $"{parametersType.Name} needs a public constructor taking exactly " +
                    $"({string.Join(", ", definitions.Select(d => d.Name))}) with int, double or enum types as declared");
            }

            return problems;
        }

        /// <summary>
        /// Returns null when the value is allowed for the named parameter, otherwise the reason it is not.
        /// </summary>
        public static string CheckValue(IRecipeParameterProfile profile, string name, double value)
        {
            var definition = profile.Definitions.FirstOrDefault(d => d.Name == name);
            if (definition == null)
                return $"'{name}' is not a declared parameter";
            if (!definition.IsInBounds(value))
                return $"{name}={value} is outside [{definition.Min}, {definition.Max}]";
            if (!definition.IsOnGrid(value))
                return definition.EnumType != null
                    ? $"{name}={value} is not a member of {definition.EnumType.Name}"
                    : $"{name}={value} is not on the {definition.Decimals}-decimal grid";

            return null;
        }

        /// <summary>Applicable recipes without parameters of the profile's type, and non-applicable ones that carry them.</summary>
        public static IReadOnlyList<string> FindMissingParameters(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            var problems = new List<string>();
            foreach (var (type, detail) in Ordered(recipes))
            {
                if (detail.Skill != profile.Skill)
                    continue;

                var has = profile.ParametersType.IsInstanceOfType(detail.MinigameParameters);
                var applies = profile.AppliesTo(RecipeParameterContext.From(type, detail));
                if (applies && !has)
                {
                    problems.Add(detail.MinigameParameters == null
                        ? $"{type} has no {profile.ParametersType.Name}"
                        : $"{type} has {detail.MinigameParameters.GetType().Name} instead of {profile.ParametersType.Name}");
                }
                else if (!applies && has)
                {
                    problems.Add($"{type} is not covered by {profile.Key} but still has {profile.ParametersType.Name}");
                }
            }

            return problems;
        }

        /// <summary>Values that are missing, undeclared, outside their bounds or off their grid.</summary>
        public static IReadOnlyList<string> FindOutOfBounds(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            var problems = new List<string>();
            foreach (var (type, detail) in Covered(profile, recipes))
            {
                var values = detail.MinigameParameters.Values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
                foreach (var definition in profile.Definitions)
                {
                    if (!values.TryGetValue(definition.Name, out var value))
                    {
                        problems.Add($"{type}: {definition.Name} is missing");
                        continue;
                    }

                    var problem = CheckValue(profile, definition.Name, value);
                    if (problem != null)
                        problems.Add($"{type}: {problem}");
                }

                foreach (var name in values.Keys.Where(name => profile.Definitions.All(d => d.Name != name)))
                {
                    problems.Add($"{type}: {name} is not a declared parameter");
                }
            }

            return problems;
        }

        /// <summary>Values that differ from the generator's output, which already includes the recorded overrides.</summary>
        public static IReadOnlyList<string> FindGeneratorDrift(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            var problems = new List<string>();
            foreach (var (type, detail) in Covered(profile, recipes))
            {
                var generated = RecipeParameterGenerator.Generate(profile, RecipeParameterContext.From(type, detail));
                var actual = detail.MinigameParameters.Values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
                foreach (var (name, expected) in generated.Values)
                {
                    if (!actual.TryGetValue(name, out var value) || Math.Abs(value - expected) > Tolerance)
                    {
                        var shown = actual.TryGetValue(name, out var found) ? found.ToString() : "missing";
                        problems.Add($"{type}: {name} is {shown} but the generator gives {expected}; record an override or regenerate");
                    }
                }
            }

            return problems;
        }

        /// <summary>Recipes whose difficulty falls outside the budget of their level.</summary>
        public static IReadOnlyList<string> FindOutOfBudget(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            var problems = new List<string>();
            foreach (var (type, detail) in Covered(profile, recipes))
            {
                var difficulty = profile.MeasureDifficulty(detail.MinigameParameters);
                var budget = profile.BudgetFor(detail.Level);
                if (!budget.Contains(difficulty))
                    problems.Add($"{type}: difficulty {difficulty} is outside the level {detail.Level} budget {budget}");
            }

            return problems;
        }

        /// <summary>Overrides with no reason, undeclared or disallowed values, or no recipe left to apply to.</summary>
        public static IReadOnlyList<string> FindInvalidOverrides(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            var problems = new List<string>();
            var applicable = Ordered(recipes)
                .Where(pair => pair.Value.Skill == profile.Skill &&
                               profile.AppliesTo(RecipeParameterContext.From(pair.Key, pair.Value)))
                .ToList();

            foreach (var (category, categoryOverride) in profile.Overrides.Categories)
            {
                problems.AddRange(CheckOverride(profile, $"category {category}", categoryOverride));
                if (applicable.All(pair => pair.Value.Category != category))
                    problems.Add($"category {category} override matches no recipe the profile covers");
            }

            foreach (var (type, recipeOverride) in profile.Overrides.Recipes)
            {
                problems.AddRange(CheckOverride(profile, $"recipe {type}", recipeOverride));
                if (applicable.All(pair => pair.Key != type))
                    problems.Add($"recipe {type} override matches no recipe the profile covers");
            }

            return problems;
        }

        private static IEnumerable<string> CheckOverride(
            IRecipeParameterProfile profile,
            string label,
            RecipeParameterOverride recipeOverride)
        {
            if (string.IsNullOrWhiteSpace(recipeOverride.Reason))
                yield return $"{label} override has no reason";

            foreach (var (name, value) in recipeOverride.Values)
            {
                var problem = CheckValue(profile, name, value);
                if (problem != null)
                    yield return $"{label} override: {problem}";
            }
        }

        private static IEnumerable<KeyValuePair<RecipeType, RecipeDetail>> Covered(
            IRecipeParameterProfile profile,
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            return Ordered(recipes).Where(pair =>
                pair.Value.Skill == profile.Skill &&
                profile.AppliesTo(RecipeParameterContext.From(pair.Key, pair.Value)) &&
                profile.ParametersType.IsInstanceOfType(pair.Value.MinigameParameters));
        }

        private static IEnumerable<KeyValuePair<RecipeType, RecipeDetail>> Ordered(
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes)
        {
            return recipes.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal);
        }

        private static bool ConstructorMatches(
            System.Reflection.ParameterInfo[] parameters,
            IReadOnlyList<RecipeParameterDefinition> definitions)
        {
            if (parameters.Length != definitions.Count)
                return false;

            for (var index = 0; index < parameters.Length; index++)
            {
                var definition = definitions[index];
                var expectedType = definition.EnumType != null
                    ? definition.EnumType
                    : definition.Decimals == 0 ? typeof(int) : typeof(double);

                if (parameters[index].Name != definition.Name || parameters[index].ParameterType != expectedType)
                    return false;
            }

            return true;
        }
    }
}
