using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class BackgroundService: IBackgroundService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _stat;

        public BackgroundService(
            IDataContext db, 
            INWScript script, 
            IPerkService perk,
            IPlayerStatService stat)
        {
            _db = db;
            _ = script;
            _perk = perk;
            _stat = stat;
        }
        
        public void ApplyBackgroundBonuses(NWPlayer oPC)
        {
            var dbPlayer = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
            string pcName = oPC.Name;
            int classID = oPC.Class1;

            string item1Resref = "";
            int item1Quantity = 1;
            string item2Resref = "";
            int item2Quantity = 1;

            switch ((BackgroundType)classID)
            {
                case BackgroundType.Freelancer:
                    dbPlayer.UnallocatedSP = dbPlayer.UnallocatedSP + 3;
                    _db.SaveChanges();
                    break;
                case BackgroundType.Smuggler:
                    item1Resref = "blaster_s";
                    break;
                case BackgroundType.Sharpshooter:
                    item1Resref = "rifle_s";
                    break;
                case BackgroundType.TerasKasi:
                    item1Resref = "powerglove_t";
                    break;
                case BackgroundType.SecurityOfficer:
                    item1Resref = "baton_s";
                    break;
                case BackgroundType.Berserker:
                    item1Resref = "doubleaxe_z";
                    break;
                case BackgroundType.Duelist:
                    item1Resref = "kukri_d";
                    break;
                case BackgroundType.Soldier:
                    item1Resref = "greatsword_s";
                    break;
                case BackgroundType.Armorsmith:
                    _perk.DoPerkUpgrade(oPC, PerkType.ArmorBlueprints);
                    break;
                case BackgroundType.Weaponsmith:
                    _perk.DoPerkUpgrade(oPC, PerkType.WeaponBlueprints);
                    break;
                case BackgroundType.Chef:
                    _perk.DoPerkUpgrade(oPC, PerkType.FoodRecipes);
                    break;
                case BackgroundType.Engineer:
                    _perk.DoPerkUpgrade(oPC, PerkType.EngineeringBlueprints);
                    break;
                case BackgroundType.Fabricator:
                    _perk.DoPerkUpgrade(oPC, PerkType.FabricationBlueprints);
                    break;
                case BackgroundType.Harvester:
                    item1Resref = "scanner_r_h";
                    item2Resref = "harvest_r_h";
                    break;
                case BackgroundType.Scavenger:
                    _perk.DoPerkUpgrade(oPC, PerkType.ScavengingExpert);
                    break;
                case BackgroundType.Medic:
                    _perk.DoPerkUpgrade(oPC, PerkType.ImmediateImprovement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!string.IsNullOrWhiteSpace(item1Resref))
            {
                NWItem oItem1 = (_.CreateItemOnObject(item1Resref, oPC.Object, item1Quantity));
                oItem1.IsCursed = true;
                oItem1.Name = pcName + "'s " + oItem1.Name;
            }
            if (!string.IsNullOrWhiteSpace(item2Resref))
            {
                NWItem oItem2 = (_.CreateItemOnObject(item2Resref, oPC.Object, item2Quantity, ""));
                oItem2.IsCursed = true;
                oItem2.Name = pcName + "'s " + oItem2.Name;
            }

            _stat.ApplyStatChanges(oPC, null);
        }

    }
}
