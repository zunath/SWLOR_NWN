using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Crafting.Model
{
    public class RecipeUnlockRequirement: IRecipeRequirement
    {
        private readonly IDatabaseService _db;
        private readonly RecipeType _recipe;
        public RecipeUnlockRequirement(IDatabaseService db, RecipeType recipe)
        {
            _db = db;
            _recipe = recipe;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.UnlockedRecipes.ContainsKey(_recipe))
                return "Recipe must be learned.";

            return string.Empty;
        }

        public string RequirementText => "Recipe must be learned.";
    }
}
