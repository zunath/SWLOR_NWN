using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Messaging.Contracts;
using SWLOR.Game.Server.Messaging.Messages;
using SWLOR.Game.Server.NWNX;

using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public class PerkService: IPerkService
    {
        private readonly IColorTokenService _color;
        
        
        public PerkService(
            IColorTokenService color)
        {
            _color = color;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            // The player perk level cache gets refreshed on the following events.
            MessageHub.Instance.Subscribe<SkillDecayedMessage>(message => CacheAllPerkLevels(message.Player));
            MessageHub.Instance.Subscribe<SkillGainedMessage>(message => CacheAllPerkLevels(message.Player));
            MessageHub.Instance.Subscribe<PerkUpgradedMessage>(message => CacheEffectivePerkLevel(message.Player, message.PerkID));
            MessageHub.Instance.Subscribe<PerkRefundedMessage>(message => CacheEffectivePerkLevel(message.Player, message.PerkID));
            MessageHub.Instance.Subscribe<QuestCompletedMessage>(message => CacheAllPerkLevels(message.Player));
        }

        private List<PCPerk> GetPCPerksByExecutionType(NWPlayer oPC, PerkExecutionType executionType)
        {
            var pcPerks = DataService.Where<PCPerk>(x => x.PlayerID == oPC.GlobalID);
            return pcPerks.Where(x =>
            {
                // Filter on equipment-based execution type.
                var perk = DataService.Get<Data.Entity.Perk>(x.PerkID);
                bool matchesExecutionType = perk.ExecutionTypeID == (int)executionType;
                if (!matchesExecutionType) return false;

                // Filter out any perks the PC doesn't meet the requirements for.
                int effectivePerkLevel = GetPCEffectivePerkLevel(oPC, x.PerkID);
                if (effectivePerkLevel <= 0) return false;

                // Meets all requirements.
                return true;
            }).ToList();

        }

        private void CacheAllPerkLevels(NWPlayer player)
        {
            var perks = DataService.Where<PCPerk>(x => x.PlayerID == player.GlobalID);
            foreach (var perk in perks)
            {
                CacheEffectivePerkLevel(player, perk.PerkID);
            }
        }

        public void OnModuleEnter()
        {
            // The first time a player logs in, build their effective perk level cache.
            // This cache gets used all over to determine whether the player can use a perk.
            // It's cheaper for us to perform this calculation up front than to do it later.

            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;
            
            // Are the player's perks already cached? This has already run for this player. Exit.
            if (AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID)) return;
            
            CacheAllPerkLevels(player);
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
                    var perk = DataService.Get<Data.Entity.Perk>(pcPerk.PerkID);
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
                    var perk = DataService.Get<Data.Entity.Perk>(pcPerk.PerkID);
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

        public int GetPCPerkLevel(NWPlayer player, int perkTypeID)
        {
            if (!player.IsPlayer) return -1;
            return GetPCEffectivePerkLevel(player, perkTypeID);
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            if (!oPC.IsPlayer) return;
            NWItem oItem = (_.GetSpellCastItem());
            int type = oItem.BaseItemType;
            var pcPerks = DataService.Where<PCPerk>(x =>
            {
                if (oPC.GlobalID != x.PlayerID) return false;

                // Only pull back perks which have a Shield On Hit execution type.
                var perk = DataService.Get<Data.Entity.Perk>(x.PerkID);
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
                var perkFeat = DataService.SingleOrDefault<PerkFeat>(x => x.PerkID == pcPerk.PerkID);
                int featID = perkFeat == null ? -1 : perkFeat.FeatID;

                if (!App.IsKeyRegistered<IPerk>("Perk." + perk.ScriptName)) continue;

                App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
                {
                    if (type == BASE_ITEM_SMALLSHIELD || type == BASE_ITEM_LARGESHIELD || type == BASE_ITEM_TOWERSHIELD)
                    {
                        perkAction.OnImpact(oPC, oItem, pcPerk.PerkLevel, featID);
                    }
                });
                
            }
        }

        public int GetPCTotalPerkCount(Guid playerID)
        {
            return DataService.GetAll<PCPerk>().Count(x => x.PlayerID == playerID);
        }


        public List<Data.Entity.Perk> GetPerksAvailableToPC(NWPlayer player)
        {
            var playerID = player.GlobalID;
            var pcSkills = DataService.Where<PCSkill>(x => x.PlayerID == playerID).ToList();

            return DataService.Where<Data.Entity.Perk>(x =>
            {
                if (!x.IsActive) return false;
                // Determination for whether a player can see a perk in the menu is based on whether they meet the
                // requirements for the first level in that perk.
                var perkLevel = DataService.Single<PerkLevel>(pl => pl.PerkID == x.ID && pl.Level == 1);
                var skillRequirements = DataService.Where<PerkLevelSkillRequirement>(sr => sr.PerkLevelID == perkLevel.ID);
                var questRequirements = DataService.Where<PerkLevelQuestRequirement>(qr => qr.PerkLevelID == perkLevel.ID);

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
                    var pcQuest = DataService.SingleOrDefault<PCQuestStatus>(q => q.PlayerID == player.GlobalID && 
                                                                            q.QuestID == questReq.RequiredQuestID);
                    if (pcQuest == null || pcQuest.CompletionDate == null)
                        return false;
                }

                return true;
            }).ToList();
        }
        
        public Data.Entity.Perk GetPerkByID(int perkID)
        {
            return DataService.Single<Data.Entity.Perk>(x => x.ID == perkID);
        }

        public PCPerk GetPCPerkByID(Guid playerID, int perkID)
        {
            return DataService.SingleOrDefault<PCPerk>(x => x.PlayerID == playerID && x.PerkID == perkID);
        }

        public PerkLevel FindPerkLevel(IEnumerable<PerkLevel> levels, int findLevel)
        {
            return levels.FirstOrDefault(lvl => lvl.Level == findLevel);
        }

        public bool CanPerkBeUpgraded(NWPlayer player, int perkID)
        {
            var dbPlayer = DataService.Get<Player>(player.GlobalID);
            var perkLevels = DataService.Where<PerkLevel>(x => x.PerkID == perkID).ToList();
            var pcPerk = DataService.SingleOrDefault<PCPerk>(x => x.PlayerID == player.GlobalID && x.PerkID == perkID);

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

            var skillRequirements = DataService.Where<PerkLevelSkillRequirement>(x => x.PerkLevelID == level.ID).ToList();

            var questRequirements = DataService.Where<PerkLevelQuestRequirement>(x => x.PerkLevelID == level.ID).ToList();

            foreach (var req in skillRequirements)
            {
                PCSkill pcSkill = DataService.Single<PCSkill>(x => x.PlayerID == dbPlayer.ID && 
                                                             x.SkillID == req.SkillID);

                if (pcSkill.Rank < req.RequiredRank) return false;
            }

            foreach (var req in questRequirements)
            {
                var pcQuest = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == dbPlayer.ID && 
                                                               x.QuestID == req.RequiredQuestID && 
                                                               x.CompletionDate != null);
                if (pcQuest == null) return false;
            }
            return true;
        }

        public void DoPerkUpgrade(NWPlayer oPC, int perkID, bool freeUpgrade = false)
        {
            var perk = DataService.Single<Data.Entity.Perk>(x => x.ID == perkID);
            var perkLevels = DataService.Where<PerkLevel>(x => x.PerkID == perkID);
            var pcPerk = DataService.SingleOrDefault<PCPerk>(x => x.PlayerID == oPC.GlobalID && x.PerkID == perkID);
            var player = DataService.Single<Player>(x => x.ID == oPC.GlobalID);

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
                DataService.SubmitDataChange(pcPerk, action);

                if (!freeUpgrade)
                {
                    player.UnallocatedSP -= nextPerkLevel.Price;
                    DataService.SubmitDataChange(player, DatabaseActionType.Update);
                }

                // Look for any perk levels to grant.
                var perkFeatsToGrant = DataService.Where<PerkFeat>(x => x.PerkID == perkID && x.PerkLevelUnlocked == pcPerk.PerkLevel);
                
                // If at least one feat ID is assigned, add the feat(s) to the player if it doesn't exist yet.
                if (perkFeatsToGrant.Count > 0)
                {
                    foreach (var perkFeat in perkFeatsToGrant)
                    {
                        if (_.GetHasFeat(perkFeat.FeatID, oPC.Object) == TRUE) continue;

                        NWNXCreature.AddFeatByLevel(oPC, perkFeat.FeatID, 1);

                        var qbs = NWNXPlayerQuickBarSlot.UseFeat(perkFeat.FeatID);

                        // Try to add the new feat to the player's hotbar.
                        if (NWNXPlayer.GetQuickBarSlot(oPC, 0).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 0, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 1).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 1, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 2).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 2, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 3).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 3, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 4).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 4, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 5).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 5, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 6).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 6, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 7).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 7, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 8).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 8, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 9).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 9, qbs);
                        else if (NWNXPlayer.GetQuickBarSlot(oPC, 10).ObjectType == QuickBarSlotType.Empty)
                            NWNXPlayer.SetQuickBarSlot(oPC, 10, qbs);

                    }

                }

                oPC.SendMessage(_color.Green("Perk Purchased: " + perk.Name + " (Lvl. " + pcPerk.PerkLevel + ")"));

                App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkScript) =>
                {
                    if (perkScript == null) return;
                    perkScript.OnPurchased(oPC, pcPerk.PerkLevel);
                });

                MessageHub.Instance.Publish(new PerkUpgradedMessage(oPC, perkID));
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
        
        /// <summary>
        /// Returns the EFFECTIVE perk level of a player.
        /// This takes into account the player's skills. If they are too low to use the perk, the level will be
        /// reduced to the appropriate level.
        /// </summary>
        /// <returns></returns>
        private int GetPCEffectivePerkLevel(NWPlayer player, int perkID)
        {
            // Effective levels are cached because they're so frequently used.
            // They get recached on the following events:
            //      - Player log-in
            //      - Player gains a skill rank
            //      - Player's skill decays
            //      - Player buys a perk
            //      - Player refunds a perk
            //      - Player completes a quest
            if (!AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID)) return 0;
            var levels = AppCache.PlayerEffectivePerkLevels[player.GlobalID];
            if (!levels.ContainsKey(perkID)) return 0;
            return levels[perkID];
        }

        public void CacheEffectivePerkLevel(NWPlayer player, int perkID)
        {
            if (!AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID))
            {
                AppCache.PlayerEffectivePerkLevels.Add(player.GlobalID, new Dictionary<int, int>());
            }

            int perkLevel = CalculateEffectivePerkLevel(player, perkID);
            var levels = AppCache.PlayerEffectivePerkLevels[player.GlobalID];
            levels[perkID] = perkLevel;
        }

        private int CalculateEffectivePerkLevel(NWPlayer player, int perkID)
        {
            using (new Profiler("PerkService::CalculateEffectivePerkLevel"))
            {
                var pcSkills = DataService.Where<PCSkill>(x => x.PlayerID == player.GlobalID);
                // Get the PC's perk information and all of the perk levels at or below their current level.
                var pcPerk = DataService.SingleOrDefault<PCPerk>(x => x.PlayerID == player.GlobalID && x.PerkID == perkID);
                if (pcPerk == null) return 0;

                // Get all of the perk levels in range, starting with the highest level.
                var perkLevelsInRange = DataService
                    .Where<PerkLevel>(x => x.PerkID == perkID && x.Level <= pcPerk.PerkLevel)
                    .OrderByDescending(o => o.Level);

                using (new Profiler("PerkService::CalculateEffectivePerkLevel::PerkLevelIteration"))
                {

                    // Iterate over each perk level. If player doesn't meet the requirements, the effective level is dropped.
                    // Iteration ends when the player meets that level's requirements. 
                    foreach (var perkLevel in perkLevelsInRange)
                    {
                        var skillRequirements = DataService.Where<PerkLevelSkillRequirement>(r => r.PerkLevelID == perkLevel.ID);
                        var questRequirements = DataService.Where<PerkLevelQuestRequirement>(q => q.PerkLevelID == perkLevel.ID);
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
                            var pcQuest = DataService.SingleOrDefault<PCQuestStatus>(q => q.PlayerID == player.GlobalID && q.QuestID == req.RequiredQuestID);
                            if (pcQuest == null || pcQuest.CompletionDate == null)
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
                }

                // Player meets none of the requirements for their purchased perk level. Their effective level is 0.
                return 0;
            }
        }

    }
}
