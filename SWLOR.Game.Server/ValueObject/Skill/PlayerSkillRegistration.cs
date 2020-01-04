using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject.Skill
{
    public class PlayerSkillRegistration
    {
        public NWPlayer Player { get; private set; }
        private Dictionary<Enumeration.Skill, PlayerSkillPointTracker> SkillPoints { get; set; }
        public int HighestRank { get; private set; }

        public PlayerSkillRegistration(NWPlayer oPC)
        {
            SkillPoints = new Dictionary<Enumeration.Skill, PlayerSkillPointTracker>();
            Player = oPC;
        }


        public void AddSkillPointRegistration(Enumeration.Skill skillID, int weaponLevel, int skillRank)
        {
            if (skillRank > HighestRank) HighestRank = skillRank;
            PlayerSkillPointTracker tracker;

            if (!SkillPoints.ContainsKey(skillID))
            {
                tracker = new PlayerSkillPointTracker(skillID);
            }
            else tracker = SkillPoints[skillID];

            tracker.Points++;

            // Always take the lowest weapon level.
            if (tracker.RegisteredLevel == -1 || weaponLevel < tracker.RegisteredLevel)
            {
                tracker.RegisteredLevel = weaponLevel;
            }

            SkillPoints[skillID] = tracker;
        }

        public List<Tuple<Enumeration.Skill, PlayerSkillPointTracker>> GetSkillRegistrationPoints()
        {
            List<Tuple<Enumeration.Skill, PlayerSkillPointTracker>> result = new List<Tuple<Enumeration.Skill, PlayerSkillPointTracker>>();

            foreach (var sp in SkillPoints)
            {
                Tuple<Enumeration.Skill, PlayerSkillPointTracker> pair = new Tuple<Enumeration.Skill, PlayerSkillPointTracker>(sp.Key, sp.Value);
                result.Add(pair);
            }
            
            return result;
        }

        public int GetTotalSkillRegistrationPoints()
        {
            int totalPoints = 0;

            foreach (Tuple<Enumeration.Skill, PlayerSkillPointTracker> reg in GetSkillRegistrationPoints())
            {
                totalPoints += reg.Item2.Points;
            }

            return totalPoints;
        }
    }
}
