using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class UseRecipeBook
    {
        /// <summary>
        /// When a player uses an item tagged with "RECIPE", the list of recipes associated with the item
        /// are added to their collection. The item is then destroyed.
        /// </summary>
        [NWNEventHandler("recipe")]
        public static void LearnRecipes()
        {
            var user = OBJECT_SELF;

            if (!GetIsPC(user) || GetIsDM(user))
            {
                SendMessageToPC(user, "Only players may use this item.");
                return;
            }

            var item = StringToObject(Events.GetEventData("ITEM_OBJECT_ID"));
            var playerId = GetObjectUUID(user);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipeList = GetLocalString(item, "RECIPES");
            var recipeIds = recipeList.Split(',');
            var recipesLearned = 0;

            foreach (var recipeId in recipeIds)
            {
                // If it fails to parse, exit early.
                if (!int.TryParse(recipeId, out var convertedId))
                {
                    SendMessageToPC(user, "This recipe book has a configuration problem. Please inform a DM.");
                    return;
                }

                // Id number is zero or negative. Skip as those aren't valid.
                if (convertedId <= 0)
                {
                    SendMessageToPC(user, "This recipe book has a configuration problem. Please inform a DM.");
                    return;
                }

                var recipeType = (RecipeType)convertedId;

                // Ensure this type of recipe has been registered.
                if (!Craft.RecipeExists(recipeType))
                {
                    SendMessageToPC(user, "This recipe has not been registered. Please inform a DM.");
                    return;
                }

                // Player already knows this recipe. Move to the next one.
                if (dbPlayer.UnlockedRecipes.ContainsKey(recipeType))
                    continue;

                recipesLearned++;
                dbPlayer.UnlockedRecipes[recipeType] = DateTime.UtcNow;

                var recipeDetail = Craft.GetRecipe(recipeType);
                var skillDetail = Skill.GetSkillDetails(recipeDetail.Skill);
                var itemName = Cache.GetItemNameByResref(recipeDetail.Resref);
                SendMessageToPC(user, $"You learn the {skillDetail.Name} recipe: {itemName}.");
            }

            // Player didn't learn any recipes. Let them know but don't destroy the item.
            if (recipesLearned <= 0)
            {
                SendMessageToPC(user, "You have already learned all of the recipes contained in this book.");
                return;
            }

            DB.Set(playerId, dbPlayer);
            DestroyObject(item);
        }
    }
}
