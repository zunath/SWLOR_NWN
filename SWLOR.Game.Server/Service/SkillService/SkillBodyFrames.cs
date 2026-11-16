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

        /// <summary>
        /// The skill a body frame trains in place of another. An astromech has no Armor skill: its Chassis
        /// rank stands wherever a humanoid's Armor rank is read (defense, evasion, combat XP, and the perks and
        /// gear that ask for Armor). Every other skill is its own equivalent.
        /// </summary>
        public static SkillType GetFrameEquivalent(SkillType skill, BodyFrame frame)
        {
            return skill == SkillType.Armor && frame == BodyFrame.Astromech ? SkillType.Chassis : skill;
        }

        /// <summary>
        /// The astromech skill that mirrors a humanoid one: its perk lines are copies of that skill's lines, so
        /// whatever asks a character to have mastered the humanoid skill (a capstone quest) accepts the mirror
        /// from an astromech. This does not open the humanoid skill's own perks; those stay behind
        /// <see cref="IsOpenTo"/>.
        /// </summary>
        public static SkillType GetMirror(SkillType skill, BodyFrame frame)
        {
            if (frame != BodyFrame.Astromech)
                return skill;

            return skill switch
            {
                SkillType.Armor => SkillType.Chassis,
                SkillType.FirstAid => SkillType.UtilitySystems,
                _ => skill
            };
        }

        /// <summary>
        /// The skill whose rank is the defensive skill level of a character: Armor, or Chassis for an astromech.
        /// </summary>
        public static SkillType GetDefenseSkill(BodyFrame frame)
        {
            return GetFrameEquivalent(SkillType.Armor, frame);
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
