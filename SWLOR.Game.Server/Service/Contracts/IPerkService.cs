using System;
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
        int GetPCTotalPerkCount(Guid playerID);
        List<Data.Entity.Perk> GetPerksAvailableToPC(NWPlayer player);
        Data.Entity.Perk GetPerkByID(int perkID);
        PCPerk GetPCPerkByID(Guid playerID, int perkID);
        PerkLevel FindPerkLevel(IEnumerable<PerkLevel> levels, int findLevel);
        bool CanPerkBeUpgraded(NWPlayer player, int perkID);
        void DoPerkUpgrade(NWPlayer player, int perkID, bool freeUpgrade = false);
        void DoPerkUpgrade(NWPlayer player, PerkType perkType, bool freeUpgrade = false);
        void OnHitCastSpell(NWPlayer oPC);
    }
}
