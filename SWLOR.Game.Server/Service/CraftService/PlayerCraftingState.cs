using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class PlayerCraftingState
    {
        public RecipeType SelectedRecipe { get; set; }
        public SkillType DeviceSkillType { get; set; }
        public bool IsOpeningMenu { get; set; }
        public bool IsAutoCrafting { get; set; }

        public PlayerCraftingState()
        {
            SelectedRecipe = RecipeType.Invalid;
            DeviceSkillType = SkillType.Invalid;
        }

    }
}
