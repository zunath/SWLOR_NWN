using System.Reflection;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Reads the gameplay enums a placeable's behavior fields pick from: crafting skills and
    /// visual effects.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="ReflectionEnumReader"/>, which answers validation questions about
    /// content identity (NPC groups, key items). These two are presentation lists for pickers, and
    /// they are the ones a placeable stores as a bare number - <c>CRAFTING_SKILL_TYPE_ID</c> is 31
    /// or 49 in the corpus with nothing to say which skill that is.
    /// </remarks>
    internal static class ReflectionGameplayEnumReader
    {
        /// <summary>Crafting-capable skills, by <c>SkillType</c> value, with their display names.</summary>
        public static IReadOnlyDictionary<int, string> ReadSkillTypes()
        {
            var result = new Dictionary<int, string>();

            foreach (SkillType value in System.Enum.GetValues<SkillType>())
            {
                if (value == SkillType.Invalid)
                    continue;

                var field = typeof(SkillType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<SkillAttribute>();
                if (attribute?.IsActive != true || !attribute.IsShownInCraftMenu)
                    continue;

                var name = attribute?.Name;

                result[(int)value] = string.IsNullOrWhiteSpace(name) ? value.ToString() : name;
            }

            return result;
        }

        /// <summary>Active market-region ids with their player-facing names.</summary>
        public static IReadOnlyDictionary<int, string> ReadMarketRegions()
        {
            var result = new Dictionary<int, string>();

            foreach (MarketRegionType value in System.Enum.GetValues<MarketRegionType>())
            {
                var field = typeof(MarketRegionType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<MarketRegionAttribute>();
                if (attribute?.IsActive != true)
                    continue;

                result[(int)value] = attribute.Name;
            }

            return result;
        }

        /// <summary>Visual effect ids with their enum names.</summary>
        public static IReadOnlyDictionary<int, string> ReadVisualEffects()
        {
            var result = new Dictionary<int, string>();

            foreach (VisualEffect value in System.Enum.GetValues<VisualEffect>())
                result[(int)value] = value.ToString();

            return result;
        }
    }
}
