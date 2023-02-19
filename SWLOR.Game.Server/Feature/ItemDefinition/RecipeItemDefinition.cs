using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ItemService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class RecipeItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Recipe();

            return _builder.Build();
        }

        private void Recipe()
        {
            _builder.Create("RECIPE")
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var characterTypeRestriction = (CharacterType)GetLocalInt(item, "CHARACTER_TYPE");

                    if (characterTypeRestriction != CharacterType.Invalid &&
                        characterTypeRestriction != dbPlayer.CharacterType)
                    {
                        var characterType = Perk.GetCharacterType(characterTypeRestriction);
                        return $"This recipe can only be learned by {characterType.Name} characters.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var recipeList = GetLocalString(item, "RECIPES");
                    var recipeIds = recipeList.Split(',');
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);
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

                    DB.Set(dbPlayer);

                    Item.ReduceItemStack(item, 1);
                });
        }
    }
}
