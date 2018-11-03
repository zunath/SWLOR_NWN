using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public class PerkService: IPerkService
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly INWNXCreature _nwnxCreature;
        private readonly INWNXPlayerQuickBarSlot _nwnxQBS;
        private readonly INWNXPlayer _nwnxPlayer;

        public PerkService(INWScript script,
            IColorTokenService color,
            IDataService data,
            IBiowareXP2 biowareXP2,
            INWNXCreature nwnxCreature,
            INWNXPlayerQuickBarSlot nwnxQBS,
            INWNXPlayer nwnxPlayer)
        {
            _ = script;
            _color = color;
            _data = data;
            _biowareXP2 = biowareXP2;
            _nwnxCreature = nwnxCreature;
            _nwnxQBS = nwnxQBS;
            _nwnxPlayer = nwnxPlayer;
        }

        public void OnModuleItemEquipped()
        {
            NWPlayer oPC = (_.GetPCItemLastEquippedBy());
            NWItem oItem = (_.GetPCItemLastEquipped());
            if (!oPC.IsPlayer || !oPC.IsInitializedAsPlayer) return;
            List<PCPerk> perks = _data.StoredProcedure<PCPerk>("GetPCPerksByExecutionType",
                new SqlParameter("PlayerID", oPC.GlobalID),
                new SqlParameter("ExecutionTypeID", (int) PerkExecutionType.EquipmentBased)).ToList();
            //TODO: Figure out caching for the above query. There's a function in use in the proc that makes it challenging to directly migrate.

            foreach (PCPerk pcPerk in perks)
            {
                var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
                string jsName = perk.ScriptName;
                if (string.IsNullOrWhiteSpace(jsName)) continue;

                App.ResolveByInterface<IPerk>("Perk." + jsName, (perkAction) =>
                {
                    perkAction?.OnItemEquipped(oPC, oItem);
                });
            }
        }

        public void OnModuleItemUnequipped()
        {
            NWPlayer oPC = (_.GetPCItemLastUnequippedBy());
            NWItem oItem = (_.GetPCItemLastUnequipped());
            if (!oPC.IsPlayer) return;

            List<PCPerk> perks = _data.StoredProcedure<PCPerk>("GetPCPerksByExecutionType",
                new SqlParameter("PlayerID", oPC.GlobalID),
                new SqlParameter("ExecutionTypeID", (int)PerkExecutionType.EquipmentBased))
                .ToList();
            //TODO: Figure out caching for the above query. There's a function in use in the proc that makes it challenging to directly migrate.

            foreach (PCPerk pcPerk in perks)
            {
                var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
                string jsName = perk.ScriptName;
                if (string.IsNullOrWhiteSpace(jsName)) continue;

                App.ResolveByInterface<IPerk>("Perk." + jsName, (perkAction) =>
                {
                    perkAction?.OnItemUnequipped(oPC, oItem);
                });
            }
        }

        public int GetPCPerkLevel(NWPlayer player, PerkType perkType)
        {
            return GetPCPerkLevel(player, (int)perkType);
        }

        public int GetPCPerkLevel(NWPlayer player, int perkID)
        {
            if (!player.IsPlayer) return -1;
            
            PerkLevel perkLevel = _data.StoredProcedureSingle<PerkLevel>("GetPCSkillAdjustedPerkLevel",
                new SqlParameter("PlayerID", player.GlobalID),
                new SqlParameter("PerkID", perkID));
            //TODO: Figure out caching for the above query. There's a function in use in the proc that makes it challenging to directly migrate.

            return perkLevel?.Level ?? 0;
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            if (!oPC.IsPlayer) return;
            NWItem oItem = (_.GetSpellCastItem());
            int type = oItem.BaseItemType;
            List<PCPerk> pcPerks = _data.StoredProcedure<PCPerk>("GetPCPerksWithExecutionType",
                new SqlParameter("PlayerID", oPC.GlobalID))
                .ToList();
            //TODO: Figure out caching for the above query. There's a function in use in the proc that makes it challenging to directly migrate.

            foreach (PCPerk pcPerk in pcPerks)
            {
                var perk = GetPerkByID(pcPerk.PerkID);
                if (string.IsNullOrWhiteSpace(perk.ScriptName) || perk.ExecutionTypeID == (int)PerkExecutionType.None) continue;
                
                if (!App.IsKeyRegistered<IPerk>("Perk." + perk.ScriptName)) continue;

                App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
                {
                    if (perk.ExecutionTypeID == (int)PerkExecutionType.ShieldOnHit)
                    {
                        if (type == BASE_ITEM_SMALLSHIELD || type == BASE_ITEM_LARGESHIELD || type == BASE_ITEM_TOWERSHIELD)
                        {
                            perkAction.OnImpact(oPC, oItem, pcPerk.PerkLevel);
                        }
                    }
                });
                
            }
        }

        public int GetPCTotalPerkCount(string playerID)
        {
            return _data.GetAll<PCPerk>().Count(x => x.PlayerID == playerID);
        }

        public List<PCPerkHeader> GetPCPerksForMenuHeader(string playerID)
        {
            return _data.StoredProcedure<PCPerkHeader>("GetPCPerksForMenuHeader",
                    new SqlParameter("PlayerID", playerID)).ToList();
            // TODO: Migrate the above query
        }

        public List<PerkCategory> GetPerkCategoriesForPC(string playerID)
        {
            return _data.StoredProcedure<PerkCategory>("GetPerkCategoriesForPC",
                new SqlParameter("PlayerID", playerID)).ToList();
            // TODO: Migrate the above query
        }

        public List<Data.Entity.Perk> GetPerksForPC(string playerID, int categoryID)
        {
            return _data.StoredProcedure<Data.Entity.Perk>("GetPerksForPC",
                new SqlParameter("PlayerID", playerID),
                new SqlParameter("CategoryID", categoryID)).ToList();
            // TODO: Migrate the above query
        }

        public Data.Entity.Perk GetPerkByID(int perkID)
        {
            return _data.Single<Data.Entity.Perk>(x => x.PerkID == perkID);
        }

        public PCPerk GetPCPerkByID(string playerID, int perkID)
        {
            return _data.SingleOrDefault<PCPerk>(x => x.PlayerID == playerID && x.PerkID == perkID);
        }

        public PerkLevel FindPerkLevel(IEnumerable<PerkLevel> levels, int findLevel)
        {
            return levels.FirstOrDefault(lvl => lvl.Level == findLevel);
        }

        public bool CanPerkBeUpgraded(Data.Entity.Perk perk, PCPerk pcPerk, PlayerCharacter player)
        {
            int rank = pcPerk?.PerkLevel ?? 0;
            int maxRank = perk.PerkLevels.Count;
            if (rank + 1 > maxRank) return false;

            PerkLevel level = FindPerkLevel(perk.PerkLevels, rank + 1);
            if (level == null) return false;

            if (player.UnallocatedSP < level.Price) return false;

            foreach (PerkLevelSkillRequirement req in level.PerkLevelSkillRequirements)
            {
                PCSkill pcSkill = _data.Single<PCSkill>(x => x.PlayerID == player.PlayerID && x.SkillID == req.SkillID);
                if (pcSkill.Rank < req.RequiredRank) return false;
            }

            return true;
        }

        public void DoPerkUpgrade(NWPlayer oPC, int perkID)
        {
            Data.Entity.Perk perk = _data.Single<Data.Entity.Perk>(x => x.PerkID == perkID);
            PCPerk pcPerk = _data.SingleOrDefault<PCPerk>(x => x.PlayerID == oPC.GlobalID && x.PerkID == perkID);
            PlayerCharacter player = _data.Single<PlayerCharacter>(x => x.PlayerID == oPC.GlobalID);

            if (CanPerkBeUpgraded(perk, pcPerk, player))
            {
                DatabaseActionType action = DatabaseActionType.Update;
                if (pcPerk == null)
                {
                    pcPerk = new PCPerk();
                    DateTime dt = DateTime.UtcNow;
                    pcPerk.AcquiredDate = dt;
                    pcPerk.PerkID = perk.PerkID;
                    pcPerk.PlayerID = oPC.GlobalID;
                    pcPerk.PerkLevel = 0;

                    action = DatabaseActionType.Insert;
                }

                PerkLevel nextPerkLevel = FindPerkLevel(perk.PerkLevels, pcPerk.PerkLevel + 1);
                if (nextPerkLevel == null) return;

                pcPerk.PerkLevel++;
                player.UnallocatedSP -= nextPerkLevel.Price;

                _data.SubmitDataChange(pcPerk, action);
                _data.SubmitDataChange(player, DatabaseActionType.Update);
                
                // If a perk is activatable, create the item on the PC.
                // Remove any existing cast spell unique power properties and add the correct one based on the DB flag.
                if (!string.IsNullOrWhiteSpace(perk.ItemResref))
                {
                    if (_.GetIsObjectValid(_.GetItemPossessedBy(oPC.Object, perk.ItemResref)) == FALSE)
                    {
                        NWItem spellItem = (_.CreateItemOnObject(perk.ItemResref, oPC.Object));
                        spellItem.IsCursed = true;
                        spellItem.SetLocalInt("ACTIVATION_PERK_ID", perk.PerkID);

                        foreach (ItemProperty ipCur in spellItem.ItemProperties)
                        {
                            int ipType = _.GetItemPropertyType(ipCur);
                            int ipSubType = _.GetItemPropertySubType(ipCur);
                            if (ipType == ITEM_PROPERTY_CAST_SPELL &&
                                    (ipSubType == IP_CONST_CASTSPELL_UNIQUE_POWER ||
                                            ipSubType == IP_CONST_CASTSPELL_UNIQUE_POWER_SELF_ONLY ||
                                            ipSubType == IP_CONST_CASTSPELL_ACTIVATE_ITEM))
                            {
                                _.RemoveItemProperty(spellItem.Object, ipCur);
                            }
                        }
                        
                        ItemProperty ip;
                        if (perk.IsTargetSelfOnly) ip = _.ItemPropertyCastSpell(IP_CONST_CASTSPELL_UNIQUE_POWER_SELF_ONLY, IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE);
                        else ip = _.ItemPropertyCastSpell(IP_CONST_CASTSPELL_UNIQUE_POWER, IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE);

                        _biowareXP2.IPSafeAddItemProperty(spellItem, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                    }

                    _.SetName(_.GetItemPossessedBy(oPC.Object, perk.ItemResref), perk.Name + " (Lvl. " + pcPerk.PerkLevel + ")");
                }
                // If a feat ID is assigned, add the feat to the player if it doesn't exist yet.
                else if (perk.FeatID != null && 
                         perk.FeatID > 0 && 
                         _.GetHasFeat((int)perk.FeatID, oPC.Object) == FALSE)
                {
                    _nwnxCreature.AddFeatByLevel(oPC, (int)perk.FeatID, 1);
                    
                    var qbs = _nwnxQBS.UseFeat((int) perk.FeatID);

                    // Try to add the new feat to the player's hotbar.
                    if (_nwnxPlayer.GetQuickBarSlot(oPC, 0).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 0, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 1).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 1, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 2).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 2, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 3).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 3, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 4).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 4, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 5).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 5, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 6).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 6, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 7).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 7, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 8).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 8, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 9).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 9, qbs);
                    else if (_nwnxPlayer.GetQuickBarSlot(oPC, 10).ObjectType == QuickBarSlotType.Empty)
                        _nwnxPlayer.SetQuickBarSlot(oPC, 10, qbs);

                }

                oPC.SendMessage(_color.Green("Perk Purchased: " + perk.Name + " (Lvl. " + pcPerk.PerkLevel + ")"));

                App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkScript) =>
                {
                    if (perkScript == null) return;
                    perkScript.OnPurchased(oPC, pcPerk.PerkLevel);
                });

            }
            else
            {
                oPC.FloatingText(_color.Red("You cannot purchase the perk at this time."));
            }
        }

        public void DoPerkUpgrade(NWPlayer player, PerkType perkType)
        {
            DoPerkUpgrade(player, (int)perkType);
        }

        public string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (!examiner.IsPlayer && !examiner.IsDM) return existingDescription;
            if (examinedObject.ObjectType != OBJECT_TYPE_ITEM) return existingDescription;
            int perkID = examinedObject.GetLocalInt("ACTIVATION_PERK_ID");
            if (perkID <= 0) return existingDescription;

            var perk = _data.Single<Data.Entity.Perk>(x => x.PerkID == perkID);
            var executionType = _data.Get<Data.Entity.PerkExecutionType>(perk.ExecutionTypeID);
            var cooldownCategory = _data.Get<CooldownCategory>(perk.CooldownCategoryID);
            string description = existingDescription;

            description += _color.Orange("Name: ") + perk.Name + "\n" +
                _color.Orange("Description: ") + perk.Description + "\n";

            switch ((PerkExecutionType)executionType.PerkExecutionTypeID)
            {
                case PerkExecutionType.CombatAbility:
                    description += _color.Orange("Type: ") + "Combat Ability\n";
                    break;
                case PerkExecutionType.ForceAbility:
                    description += _color.Orange("Type: ") + "Spell\n";
                    break;
                case PerkExecutionType.ShieldOnHit:
                    description += _color.Orange("Type: ") + "Reactive\n";
                    break;
                case PerkExecutionType.QueuedWeaponSkill:
                    description += _color.Orange("Type: ") + "Queued Attack\n";
                    break;
                case PerkExecutionType.Stance:
                    description += _color.Orange("Type: ") + "Stance\n";
                    break;
            }

            if (perk.BaseFPCost > 0)
            {
                description += _color.Orange("Base FP Cost: ") + perk.BaseFPCost + "\n";
            }
            if (cooldownCategory.BaseCooldownTime > 0.0f)
            {
                description += _color.Orange("Cooldown: ") + cooldownCategory.BaseCooldownTime + "s\n";
            }
            if (perk.BaseCastingTime > 0.0f)
            {
                description += _color.Orange("Base Casting Time: ") + perk.BaseCastingTime + "s\n";
            }


            return description;
        }
    }
}
