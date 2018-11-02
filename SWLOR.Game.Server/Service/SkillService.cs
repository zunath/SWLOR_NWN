using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Skill;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class SkillService : ISkillService
    {
        private const string IPWeaponPenaltyTag = "SKILL_PENALTY_WEAPON_ITEM_PROPERTY";
        private const string IPEquipmentPenaltyTag = "SKILL_PENALTY_EQUIPMENT_ITEM_PROPERTY";


        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IEnmityService _enmity;
        private readonly IPlayerStatService _playerStat;
        private readonly IItemService _item;
        private readonly AppState _state;

        public SkillService(IDataContext db,
            INWScript script,
            IRandomService random,
            IBiowareXP2 biowareXP2,
            IEnmityService enmity,
            IPlayerStatService playerStat,
            IItemService item,
            AppState state)
        {
            _db = db;
            _ = script;
            _random = random;
            _biowareXP2 = biowareXP2;
            _enmity = enmity;
            _playerStat = playerStat;
            _item = item;
            _state = state;
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

        public void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp)
        {
            GiveSkillXP(oPC, (int)skill, xp);
        }

        public void GiveSkillXP(NWPlayer oPC, int skillID, int xp)
        {
            if (skillID <= 0 || xp <= 0 || !oPC.IsPlayer) return;

            xp = (int)(xp + xp * _playerStat.EffectiveResidencyBonus(oPC));
            PlayerCharacter player = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
            PCSkill skill = GetPCSkillByID(oPC.GlobalID, skillID);
            SkillXPRequirement req = _db.SkillXPRequirements.Single(x => x.SkillID == skillID && x.Rank == skill.Rank);
            int maxRank = _db.SkillXPRequirements.Where(x => x.SkillID == skillID).Max(m => m.Rank);
            int originalRank = skill.Rank;
            xp = CalculateTotalSkillPointsPenalty(player.TotalSPAcquired, xp);

            // Run the skill decay rules.
            // If the method returns false, that means all skills are locked.
            // So we can't give the player any XP.
            if (!ApplySkillDecay(oPC, skill, xp))
            {
                return;
            }

            skill.XP = skill.XP + xp;
            oPC.SendMessage("You earned " + skill.Skill.Name + " skill experience. (" + xp + ")");

            // Skill is at cap and player would level up.
            // Reduce XP to required amount minus 1 XP
            if (skill.Rank >= maxRank && skill.XP > req.XP)
            {
                skill.XP = skill.XP - 1;
            }

            while (skill.XP >= req.XP)
            {
                skill.XP = skill.XP - req.XP;

                if (player.TotalSPAcquired < SkillCap && skill.Skill.ContributesToSkillCap)
                {
                    player.UnallocatedSP++;
                    player.TotalSPAcquired++;
                }

                skill.Rank++;
                oPC.FloatingText("Your " + skill.Skill.Name + " skill level increased to rank " + skill.Rank + "!");
                req = _db.SkillXPRequirements.Single(x => x.SkillID == skillID && x.Rank == skill.Rank);

                // Reapply skill penalties on a skill level up.
                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = _.GetItemInSlot(slot, oPC.Object);
                    RemoveWeaponPenalties(item);
                    ApplyWeaponPenalties(oPC, new Object());
                    RemoveEquipmentPenalties(item);
                    ApplyEquipmentPenalties(oPC, new Object());
                }
            }

            _db.SaveChanges();

            // Update player and apply stat changes only if a level up occurred.
            if (originalRank != skill.Rank)
            {
                _playerStat.ApplyStatChanges(oPC, null);
            }


        }

        public PCSkill GetPCSkill(NWPlayer player, SkillType skill)
        {
            return GetPCSkill(player, (int)skill);
        }

        public PCSkill GetPCSkill(NWPlayer player, int skillID)
        {
            if (!player.IsPlayer) return null;
            return _db.PCSkills.Single(x => x.PlayerID == player.GlobalID && x.SkillID == skillID);
        }

        public int GetPCTotalSkillCount(string playerID)
        {
            return _db.PCSkills.Where(x => x.PlayerID == playerID && x.Skill.ContributesToSkillCap).Sum(x => x.Rank);
        }

        public PCSkill GetPCSkillByID(string playerID, int skillID)
        {
            return _db.PCSkills.Single(x => x.PlayerID == playerID && x.SkillID == skillID);
        }

        public List<SkillCategory> GetActiveCategories()
        {
            return _db.SkillCategories.Where(x => x.IsActive).ToList();
        }

        public List<PCSkill> GetPCSkillsForCategory(string playerID, int skillCategoryID)
        {
            return _db.PCSkills.Where(x => x.Skill.IsActive && x.Skill.SkillCategoryID == skillCategoryID && x.PlayerID == playerID).ToList();
        }

        public SkillXPRequirement GetSkillXPRequirementByRank(int skillID, int rank)
        {
            return _db.SkillXPRequirements.Single(x => x.SkillID == skillID && x.Rank == rank);
        }

        public void ToggleSkillLock(string playerID, int skillID)
        {
            PCSkill pcSkill = GetPCSkillByID(playerID, skillID);
            pcSkill.IsLocked = !pcSkill.IsLocked;

            _db.SaveChanges();
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

            if (delta >= 6) baseXP = 500;
            else if (delta == 5) baseXP = 450;
            else if (delta == 4) baseXP = 425;
            else if (delta == 3) baseXP = 400;
            else if (delta == 2) baseXP = 350;
            else if (delta == 1) baseXP = 325;
            else if (delta == 0) baseXP = 300;
            else if (delta == -1) baseXP = 250;
            else if (delta == -2) baseXP = 200;
            else if (delta == -3) baseXP = 150;
            else if (delta == -4) baseXP = 100;
            else if (delta == -5) baseXP = 90;
            else if (delta == -6) baseXP = 70;

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
                        _.GetDistanceBetween(preg.Player.Object, creature.Object) > 30.0f)
                    continue;

                List<Tuple<int, PlayerSkillPointTracker>> skillRegs = preg.GetSkillRegistrationPoints();
                int totalPoints = preg.GetTotalSkillRegistrationPoints();
                bool receivesMartialArtsPenalty = CheckForMartialArtsPenalty(skillRegs);

                // Retrieve all necessary PC skills up front
                int[] skillIDsToSearchFor = skillRegs.Select(x => x.Item2.SkillID).ToArray();
                var pcSkills = _db
                    .PCSkills
                    .AsNoTracking()
                    .Where(x => x.PlayerID == preg.Player.GlobalID && 
                                skillIDsToSearchFor.Contains(x.SkillID))
                    .Select(s => new
                    {
                        s.SkillID,
                        s.Rank
                    })
                    .ToList();
                
                // Grant XP based on points acquired during combat.
                foreach (Tuple<int, PlayerSkillPointTracker> skreg in skillRegs)
                {
                    int skillID = skreg.Item1;
                    int skillRank = pcSkills.Single(x => x.SkillID == skillID).Rank;

                    int points = skreg.Item2.Points;
                    int itemLevel = skreg.Item2.RegisteredLevel;
                    if (itemLevel > skillRank) itemLevel = skillRank - 5;
                    if (itemLevel < 0) itemLevel = 0;

                    float percentage = points / (float)totalPoints;
                    float skillLDP = CalculatePartyLevelDifferencePenalty(partyLevel, skillRank);
                    float adjustedXP = baseXP * percentage * skillLDP;
                    adjustedXP = CalculateRegisteredSkillLevelAdjustedXP(adjustedXP, itemLevel, skillRank);

                    // Penalty to martial arts XP for using a shield.
                    if (skillID == (int)SkillType.MartialArts && receivesMartialArtsPenalty)
                        adjustedXP = adjustedXP * 0.4f;

                    GiveSkillXP(preg.Player, skillID, (int)adjustedXP);
                }

                float armorXP = baseXP * 0.20f;
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

                int armorRank = GetPCSkillByID(preg.Player.GlobalID, (int)SkillType.LightArmor).Rank;
                float armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                float percent = lightArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.LightArmor, (int)(armorXP * percent * armorLDP));

                armorRank = GetPCSkillByID(preg.Player.GlobalID, (int)SkillType.HeavyArmor).Rank;
                armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                percent = heavyArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.HeavyArmor, (int)(armorXP * percent * armorLDP));

                armorRank = GetPCSkillByID(preg.Player.GlobalID, (int)SkillType.ForceArmor).Rank;
                armorLDP = CalculatePartyLevelDifferencePenalty(partyLevel, armorRank);
                percent = forceArmorPoints / (float)totalPoints;

                GiveSkillXP(preg.Player, SkillType.ForceArmor, (int)(armorXP * percent * armorLDP));


            }

            _state.CreatureSkillRegistrations.Remove(creature.GlobalID);
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

        private bool CheckForMartialArtsPenalty(List<Tuple<int, PlayerSkillPointTracker>> skillRegs)
        {
            bool usedShield = false;
            bool usedMartialArts = false;
            foreach (Tuple<int, PlayerSkillPointTracker> sreg in skillRegs)
            {
                if (sreg.Item1 == (int)SkillType.Shields) usedShield = true;
                else if (sreg.Item1 == (int)SkillType.MartialArts) usedMartialArts = true;

                if (usedMartialArts && usedShield) return true;
            }

            return false;
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
                _db.StoredProcedure("InsertAllPCSkillsByID",
                    new SqlParameter("PlayerID", oPC.GlobalID));
                ForceEquipFistGlove(oPC);
            }
        }

        public void OnModuleClientLeave()
        {
            NWPlayer oPC = _.GetExitingObject();
            RemovePlayerFromRegistrations(oPC);
        }

        public void OnModuleItemEquipped()
        {
            NWPlayer oPC = _.GetPCItemLastEquippedBy();
            if (!oPC.IsInitializedAsPlayer) return; // Players who log in for the first time don't have an ID yet.

            NWItem oItem = _.GetPCItemLastEquipped();
            _playerStat.ApplyStatChanges(oPC, null);
            ApplyWeaponPenalties(oPC, oItem);
            ApplyEquipmentPenalties(oPC, oItem);
        }

        public void OnModuleItemUnequipped()
        {
            NWPlayer oPC = _.GetPCItemLastUnequippedBy();
            NWItem oItem = _.GetPCItemLastUnequipped();
            HandleGlovesUnequipEvent();
            _playerStat.ApplyStatChanges(oPC, oItem);
            RemoveWeaponPenalties(oItem);
            RemoveEquipmentPenalties(oItem);
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
            foreach (CreatureSkillRegistration reg in _state.CreatureSkillRegistrations.Values.ToArray())
            {
                reg.RemovePlayerRegistration(oPC);

                if (reg.IsRegistrationEmpty())
                {
                    _state.CreatureSkillRegistrations.Remove(reg.CreatureID);
                }
                else
                {
                    _state.CreatureSkillRegistrations[reg.CreatureID] = reg;
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
            int totalSkillRanks = GetPCTotalSkillCount(oPC.GlobalID);
            if (totalSkillRanks < SkillCap) return true;

            // Find out if we have enough XP to remove. If we don't, make no changes and return false signifying no XP could be removed.
            List<TotalSkillXPResult> skillTotalXP = _db.StoredProcedure<TotalSkillXPResult>("GetTotalXPAmountsForPC",
                new SqlParameter("PlayerID", oPC.GlobalID),
                new SqlParameter("SkillID", levelingSkill.SkillID));
            int aggregateXP = 0;
            foreach (TotalSkillXPResult p in skillTotalXP)
            {
                aggregateXP += p.TotalSkillXP;
            }
            if (aggregateXP < xp) return false;

            // We have enough XP to remove. Reduce XP, picking random skills each time we reduce.
            List<PCSkill> skillsPossibleToDecay = _db.PCSkills
                .Where(x => !x.IsLocked &&
                            x.Skill.ContributesToSkillCap &&
                            x.PlayerID == oPC.GlobalID &&
                            x.SkillID != levelingSkill.SkillID &&
                            (x.XP > 0 || x.Rank > 0)).ToList();
            while (xp > 0)
            {
                int skillIndex = _random.Random(skillsPossibleToDecay.Count);
                PCSkill decaySkill = skillsPossibleToDecay[skillIndex];
                int totalDecaySkillXP = skillTotalXP[skillIndex].TotalSkillXP;

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
                    List<SkillXPRequirement> reqs = _db.SkillXPRequirements.Where(x => x.SkillID == decaySkill.SkillID && x.Rank <= decaySkill.Rank).ToList();
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

                _db.SaveChanges();
            }

            _playerStat.ApplyStatChanges(oPC, null);
            return true;
        }


        private CreatureSkillRegistration GetCreatureSkillRegistration(string creatureUUID)
        {
            if (_state.CreatureSkillRegistrations.ContainsKey(creatureUUID))
            {
                return _state.CreatureSkillRegistrations[creatureUUID];
            }
            else
            {
                var reg = new CreatureSkillRegistration(creatureUUID);
                _state.CreatureSkillRegistrations[creatureUUID] = reg;
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
            PCSkill pcSkill = GetPCSkill(oPC, skillID);
            reg.AddSkillRegistrationPoint(oPC, skillID, oSpellOrigin.RecommendedLevel, pcSkill.Rank);

            // Add a registration point if a shield is equipped. This is to prevent players from swapping out a weapon for a shield
            // just before they kill an enemy.
            NWItem oShield = oPC.LeftHand;
            if (oShield.BaseItemType == BASE_ITEM_SMALLSHIELD ||
                oShield.BaseItemType == BASE_ITEM_LARGESHIELD ||
                oShield.BaseItemType == BASE_ITEM_TOWERSHIELD)
            {
                pcSkill = GetPCSkill(oPC, SkillType.Shields);
                reg.AddSkillRegistrationPoint(oPC, (int)SkillType.Shields, oShield.RecommendedLevel, pcSkill.Rank);
            }
        }

        private void RegisterPCToNPCForSkill(NWPlayer pc, NWCreature npc, int skillID)
        {
            if (!pc.IsPlayer || !pc.IsValid) return;
            if (npc.IsPlayer || npc.IsDM || !npc.IsValid) return;
            if (skillID <= 0) return;

            PCSkill pcSkill = GetPCSkill(pc, skillID);
            if (pcSkill == null) return;

            CreatureSkillRegistration reg = GetCreatureSkillRegistration(npc.GlobalID);
            reg.AddSkillRegistrationPoint(pc, skillID, pcSkill.Rank, pcSkill.Rank);
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
            PCSkill pcSkill = GetPCSkill(oPC, skillID);
            if (pcSkill == null) return;
            int rank = pcSkill.Rank;
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
                _biowareXP2.IPSafeAddItemProperty(oItem, noDamage, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                penalty = 5; // Reset to 5 so that the following penalties apply.
            }

            // Decreased attack penalty
            ItemProperty ipPenalty = _.ItemPropertyAttackPenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            _biowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            // Decreased damage penalty
            ipPenalty = _.ItemPropertyDamagePenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            _biowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            // Decreased enhancement bonus penalty
            ipPenalty = _.ItemPropertyEnhancementPenalty(penalty);
            ipPenalty = _.TagItemProperty(ipPenalty, IPWeaponPenaltyTag);
            _biowareXP2.IPSafeAddItemProperty(oItem, ipPenalty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

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
            int rank = GetPCSkill(oPC, skill).Rank;
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
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (dex > 0)
            {
                int newDex = 1 + delta / 5;
                if (newDex > dex) newDex = dex;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_DEXTERITY, newDex);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (con > 0)
            {
                int newCon = 1 + delta / 5;
                if (newCon > con) newCon = con;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CONSTITUTION, newCon);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (@int > 0)
            {
                int newInt = 1 + delta / 5;
                if (newInt > @int) newInt = @int;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_INTELLIGENCE, newInt);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (wis > 0)
            {
                int newWis = 1 + delta / 5;
                if (newWis > wis) newWis = wis;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_WISDOM, newWis);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (cha > 0)
            {
                int newCha = 1 + delta / 5;
                if (newCha > cha) newCha = cha;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CHARISMA, newCha);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (ab > 0)
            {
                int newAB = 1 + delta / 5;
                if (newAB > ab) newAB = ab;

                ItemProperty ip = _.ItemPropertyAttackPenalty(newAB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (eb > 0)
            {
                int newEB = 1 + delta / 5;
                if (newEB > eb) newEB = eb;

                ItemProperty ip = _.ItemPropertyEnhancementPenalty(newEB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                _biowareXP2.IPSafeAddItemProperty(oItem, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
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
