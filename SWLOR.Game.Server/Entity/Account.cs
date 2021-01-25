using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Entity
{
    public class Account: EntityBase
    {
        public Account()
        {
            Achievements = new Dictionary<AchievementType, DateTime>();
            AchievementProgress = new AchievementProgress();
        }

        public override string KeyPrefix => "Account";

        public ulong TimesLoggedIn { get; set; }

        public bool HasCompletedTutorial { get; set; }

        public Dictionary<AchievementType, DateTime> Achievements { get; set; }

        public AchievementProgress AchievementProgress { get; set; }
    }

    public class AchievementProgress
    {
        public ulong EnemiesKilled { get; set; }
        public ulong PerksLearned { get; set; }
        public ulong SkillsLearned { get; set; }
        public ulong QuestsCompleted { get; set; }
        public ulong ItemsCrafted { get; set; }
    }
}
