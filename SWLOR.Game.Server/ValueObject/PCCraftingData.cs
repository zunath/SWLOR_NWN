using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCCraftingData
    {
        // Normal Crafting properties
        public int BlueprintID { get; set; }
        public CraftBlueprint Blueprint { get; set; }
        public int MainMinimum { get; set; }
        public int MainMaximum { get; set; }
        public int SecondaryMinimum { get; set; }
        public int SecondaryMaximum { get; set; }
        public int TertiaryMinimum { get; set; }
        public int TertiaryMaximum { get; set; }
        public List<NWItem> MainComponents { get; set; }
        public List<NWItem> SecondaryComponents { get; set; }
        public List<NWItem> TertiaryComponents { get; set; }
        public List<NWItem> EnhancementComponents { get; set; }
        public bool IsAccessingStorage { get; set; }
        public CraftingAccessType Access { get; set; }
        public int PlayerPerkLevel { get; set; }
        public int PlayerSkillRank { get; set; }
        public bool IsInitialized { get; set; }
        
        public int AdjustedLevel
        {
            get
            {
                int adjustedLevel = Blueprint.BaseLevel;
                foreach (var comp in MainComponents)
                {
                    adjustedLevel += comp.LevelIncrease > 0 ? comp.LevelIncrease : comp.RecommendedLevel;
                }
                foreach (var comp in SecondaryComponents)
                {
                    adjustedLevel += comp.LevelIncrease > 0 ? comp.LevelIncrease : comp.RecommendedLevel;
                }
                foreach (var comp in TertiaryComponents)
                {
                    adjustedLevel += comp.LevelIncrease > 0 ? comp.LevelIncrease : comp.RecommendedLevel;
                }
                foreach (var comp in EnhancementComponents)
                {
                    adjustedLevel += comp.LevelIncrease > 0 ? comp.LevelIncrease : comp.RecommendedLevel;
                }

                return adjustedLevel;
            }
        }

        public bool HasPlayerComponents => MainComponents.Count
                                           + SecondaryComponents.Count
                                           + TertiaryComponents.Count
                                           + EnhancementComponents.Count > 0;

        public bool CanBuildItem => MainComponents.Count >= MainMinimum
                                    && SecondaryComponents.Count >= SecondaryMinimum
                                    && TertiaryComponents.Count >= TertiaryMinimum;
        
        // Molecular Reassembly properties
        public int SalvageComponentTypeID { get; set; }
        public string SerializedSalvageItem { get; set; }
        public bool IsConfirmingReassemble { get; set; }

        public PCCraftingData()
        {
            MainComponents = new List<NWItem>();
            SecondaryComponents = new List<NWItem>();
            TertiaryComponents = new List<NWItem>();
            EnhancementComponents = new List<NWItem>();
        }
    }
}
