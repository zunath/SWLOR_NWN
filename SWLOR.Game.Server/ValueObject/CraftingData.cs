using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class CraftingData
    {
        public int BlueprintID { get; set; }
        public CraftBlueprint Blueprint { get; set; }
        public List<NWItem> MainComponents { get; set; }
        public List<NWItem> SecondaryComponents { get; set; }
        public List<NWItem> TertiaryComponents { get; set; }
        public List<NWItem> EnhancementComponents { get; set; }
        public bool IsAccessingStorage { get; set; }
        public CraftingAccessType Access { get; set; }

        public int AdjustedLevel
        {
            get
            {
                int adjustedLevel = Blueprint.BaseLevel;
                foreach (var comp in MainComponents)
                {
                    adjustedLevel += comp.RecommendedLevel;
                }
                foreach (var comp in SecondaryComponents)
                {
                    adjustedLevel += comp.RecommendedLevel;
                }
                foreach (var comp in TertiaryComponents)
                {
                    adjustedLevel += comp.RecommendedLevel;
                }
                foreach (var comp in EnhancementComponents)
                {
                    adjustedLevel += comp.RecommendedLevel;
                }

                return adjustedLevel;
            }
        }

        public bool HasPlayerComponents => MainComponents.Count
                                           + SecondaryComponents.Count
                                           + TertiaryComponents.Count
                                           + EnhancementComponents.Count > 0;

        public bool CanBuildItem => MainComponents.Count >= Blueprint.MainMinimum
                                    && SecondaryComponents.Count >= Blueprint.SecondaryMinimum
                                    && TertiaryComponents.Count >= Blueprint.TertiaryMinimum;

        public CraftingData()
        {
            MainComponents = new List<NWItem>();
            SecondaryComponents = new List<NWItem>();
            TertiaryComponents = new List<NWItem>();
            EnhancementComponents = new List<NWItem>();
        }
    }
}
