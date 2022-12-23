using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeDetail
    {
        public int Quantity { get; set; }
        public string Resref { get; set; }
        public List<IRecipeRequirement> Requirements { get; set; }
        public Dictionary<string, int> Components { get; set; }
        public SkillType Skill { get; set; }
        public RecipeCategoryType Category { get; set; }
        public bool IsActive { get; set; }
        public int Level { get; set; }
        public RecipeEnhancementType EnhancementType { get; set; }
        public int EnhancementSlots { get; set; }

        public RecipeDetail()
        {
            IsActive = true;
            Quantity = 1;
            Category = RecipeCategoryType.Uncategorized;
            EnhancementType = RecipeEnhancementType.None;
            EnhancementSlots = 0;

            Requirements = new List<IRecipeRequirement>();
            Components = new Dictionary<string, int>();
        }
    }
}
