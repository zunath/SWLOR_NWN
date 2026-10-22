using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>A new crafter's climb from rank 0 to rank 50 in one skill at one gear level.</summary>
    public sealed class SkillPath
    {
        public SkillPath(SkillType skill, string gear, IReadOnlyList<RankStep> steps, int guildRankReached = 0, double lowestCatchUpBoardChance = 1.0)
        {
            Skill = skill;
            Gear = gear;
            Steps = steps;
            GuildRankReached = guildRankReached;
            LowestCatchUpBoardChance = lowestCatchUpBoardChance;
        }

        public SkillType Skill { get; }
        public string Gear { get; }
        public IReadOnlyList<RankStep> Steps { get; }

        /// <summary>The guild rank the hand-ins reached by rank 50; 0 when the path makes no hand-ins.</summary>
        public int GuildRankReached { get; }

        /// <summary>
        /// The lowest chance, over the catch-up hand-ins the path makes, that the guild board held a quick-craftable task of the tier. A board
        /// without one makes the crafter wait for the next server start, which costs calendar time, not play hours, and is not charged. 1 when the
        /// path makes no catch-up hand-ins.
        /// </summary>
        public double LowestCatchUpBoardChance { get; }

        public int RequiredXp => Steps.Sum(s => s.RequiredXp);
        public double Crafts => Steps.Sum(s => s.Crafts);
        public double CraftHours => Steps.Sum(s => s.CraftHours);
        public double GatherHours => Steps.Sum(s => s.GatherHours);
        public double HandInHours => Steps.Sum(s => s.HandInHours);
        public double CatchUpHours => Steps.Sum(s => s.CatchUpHours);
        public double CatchUpHandIns => Steps.Sum(s => s.CatchUpHandIns);
        public double Credits => Steps.Sum(s => s.Credits);
        public double TotalHours => Steps.Sum(s => s.TotalHours);
        public double HandIns => Steps.Sum(s => s.HandIns);
        public double GuildXp => Steps.Sum(s => s.GuildXp);
        public double ModelledUnits => Steps.Sum(s => s.ModelledUnits);
        public double UnmodelledUnits => Steps.Sum(s => s.UnmodelledUnits);

        /// <summary>XP per hour over the whole climb, to compare with RP XP's 3,000.</summary>
        public double XpPerHour => RequiredXp / TotalHours;

        /// <summary>The steps that start at ranks <paramref name="firstRank"/> up to but not including <paramref name="endRank"/>.</summary>
        public IEnumerable<RankStep> Band(int firstRank, int endRank) => Steps.Where(s => s.Rank >= firstRank && s.Rank < endRank);
    }
}
