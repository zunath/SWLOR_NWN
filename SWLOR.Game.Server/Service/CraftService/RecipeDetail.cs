using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

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
        public int? PracticeRankLimit { get; set; }
        public RecipeEnhancementType EnhancementType { get; set; }
        public int EnhancementSlots { get; set; }
        public float ResearchCostModifier { get; set; }
        public bool IsItemIntendedForCrafting { get; set; }
        public List<string> EffectLines { get; set; }
        public IReadOnlyCollection<EnhancementSubType> AllowedEnhancements { get; set; }
        public IReadOnlyCollection<ItemPropertyType> AllowedOutputProperties { get; set; }

        public bool AllowsEnhancement(EnhancementSubType type) => AllowedEnhancements == null || AllowedEnhancements.Contains(type);
        public bool AllowsOutputProperty(ItemPropertyType type) => AllowedOutputProperties == null || AllowedOutputProperties.Contains(type);

        public RecipeDetail()
        {
            IsActive = true;
            Quantity = 1;
            Category = RecipeCategoryType.Uncategorized;
            EnhancementType = RecipeEnhancementType.None;
            EnhancementSlots = 0;

            Requirements = new List<IRecipeRequirement>();
            Components = new Dictionary<string, int>();
            EffectLines = new List<string>();
        }
    }
}
