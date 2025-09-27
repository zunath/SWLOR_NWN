using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class AchievementProgression
    {
        private readonly IDatabaseService _db;
        private readonly IAchievementService _achievement;

        public AchievementProgression(IDatabaseService db, IAchievementService achievement)
        {
            _db = db;
            _achievement = achievement;
        }
        
        /// <summary>
        /// When a player enters the mod, increase their number of logins
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void LogIn()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = _db.Get<Account>(cdKey) ?? new Account(cdKey);

            dbAccount.TimesLoggedIn++;
            _db.Set(dbAccount);
        }

        /// <summary>
        /// When a player enters an area, if an achievement is assigned to the area grant it to them.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void EnterArea()
        {
            var area = OBJECT_SELF;
            var player = GetEnteringObject();
            var exploreAchievementType = (AchievementType)GetLocalInt(area, "EXPLORE_ACHIEVEMENT_ID");

            if (exploreAchievementType == AchievementType.Invalid)
                return;

            _achievement.GiveAchievement(player, exploreAchievementType);
        }

        /// <summary>
        /// Handles the Kill Enemy line of achievements.
        /// </summary>
        [ScriptHandler<OnCreatureDeathBefore>]
        public void KillEnemy()
        {
            var killer = GetLastKiller();
            if (!GetIsPC(killer) || GetIsDM(killer)) return;

            var cdKey = GetPCPublicCDKey(killer);
            var dbAccount = _db.Get<Account>(cdKey);

            dbAccount.AchievementProgress.EnemiesKilled++;
            _db.Set(dbAccount);

            var kills = dbAccount.AchievementProgress.EnemiesKilled;

            if (kills >= 10)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies1);
            }
            if (kills >= 50)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies2);
            }
            if (kills >= 500)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies3);
            }
            if (kills >= 2000)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies4);
            }
            if (kills >= 10000)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies5);
            }
            if (kills >= 100000)
            {
                _achievement.GiveAchievement(killer, AchievementType.KillEnemies6);
            }
        }

        /// <summary>
        /// Handles the Buy Perk line of achievements.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorBuyPerk)]
        public void BuyPerk()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = _db.Get<Account>(cdKey);

            dbAccount.AchievementProgress.PerksLearned++;
            _db.Set(dbAccount);

            var numberLearned = dbAccount.AchievementProgress.PerksLearned;

            if (numberLearned >= 1)
            {
                _achievement.GiveAchievement(player, AchievementType.LearnPerks1);
            }
            if (numberLearned >= 20)
            {
                _achievement.GiveAchievement(player, AchievementType.LearnPerks2);
            }
            if (numberLearned >= 50)
            {
                _achievement.GiveAchievement(player, AchievementType.LearnPerks3);
            }
            if (numberLearned >= 100)
            {
                _achievement.GiveAchievement(player, AchievementType.LearnPerks4);
            }
            if (numberLearned >= 500)
            {
                _achievement.GiveAchievement(player, AchievementType.LearnPerks5);
            }
        }

        /// <summary>
        /// Handles the Gain Skill line of achievements.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorGainSkill)]
        public void GainSkillPoint()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = _db.Get<Account>(cdKey);

            dbAccount.AchievementProgress.SkillsLearned++;
            _db.Set(dbAccount);

            var numberLearned = dbAccount.AchievementProgress.SkillsLearned;

            if (numberLearned >= 1)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills1);
            }
            if (numberLearned >= 50)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills2);
            }
            if (numberLearned >= 150)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills3);
            }
            if (numberLearned >= 250)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills4);
            }
            if (numberLearned >= 500)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills5);
            }
            if (numberLearned >= 1000)
            {
                _achievement.GiveAchievement(player, AchievementType.GainSkills6);
            }
        }

        /// <summary>
        /// Handles the Complete Quests line of achievements.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorCompleteQuest)]
        public void CompleteQuests()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = _db.Get<Account>(cdKey);

            dbAccount.AchievementProgress.QuestsCompleted++;
            _db.Set(dbAccount);

            var numberCompleted = dbAccount.AchievementProgress.QuestsCompleted;

            if (numberCompleted >= 1)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests1);
            }
            if (numberCompleted >= 10)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests2);
            }
            if (numberCompleted >= 50)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests3);
            }
            if (numberCompleted >= 100)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests4);
            }
            if (numberCompleted >= 500)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests5);
            }
            if (numberCompleted >= 1000)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests6);
            }
            if (numberCompleted >= 1500)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests7);
            }
            if (numberCompleted >= 2000)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests8);
            }
            if (numberCompleted >= 3500)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests9);
            }
            if (numberCompleted >= 5000)
            {
                _achievement.GiveAchievement(player, AchievementType.CompleteQuests10);
            }
        }

        /// <summary>
        /// Handles the Craft Item line of achievements
        /// </summary>
        [ScriptHandler(ScriptName.OnCraftSuccess)]
        public void CompleteCraftSuccessfully()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = _db.Get<Account>(cdKey);

            dbAccount.AchievementProgress.ItemsCrafted++;
            _db.Set(dbAccount);

            var numberCompleted = dbAccount.AchievementProgress.ItemsCrafted;
            if (numberCompleted >= 1)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems1);
            }
            if (numberCompleted >= 10)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems2);
            }
            if (numberCompleted >= 50)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems3);
            }
            if (numberCompleted >= 100)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems4);
            }
            if (numberCompleted >= 500)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems5);
            }
            if (numberCompleted >= 1000)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems6);
            }
            if (numberCompleted >= 1500)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems7);
            }
            if (numberCompleted >= 2000)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems8);
            }
            if (numberCompleted >= 3500)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems9);
            }
            if (numberCompleted >= 5000)
            {
                _achievement.GiveAchievement(player, AchievementType.CraftItems10);
            }
        }
    }
}
