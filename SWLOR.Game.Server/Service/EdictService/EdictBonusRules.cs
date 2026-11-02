using System;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// The numbers behind what Edicts and City Affairs outcomes add to a citizen's calculations (Player City Revamp, "Bonuses at a
    /// glance"). Each method is the math of one hook point and holds no database or engine calls. Who qualifies, and which city and
    /// planet count, is decided by <see cref="EdictEffects"/>.
    /// </summary>
    public static class EdictBonusRules
    {
        #region Festival Season

        /// <summary>The Festival Season Edict adds this share of an RP XP award's base.</summary>
        public const int FestivalSeasonPercent = 10;

        /// <summary>A funded festival situation adds this share of an RP XP award's base, beside the Edict.</summary>
        public const int FestivalSituationPercent = 5;

        /// <summary>
        /// The RP XP the festival Edict and the festival situation add beside the Cantina's: a share of the award's base, so the
        /// two add together and neither scales with the other bonuses.
        /// </summary>
        public static int GetFestivalXp(int baseXp, bool festivalSeason, bool festivalSituation)
        {
            var percent = (festivalSeason ? FestivalSeasonPercent : 0) + (festivalSituation ? FestivalSituationPercent : 0);
            return baseXp * percent / 100;
        }

        #endregion

        #region Medical Center

        /// <summary>The share of new death debt each Medical Center level removes.</summary>
        public const float MedicalCenterReductionPerLevel = 0.05f;

        /// <summary>The Medical Center's reduction of new death debt, halved while a situation has the Center rationing.</summary>
        public static float GetMedicalCenterReduction(int level, bool halved)
        {
            return level * MedicalCenterReductionPerLevel * (halved ? 0.5f : 1f);
        }

        #endregion

        #region Guild Charter

        /// <summary>The Guild Charter adds this share of a guild task's guild points and credits.</summary>
        public const float GuildCharterBonus = 0.10f;

        public static float GetGuildCharterBonus(bool active)
        {
            return active ? GuildCharterBonus : 0f;
        }

        #endregion

        #region Prospectors' Charter and Home Ground

        /// <summary>The chance, in percent, that Prospectors' Charter adds 1 item to a finished harvest.</summary>
        public const int ProspectorsCharterChancePercent = 10;

        /// <summary>The chance, in percent, that Home Ground adds 1 item to a finished harvest, rolled separately.</summary>
        public const int HomeGroundChancePercent = 5;

        /// <summary>
        /// How many more items a finished harvest gains: 0, 1 or 2. Each chance that applies is its own d100 roll (a roll of the chance
        /// or lower wins), made in this order: Prospectors' Charter, then Home Ground. A chance of 0 makes no roll.
        /// </summary>
        /// <param name="prospectorsChancePercent">The Prospectors' Charter chance, or 0 when it does not apply.</param>
        /// <param name="homeGroundChancePercent">The Home Ground chance, or 0 when it does not apply.</param>
        /// <param name="rollD100">Rolls 1 to 100.</param>
        public static int RollHarvestBonusItems(int prospectorsChancePercent, int homeGroundChancePercent, Func<int> rollD100)
        {
            var items = 0;
            if (prospectorsChancePercent > 0 && rollD100() <= prospectorsChancePercent)
                items++;
            if (homeGroundChancePercent > 0 && rollD100() <= homeGroundChancePercent)
                items++;

            return items;
        }

        #endregion

        #region Hunting Grounds

        /// <summary>The points Hunting Grounds adds to the skill XP bonus for XP from defeating enemies.</summary>
        public const int HuntingGroundsBonusPoints = 10;

        #endregion

        #region Drydock Subsidy

        /// <summary>The share Drydock Subsidy adds to the Starport's repair discount, before the 90% cap.</summary>
        public const double DrydockSubsidyDiscount = 0.15;

        #endregion

        #region Artisans' Charter

        /// <summary>The points Artisans' Charter adds to the XP of a craft finished at a station inside the city.</summary>
        public const int ArtisansCharterBonusPoints = 10;

        /// <summary>A craft's XP with the bonus points added: 110% of it for 10 points, rounded to the nearest whole XP.</summary>
        public static int ScaleCraftXp(int xp, int bonusPoints)
        {
            if (xp <= 0 || bonusPoints <= 0)
                return xp;

            return (int)Math.Round(xp * (100 + bonusPoints) / 100.0, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region Breeders' Charter

        /// <summary>
        /// What Breeders' Charter adds to a purity stat when a stage starts: a tenth of what the stage's enzymes added, after the stage
        /// cap, rounded down. Units are 0.1%, as the stats are stored, so a stage gain of 73 (7.3%) adds 7 (0.7%).
        /// </summary>
        /// <param name="stageGain">What the stage's enzymes added to the stat, before the cap.</param>
        /// <param name="maxStageIncrease">The most one stage can add to a stat (10.0%).</param>
        public static int GetBreedersCharterBonus(int stageGain, int maxStageIncrease)
        {
            return Math.Min(Math.Max(stageGain, 0), maxStageIncrease) / 10;
        }

        #endregion
    }
}
