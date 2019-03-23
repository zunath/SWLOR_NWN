using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Messaging.Messages;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Skill;
using System;
using System.Collections.Generic;
using System.Linq;
using static NWN._;


namespace SWLOR.Game.Server.Service
{
    public class SkillService : ISkillService
    {
        private const string IPWeaponPenaltyTag = "SKILL_PENALTY_WEAPON_ITEM_PROPERTY";
        private const string IPEquipmentPenaltyTag = "SKILL_PENALTY_EQUIPMENT_ITEM_PROPERTY";
        
        
        private readonly IEnmityService _enmity;
        private readonly IPlayerStatService _playerStat;
        private readonly IItemService _item;
        


        public SkillService(
            
            IEnmityService enmity,
            IPlayerStatService playerStat,
            IItemService item)
        {

            

            _enmity = enmity;
            _playerStat = playerStat;
            _item = item;
        }

        public int SkillCap => 500;


        public void RegisterPCToAllCombatTargetsForSkill(NWPlayer player, SkillType skillType, NWCreature target)
        {
            int skillID = (int)skillType;
            if (!player.IsPlayer) return;
            if (skillID <= 0) return;

            List<NWPlayer> members = player.PartyMembers.ToList();

            int nth = 1;
            NWCreature creature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, player.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0);
            while (creature.IsValid)
            {
                if (_.GetDistanceBetween(player.Object, creature.Object) > 20.0f) break;

                // Check NPC's enmity table 
                EnmityTable enmityTable = _enmity.GetEnmityTable(creature);
                foreach (var member in members)
                {
                    if (enmityTable.ContainsKey(member.GlobalID) || (target != null && target.IsValid && target == creature))
                    {
                        RegisterPCToNPCForSkill(player, creature, skillID);
                        break;
                    }
                }

                nth++;
                creature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, player.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0);
            }
        }

        public void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp, bool enableResidencyBonus = true)
        {
            GiveSkillXP(oPC, (int)skill, xp, enableResidencyBonus);
        }

        public void GiveSkillXP(NWPlayer oPC, int skillID, int xp, bool enableResidencyBonus = true)
        {
            if (skillID <= 0 || xp <= 0 || !oPC.IsPlayer) return;

            if (enableResidencyBonus)
            {
                xp = (int)(xp + xp * _playerStat.EffectiveResidencyBonus(oPC));
            }
            Player player = DataService.Get<Player>(oPC.GlobalID);
            Skill skill = GetSkill(skillID);

            // Check if the player has any undistributed skill ranks for this skill category.
            // If they haven't been distributed yet, the player CANNOT gain XP for this skill.
            var pool = DataService.SingleOrDefault<PCSkillPool>(x => x.PlayerID == oPC.GlobalID &&
                                                               x.SkillCategoryID == skill.SkillCategoryID &&
                                                               x.Levels > 0);
            if (pool != null)
            {
                oPC.FloatingText("You must distribute all pooled skill ranks before you can gain any new XP in the '" + skill.Name + "' skill. Access this menu from the 'View Skills' section of your rest menu.");
                return;
            }


            PCSkill pcSkill = GetPCSkill(oPC, skillID);
            SkillXPRequirement req = DataService.Single<SkillXPRequirement>(x => x.SkillID == skillID && x.Rank == pcSkill.Rank);
            int maxRank = skill.MaxRank;
            int originalRank = pcSkill.Rank;
            float xpBonusModifier = player.XPBonus * 0.01f;

            // Guard against XP bonuses being too high.
            if (xpBonusModifier > 0.25)
                xpBonusModifier = 0.25f;

            xp = CalculateTotalSkillPointsPenalty(player.TotalSPAcquired, xp);
            xp = xp + (int)(xp * xpBonusModifier);

            // Run the skill decay rules.
            // If the method returns false, that means all skills are locked.
            // So we can't give the player any XP.
            if (!ApplySkillDecay(oPC, pcSkill, xp))
            {
                return;
            }


            pcSkill.XP = pcSkill.XP + xp;
            oPC.SendMessage("You earned " + skill.Name + " skill experience. (" + xp + ")");

            // Skill is at cap and player would level up.
            // Reduce XP to required amount minus 1 XP
            if (pcSkill.Rank >= maxRank && pcSkill.XP > req.XP)
            {
                pcSkill.XP = req.XP - 1;
            }

            while (pcSkill.XP >= req.XP)
            {
                pcSkill.XP = pcSkill.XP - req.XP;

                if (player.TotalSPAcquired < SkillCap && skill.ContributesToSkillCap)
                {
                    player.UnallocatedSP++;
                    player.TotalSPAcquired++;
                }

                pcSkill.Rank++;
                oPC.FloatingText("Your " + skill.Name + " skill level increased to rank " + pcSkill.Rank + "!");
                req = DataService.Single<SkillXPRequirement>(x => x.SkillID == skillID && x.Rank == pcSkill.Rank);

                // Reapply skill penalties on a skill level up.
                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = _.GetItemInSlot(slot, oPC.Object);
                    RemoveWeaponPenalties(item);
                    ApplyWeaponPenalties(oPC, item);
                    RemoveEquipmentPenalties(item);
                    ApplyEquipmentPenalties(oPC, item);
                }

                MessageHub.Instance.Publish(new SkillGainedMessage(oPC, skillID));
            }

            DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);

            // Update player and apply stat changes only if a level up occurred.
            if (originalRank != pcSkill.Rank)
            {
                _playerStat.ApplyStatChanges(oPC, null);
            }
        }

        public int GetPCSkillRank(NWPlayer player, SkillType skill)
        {
            if (!player.IsPlayer || skill == SkillType.Unknown) return 0;

            return DataService.Single<PCSkill>(x => x.PlayerID == player.GlobalID && x.SkillID == (int)skill).Rank;
        }

        public int GetPCSkillRank(NWPlayer player, int skillID)
        {
            return GetPCSkillRank(player, (SkillType)skillID);
        }

        public PCSkill GetPCSkill(NWPlayer player, int skillID)
        {
            return DataService.Single<PCSkill>(x => x.PlayerID == player.GlobalID && x.SkillID == skillID);
        }

        public List<PCSkill> GetAllPCSkills(NWPlayer player)
        {
            return DataService.Where<PCSkill>(x => x.PlayerID == player.GlobalID).ToList();
        }

        public Skill GetSkill(int skillID)
        {
            return GetSkill((SkillType)skillID);
        }

        public Skill GetSkill(SkillType skillType)
        {
            return DataService.Get<Skill>((int)skillType);
        }

        public int GetPCTotalSkillCount(NWPlayer player)
        {
            var skills = DataService
                .Where<Skill>(x => x.ContributesToSkillCap)
                .Select(s => s.ID);
            var pcSkills = GetAllPCSkills(player)
                .Where(x => skills.Contains(x.SkillID));
            return pcSkills.Sum(x => x.Rank);
        }

        public List<SkillCategory> GetActiveCategories()
        {
            return DataService.Where<SkillCategory>(x => x.ID != 0).ToList();
        }

        public List<PCSkill> GetPCSkillsForCategory(Guid playerID, int skillCategoryID)
        {
            // Get list of skills part of this category.
            var skillIDs = DataService
                .Where<Skill>(x => x.SkillCategoryID == skillCategoryID && x.IsActive)
                .Select(s => s.ID);

            // Get all PC Skills with a matching category.
            var pcSkills = DataService.Where<PCSkill>(x => x.PlayerID == playerID &&
                                                     skillIDs.Contains(x.SkillID))
                .ToList();

            return pcSkills;
        }

        public void ToggleSkillLock(Guid playerID, int skillID)
        {
            PCSkill pcSkill = DataService.Single<PCSkill>(x => x.PlayerID == playerID && x.SkillID == skillID);
            pcSkill.IsLocked = !pcSkill.IsLocked;

            DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
        }

        public void OnCreatureDeath(NWCreature creature)
        {
            CreatureSkillRegistration reg = GetCreatureSkillRegistration(creature.GlobalID);
            List<PlayerSkillRegistration> playerRegs = reg.GetAllRegistrations();
            var registration = reg.Registrations.OrderByDescending(o => o.Value.HighestRank).FirstOrDefault();
            if (registration.Value == null) return;

            int partyLevel = registration.Value.HighestRank;

            // Identify base XP using delta between party level and enemy level.
            float cr = creature.ChallengeRating;
            int enemyLevel = (int)(cr * 5.0f);
            int delta = enemyLevel - partyLevel;
            float baseXP = 0;

            if (delta >= 6) baseXP = 400;
            else if (delta == 5) baseXP = 350;
            else if (delta == 4) baseXP = 325;
            else if (delta == 3) baseXP = 300;
            else if (delta == 2) baseXP = 250;
            else if (delta == 1) baseXP = 225;
            else if (delta == 0) baseXP = 200;
            else if (delta == -1) baseXP = 150;
            else if (delta == -2) baseXP = 100;
            else if (delta == -3) baseXP = 50;
            else if (delta == -4) baseXP = 25;

            float bonusXPPercentage = creature.GetLocalFloat("BONUS_XP_PERCENTAGE");
            if (bonusXPPercentage > 1) bonusXPPercentage = 1;
            else if (bonusXPPercentage < 0) bonusXPPercentage = 0;

            baseXP = baseXP + (baseXP * bonusXPPercentage);

            // Process each player skill registration.
            foreach (PlayerSkillRegistration preg in playerRegs)
            {
                // Rules for acquiring skill XP:
                // Player must be valid.
                // Player must be in the same area as the creature that just died.
                // Player must be within 30 meters of the creature that just died.
                if (!preg.Player.IsValid ||
                    preg.Player.Area.Resref != creature.Area.Resref ||
                        _.GetDistanceBetween(preg.Player.Object, creature.Object) > 40.0f)
                    continue;

                List<Tuple<int, PlayerSkillPointTracker>> skillRegs = preg.GetSkillRegistrationPoints();
                int totalPoints = preg.GetTotalSkillRegistrationPoints();

                // Retrieve all necessary PC skills up front
                int[] skillIDsToSearchFor = skillRegs.Select(x => x.Item2.SkillID).ToArray();

                var pcSkills = GetAllPCSkills(preg.Player)
                    .Where(x => skillIDsToSearchFor.Contains(x.SkillID))
                    .ToList();

                // Grant XP based on points acquired during combat.
                foreach (Tuple<int, PlayerSkillPointTracker> skreg in skillRegs)
                {
                    int skillID = skreg.Item1;
                    int skillRank = pcSkills.Single(x => x.SkillID == skillID).Rank;

                    int points = skreg.Item2.Points;

                    float percentage = points / (float)totalPoints;
                    float skillLDP = CalculatePartyLevelDifferencePenalty(partyLevel, skillRank);
                    float adjustedXP = baseXP * percentage * skillLDP;
                    adjustedXP = CalculateRegisteredSkillLevelAdjustedXP(adjustedXP, skillRank, skillRank);

                    GiveSkillXP(preg.Player, skillID, (int)adjustedXP);
                }

                float armorXP = baseXP;
                int lightArmorPoints = 0;
                int heavyArmorPoints = 0;
                int forceArmorPoints = 0;

                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = _.GetItemInSlot(slot, preg.Player.Object);
                    if (item.CustomItemType == CustomItemType.LightArmor)
                    {
                        lightArmorPoints++;
                    }
                    else if (item.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        heavyArmorPoints++;
                    }
                    else if (item.CustomItemType == CustomItemType.ForceArmor)
                    {
                        forceArmorPoints++;
                    }
                }
                totalPoints = lightArmorPoints + heavyArmorPoints + forceArmorPoints;
                if (totalPoints <= 0) continue;

                int armorRank = GetPCSkillRank(preg.Player, SkillType.LightArmor);
                float armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                float percent = lightArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.LightArmor, (int)(armorXP * percent * armorLDP));

                armorRank = GetPCSkillRank(preg.Player, SkillType.HeavyArmor);
                armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                percent = heavyArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.HeavyArmor, (int)(armorXP * percent * armorLDP));

                armorRank = GetPCSkillRank(preg.Player, SkillType.ForceArmor);
                armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                percent = forceArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.ForceArmor, (int)(armorXP * percent * armorLDP));


            }

            AppCache.CreatureSkillRegistrations.Remove(creature.GlobalID);
        }

        private float CalculatePartyLevelDifferencePenalty(int highestSkillRank, int skillRank)
        {
            int levelDifference = highestSkillRank - skillRank;
            float levelDifferencePenalty = 1.0f;
            if (levelDifference > 10)
            {
                levelDifferencePenalty = 1.0f - 0.05f * (levelDifference - 10);
                if (levelDifferencePenalty < 0.20f) levelDifferencePenalty = 0.20f;
            }

            return levelDifferencePenalty;
        }

        public void OnAreaExit()
        {
            NWPlayer oPC = _.GetExitingObject();
            RemovePlayerFromRegistrations(oPC);
        }

        public void OnModuleEnter()
        {
            NWPlayer oPC = _.GetEnteringObject();
            if (oPC.IsPlayer)
            {
                // Add any missing skills the player does not have.
                var skills = DataService.Where<Skill>(x =>
                {
                    var pcSkill = DataService.SingleOrDefault<PCSkill>(s => s.SkillID == x.ID && s.PlayerID == oPC.GlobalID);
                    return pcSkill == null;
                });
                foreach (var skill in skills)
                {
                    var pcSkill = new PCSkill
                    {
                        IsLocked = false,
                        SkillID = skill.ID,
                        PlayerID = oPC.GlobalID,
                        Rank = 0,
                        XP = 0
                    };

                    DataService.SubmitDataChange(pcSkill, DatabaseActionType.Insert);
                }
                ForceEquipFistGlove(oPC);
            }
        }

        public void OnModuleClientLeave()
        {
            NWPlayer oPC = _.GetExitingObject();
            if (!oPC.IsPlayer) return;

            RemovePlayerFromRegistrations(oPC);
        }

        public void OnModuleItemEquipped()
        {
            using (new Profiler("SkillService::OnModuleItemEquipped()"))
            {
                NWPlayer oPC = _.GetPCItemLastEquippedBy();
                if (!oPC.IsInitializedAsPlayer) return; // Players who log in for the first time don't have an ID yet.
                if (oPC.GetLocalInt("LOGGED_IN_ONCE") <= 0) return; // Don't fire heavy calculations if this is the player's first log in after a restart.

                NWItem oItem = _.GetPCItemLastEquipped();
                _playerStat.ApplyStatChanges(oPC, null);
                ApplyWeaponPenalties(oPC, oItem);
                ApplyEquipmentPenalties(oPC, oItem);
            }
        }

        public void OnModuleItemUnequipped()
        {
            using (new Profiler("SkillService::OnModuleItemUnequipped()"))
            {
                NWPlayer oPC = _.GetPCItemLastUnequippedBy();
                NWItem oItem = _.GetPCItemLastUnequipped();
                HandleGlovesUnequipEvent();
                _playerStat.ApplyStatChanges(oPC, oItem);
                RemoveWeaponPenalties(oItem);
                RemoveEquipmentPenalties(oItem);
            }
        }

        public float CalculateRegisteredSkillLevelAdjustedXP(float xp, int registeredLevel, int skillRank)
        {
            int delta = registeredLevel - skillRank;
            float levelAdjustment = 0.14f * delta;

            if (levelAdjustment > 0.0f) levelAdjustment = 0.0f;
            if (levelAdjustment < -1.0f) levelAdjustment = -1.0f;

            xp = xp + (xp * levelAdjustment);
            return xp;
        }

        private void ForceEquipFistGlove(NWPlayer oPC)
        {
            _.DelayCommand(1.0f, () =>
            {
                if (!oPC.Arms.IsValid)
                {
                    oPC.ClearAllActions();
                    NWItem glove = (_.CreateItemOnObject("fist", oPC.Object));
                    glove.SetLocalInt("UNBREAKABLE", 1);

                    oPC.AssignCommand(() => _.ActionEquipItem(glove.Object, INVENTORY_SLOT_ARMS));
                }
            });
        }

        private void RemovePlayerFromRegistrations(NWPlayer oPC)
        {
            foreach (CreatureSkillRegistration reg in AppCache.CreatureSkillRegistrations.Values.ToArray())
            {
                reg.RemovePlayerRegistration(oPC);

                if (reg.IsRegistrationEmpty())
                {
                    AppCache.CreatureSkillRegistrations.Remove(reg.CreatureID);
                }
                else
                {
                    AppCache.CreatureSkillRegistrations[reg.CreatureID] = reg;
                }
            }
        }

        private void HandleGlovesUnequipEvent()
        {
            NWPlayer oPC = (_.GetPCItemLastUnequippedBy());
            NWItem oItem = (_.GetPCItemLastUnequipped());
            int type = oItem.BaseItemType;

            if (!oPC.IsPlayer) return;
            if (type != BASE_ITEM_BRACER && type != BASE_ITEM_GLOVES) return;

            // If fist was unequipped, destroy it.
            if (oItem.Resref == "fist")
            {
                oItem.Destroy();
            }

            // Remove any other fists in the PC's inventory.
            foreach (var fist in oPC.InventoryItems.Where(x => x.Resref == "fist"))
            {
                fist.Destroy();
            }

            // Check in 1 second to see if PC has a glove equipped. If they don't, create a fist glove and equip it.
            ForceEquipFistGlove(oPC);
        }


        private int CalculateTotalSkillPointsPenalty(int totalSkillPoints, int xp)
        {
            if (totalSkillPoints >= 450)
            {
                xp = (int)(xp * 0.70f);
            }
            else if (totalSkillPoints >= 400)
            {
                xp = (int)(xp * 0.80f);
            }
            else if (totalSkillPoints >= 350)
            {
                xp = (int)(xp * 0.85f);
            }
            else if (totalSkillPoints >= 300)
            {
                xp = (int)(xp * 0.90f);
            }
            else if (totalSkillPoints >= 250)
            {
                xp = (int)(xp * 0.95f);
            }

            return xp;
        }

        private bool ApplySkillDecay(NWPlayer oPC, PCSkill levelingSkill, int xp)
        {
            int totalSkillRanks = GetPCTotalSkillCount(oPC);
            if (totalSkillRanks < SkillCap) return true;

            // Find out if we have enough XP to remove. If we don't, make no changes and return false signifying no XP could be removed.
            var pcSkills = DataService.Where<PCSkill>(x => x.PlayerID == oPC.GlobalID && x.SkillID != levelingSkill.SkillID);
            var totalXPs = pcSkills.Select(s =>
            {
                var reqXP = DataService.Where<SkillXPRequirement>(x => x.SkillID == s.SkillID && (x.Rank < s.Rank || x.Rank == 0 && s.XP > 0));
                var totalXP = reqXP.Sum(x => x.XP);
                return new { s.SkillID, TotalSkillXP = totalXP };
            }).ToList();

            int aggregateXP = 0;
            foreach (var p in totalXPs)
            {
                aggregateXP += p.TotalSkillXP;
            }
            if (aggregateXP < xp) return false;

            // We have enough XP to remove. Reduce XP, picking random skills each time we reduce.
            var skillsPossibleToDecay = GetAllPCSkills(oPC)
                .Where(x =>
                {
                    var skill = DataService.Get<Skill>(x.SkillID);
                    return !x.IsLocked &&
                           skill.ContributesToSkillCap &&
                           x.SkillID != levelingSkill.SkillID &&
                           (x.XP > 0 || x.Rank > 0);
                }).ToList();

            // There's an edge case where players can be at the cap, but we're unable to find a skill to decay.
            // In this scenario we can't go any further. Return false which will cause the GiveSkillXP method to 
            // bail out with no changes to XP or decayed skills.
            if (skillsPossibleToDecay.Count <= 0)
                return false;

            while (xp > 0)
            {
                int skillIndex = RandomService.Random(skillsPossibleToDecay.Count);
                PCSkill decaySkill = skillsPossibleToDecay[skillIndex];
                int totalDecaySkillXP = totalXPs.Find(x => x.SkillID == decaySkill.SkillID).TotalSkillXP;
                int oldRank = decaySkill.Rank;

                if (totalDecaySkillXP >= xp)
                {
                    totalDecaySkillXP = totalDecaySkillXP - xp;
                    xp = 0;
                }
                else if (totalDecaySkillXP < xp)
                {
                    totalDecaySkillXP = 0;
                    xp = xp - totalDecaySkillXP;
                }

                // If skill drops to 0 total XP, remove it from the possible list of skills
                if (totalDecaySkillXP <= 0)
                {
                    skillsPossibleToDecay.Remove(decaySkill);
                    decaySkill.XP = 0;
                    decaySkill.Rank = 0;
                }
                // Otherwise calculate what rank and XP value the skill should now be.
                else
                {
                    // Get the XP amounts required per level, in ascending order, so we can see how many levels we're now meant to have. 
                    List<SkillXPRequirement> reqs = DataService.Where<SkillXPRequirement>(x => x.SkillID == decaySkill.SkillID && x.Rank <= decaySkill.Rank).OrderBy(o => o.Rank).ToList();


                    // The first entry in the database is for rank 0, and if passed, will raise us to 1.  So start our count at 0.
                    int newDecaySkillRank = 0;
                    foreach (SkillXPRequirement req in reqs)
                    {
                        if (totalDecaySkillXP >= req.XP)
                        {
                            totalDecaySkillXP = totalDecaySkillXP - req.XP;
                            newDecaySkillRank++;
                        }
                        else if (totalDecaySkillXP < req.XP)
                        {
                            break;
                        }
                    }

                    decaySkill.Rank = newDecaySkillRank;
                    decaySkill.XP = totalDecaySkillXP;
                }

                PCSkill dbDecaySkill = new PCSkill
                {
                    SkillID = decaySkill.SkillID,
                    IsLocked = decaySkill.IsLocked,
                    ID = decaySkill.ID,
                    PlayerID = decaySkill.PlayerID,
                    Rank = decaySkill.Rank,
                    XP = decaySkill.XP
                };
                DataService.SubmitDataChange(dbDecaySkill, DatabaseActionType.Update);
                MessageHub.Instance.Publish(new SkillDecayedMessage(oPC, decaySkill.SkillID, oldRank, decaySkill.Rank));
            }

            _playerStat.ApplyStatChanges(oPC, null);
            return true;
        }


        private CreatureSkillRegistration GetCreatureSkillRegistration(Guid creatureUUID)
        {
            if (AppCache.CreatureSkillRegistrations.ContainsKey(creatureUUID))
            {
                return AppCache.CreatureSkillRegistrations[creatureUUID];
            }
            else
            {
                var reg = new CreatureSkillRegistration(creatureUUID);
                AppCache.CreatureSkillRegistrations[creatureUUID] = reg;
                return reg;
            }
        }


        public void OnHitCastSpell(NWPlayer oPC)
        {
            if (!oPC.IsPlayer) return;
            NWItem oSpellOrigin = (_.GetSpellCastItem());
            NWCreature oTarget = (_.GetSpellTargetObject());

            SkillType skillType = _item.GetSkillTypeForItem(oSpellOrigin);

            if (skillType == SkillType.Unknown ||
                skillType == SkillType.LightArmor ||
                skillType == SkillType.HeavyArmor ||
                skillType == SkillType.ForceArmor ||
                skillType == SkillType.Shields) return;
            if (oTarget.IsPlayer || oTarget.IsDM) return;
            if (oTarget.ObjectType != OBJECT_TYPE_CREATURE) return;

            int skillID = (int)skillType;
            CreatureSkillRegistration reg = GetCreatureSkillRegistration(oTarget.GlobalID);
            int rank = GetPCSkillRank(oPC, skillID);
            reg.AddSkillRegistrationPoint(oPC, skillID, oSpellOrigin.RecommendedLevel, rank);

            // Add a registration point if a shield is equipped. This is to prevent players from swapping out a weapon for a shield
            // just before they kill an enemy.
            NWItem oShield = oPC.LeftHand;
            if (oShield.BaseItemType == BASE_ITEM_SMALLSHIELD ||
                oShield.BaseItemType == BASE_ITEM_LARGESHIELD ||
                oShield.BaseItemType == BASE_ITEM_TOWERSHIELD)
            {
                rank = GetPCSkillRank(oPC, SkillType.Shields);
                reg.AddSkillRegistrationPoint(oPC, (int)SkillType.Shields, oShield.RecommendedLevel, rank);
            }
        }

        public void RegisterPCToNPCForSkill(NWPlayer pc, NWObject npc, SkillType skill)
        {
            RegisterPCToNPCForSkill(pc, npc, (int)skill);
        }

        private void RegisterPCToNPCForSkill(NWPlayer pc, NWObject npc, int skillID)
        {
            if (!pc.IsPlayer || !pc.IsValid) return;
            if (npc.IsPlayer || npc.IsDM || !npc.IsValid) return;
            if (skillID <= 0) return;

            int rank = GetPCSkillRank(pc, skillID);

            CreatureSkillRegistration reg = GetCreatureSkillRegistration(npc.GlobalID);
            reg.AddSkillRegistrationPoint(pc, skillID, rank, rank);
        }

        private void ApplyWeaponPenalties(NWPlayer oPC, NWItem oItem)
        {
            SkillType skillType = _item.GetSkillTypeForItem(oItem);

            if (skillType == SkillType.Unknown ||
                skillType == SkillType.HeavyArmor ||
                skillType == SkillType.LightArmor ||
                skillType == SkillType.ForceArmor ||
                skillType == SkillType.Shields) return;

            int skillID = (int)skillType;
            int rank = GetPCSkillRank(oPC, skillID);
            int recommendedRank = oItem.RecommendedLevel;
            if (rank >= recommendedRank) return;

            int delta = rank - recommendedRank;
            int penalty;

            if (delta <= -20)
            {
                penalty = 99;
            }
            else if (delta <= -16)
            {
                penalty = 5;
            }
            else if (delta <= -12)
            {
                penalty = 4;
            }
            else if (delta <= -8)
            {
                penalty = 3;
            }
            else if (delta <= -4)
            {
                penalty = 2;
            }
            else if (delta <= 0)
            {
                penalty = 1;
            }
            else penalty = 99;

            // No combat damage penalty
            if (penalty == 99)
            {
                ItemProperty noDamage = _.ItemPropertyNoDamage();
                noDamage = _.TagItemProperty(noDamage, IPWeaponPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, noDamage, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                penalty = 5; // Reset to 5 so that the following penalties apply.
            }

            // Decreased attack penalty
            ItemProperty ipPenalty = _.ItemPropertyAttackPenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            BiowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            // Decreased damage penalty
            ipPenalty = _.ItemPropertyDamagePenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            BiowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            // Decreased enhancement bonus penalty
            ipPenalty = _.ItemPropertyEnhancementPenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            BiowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            oPC.SendMessage("A penalty has been applied to your weapon '" + oItem.Name + "' due to your skill being under the recommended level.");
        }

        private void RemoveWeaponPenalties(NWItem oItem)
        {
            SkillType skillType = _item.GetSkillTypeForItem(oItem);
            if (skillType == SkillType.Unknown ||
                skillType == SkillType.HeavyArmor ||
                skillType == SkillType.LightArmor ||
                skillType == SkillType.ForceArmor ||
                skillType == SkillType.Shields) return;

            foreach (ItemProperty ip in oItem.ItemProperties)
            {
                string tag = _.GetItemPropertyTag(ip);
                if (tag == IPWeaponPenaltyTag)
                {
                    _.RemoveItemProperty(oItem.Object, ip);
                }
            }
        }

        private void ApplyEquipmentPenalties(NWPlayer oPC, NWItem oItem)
        {
            SkillType skill = _item.GetSkillTypeForItem(oItem);
            if (skill == SkillType.Unknown) return;

            int rank = GetPCSkillRank(oPC, skill);
            int delta = oItem.RecommendedLevel - rank;
            if (delta <= 0) return;

            int str = 0;
            int dex = 0;
            int con = 0;
            int wis = 0;
            int @int = 0;
            int cha = 0;
            int ab = 0;
            int eb = 0;

            foreach (var ip in oItem.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                int value = _.GetItemPropertyCostTableValue(ip);
                if (type == ITEM_PROPERTY_ABILITY_BONUS)
                {
                    int abilityType = _.GetItemPropertySubType(ip);
                    switch (abilityType)
                    {
                        case ABILITY_STRENGTH: str += value; break;
                        case ABILITY_CONSTITUTION: con += value; break;
                        case ABILITY_DEXTERITY: dex += value; break;
                        case ABILITY_WISDOM: wis += value; break;
                        case ABILITY_INTELLIGENCE: @int += value; break;
                        case ABILITY_CHARISMA: cha += value; break;
                    }
                }
                else if (type == ITEM_PROPERTY_DECREASED_ABILITY_SCORE)
                {
                    int abilityType = _.GetItemPropertySubType(ip);
                    switch (abilityType)
                    {
                        case ABILITY_STRENGTH: str -= value; break;
                        case ABILITY_CONSTITUTION: con -= value; break;
                        case ABILITY_DEXTERITY: dex -= value; break;
                        case ABILITY_WISDOM: wis -= value; break;
                        case ABILITY_INTELLIGENCE: @int -= value; break;
                        case ABILITY_CHARISMA: cha -= value; break;
                    }

                }
                else if (type == ITEM_PROPERTY_ATTACK_BONUS)
                {
                    ab += value;
                }
                else if (type == ITEM_PROPERTY_DECREASED_ATTACK_MODIFIER)
                {
                    ab -= value;
                }
                else if (type == ITEM_PROPERTY_ENHANCEMENT_BONUS)
                {
                    eb += value;
                }
                else if (type == ITEM_PROPERTY_DECREASED_ENHANCEMENT_MODIFIER)
                {
                    eb -= value;
                }
            }

            // Apply penalties only if total value is greater than 0. Penalties don't scale.
            if (str > 0)
            {
                int newStr = 1 + delta / 5;
                if (newStr > str) newStr = str;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_STRENGTH, newStr);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (dex > 0)
            {
                int newDex = 1 + delta / 5;
                if (newDex > dex) newDex = dex;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_DEXTERITY, newDex);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (con > 0)
            {
                int newCon = 1 + delta / 5;
                if (newCon > con) newCon = con;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CONSTITUTION, newCon);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (@int > 0)
            {
                int newInt = 1 + delta / 5;
                if (newInt > @int) newInt = @int;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_INTELLIGENCE, newInt);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (wis > 0)
            {
                int newWis = 1 + delta / 5;
                if (newWis > wis) newWis = wis;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_WISDOM, newWis);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (cha > 0)
            {
                int newCha = 1 + delta / 5;
                if (newCha > cha) newCha = cha;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CHARISMA, newCha);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (ab > 0)
            {
                int newAB = 1 + delta / 5;
                if (newAB > ab) newAB = ab;

                ItemProperty ip = _.ItemPropertyAttackPenalty(newAB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (eb > 0)
            {
                int newEB = 1 + delta / 5;
                if (newEB > eb) newEB = eb;

                ItemProperty ip = _.ItemPropertyEnhancementPenalty(newEB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }

        }

        private void RemoveEquipmentPenalties(NWItem oItem)
        {
            foreach (var ip in oItem.ItemProperties)
            {
                string tag = _.GetItemPropertyTag(ip);
                if (tag == IPEquipmentPenaltyTag)
                {
                    _.RemoveItemProperty(oItem.Object, ip);
                }
            }
        }

    }
}
