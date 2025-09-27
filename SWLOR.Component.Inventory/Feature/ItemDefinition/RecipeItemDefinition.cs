using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class RecipeItemDefinition: IItemListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public RecipeItemDefinition(IDatabaseService db, IItemCacheService itemCache, IServiceProvider serviceProvider)
        {
            _db = db;
            _itemCache = itemCache;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private ICraftService CraftService => _serviceProvider.GetRequiredService<ICraftService>();
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Recipe();

            return Builder.Build();
        }

        private void Recipe()
        {
            Builder.Create("RECIPE")
                .Delay(3f)
                .PlaysAnimation(AnimationType.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var characterTypeRestriction = (CharacterType)GetLocalInt(item, "CHARACTER_TYPE");

                    if (characterTypeRestriction != CharacterType.Invalid &&
                        characterTypeRestriction != dbPlayer.CharacterType)
                    {
                        var characterType = PerkService.GetCharacterType(characterTypeRestriction);
                        return $"This recipe can only be learned by {characterType.Name} characters.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var recipeList = GetLocalString(item, "RECIPES");
                    var recipeIds = recipeList.Split(',');
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = _db.Get<Player>(playerId);
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
                        if (!CraftService.RecipeExists(recipeType))
                        {
                            SendMessageToPC(user, "This recipe has not been registered. Please inform a DM.");
                            return;
                        }

                        // Player already knows this recipe. Move to the next one.
                        if (dbPlayer.UnlockedRecipes.ContainsKey(recipeType))
                            continue;

                        recipesLearned++;
                        dbPlayer.UnlockedRecipes[recipeType] = DateTime.UtcNow;

                        var recipeDetail = CraftService.GetRecipe(recipeType);
                        var skillDetail = SkillService.GetSkillDetails(recipeDetail.Skill);
                        var itemName = _itemCache.GetItemNameByResref(recipeDetail.Resref);
                        SendMessageToPC(user, $"You learn the {skillDetail.Name} recipe: {itemName}.");
                    }

                    // Player didn't learn any recipes. Let them know but don't destroy the item.
                    if (recipesLearned <= 0)
                    {
                        SendMessageToPC(user, "You have already learned all of the recipes contained in this book.");
                        return;
                    }

                    _db.Set(dbPlayer);

                    ItemService.ReduceItemStack(item, 1);
                });
        }
    }
}
