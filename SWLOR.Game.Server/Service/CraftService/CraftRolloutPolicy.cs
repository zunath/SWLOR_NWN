using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftRollout { Legacy, Pilot, Full }
    public static class CraftRolloutPolicy
    {
        public static bool UseConditions(CraftRollout rollout, RecipeDetail recipe) => recipe.CraftingProfile != CraftProfile.Legacy &&
            (rollout == CraftRollout.Full || rollout == CraftRollout.Pilot && recipe.IsCraftingPilot);
        public static string BuffName(CraftBuffType buff) => Regex.Replace(buff.ToString(), "([a-z])([A-Z])", "$1 $2");
        public static string StatName(StatType stat) => Regex.Replace(stat.ToString().Replace("Crafting", ""), "([a-z])([A-Z])", "$1 $2");
        public static string ProfileDescription(CraftProfile profile) => profile switch
        {
            CraftProfile.Sturdy => "Synthesis spends less durability; touches gain less quality. Quality target is adjusted.",
            CraftProfile.Delicate => "Touches gain more quality; Rapid Synthesis spends more durability. Quality target is adjusted.",
            CraftProfile.Calibrated => "Switching successful synthesis and touch improves the next work action. Progress target is adjusted.",
            _ => "Classic crafting rules."
        };
    }
}
