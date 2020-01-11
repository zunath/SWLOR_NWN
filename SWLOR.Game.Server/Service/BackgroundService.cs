using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;


namespace SWLOR.Game.Server.Service
{
    public static class BackgroundService
    {
        
        public static void ApplyBackgroundBonuses(NWPlayer oPC)
        {
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            string pcName = oPC.Name;
            var classID = oPC.Class1;

            string item1Resref = "";
            int item1Quantity = 1;
            string item2Resref = "";
            int item2Quantity = 1;

            switch ((ClassType)classID)
            {
                case ClassType.Freelancer:
                    dbPlayer.UnallocatedSP = dbPlayer.UnallocatedSP + 3;
                    DataService.Set(dbPlayer);
                    break;
                case ClassType.Smuggler:
                    item1Resref = "blaster_s";
                    break;
                case ClassType.Sharpshooter:
                    item1Resref = "rifle_s";
                    break;
                case ClassType.TerasKasi:
                    item1Resref = "powerglove_t";
                    break;
                case ClassType.SecurityOfficer:
                    item1Resref = "club_s";
                    break;
                case ClassType.Berserker:
                    item1Resref = "doubleaxe_z";
                    break;
                case ClassType.Duelist:
                    item1Resref = "kukri_d";
                    break;
                case ClassType.Soldier:
                    item1Resref = "greatsword_s";
                    break;
                case ClassType.Armorsmith:
                    PerkService.DoPerkUpgrade(oPC, PerkType.ArmorBlueprints, true);
                    break;
                case ClassType.Weaponsmith:
                    PerkService.DoPerkUpgrade(oPC, PerkType.WeaponBlueprints, true);
                    break;
                case ClassType.Chef:
                    PerkService.DoPerkUpgrade(oPC, PerkType.FoodRecipes, true);
                    break;
                case ClassType.Engineer:
                    PerkService.DoPerkUpgrade(oPC, PerkType.EngineeringBlueprints, true);
                    break;
                case ClassType.Fabricator:
                    PerkService.DoPerkUpgrade(oPC, PerkType.FabricationBlueprints, true);
                    break;
                case ClassType.Harvester:
                    item1Resref = "scanner_r_h";
                    item2Resref = "harvest_r_h";
                    break;
                case ClassType.Scavenger:
                    PerkService.DoPerkUpgrade(oPC, PerkType.ScavengingExpert, true);
                    break;
                case ClassType.Medic:
                    PerkService.DoPerkUpgrade(oPC, PerkType.ImmediateImprovement, true);
                    break;
                case ClassType.Mandalorian:
                    item1Resref = "man_armor";
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
        }

    }
}
