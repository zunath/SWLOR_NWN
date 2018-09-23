using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISkillService
    {
        int SkillCap { get; }

        float CalculateRegisteredSkillLevelAdjustedXP(float xp, int registeredLevel, int skillRank);
        List<SkillCategory> GetActiveCategories();
        PCSkill GetPCSkill(NWPlayer player, int skillID);
        PCSkill GetPCSkill(NWPlayer player, SkillType skill);
        PCSkill GetPCSkillByID(string playerID, int skillID);
        List<PCSkill> GetPCSkillsForCategory(string playerID, int skillCategoryID);
        int GetPCTotalSkillCount(string playerID);
        SkillXPRequirement GetSkillXPRequirementByRank(int skillID, int rank);
        void GiveSkillXP(NWPlayer oPC, int skillID, int xp);
        void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp);
        void OnAreaExit();
        void OnCreatureDeath(NWCreature creature);
        void OnHitCastSpell(NWPlayer oPC);
        void OnModuleClientLeave();
        void OnModuleEnter();
        void OnModuleItemEquipped();
        void OnModuleItemUnequipped();
        void RegisterPCToAllCombatTargetsForSkill(NWPlayer pc, int skillID);
        void RegisterPCToAllCombatTargetsForSkill(NWPlayer player, SkillType skillType);
        void RegisterPCToNPCForSkill(NWPlayer pc, NWCreature npc, int skillID);
        void RegisterPCToNPCForSkill(NWPlayer pc, NWCreature npc, SkillType skillType);
        void ToggleSkillLock(string playerID, int skillID);
    }
}
