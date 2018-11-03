using System.Data.Entity;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class CachingService : ICachingService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly AppCache _cache;

        public CachingService(
            INWScript script,
            IDataService data,
            AppCache cache)
        {
            _ = script;
            _data = data;
            _cache = cache;
        }

        private void CacheSkillCategories()
        {
            var categories = _data.SkillCategories
                .Where(x => x.IsActive)
                .ToList();

            foreach (var category in categories)
            {
                _cache.SkillCategories.Add(new CachedSkillCategory(category));
            }
        }

        private void CacheSkills()
        {
            var skills = _data.Skills.AsNoTracking()
                .Include(i => i.SkillCategory)
                .Include(i => i.SkillXPRequirements)
                .ToList();
            foreach (var skill in skills)
            {
                _cache.Skills.Add((SkillType)skill.SkillID, new CachedSkill(skill));
            }
        }

        public void CachePCSkills(NWPlayer player)
        {
            if (_cache.PCSkills.ContainsKey(player.GlobalID))
            {
                return;
            }

            var ranks = new CachedPCSkills();
            var pcSkills = _data.PCSkills
                .Include(i => i.Skill)
                .AsNoTracking()
                .Where(x => x.PlayerID == player.GlobalID && 
                            x.Skill.IsActive).ToList();
            foreach (var skill in pcSkills)
            {
                ranks.Add((SkillType)skill.SkillID, new CachedPCSkill(skill));
            }

            _cache.PCSkills.Add(player.GlobalID, ranks);
        }

        public void OnModuleLoad()
        {
            CacheSkillCategories();
            CacheSkills();
        }

        public void OnModuleClientEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            CachePCSkills(player);
        }

        public void OnModuleEquipItem()
        {
            // This is a really weird place to put this,
            // but the OnEquip event appears to fire before the OnEnter event does.
            // So attempt to cache PC skills here. Otherwise we'll do it on client enter.
            NWPlayer player = _.GetPCItemLastEquippedBy();
            if (!player.IsPlayer) return;

            CachePCSkills(player);
        }
    }
}
