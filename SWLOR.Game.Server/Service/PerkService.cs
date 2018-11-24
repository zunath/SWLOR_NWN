using System;
using System.Collections.Generic;
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
        private readonly INWNXProfiler _nwnxProfiler;

        public PerkService(INWScript script,
            IColorTokenService color,
            IDataService data,
            IBiowareXP2 biowareXP2,
            INWNXCreature nwnxCreature,
            INWNXPlayerQuickBarSlot nwnxQBS,
            INWNXPlayer nwnxPlayer,
            INWNXProfiler nwnxProfiler)
        {
            _ = script;
            _color = color;
            _data = data;
            _biowareXP2 = biowareXP2;
            _nwnxCreature = nwnxCreature;
            _nwnxQBS = nwnxQBS;
            _nwnxPlayer = nwnxPlayer;
            _nwnxProfiler = nwnxProfiler;
        }

        private List<PCPerk> GetPCPerksByExecutionType(NWPlayer oPC, PerkExecutionType executionType)
        {
            var pcPerks = _data.Where<PCPerk>(x => x.PlayerID == oPC.GlobalID);
            return pcPerks.Where(x =>
            {
                // Filter on equipment-based execution type.
                var perk = _data.Get<Data.Entity.Perk>(x.PerkID);
                bool matchesExecutionType = perk.ExecutionTypeID == (int)executionType;
                if (!matchesExecutionType) return false;

                // Filter out any perks the PC doesn't meet the requirements for.
                int effectivePerkLevel = GetPCEffectivePerkLevel(oPC, x.PerkID);
                if (effectivePerkLevel <= 0) return false;

                // Meets all requirements.
                return true;
            }).ToList();

        }

        public void OnModuleItemEquipped()
        {
            using (new Profiler("PerkService::OnModuleItemEquipped()"))
            {
                NWPlayer oPC = (_.GetPCItemLastEquippedBy());
                NWItem oItem = (_.GetPCItemLastEquipped());
                if (!oPC.IsPlayer || !oPC.IsInitializedAsPlayer) return;
                if (oPC.GetLocalInt("LOGGED_IN_ONCE") == FALSE) return;

                var executionPerks = GetPCPerksByExecutionType(oPC, PerkExecutionType.EquipmentBased);
                foreach (PCPerk pcPerk in executionPerks)
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
        }

        public void OnModuleItemUnequipped()
        {
            using (new Profiler("PerkService::OnModuleItemUnequipped()"))
            {
                NWPlayer oPC = (_.GetPCItemLastUnequippedBy());
                NWItem oItem = (_.GetPCItemLastUnequipped());
                if (!oPC.IsPlayer) return;

                var executionPerks = GetPCPerksByExecutionType(oPC, PerkExecutionType.EquipmentBased);
                foreach (PCPerk pcPerk in executionPerks)
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
        }

        public int GetPCPerkLevel(NWPlayer player, PerkType perkType)
        {
            return GetPCPerkLevel(player, (int)perkType);
        }

        public int GetPCPerkLevel(NWPlayer player, int perkID)
        {
            if (!player.IsPlayer) return -1;
            return GetPCEffectivePerkLevel(player, perkID);
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            if (!oPC.IsPlayer) return;
            NWItem oItem = (_.GetSpellCastItem());
            int type = oItem.BaseItemType;
            var pcPerks = _data.Where<PCPerk>(x =>
            {
                // Only pull back perks which have a Shield On Hit execution type.
                var perk = _data.Get<Data.Entity.Perk>(x.PerkID);
                if (perk.ExecutionTypeID != (int) PerkExecutionType.ShieldOnHit)
                    return false;

                // If player's effective level is zero, it's not in effect.
                int effectiveLevel = GetPCEffectivePerkLevel(oPC, x.PerkID);
                if (effectiveLevel <= 0) return false;
                
                return true;
            });

            foreach (PCPerk pcPerk in pcPerks)
            {
                var perk = GetPerkByID(pcPerk.PerkID);
                if (string.IsNullOrWhiteSpace(perk.ScriptName) || perk.ExecutionTypeID == (int)PerkExecutionType.None) continue;
                
                if (!App.IsKeyRegistered<IPerk>("Perk." + perk.ScriptName)) continue;

                App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
                {
                    if (type == BASE_ITEM_SMALLSHIELD || type == BASE_ITEM_LARGESHIELD || type == BASE_ITEM_TOWERSHIELD)
                    {
                        perkAction.OnImpact(oPC, oItem, pcPerk.PerkLevel);
                    }
                });
                
            }
        }

        public int GetPCTotalPerkCount(Guid playerID)
        {
            return _data.GetAll<PCPerk>().Count(x => x.PlayerID == playerID);
        }


        public List<Data.Entity.Perk> GetPerksAvailableToPC(NWPlayer player)
        {
            var playerID = player.GlobalID;
            var pcSkills = _data.Where<PCSkill>(x => x.PlayerID == playerID).ToList();

            return _data.Where<Data.Entity.Perk>(x =>
            {
                if (!x.IsActive) return false;
                // Determination for whether a player can see a perk in the menu is based on whether they meet the
                // requirements for the first level in that perk.
                var perkLevel = _data.Single<PerkLevel>(pl => pl.PerkID == x.ID && pl.Level == 1);
                var skillRequirements = _data.Where<PerkLevelSkillRequirement>(sr => sr.PerkLevelID == perkLevel.ID);
                var questRequirements = _data.Where<PerkLevelQuestRequirement>(qr => qr.PerkLevelID == perkLevel.ID);

                // Check the player's skill level against the perk requirements.
                foreach (var skillReq in skillRequirements)
                {
                    var pcSkill = pcSkills.Single(s => s.SkillID == skillReq.SkillID);

                    if (pcSkill.Rank < skillReq.RequiredRank)
                    {
                        return false;
                    }
                }

                // Check the player's quest completion status against the perk requirements.
                foreach (var questReq in questRequirements)
                {
                    var pcQuest = _data.SingleOrDefault<PCQuestStatus>(q => q.PlayerID == player.GlobalID && 
                                                                            q.QuestID == questReq.RequiredQuestID);
                    if (pcQuest == null || pcQuest.CompletionDate == null)
                        return false;
                }

                return true;
            }).ToList();
        }
        
        public Data.Entity.Perk GetPerkByID(int perkID)
        {
            return _data.Single<Data.Entity.Perk>(x => x.ID == perkID);
        }

        public PCPerk GetPCPerkByID(Guid playerID, int perkID)
        {
            return _data.SingleOrDefault<PCPerk>(x => x.PlayerID == playerID && x.PerkID == perkID);
        }

        public PerkLevel FindPerkLevel(IEnumerable<PerkLevel> levels, int findLevel)
        {
            return levels.FirstOrDefault(lvl => lvl.Level == findLevel);
        }

        public bool CanPerkBeUpgraded(NWPlayer player, int perkID)
        {
            var dbPlayer = _data.Get<Player>(player.GlobalID);
            var perkLevels = _data.Where<PerkLevel>(x => x.PerkID == perkID).ToList();
            var pcPerk = _data.SingleOrDefault<PCPerk>(x => x.PlayerID == player.GlobalID && x.PerkID == perkID);

            int rank = 0;
            if (pcPerk != null)
            {
                rank = pcPerk.PerkLevel;
            }
            int maxRank = perkLevels.Count;
            if (rank + 1 > maxRank) return false;

            PerkLevel level = FindPerkLevel(perkLevels, rank + 1);
            if (level == null) return false;

            if (dbPlayer.UnallocatedSP < level.Price) return false;

            var skillRequirements = _data.Where<PerkLevelSkillRequirement>(x => x.PerkLevelID == level.ID).ToList();

            var questRequirements = _data.Where<PerkLevelQuestRequirement>(x => x.PerkLevelID == level.ID).ToList();

            foreach (var req in skillRequirements)
            {
                PCSkill pcSkill = _data.Single<PCSkill>(x => x.PlayerID == dbPlayer.ID && 
                                                             x.SkillID == req.SkillID);

                if (pcSkill.Rank < req.RequiredRank) return false;
            }

            foreach (var req in questRequirements)
            {
                var pcQuest = _data.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == dbPlayer.ID && 
                                                               x.QuestID == req.RequiredQuestID && 
                                                               x.CompletionDate != null);
                if (pcQuest == null) return false;
            }
            return true;
        }

        public void DoPerkUpgrade(NWPlayer oPC, int perkID, bool freeUpgrade = false)
        {
            var perk = _data.Single<Data.Entity.Perk>(x => x.ID == perkID);
            var perkLevels = _data.Where<PerkLevel>(x => x.PerkID == perkID);
            var pcPerk = _data.SingleOrDefault<PCPerk>(x => x.PlayerID == oPC.GlobalID && x.PerkID == perkID);
            var player = _data.Single<Player>(x => x.ID == oPC.GlobalID);

            if (freeUpgrade || CanPerkBeUpgraded(oPC, perkID))
            {
                DatabaseActionType action = DatabaseActionType.Update;
                if (pcPerk == null)
                {
                    pcPerk = new PCPerk();
                    DateTime dt = DateTime.UtcNow;
                    pcPerk.AcquiredDate = dt;
                    pcPerk.PerkID = perk.ID;
                    pcPerk.PlayerID = oPC.GlobalID;
                    pcPerk.PerkLevel = 0;

                    action = DatabaseActionType.Insert;
                }

                PerkLevel nextPerkLevel = FindPerkLevel(perkLevels, pcPerk.PerkLevel + 1);
                if (nextPerkLevel == null) return;

                pcPerk.PerkLevel++;
                _data.SubmitDataChange(pcPerk, action);

                if (!freeUpgrade)
                {
                    player.UnallocatedSP -= nextPerkLevel.Price;
                    _data.SubmitDataChange(player, DatabaseActionType.Update);
                }
                
                // If a perk is activatable, create the item on the PC.
                // Remove any existing cast spell unique power properties and add the correct one based on the DB flag.
                if (!string.IsNullOrWhiteSpace(perk.ItemResref))
                {
                    if (_.GetIsObjectValid(_.GetItemPossessedBy(oPC.Object, perk.ItemResref)) == FALSE)
                    {
                        NWItem spellItem = (_.CreateItemOnObject(perk.ItemResref, oPC.Object));
                        spellItem.IsCursed = true;
                        spellItem.SetLocalInt("ACTIVATION_PERK_ID", perk.ID);

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

        public void DoPerkUpgrade(NWPlayer player, PerkType perkType, bool freeUpgrade = false)
        {
            DoPerkUpgrade(player, (int)perkType, freeUpgrade);
        }

        public string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (!examiner.IsPlayer && !examiner.IsDM) return existingDescription;
            if (examinedObject.ObjectType != OBJECT_TYPE_ITEM) return existingDescription;
            int perkID = examinedObject.GetLocalInt("ACTIVATION_PERK_ID");
            if (perkID <= 0) return existingDescription;

            var perk = _data.Single<Data.Entity.Perk>(x => x.ID == perkID);
            var executionType = _data.Get<Data.Entity.PerkExecutionType>(perk.ExecutionTypeID);
            var cooldownCategory = _data.Get<CooldownCategory>(perk.CooldownCategoryID);
            string description = existingDescription;

            description += _color.Orange("Name: ") + perk.Name + "\n" +
                _color.Orange("Description: ") + perk.Description + "\n";

            switch ((PerkExecutionType)executionType.ID)
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

        /// <summary>
        /// Returns the EFFECTIVE perk level of a player.
        /// This takes into account the player's skills. If they are too low to use the perk, the level will be
        /// reduced to the appropriate level.
        /// </summary>
        /// <returns></returns>
        private int GetPCEffectivePerkLevel(NWPlayer player, int perkID)
        {
            var pcSkills = _data.Where<PCSkill>(x => x.PlayerID == player.GlobalID).ToList();
            // Get the PC's perk information and all of the perk levels at or below their current level.
            var pcPerk = _data.SingleOrDefault<PCPerk>(x => x.PlayerID == player.GlobalID && x.PerkID == perkID);
            if (pcPerk == null) return 0;

            // Get all of the perk levels in range, starting with the highest level.
            var perkLevelsInRange = _data
                .Where<PerkLevel>(x => x.PerkID == perkID && x.Level <= pcPerk.PerkLevel)
                .OrderByDescending(o => o.Level);

            // Iterate over each perk level. If player doesn't meet the requirements, the effective level is dropped.
            // Iteration ends when the player meets that level's requirements. 
            foreach (var perkLevel in perkLevelsInRange)
            {
                var skillRequirements = _data.Where<PerkLevelSkillRequirement>(r => r.PerkLevelID == perkLevel.ID);
                var questRequirements = _data.Where<PerkLevelQuestRequirement>(q => q.PerkLevelID == perkLevel.ID);
                int effectiveLevel = pcPerk.PerkLevel;

                // Check the skill requirements.
                foreach (var req in skillRequirements)
                {
                    var pcSkill = pcSkills.Single(x => x.SkillID == req.SkillID);
                    if (pcSkill.Rank < req.RequiredRank)
                    {
                        effectiveLevel--;
                        break;
                    }
                }

                // Was the effective level reduced during the skill check? No need to check quests.
                if (effectiveLevel != pcPerk.PerkLevel) continue;

                // Check the quest requirements.
                foreach (var req in questRequirements)
                {
                    var pcQuest = _data.SingleOrDefault<PCQuestStatus>(q => q.QuestID == req.RequiredQuestID);
                    if(pcQuest == null || pcQuest.CompletionDate == null)
                    {
                        effectiveLevel--;
                        break;
                    }
                }

                // Was the effective level reduced during the quest check? Move to the next lowest perk level.
                if (effectiveLevel != pcPerk.PerkLevel) continue;

                // Otherwise the player meets all requirements. This is their effective perk level.
                return effectiveLevel;
            }

            // Player meets none of the requirements for their purchased perk level. Their effective level is 0.
            return 0;
        }

    }
}
