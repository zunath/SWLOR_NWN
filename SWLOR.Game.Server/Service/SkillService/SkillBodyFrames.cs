using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Service.SkillService
{
    /// <summary>
    /// Which body frames may train each skill, read once from the skill definitions.
    /// </summary>
    public static class SkillBodyFrames
    {
        private static readonly Lazy<Dictionary<SkillType, SkillAttribute>> Definitions = new(() =>
            Enum.GetValues(typeof(SkillType)).Cast<SkillType>()
                .ToDictionary(skill => skill, skill => skill.GetAttribute<SkillType, SkillAttribute>()));

        /// <summary>
        /// Whether a character with the given body frame can train a skill.
        /// </summary>
        public static bool IsOpenTo(SkillType skill, BodyFrame frame)
        {
            return !Definitions.Value.TryGetValue(skill, out var definition) ||
                   definition == null ||
                   definition.BodyFrameRestriction.Allows(frame);
        }

        public static string GetRefusal(SkillType skill)
        {
            var name = Definitions.Value.TryGetValue(skill, out var definition) && definition != null
                ? definition.Name
                : skill.ToString();
            return $"{name} cannot be trained by your species.";
        }
    }
}
