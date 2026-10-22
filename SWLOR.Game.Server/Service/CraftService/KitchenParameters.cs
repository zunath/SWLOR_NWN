using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The per-recipe values the Kitchen minigame reads: the dish, the flavor target (its center and the inner and outer
    /// radius) and the Freshness the dish starts with. Set by <see cref="RecipeBuilder.KitchenParameters"/>.
    /// </summary>
    public sealed class KitchenParameters : RecipeMinigameParameters
    {
        public KitchenDish Dish { get; }

        /// <summary>The center of the target zone on the Savory axis.</summary>
        public int TargetSavory { get; }

        /// <summary>The center of the target zone on the Sweet axis.</summary>
        public int TargetSweet { get; }

        /// <summary>The center of the target zone on the Spice axis.</summary>
        public int TargetSpice { get; }

        /// <summary>Quality is 100% while the flavor stays this close to the center.</summary>
        public int InnerRadius { get; }

        /// <summary>Quality falls to 0% at this distance from the center.</summary>
        public int OuterRadius { get; }

        /// <summary>The Freshness the dish starts with; the dish is ruined at 0.</summary>
        public int Freshness { get; }

        public KitchenParameters(
            KitchenDish dish,
            int targetSavory,
            int targetSweet,
            int targetSpice,
            int innerRadius,
            int outerRadius,
            int freshness)
        {
            Dish = dish;
            TargetSavory = targetSavory;
            TargetSweet = targetSweet;
            TargetSpice = targetSpice;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            Freshness = freshness;
        }

        public override IReadOnlyList<RecipeParameterValue> Values => new[]
        {
            new RecipeParameterValue("dish", (int)Dish),
            new RecipeParameterValue("targetSavory", TargetSavory),
            new RecipeParameterValue("targetSweet", TargetSweet),
            new RecipeParameterValue("targetSpice", TargetSpice),
            new RecipeParameterValue("innerRadius", InnerRadius),
            new RecipeParameterValue("outerRadius", OuterRadius),
            new RecipeParameterValue("freshness", Freshness)
        };
    }
}
