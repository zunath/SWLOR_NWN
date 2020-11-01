using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string Resref { get; set; }
        public List<IRecipeRequirement> Requirements { get; set; }
        public Dictionary<string, int> Components { get; set; }
        public SkillType Skill { get; set; }
        public RecipeCategoryType Category { get; set; }
        public bool IsActive { get; set; }
        public int Level { get; set; }

        public RecipeDetail()
        {
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            Quantity = 1;
            Category = RecipeCategoryType.Uncategorized;

            Requirements = new List<IRecipeRequirement>();
            Components = new Dictionary<string, int>();
        }
    }
}
