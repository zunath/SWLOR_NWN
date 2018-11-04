using System.Collections.Generic;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPerkService
    {
        void OnModuleItemEquipped();
        void OnModuleItemUnequipped();
        int GetPCPerkLevel(NWPlayer player, PerkType perkType);
        int GetPCPerkLevel(NWPlayer player, int perkTypeID);
        int GetPCTotalPerkCount(string playerID);
        List<Data.Entity.Perk> GetPerksAvailableToPC(NWPlayer player);
        Data.Entity.Perk GetPerkByID(int perkID);
        PCPerk GetPCPerkByID(string playerID, int perkID);
        PerkLevel FindPerkLevel(IEnumerable<PerkLevel> levels, int findLevel);
        bool CanPerkBeUpgraded(Data.Entity.Perk perk, PCPerk pcPerk, PlayerCharacter player);
        void DoPerkUpgrade(NWPlayer player, int perkID);
        void DoPerkUpgrade(NWPlayer player, PerkType perkType);
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void OnHitCastSpell(NWPlayer oPC);
    }
}
