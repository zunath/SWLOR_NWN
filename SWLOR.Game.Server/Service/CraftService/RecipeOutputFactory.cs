using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public static class RecipeOutputFactory
    {
        /// <summary>
        /// Creates separate food portions because food cannot stack, while preserving
        /// the native stack creation behavior for other recipe categories.
        /// </summary>
        public static IReadOnlyList<uint> Create(RecipeDetail recipe, Func<int, uint> createItem)
        {
            ArgumentNullException.ThrowIfNull(recipe);
            ArgumentNullException.ThrowIfNull(createItem);

            var isFood = recipe.Category == RecipeCategoryType.Food;
            var outputs = new uint[isFood ? recipe.Quantity : 1];
            var stackSize = isFood ? 1 : recipe.Quantity;

            for (var index = 0; index < outputs.Length; index++)
            {
                outputs[index] = createItem(stackSize);
            }

            return outputs;
        }

        /// <summary>
        /// Keeps a batch atomic when the inventory cannot receive every output.
        /// </summary>
        public static bool TryCreate(
            RecipeDetail recipe,
            Func<int, uint> createItem,
            Func<uint, bool> isValidItem,
            Action<uint> destroyItem,
            out IReadOnlyList<uint> outputs)
        {
            ArgumentNullException.ThrowIfNull(isValidItem);
            ArgumentNullException.ThrowIfNull(destroyItem);

            outputs = Create(recipe, createItem);
            var isComplete = true;
            foreach (var output in outputs)
            {
                if (!isValidItem(output))
                    isComplete = false;
            }

            if (isComplete)
                return true;

            foreach (var output in outputs)
            {
                if (isValidItem(output))
                    destroyItem(output);
            }

            outputs = Array.Empty<uint>();
            return false;
        }
    }
}
