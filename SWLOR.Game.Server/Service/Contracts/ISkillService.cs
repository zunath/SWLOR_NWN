using System.Collections.Generic;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISkillService
    {
        int SkillCap { get; }

        float CalculateRegisteredSkillLevelAdjustedXP(float xp, int registeredLevel, int skillRank);
        List<CachedSkillCategory> GetActiveCategories();
        int GetPCSkillRank(NWPlayer player, int skillID);
        int GetPCSkillRank(NWPlayer player, SkillType skill);
        CachedPCSkill GetPCSkill(NWPlayer player, int skillID);
        List<CachedPCSkill> GetAllPCSkills(NWPlayer player);
        CachedSkill GetSkill(int skillID);
        CachedSkill GetSkill(SkillType skillType);
        List<CachedPCSkill> GetPCSkillsForCategory(string playerID, int skillCategoryID);
        int GetPCTotalSkillCount(NWPlayer player);
        void GiveSkillXP(NWPlayer oPC, int skillID, int xp);
        void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp);
        void OnAreaExit();
        void OnCreatureDeath(NWCreature creature);
        void OnHitCastSpell(NWPlayer oPC);
        void OnModuleClientLeave();
        void OnModuleEnter();
        void OnModuleItemEquipped();
        void OnModuleItemUnequipped();
        void RegisterPCToAllCombatTargetsForSkill(NWPlayer player, SkillType skillType, NWCreature target);
        void ToggleSkillLock(string playerID, int skillID);
    }
}
