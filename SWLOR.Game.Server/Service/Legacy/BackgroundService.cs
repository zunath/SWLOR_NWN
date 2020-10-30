using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Legacy
{
    public static class BackgroundService
    {
        
        public static void ApplyBackgroundBonuses(NWPlayer oPC)
        {
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            var pcName = oPC.Name;
            var classID = oPC.Class1;

            var item1Resref = "";
            var item1Quantity = 1;
            var item2Resref = "";
            var item2Quantity = 1;

            switch ((BackgroundType)classID)
            {
                case BackgroundType.Freelancer:
                    dbPlayer.UnallocatedSP = dbPlayer.UnallocatedSP + 3;
                    DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
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
                    item1Resref = "club_s";
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
                    PerkService.DoPerkUpgrade(oPC, PerkType.ArmorBlueprints, true);
                    break;
                case BackgroundType.Weaponsmith:
                    PerkService.DoPerkUpgrade(oPC, PerkType.WeaponBlueprints, true);
                    break;
                case BackgroundType.Chef:
                    PerkService.DoPerkUpgrade(oPC, PerkType.FoodRecipes, true);
                    break;
                case BackgroundType.Engineer:
                    PerkService.DoPerkUpgrade(oPC, PerkType.EngineeringBlueprints, true);
                    break;
                case BackgroundType.Fabricator:
                    PerkService.DoPerkUpgrade(oPC, PerkType.FabricationBlueprints, true);
                    break;
                case BackgroundType.Harvester:
                    item1Resref = "scanner_r_h";
                    item2Resref = "harvest_r_h";
                    break;
                case BackgroundType.Scavenger:
                    PerkService.DoPerkUpgrade(oPC, PerkType.ScavengingExpert, true);
                    break;
                case BackgroundType.Medic:
                    PerkService.DoPerkUpgrade(oPC, PerkType.ImmediateImprovement, true);
                    break;
                case BackgroundType.Mandalorian:
                    item1Resref = "man_armor";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!string.IsNullOrWhiteSpace(item1Resref))
            {
                NWItem oItem1 = (NWScript.CreateItemOnObject(item1Resref, oPC.Object, item1Quantity));
                oItem1.IsCursed = true;
                oItem1.Name = pcName + "'s " + oItem1.Name;
            }
            if (!string.IsNullOrWhiteSpace(item2Resref))
            {
                NWItem oItem2 = (NWScript.CreateItemOnObject(item2Resref, oPC.Object, item2Quantity, ""));
                oItem2.IsCursed = true;
                oItem2.Name = pcName + "'s " + oItem2.Name;
            }
        }

    }
}
