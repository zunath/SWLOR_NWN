using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class Account: EntityBase
    {
        public Account()
        {
            Init();
        }

        public Account(string cdKey)
        {
            Id = cdKey;
            Init();
        }

        private void Init()
        {
            Achievements = new Dictionary<AchievementType, DateTime>();
            AchievementProgress = new AchievementProgress();
        }

        [Indexed]
        public ulong TimesLoggedIn { get; set; }

        [Indexed]
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
