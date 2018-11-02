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
        private readonly IDataContext _db;
        private readonly AppCache _cache;

        public CachingService(
            INWScript script,
            IDataContext db,
            AppCache cache)
        {
            _ = script;
            _db = db;
            _cache = cache;
        }

        private void CacheSkills()
        {
            var skills = _db.Skills.AsNoTracking()
                .Include(i => i.SkillCategory)
                .Include(i => i.SkillXPRequirements)
                .ToList();
            foreach (var skill in skills)
            {
                _db.Entry(skill).State = EntityState.Detached;
                _cache.Skills.Add((SkillType)skill.SkillID, skill);
            }
        }

        public void CachePCSkills(NWPlayer player)
        {
            if (_cache.PCSkills.ContainsKey(player.GlobalID))
            {
                return;
            }

            var ranks = new CachedPCSkills();
            var pcSkills = _db.PCSkills.AsNoTracking().Where(x => x.PlayerID == player.GlobalID).ToList();
            foreach (var skill in pcSkills)
            {
                _db.Entry(skill).State = EntityState.Detached;
                ranks.Add((SkillType)skill.SkillID, skill);
            }

            _cache.PCSkills.Add(player.GlobalID, ranks);
        }

        public void OnModuleLoad()
        {
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
