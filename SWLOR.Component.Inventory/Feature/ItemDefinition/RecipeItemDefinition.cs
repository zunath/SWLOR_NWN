using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Model;
using SWLOR.Component.Inventory.Service;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class RecipeItemDefinition: IItemListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly IPerkService _perkService;
        private readonly ICraftService _craftService;
        private readonly ISkillService _skillService;
        private readonly IItemService _itemService;
        private readonly ItemBuilder _builder = new();

        public RecipeItemDefinition(IDatabaseService db, IItemCacheService itemCache, IPerkService perkService, ICraftService craftService, ISkillService skillService, IItemService itemService)
        {
            _db = db;
            _itemCache = itemCache;
            _perkService = perkService;
            _craftService = craftService;
            _skillService = skillService;
            _itemService = itemService;
        }

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
                    var dbPlayer = _db.Get<Player>(playerId);
                    var characterTypeRestriction = (CharacterType)GetLocalInt(item, "CHARACTER_TYPE");

                    if (characterTypeRestriction != CharacterType.Invalid &&
                        characterTypeRestriction != dbPlayer.CharacterType)
                    {
                        var characterType = _perkService.GetCharacterType(characterTypeRestriction);
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
                        if (!_craftService.RecipeExists(recipeType))
                        {
                            SendMessageToPC(user, "This recipe has not been registered. Please inform a DM.");
                            return;
                        }

                        // Player already knows this recipe. Move to the next one.
                        if (dbPlayer.UnlockedRecipes.ContainsKey(recipeType))
                            continue;

                        recipesLearned++;
                        dbPlayer.UnlockedRecipes[recipeType] = DateTime.UtcNow;

                        var recipeDetail = _craftService.GetRecipe(recipeType);
                        var skillDetail = _skillService.GetSkillDetails(recipeDetail.Skill);
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

                    _itemService.ReduceItemStack(item, 1);
                });
        }
    }
}
