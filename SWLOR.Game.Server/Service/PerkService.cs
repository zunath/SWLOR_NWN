using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;
using BaseItemType = SWLOR.Game.Server.NWScript.Enumerations.BaseItemType;
using PerkType = SWLOR.Game.Server.Enumeration.PerkType;

namespace SWLOR.Game.Server.Service
{
    public static class PerkService
    {
        private static readonly Dictionary<PerkType, IPerk> _perkHandlers;
        private static readonly Dictionary<PerkCategoryType, PerkCategory> _perkCategories;
        private static readonly Dictionary<SkillType, HashSet<PerkType>> _perkRequirementsBySkill;
        private static readonly Dictionary<int, HashSet<PerkType>> _perkRequirementsByQuest;

        static PerkService()
        {
            _perkHandlers = new Dictionary<PerkType, IPerk>();
            _perkCategories = new Dictionary<PerkCategoryType, PerkCategory>();
            _perkRequirementsBySkill = new Dictionary<SkillType, HashSet<PerkType>>();
            _perkRequirementsByQuest = new Dictionary<int, HashSet<PerkType>>();
        }

        public static void SubscribeEvents()
        {
            // The player perk level cache gets refreshed on the following events.
            MessageHub.Instance.Subscribe<OnSkillDecayed>(message => CachePerkIDsRequiringSkill(message.Player, message.SkillType));
            MessageHub.Instance.Subscribe<OnSkillGained>(message => CachePerkIDsRequiringSkill(message.Player, message.SkillType));
            MessageHub.Instance.Subscribe<OnPerkUpgraded>(message => CacheEffectivePerkLevel(message.Player, message.PerkType));
            MessageHub.Instance.Subscribe<OnPerkRefunded>(message => CacheEffectivePerkLevel(message.Player, message.PerkType));
            MessageHub.Instance.Subscribe<OnQuestCompleted>(message => CachePerkIDsRequiringQuest(message.Player, message.QuestID));

            // Feat Events
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());

            // Module Events
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(message => OnModuleUnequipItem());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            Console.WriteLine("PerkService -> OnModuleLoad RegisterPerkHandlers");
            RegisterPerkHandlers();
            Console.WriteLine("PerkService -> OnModuleLoad RegisterPerkCategories");
            RegisterPerkCategories();

            Console.WriteLine("PerkService -> OnModuleLoad OrganizePerkRequirements");
            OrganizePerkRequirements();
            Console.WriteLine("PerkService -> OnModuleLoad Complete");
        }

        private static void RegisterPerkHandlers()
        {
            // Use reflection to get all of IPerkHandler implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IPerk).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                IPerk instance = Activator.CreateInstance(type) as IPerk;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _perkHandlers.Add(instance.PerkType, instance);
            }
        }

        private static void RegisterPerkCategories()
        {
            var values = Enum.GetValues(typeof(PerkCategoryType)).Cast<PerkCategoryType>();

            foreach (var value in values)
            {
                var category = new PerkCategory(value, value.GetName(), value.GetIsActive(), value.GetSequence());
                _perkCategories[value] = category;
            }
        }

        private static void OrganizePerkRequirements()
        {
            // Calculating effective perk levels can be expensive. To aid with the performance,
            // organize skill IDs and quest IDs by which perks require them.
            // That way, later checks are much quicker than iterating through the data cache for this info.
            foreach (var perk in _perkHandlers.Values)
            {
                // Check for a skill requirement on this perk. We don't care WHICH perk level has which skill requirement,
                // we only care to know that there IS one.

                var skillReqs = perk.PerkLevels.Where(x => x.Value.SkillRequirements.Count > 0)
                    .SelectMany(s => s.Value.SkillRequirements);
                foreach (var skillReq in skillReqs)
                {
                    // Add a new skill hashset if it doesn't exist yet.
                    if (!_perkRequirementsBySkill.ContainsKey(skillReq.Key))
                    {
                        _perkRequirementsBySkill.Add(skillReq.Key, new HashSet<PerkType>());
                    }

                    // Get the perk ID hashset and see if this perk is already contained.
                    // If not, add it.
                    var perkIDs = _perkRequirementsBySkill[skillReq.Key];
                    if (!perkIDs.Contains(perk.PerkType))
                    {
                        _perkRequirementsBySkill[skillReq.Key].Add(perk.PerkType);
                    }
                }

                // Now check for a quest requirement on this perk. Again, we don't care which perk level has which quest requirement,
                // we only care to know that there IS one.

                var questReqs = perk.PerkLevels.Where(x => x.Value.QuestRequirements.Count > 0)
                    .SelectMany(s => s.Value.QuestRequirements);
                foreach (var questReq in questReqs)
                {
                    // Add a new quest hashset if it doesn't exist yet.
                    if (!_perkRequirementsByQuest.ContainsKey(questReq))
                    {
                        _perkRequirementsByQuest.Add(questReq, new HashSet<PerkType>());
                    }

                    // Get the perk ID hashset and see if this perk is already contained.
                    // If not, add it.
                    var perkIDs = _perkRequirementsByQuest[questReq];
                    if (!perkIDs.Contains(perk.PerkType))
                    {
                        _perkRequirementsByQuest[questReq].Add(perk.PerkType);
                    }
                }
            }
        }

        public static IPerk GetPerkHandler(PerkType perkType)
        {
            if (!_perkHandlers.ContainsKey(perkType))
            {
                throw new KeyNotFoundException("PerkType '" + perkType + "' is not registered. Did you make the perk script?");
            }

            return _perkHandlers[perkType];
        }

        public static IPerk GetPerkHandler(int perkID)
        {
            PerkType perkType = (PerkType)perkID;
            return GetPerkHandler(perkType);
        }

        public static PerkCategory GetPerkCategory(PerkCategoryType categoryType)
        {
            return _perkCategories[categoryType];
        }

        private static List<PCPerk> GetPCPerksByExecutionType(NWPlayer oPC, PerkExecutionType executionType)
        {
            var pcPerks = DataService.PCPerk.GetAllByPlayerID(oPC.GlobalID);
            return pcPerks.Where(x =>
            {
                // Filter on equipment-based execution type.
                var perk = GetPerkHandler(x.PerkID);
                bool matchesExecutionType = perk.ExecutionType == executionType;
                if (!matchesExecutionType) return false;

                // Filter out any perks the PC doesn't meet the requirements for.
                int effectivePerkLevel = GetPCEffectivePerkLevel(oPC, perk.PerkType);
                if (effectivePerkLevel <= 0) return false;

                // Meets all requirements.
                return true;
            }).ToList();

        }

        private static void CachePerkIDsRequiringSkill(NWPlayer player, SkillType skillType)
        {
            if (_perkRequirementsBySkill.TryGetValue(skillType, out var perkIDs))
            {
                CacheEffectivePerkLevels(player, perkIDs);
            }
        }

        private static void CachePerkIDsRequiringQuest(NWPlayer player, int questID)
        {
            if (_perkRequirementsByQuest.TryGetValue(questID, out var perkIDs))
            {
                CacheEffectivePerkLevels(player, perkIDs);
            }
        }

        private static void CacheEffectivePerkLevels(NWPlayer player, IEnumerable<PerkType> perkTypes)
        {
            foreach (var perkType in perkTypes)
            {
                CacheEffectivePerkLevel(player, perkType);
            }
        }

        public static void OnModuleEnter()
        {
            // The first time a player logs in, add an entry to the effective perk level cache.
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            // Are the player's perks already cached? This has already run for this player. Exit.
            if (AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID)) return;

            AppCache.PlayerEffectivePerkLevels.Add(player.GlobalID, new Dictionary<PerkType, int>());
        }

        private static void OnModuleEquipItem()
        {
            NWPlayer oPC = (_.GetPCItemLastEquippedBy());
            if (oPC.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            NWItem oItem = (_.GetPCItemLastEquipped());
            if (!oPC.IsPlayer || !oPC.IsInitializedAsPlayer) return;
            if (oPC.GetLocalBoolean("LOGGED_IN_ONCE") == false) return;

            var executionPerks = GetPCPerksByExecutionType(oPC, PerkExecutionType.EquipmentBased);
            foreach (PCPerk pcPerk in executionPerks)
            {
                var handler = GetPerkHandler(pcPerk.PerkID);
                handler.OnItemEquipped(oPC, oItem);
            }

        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer oPC = (_.GetPCItemLastUnequippedBy());

            if (oPC.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.
            NWItem oItem = (_.GetPCItemLastUnequipped());
            if (!oPC.IsPlayer) return;

            var executionPerks = GetPCPerksByExecutionType(oPC, PerkExecutionType.EquipmentBased);
            foreach (PCPerk pcPerk in executionPerks)
            {
                var handler = GetPerkHandler(pcPerk.PerkID);
                handler.OnItemUnequipped(oPC, oItem);
            }
        }

        public static int GetCreaturePerkLevel(NWCreature creature, PerkType perkType)
        {
            if (creature.IsPlayer)
            {
                NWPlayer player = creature.Object;
                return GetPCEffectivePerkLevel(player, perkType);
            }
            else
            {
                return creature.GetLocalInt("PERK_LEVEL_" + perkType);
            }
        }

        private static void OnHitCastSpell()
        {
            NWPlayer oPC = NWGameObject.OBJECT_SELF;
            if (!oPC.IsValid || !oPC.IsPlayer) return;
            NWItem oItem = (_.GetSpellCastItem());
            var type = oItem.BaseItemType;
            var pcPerks = DataService.PCPerk.GetAllByPlayerID(oPC.GlobalID).Where(x =>
            {
                if (oPC.GlobalID != x.PlayerID) return false;

                // Only pull back perks which have a Shield On Hit execution type.
                var perk = GetPerkHandler(x.PerkID);
                if (perk.ExecutionType != PerkExecutionType.ShieldOnHit)
                    return false;

                // If player's effective level is zero, it's not in effect.
                int effectiveLevel = GetPCEffectivePerkLevel(oPC, perk.PerkType);
                if (effectiveLevel <= 0) return false;

                return true;
            });

            if (type == BaseItemType.SmallShield || type == BaseItemType.LargeShield || type == BaseItemType.TowerShield)
            {
                foreach (PCPerk pcPerk in pcPerks)
                {
                    var perk = GetPerkHandler(pcPerk.PerkID);
                    if (perk.ExecutionType == (int)PerkExecutionType.None) continue;
                    var perkFeat = DataService.PerkFeat.GetByPerkIDAndLevelUnlockedOrDefault(pcPerk.PerkID, pcPerk.PerkLevel);
                    int spellTier = perkFeat?.PerkLevelUnlocked ?? 0;

                    var handler = GetPerkHandler(pcPerk.PerkID);
                    handler.OnImpact(oPC, oItem, pcPerk.PerkLevel, spellTier);
                }
            }
        }

        public static int GetPCTotalPerkCount(Guid playerID)
        {
            return DataService.PCPerk.GetAllByPlayerID(playerID).Count();
        }


        public static List<IPerk> GetPerksAvailableToPC(NWPlayer player)
        {
            var playerID = player.GlobalID;
            var pcSkills = DataService.PCSkill.GetAllByPlayerID(playerID).ToList();

            return _perkHandlers.Values.Where(x =>
            {
                if (!x.IsActive) return false;
                // Determination for whether a player can see a perk in the menu is based on whether they meet the
                // requirements for the first level in that perk.
                var perkLevel = x.PerkLevels[1];
                var skillRequirements = perkLevel.SkillRequirements;
                var questRequirements = perkLevel.QuestRequirements;

                // Check the player's skill level against the perk requirements.
                foreach (var skillReq in skillRequirements)
                {
                    var pcSkill = pcSkills.Single(s => s.SkillID == (int)skillReq.Key);

                    if (pcSkill.Rank < skillReq.Value)
                    {
                        return false;
                    }
                }

                // Check the player's quest completion status against the perk requirements.
                foreach (var questReq in questRequirements)
                {
                    var pcQuest = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questReq);
                    if (pcQuest == null || pcQuest.CompletionDate == null)
                        return false;
                }

                // Note: We do not filter out missing specialization requirements. This is because we want to show
                // the player what's available should they decide to swap specializations.

                return true;
            }).ToList();
        }

        public static IPerk GetPerkByID(PerkType perkType)
        {
            return GetPerkHandler(perkType);
        }

        public static PCPerk GetPCPerkByID(Guid playerID, int perkID)
        {
            return DataService.PCPerk.GetByPlayerAndPerkIDOrDefault(playerID, perkID);
        }

        /// <summary>
        /// Checks whether a player can upgrade a perk to the next level.
        /// </summary>
        /// <param name="player">The player upgrading.</param>
        /// <param name="perkID">The perk that's being upgraded.</param>
        /// <returns>true if the perk can be upgraded, false otherwise.</returns>
        public static bool CanPerkBeUpgraded(NWPlayer player, int perkID)
        {
            // Retrieve database records.
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var perk = GetPerkHandler(perkID);
            var perkLevels = perk.PerkLevels;
            var pcPerk = DataService.PCPerk.GetByPlayerAndPerkIDOrDefault(player.GlobalID, perkID);
            
            // Identify the max number of ranks for this perk.
            int rank = 0;
            if (pcPerk != null)
            {
                rank = pcPerk.PerkLevel;
            }
            int maxRank = perkLevels.Count;

            // If there's no more levels in this perk, exit early and return false.
            if (rank + 1 > maxRank) return false;

            // Get the next perk level.
            PerkLevel level = perkLevels.ContainsKey(rank+1) ?
                perkLevels[rank+1] :
                null;

            if (level == null) return false;

            // If the player doesn't have enough SP to purchase this rank, exit early and return false.
            if (dbPlayer.UnallocatedSP < level.Price) return false;

            // Retrieve skill and quest requirements for this perk.
            var skillRequirements = level.SkillRequirements;
            var questRequirements = level.QuestRequirements;

            // Cycle through the skill requirements
            foreach (var req in skillRequirements)
            {
                PCSkill pcSkill = DataService.PCSkill.GetByPlayerIDAndSkillID(dbPlayer.ID, (int)req.Key);

                // Player has not completed this required quest. Exit early and return false.
                if (pcSkill.Rank < req.Value) return false;
            }

            // Cycle through the quest requirements.
            foreach (var req in questRequirements)
            {
                var pcQuest = DataService.PCQuestStatus.GetByPlayerAndQuestID(dbPlayer.ID, req);
                
                // Player has not completed this required quest. Exit early and return false.
                if (pcQuest == null || pcQuest.CompletionDate == null) return false;
            }

            // If this perk level requires a specialization, confirm the player has the required specialization.
            if (level.Specialization != SpecializationType.None)
            {
                if (level.Specialization != dbPlayer.SpecializationID)
                    return false;
            }

            // All requirements have been met. Return true.
            return true;
        }

        /// <summary>
        /// Performs a perk purchase for a player. This handles deducting SP, inserting perk records,
        /// and adjusting hotbar slots as necessary. 
        /// </summary>
        /// <param name="oPC">The player receiving the perk upgrade.</param>
        /// <param name="perkID">The ID number of the perk.</param>
        /// <param name="freeUpgrade">If true, no SP will be deducted. Otherwise, SP will be deducted from player.</param>
        public static void DoPerkUpgrade(NWPlayer oPC, int perkID, bool freeUpgrade = false)
        {
            var perk = GetPerkHandler(perkID);
            var perkLevels = perk.PerkLevels;
            var pcPerk = DataService.PCPerk.GetByPlayerAndPerkIDOrDefault(oPC.GlobalID, perkID);
            var player = DataService.Player.GetByID(oPC.GlobalID);

            if (freeUpgrade || CanPerkBeUpgraded(oPC, perkID))
            {
                DatabaseActionType action = DatabaseActionType.Update;
                if (pcPerk == null)
                {
                    pcPerk = new PCPerk();
                    DateTime dt = DateTime.UtcNow;
                    pcPerk.AcquiredDate = dt;
                    pcPerk.PerkID = (int)perk.PerkType;
                    pcPerk.PlayerID = oPC.GlobalID;
                    pcPerk.PerkLevel = 0;

                    action = DatabaseActionType.Insert;
                }

                PerkLevel nextPerkLevel = perkLevels.ContainsKey(pcPerk.PerkLevel + 1) ? 
                    perkLevels[pcPerk.PerkLevel + 1] : 
                    null;
                
                if (nextPerkLevel == null) return;
                pcPerk.PerkLevel++;
                DataService.SubmitDataChange(pcPerk, action);

                if (!freeUpgrade)
                {
                    player.UnallocatedSP -= nextPerkLevel.Price;
                    DataService.SubmitDataChange(player, DatabaseActionType.Update);
                }

                // Look for a perk feat to grant.
                var perkFeatToGrant = DataService.PerkFeat.GetByPerkIDAndLevelUnlockedOrDefault(perkID, pcPerk.PerkLevel);

                // Add the feat(s) to the player if it doesn't exist yet.
                if (perkFeatToGrant != null && _.GetHasFeat((Feat)perkFeatToGrant.FeatID, oPC.Object) == false)
                {
                    NWNXCreature.AddFeatByLevel(oPC, (Feat)perkFeatToGrant.FeatID, 1);

                    var qbs = NWNXPlayerQuickBarSlot.UseFeat((Feat)perkFeatToGrant.FeatID);

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

                oPC.SendMessage(ColorTokenService.Green("Perk Purchased: " + perk.Name + " (Lvl. " + pcPerk.PerkLevel + ")"));

                MessageHub.Instance.Publish(new OnPerkUpgraded(oPC, (PerkType)perkID));

                var handler = GetPerkHandler(perkID);
                handler.OnPurchased(oPC, pcPerk.PerkLevel);
            }
            else
            {
                oPC.FloatingText(ColorTokenService.Red("You cannot purchase the perk at this time."));
            }
        }
        /// <summary>
        /// Performs a perk purchase for a player. This handles deducting SP, inserting perk records,
        /// and adjusting hotbar slots as necessary. 
        /// </summary>
        /// <param name="player">The player receiving the upgrade.</param>
        /// <param name="perkType">The type of perk to upgrade.</param>
        /// <param name="freeUpgrade">If true, no SP will be deducted. Otherwise, SP will be deducted from player.</param>
        public static void DoPerkUpgrade(NWPlayer player, PerkType perkType, bool freeUpgrade = false)
        {
            DoPerkUpgrade(player, (int)perkType, freeUpgrade);
        }

        /// <summary>
        /// Returns the EFFECTIVE perk level of a player.
        /// This takes into account the player's skills. If they are too low to use the perk, the level will be
        /// reduced to the appropriate level.
        /// </summary>
        /// <returns></returns>
        private static int GetPCEffectivePerkLevel(NWPlayer player, PerkType perkID)
        {
            // Effective levels are cached because they're so frequently used.
            // They get recached on the following events:
            //      - Player gains a skill rank
            //      - Player's skill decays
            //      - Player buys a perk
            //      - Player refunds a perk
            //      - Player completes a quest
            //      - If a request for the value doesn't exist.

            // Player entry doesn't exist in the cache. Add it now.
            if (!AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID))
            {
                AppCache.PlayerEffectivePerkLevels.Add(player.GlobalID, new Dictionary<PerkType, int>());
            }

            // If the cache doesn't contain this perkID, we need to cache it.
            var levels = AppCache.PlayerEffectivePerkLevels[player.GlobalID];
            if (!levels.ContainsKey(perkID))
            {
                // This value has not been cached since the player logged in. 
                // Cache it if possible.
                CacheEffectivePerkLevel(player, perkID);
            }
            if (!levels.ContainsKey(perkID)) return 0;
            return levels[perkID];
        }

        public static void CacheEffectivePerkLevel(NWPlayer player, PerkType perkID)
        {
            if (!AppCache.PlayerEffectivePerkLevels.ContainsKey(player.GlobalID))
            {
                AppCache.PlayerEffectivePerkLevels.Add(player.GlobalID, new Dictionary<PerkType, int>());
            }

            int perkLevel = CalculateEffectivePerkLevel(player, perkID);
            var levels = AppCache.PlayerEffectivePerkLevels[player.GlobalID];
            levels[perkID] = perkLevel;
        }

        private static int CalculateEffectivePerkLevel(NWPlayer player, PerkType perkType)
        {
            using (new Profiler("PerkService::CalculateEffectivePerkLevel"))
            {
                var pcSkills = DataService.PCSkill.GetAllByPlayerID(player.GlobalID).ToList();
                // Get the PC's perk information and all of the perk levels at or below their current level.
                var pcPerk = DataService.PCPerk.GetByPlayerAndPerkIDOrDefault(player.GlobalID, (int)perkType);
                if (pcPerk == null) return 0;

                // Get all of the perk levels in range, starting with the highest level.
                var perk = GetPerkHandler(pcPerk.PerkID);
                var perkLevelsInRange = perk.PerkLevels.Where(x => x.Key <= pcPerk.PerkLevel)
                    .OrderByDescending(o => o.Key);

                using (new Profiler("PerkService::CalculateEffectivePerkLevel::PerkLevelIteration"))
                {

                    // Iterate over each perk level. If player doesn't meet the requirements, the effective level is dropped.
                    // Iteration ends when the player meets that level's requirements. 
                    foreach (var perkLevel in perkLevelsInRange)
                    {
                        var skillRequirements = perkLevel.Value.SkillRequirements;
                        var questRequirements = perkLevel.Value.QuestRequirements;
                        int effectiveLevel = pcPerk.PerkLevel;

                        // Check the skill requirements.
                        foreach (var req in skillRequirements)
                        {
                            var pcSkill = pcSkills.Single(x => x.SkillID == (int)req.Key);
                            if (pcSkill.Rank < req.Value)
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
                            var pcQuest = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, req);
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
