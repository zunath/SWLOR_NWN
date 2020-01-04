using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillCache: CacheBase<PCSkill>
    {
        public PCSkillCache() 
            : base("PCSkill")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        private const string ByPlayerIDAndSkillIDIndex = "ByPlayerIDAndSkillID";

        protected override void OnCacheObjectSet(PCSkill entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
            SetIntoIndex(ByPlayerIDAndSkillIDIndex, entity.PlayerID + ":" + entity.SkillID, entity);
        }

        protected override void OnCacheObjectRemoved(PCSkill entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
            RemoveFromIndex(ByPlayerIDAndSkillIDIndex, entity.PlayerID + ":" + entity.SkillID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSkill GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCSkill> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
            {
                return new List<PCSkill>();
            }

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public PCSkill GetByPlayerIDAndSkillID(Guid playerID, Skill skillID)
        {
            return GetFromIndex(ByPlayerIDAndSkillIDIndex, playerID + ":" + skillID);
        }

        public PCSkill GetByPlayerIDAndSkillIDOrDefault(Guid playerID, Skill skillID)
        {
            if (!ExistsByIndex(ByPlayerIDAndSkillIDIndex, playerID + ":" + skillID))
                return default;

            return GetFromIndex(ByPlayerIDAndSkillIDIndex, playerID + ":" + skillID);
        }

        public IEnumerable<PCSkill> GetAllByPlayerIDAndSkillIDs(Guid playerID, IEnumerable<Skill> skillIDs)
        {
            var list = new List<PCSkill>();
            foreach(var skillID in skillIDs)
            {
                list.Add(GetFromIndex(ByPlayerIDAndSkillIDIndex, playerID + ":" + skillID));
            }

            return list;
        }
    }
}
