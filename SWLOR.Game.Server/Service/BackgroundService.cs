using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using Background = SWLOR.Game.Server.Data.Entities.Background;

namespace SWLOR.Game.Server.Service
{
    public class BackgroundService: IBackgroundService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;

        public BackgroundService(IDataContext db, 
            INWScript script, 
            IPerkService perk,
            ISkillService skill)
        {
            _db = db;
            _ = script;
            _perk = perk;
            _skill = skill;
        }

        public IEnumerable<Background> GetActiveBackgrounds()
        {
            return _db.Backgrounds.Where(x => x.IsActive);
        }

        public void SetPlayerBackground(NWPlayer player, Background background)
        {
            if(player == null) throw new ArgumentNullException(nameof(player));
            if(background == null) throw new ArgumentNullException(nameof(background));

            PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            pc.BackgroundID = background.BackgroundID;
            _db.SaveChanges();
            ApplyBackgroundBonuses(player, background.BackgroundID);

            NWItem token = NWItem.Wrap(_.GetItemPossessedBy(player.Object, "bkg_token"));
            token.Destroy();

            player.FloatingText("Background confirmed!");
        }

        public void OnModuleClientEnter()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetEnteringObject());
            if (!oPC.IsPlayer || oPC.IsDM) return;
            
            NWObject token = NWObject.Wrap(_.GetItemPossessedBy(oPC.Object, "bkg_token"));
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);

            if (entity.BackgroundID > 0) return;

            if (!token.IsValid)
            {
                _.CreateItemOnObject("bkg_token", oPC.Object);
            }
        }


        private void ApplyBackgroundBonuses(NWPlayer oPC, int backgroundID)
        {
            string pcName = oPC.Name;
            PlayerCharacter pcEntity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);

            string item1Resref = "";
            int item1Quantity = 1;
            string item2Resref = "";
            int item2Quantity = 1;

            switch ((BackgroundType)backgroundID)
            {
                case BackgroundType.Weaponsmith:
                    _perk.DoPerkUpgrade(oPC, PerkType.WeaponBlueprints);
                    break;
                case BackgroundType.Armorsmith:
                    _perk.DoPerkUpgrade(oPC, PerkType.ArmorBlueprints);
                    break;
                case BackgroundType.Fabricator:
                    _perk.DoPerkUpgrade(oPC, PerkType.FabricationBlueprints);
                    break;
                case BackgroundType.Knight:
                    item1Resref = "bkg_knightarmor";
                    break;
                case BackgroundType.Wizard:
                    _perk.DoPerkUpgrade(oPC, PerkType.FireBlast);
                    break;
                case BackgroundType.Cleric:
                    _perk.DoPerkUpgrade(oPC, PerkType.Recover);
                    break;
                case BackgroundType.Gunslinger:
                    item1Resref = "bkg_gunblaster";
                    item2Resref = "blaster_bullets";
                    item2Quantity = 99;
                    break;
                case BackgroundType.Rifleman:
                    item1Resref = "bkg_rifleman";
                    item2Resref = "rifle_bullets";
                    item2Quantity = 99;
                    break;
                case BackgroundType.Chef:
                    _perk.DoPerkUpgrade(oPC, PerkType.FoodRecipes);
                    break;
                case BackgroundType.Engineer:
                    _perk.DoPerkUpgrade(oPC, PerkType.EngineeringBlueprints);
                    break;
                case BackgroundType.Vagabond:
                    pcEntity.UnallocatedSP = pcEntity.UnallocatedSP + 3;
                    _db.SaveChanges();
                    break;
                case BackgroundType.Guard:
                    item1Resref = "bkg_guardlgswd";
                    item2Resref = "bkg_guardshld";
                    break;
                case BackgroundType.Berserker:
                    item1Resref = "bkg_bersgswd";
                    break;
                case BackgroundType.TwinBladeSpecialist:
                    item1Resref = "bkg_spectwinbld";
                    break;
                case BackgroundType.MartialArtist:
                    item1Resref = "bkg_mrtlglove";
                    item2Resref = "bkg_mtlshuriken";
                    item2Quantity = 50;
                    break;
                case BackgroundType.Medic:
                    _perk.DoPerkUpgrade(oPC, PerkType.HealingKitExpert);
                    _perk.DoPerkUpgrade(oPC, PerkType.ImmediateImprovement);
                    break;
                case BackgroundType.Farmer:
                    _perk.DoPerkUpgrade(oPC, PerkType.ExpertFarmer);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(item1Resref))
            {
                NWItem oItem1 = NWItem.Wrap(_.CreateItemOnObject(item1Resref, oPC.Object, item1Quantity));
                oItem1.IsCursed = true;
                oItem1.Name = pcName + "'s " + oItem1.Name;
            }
            if (!string.IsNullOrWhiteSpace(item2Resref))
            {
                NWItem oItem2 = NWItem.Wrap(_.CreateItemOnObject(item2Resref, oPC.Object, item2Quantity, ""));
                oItem2.IsCursed = true;
                oItem2.Name = pcName + "'s " + oItem2.Name;
            }

            _skill.ApplyStatChanges(oPC, null);
        }

    }
}
