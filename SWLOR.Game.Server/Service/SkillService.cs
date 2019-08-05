using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Skill;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.Module;
using static NWN._;


namespace SWLOR.Game.Server.Service
{
    public static class SkillService
    {
        private const string IPWeaponPenaltyTag = "SKILL_PENALTY_WEAPON_ITEM_PROPERTY";
        private const string IPEquipmentPenaltyTag = "SKILL_PENALTY_EQUIPMENT_ITEM_PROPERTY";

        public static int SkillCap => 500;

        public static void SubscribeEvents()
        {
            // Area Events
            MessageHub.Instance.Subscribe<OnAreaExit>(message => OnAreaExit());

            // Creature Events
            MessageHub.Instance.Subscribe<OnCreatureDeath>(message => OnCreatureDeath());

            // Feat Events
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());

            // Module Events
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(message => OnModuleUnequipItem());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
        }

        private static Dictionary<int, int> _skillXPRequirements;

        public static Dictionary<int, int> SkillXPRequirements
        {
            get
            {
                if (_skillXPRequirements == null)
                {
                    _skillXPRequirements = new Dictionary<int, int>
                    {
                        { 0, 550 },
                        { 1, 825 },
                        { 2, 1100 },
                        { 3, 1375 },
                        { 4, 1650 },
                        { 5, 1925 },
                        { 6, 2200 },
                        { 7, 2420 },
                        { 8, 2640 },
                        { 9, 2860 },
                        { 10, 3080 },
                        { 11, 4200 },
                        { 12, 4480 },
                        { 13, 4760 },
                        { 14, 5040 },
                        { 15, 5320 },
                        { 16, 5600 },
                        { 17, 5880 },
                        { 18, 6160 },
                        { 19, 6440 },
                        { 20, 6720 },
                        { 21, 8500 },
                        { 22, 8670 },
                        { 23, 8840 },
                        { 24, 9010 },
                        { 25, 9180 },
                        { 26, 9350 },
                        { 27, 9520 },
                        { 28, 9690 },
                        { 29, 9860 },
                        { 30, 10030 },
                        { 31, 10200 },
                        { 32, 10370 },
                        { 33, 10540 },
                        { 34, 10710 },
                        { 35, 10880 },
                        { 36, 11050 },
                        { 37, 11220 },
                        { 38, 11390 },
                        { 39, 11560 },
                        { 40, 11730 },
                        { 41, 14000 },
                        { 42, 14200 },
                        { 43, 14400 },
                        { 44, 14600 },
                        { 45, 14800 },
                        { 46, 15000 },
                        { 47, 15200 },
                        { 48, 15400 },
                        { 49, 16000 },
                        { 50, 18400 },
                        { 51, 24960 },
                        { 52, 27840 },
                        { 53, 30720 },
                        { 54, 33600 },
                        { 55, 36480 },
                        { 56, 39360 },
                        { 57, 42240 },
                        { 58, 45120 },
                        { 59, 48000 },
                        { 60, 51600 },
                        { 61, 55200 },
                        { 62, 58800 },
                        { 63, 62400 },
                        { 64, 66000 },
                        { 65, 69600 },
                        { 66, 73200 },
                        { 67, 76800 },
                        { 68, 81600 },
                        { 69, 86400 },
                        { 70, 91200 },
                        { 71, 108000 },
                        { 72, 113400 },
                        { 73, 118800 },
                        { 74, 120150 },
                        { 75, 121500 },
                        { 76, 122850 },
                        { 77, 124200 },
                        { 78, 125550 },
                        { 79, 126900 },
                        { 80, 128250 },
                        { 81, 144000 },
                        { 82, 145500 },
                        { 83, 147000 },
                        { 84, 148500 },
                        { 85, 150000 },
                        { 86, 151500 },
                        { 87, 153000 },
                        { 88, 154500 },
                        { 89, 156000 },
                        { 90, 159000 },
                        { 91, 216000 },
                        { 92, 220000 },
                        { 93, 224000 },
                        { 94, 228000 },
                        { 95, 232000 },
                        { 96, 236000 },
                        { 97, 240000 },
                        { 98, 260000 },
                        { 99, 280000 },
                        { 100, 400000 }
                    };
                }

                return _skillXPRequirements;
            }
        }

        public static void RegisterPCToAllCombatTargetsForSkill(NWPlayer player, SkillType skillType, NWCreature target)
        {
            int skillID = (int)skillType;
            if (!player.IsPlayer) return;
            if (skillID <= 0) return;

            List<NWCreature> members = player.PartyMembers.ToList();

            int nth = 1;
            NWCreature creature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, player.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0);
            while (creature.IsValid)
            {
                if (_.GetDistanceBetween(player.Object, creature.Object) > 20.0f) break;

                // Check NPC's enmity table 
                EnmityTable enmityTable = EnmityService.GetEnmityTable(creature);
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

        /// <summary>
        /// Gives XP towards a specific player's skill. XP bonuses granted by residency and DM bonuses can be enabled or disabled.
        /// Penalties can also be enabled or disabled.
        /// </summary>
        /// <param name="oPC">The player character receiving the XP</param>
        /// <param name="skill">The type of the skill we're granting XP to.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="enableResidencyBonus">If enabled, a player's primary residence will be factored into the XP gain.</param>
        /// <param name="enableTotalSkillPointPenalty">If enabled, penalties will be applied to the XP if the player falls into the ranges of the skill penalties.</param>
        /// <param name="enableDMBonus">If enabled, bonuses granted by DMs will be applied.</param>
        public static void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp, bool enableResidencyBonus = true, bool enableTotalSkillPointPenalty = true, bool enableDMBonus = true)
        {
            GiveSkillXP(oPC, (int)skill, xp, enableResidencyBonus, enableTotalSkillPointPenalty);
        }

        /// <summary>
        /// Gives XP towards a specific player's skill. XP bonuses granted by residency and DM bonuses can be enabled or disabled.
        /// Penalties can also be enabled or disabled.
        /// </summary>
        /// <param name="oPC">The player character receiving the XP</param>
        /// <param name="skillID">The ID of the skill we're granting XP to.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="enableResidencyBonus">If enabled, a player's primary residence will be factored into the XP gain.</param>
        /// <param name="enableTotalSkillPointPenalty">If enabled, penalties will be applied to the XP if the player falls into the ranges of the skill penalties.</param>
        /// <param name="enableDMBonus">If enabled, bonuses granted by DMs will be applied.</param>
        public static void GiveSkillXP(NWPlayer oPC, int skillID, int xp, bool enableResidencyBonus = true, bool enableTotalSkillPointPenalty = true, bool enableDMBonus = true)
        {
            if (skillID <= 0 || xp <= 0 || !oPC.IsPlayer) return;

            if (enableResidencyBonus)
            {
                xp = (int)(xp + xp * PlayerStatService.EffectiveResidencyBonus(oPC));
            }
            Player player = DataService.Player.GetByID(oPC.GlobalID);
            Skill skill = GetSkill(skillID);

            // Check if the player has any undistributed skill ranks for this skill category.
            // If they haven't been distributed yet, the player CANNOT gain XP for this skill.
            var pool = DataService.PCSkillPool.GetByPlayerIDAndSkillCategoryIDOrDefault(oPC.GlobalID, skill.SkillCategoryID);
            if (pool != null && pool.Levels > 0)
            {
                oPC.FloatingText("You must distribute all pooled skill ranks before you can gain any new XP in the '" + skill.Name + "' skill. Access this menu from the 'View Skills' section of your rest menu.");
                return;
            }

            PCSkill pcSkill = GetPCSkill(oPC, skillID);
            int req = SkillXPRequirements[pcSkill.Rank];
            int maxRank = skill.MaxRank;
            int originalRank = pcSkill.Rank;
            float xpBonusModifier = player.XPBonus * 0.01f;

            // Guard against XP bonuses being too high.
            if (xpBonusModifier > 0.25)
                xpBonusModifier = 0.25f;

            // An XP penalty will be applied depending on how many skill points a player has earned so far.
            // This can be disabled with the enableTotalSkillPointPenalty flag.
            if (enableTotalSkillPointPenalty)
            {
                xp = CalculateTotalSkillPointsPenalty(player.TotalSPAcquired, xp);
            }

            // Characters can receive permanent XP bonuses from DMs. If this skill XP distribution
            // shouldn't grant that bonus, it can be disabled with the enableDMBonus flag.
            if (enableDMBonus)
            {
                xp = xp + (int)(xp * xpBonusModifier);
            }

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
            if (pcSkill.Rank >= maxRank && pcSkill.XP > req)
            {
                pcSkill.XP = req - 1;
            }

            while (pcSkill.XP >= req)
            {
                pcSkill.XP = pcSkill.XP - req;

                if (player.TotalSPAcquired < SkillCap && skill.ContributesToSkillCap)
                {
                    player.UnallocatedSP++;
                    player.TotalSPAcquired++;
                }

                pcSkill.Rank++;
                oPC.FloatingText("Your " + skill.Name + " skill level increased to rank " + pcSkill.Rank + "!");
                req = SkillXPRequirements[pcSkill.Rank];

                // Reapply skill penalties on a skill level up.
                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = _.GetItemInSlot(slot, oPC.Object);
                    RemoveWeaponPenalties(item);
                    ApplyWeaponPenalties(oPC, item);
                    RemoveEquipmentPenalties(item);
                    ApplyEquipmentPenalties(oPC, item);
                }

                if (pcSkill.Rank >= maxRank && pcSkill.XP > req)
                {
                    pcSkill.XP = req - 1;
                }

                DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
                DataService.SubmitDataChange(player, DatabaseActionType.Update);
                MessageHub.Instance.Publish(new OnSkillGained(oPC, skillID));

                pcSkill = GetPCSkill(oPC, skillID);
                player = DataService.Player.GetByID(oPC.GlobalID);
            }

            DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
            DataService.SubmitDataChange(player, DatabaseActionType.Update);

            // Update player and apply stat changes only if a level up occurred.
            if (originalRank != pcSkill.Rank)
            {
                PlayerStatService.ApplyStatChanges(oPC, null);
            }
        }

        public static int GetPCSkillRank(NWPlayer player, SkillType skill)
        {
            if (!player.IsPlayer || skill == SkillType.Unknown) return 0;

            return DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, (int)skill).Rank;
        }

        public static int GetPCSkillRank(NWPlayer player, int skillID)
        {
            return GetPCSkillRank(player, (SkillType)skillID);
        }

        public static PCSkill GetPCSkill(NWPlayer player, int skillID)
        {
            return DataService.PCSkill.GetByPlayerIDAndSkillID(player.GlobalID, skillID);
        }

        public static List<PCSkill> GetAllPCSkills(NWPlayer player)
        {
            return DataService.PCSkill.GetAllByPlayerID(player.GlobalID).ToList();
        }

        public static Skill GetSkill(int skillID)
        {
            return GetSkill((SkillType)skillID);
        }

        public static Skill GetSkill(SkillType skillType)
        {
            return DataService.Skill.GetByID((int)skillType);
        }

        public static int GetPCTotalSkillCount(NWPlayer player)
        {
            var skills = DataService
                .Skill.GetAllWhereContributesToSkillCap()
                .Select(s => s.ID);
            var pcSkills = GetAllPCSkills(player)
                .Where(x => skills.Contains(x.SkillID));
            return pcSkills.Sum(x => x.Rank);
        }

        public static List<SkillCategory> GetActiveCategories()
        {
            return DataService.SkillCategory.GetAllActive().ToList();
        }

        public static List<PCSkill> GetPCSkillsForCategory(Guid playerID, int skillCategoryID)
        {
            // Get list of skills part of this category.
            var skillIDs = DataService
                .Skill.GetAllBySkillCategoryIDAndActive(skillCategoryID)
                .Select(s => s.ID);

            // Get all PC Skills with a matching category.
            var pcSkills = DataService.PCSkill.GetAllByPlayerIDAndSkillIDs(playerID, skillIDs).ToList();

            return pcSkills;
        }

        public static void ToggleSkillLock(Guid playerID, int skillID)
        {
            PCSkill pcSkill = DataService.PCSkill.GetByPlayerIDAndSkillID(playerID, skillID);
            pcSkill.IsLocked = !pcSkill.IsLocked;

            DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
        }

        private static void OnCreatureDeath()
        {
            NWCreature creature = NWGameObject.OBJECT_SELF;
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

        private static float CalculatePartyLevelDifferencePenalty(int highestSkillRank, int skillRank)
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

        private static void OnAreaExit()
        {
            NWPlayer oPC = _.GetExitingObject();
            RemovePlayerFromRegistrations(oPC);
        }

        public static void OnModuleEnter()
        {
            NWPlayer oPC = _.GetEnteringObject();
            if (oPC.IsPlayer)
            {
                // Add any missing skills the player does not have.
                var skills = DataService.Skill.GetAll().Where(x =>
                {
                    var pcSkill = DataService.PCSkill.GetByPlayerIDAndSkillIDOrDefault(oPC.GlobalID, x.ID);
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

        private static void OnModuleLeave()
        {
            NWPlayer oPC = _.GetExitingObject();
            if (!oPC.IsPlayer) return;

            RemovePlayerFromRegistrations(oPC);
        }

        private static void OnModuleEquipItem()
        {
            NWPlayer oPC = _.GetPCItemLastEquippedBy();

            if (oPC.GetLocalInt("IS_CUSTOMIZING_ITEM") == _.TRUE) return; // Don't run heavy code when customizing equipment.
            if (!oPC.IsInitializedAsPlayer) return; // Players who log in for the first time don't have an ID yet.
            if (oPC.GetLocalInt("LOGGED_IN_ONCE") <= 0) return; // Don't fire heavy calculations if this is the player's first log in after a restart.

            NWItem oItem = _.GetPCItemLastEquipped();
            PlayerStatService.ApplyStatChanges(oPC, null);
            ApplyWeaponPenalties(oPC, oItem);
            ApplyEquipmentPenalties(oPC, oItem);

        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer oPC = _.GetPCItemLastUnequippedBy();
            if (oPC.GetLocalInt("IS_CUSTOMIZING_ITEM") == _.TRUE) return; // Don't run heavy code when customizing equipment.

            NWItem oItem = _.GetPCItemLastUnequipped();
            HandleGlovesUnequipEvent();
            PlayerStatService.ApplyStatChanges(oPC, oItem);
            RemoveWeaponPenalties(oItem);
            RemoveEquipmentPenalties(oItem);
        }

        public static float CalculateRegisteredSkillLevelAdjustedXP(float xp, int registeredLevel, int skillRank)
        {
            int delta = registeredLevel - skillRank;
            float levelAdjustment = 0.14f * delta;

            if (levelAdjustment > 0.0f) levelAdjustment = 0.0f;
            if (levelAdjustment < -1.0f) levelAdjustment = -1.0f;

            xp = xp + (xp * levelAdjustment);
            return xp;
        }

        private static void ForceEquipFistGlove(NWPlayer oPC)
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

        private static void RemovePlayerFromRegistrations(NWPlayer oPC)
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

        private static void HandleGlovesUnequipEvent()
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


        private static int CalculateTotalSkillPointsPenalty(int totalSkillPoints, int xp)
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

        private static bool ApplySkillDecay(NWPlayer oPC, PCSkill levelingSkill, int xp)
        {
            int totalSkillRanks = GetPCTotalSkillCount(oPC);
            if (totalSkillRanks < SkillCap) return true;

            // Find out if we have enough XP to remove. If we don't, make no changes and return false signifying no XP could be removed.
            var pcSkills = DataService.PCSkill.GetAllByPlayerID(oPC.GlobalID).Where(x => x.SkillID != levelingSkill.SkillID);
            var totalXPs = pcSkills.Select(s =>
            {
                var reqXP = SkillXPRequirements.Where(x => (x.Key < s.Rank || x.Key == 0 && s.XP > 0));
                var totalXP = reqXP.Sum(x => x.Value);
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
                    var skill = DataService.Skill.GetByID(x.SkillID);
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
                    var reqs = SkillXPRequirements.Where(x => x.Key <= decaySkill.Rank).OrderBy(o => o.Key).ToList();


                    // The first entry in the database is for rank 0, and if passed, will raise us to 1.  So start our count at 0.
                    int newDecaySkillRank = 0;
                    foreach (var req in reqs)
                    {
                        if (totalDecaySkillXP >= req.Value)
                        {
                            totalDecaySkillXP = totalDecaySkillXP - req.Value;
                            newDecaySkillRank++;
                        }
                        else if (totalDecaySkillXP < req.Value)
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
                MessageHub.Instance.Publish(new OnSkillDecayed(oPC, decaySkill.SkillID, oldRank, decaySkill.Rank));
            }

            PlayerStatService.ApplyStatChanges(oPC, null);
            return true;
        }


        private static CreatureSkillRegistration GetCreatureSkillRegistration(Guid creatureUUID)
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


        private static void OnHitCastSpell()
        {
            NWPlayer oPC = NWGameObject.OBJECT_SELF;
            if (!oPC.IsValid || !oPC.IsPlayer) return;
            NWItem oSpellOrigin = (_.GetSpellCastItem());
            NWCreature oTarget = (_.GetSpellTargetObject());

            SkillType skillType = ItemService.GetSkillTypeForItem(oSpellOrigin);

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

        public static void RegisterPCToNPCForSkill(NWPlayer pc, NWObject npc, SkillType skill)
        {
            RegisterPCToNPCForSkill(pc, npc, (int)skill);
        }

        private static void RegisterPCToNPCForSkill(NWPlayer pc, NWObject npc, int skillID)
        {
            if (!pc.IsPlayer || !pc.IsValid) return;
            if (npc.IsPlayer || npc.IsDM || !npc.IsValid) return;
            if (skillID <= 0) return;

            int rank = GetPCSkillRank(pc, skillID);

            CreatureSkillRegistration reg = GetCreatureSkillRegistration(npc.GlobalID);
            reg.AddSkillRegistrationPoint(pc, skillID, rank, rank);
        }

        private static void ApplyWeaponPenalties(NWPlayer oPC, NWItem oItem)
        {
            SkillType skillType = ItemService.GetSkillTypeForItem(oItem);

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

        private static void RemoveWeaponPenalties(NWItem oItem)
        {
            SkillType skillType = ItemService.GetSkillTypeForItem(oItem);
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

        private static Dictionary<int, StoredItemPropertyDetail> BuildImmunityItemPropertiesContainer()
        {
            return new Dictionary<int, StoredItemPropertyDetail>
            {
                {IP_CONST_DAMAGETYPE_ACID, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_ACID")},
                {(int) CustomItemPropertyDamageType.Ballistic, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_BALLISTIC")},
                {IP_CONST_DAMAGETYPE_BLUDGEONING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_BLUDGEONING")},
                {(int) CustomItemPropertyDamageType.Bullet, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_BULLET")},
                {IP_CONST_DAMAGETYPE_COLD, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_COLD")},
                {IP_CONST_DAMAGETYPE_DIVINE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_DIVINE")},
                {IP_CONST_DAMAGETYPE_ELECTRICAL, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_ELECTRICAL")},
                {(int) CustomItemPropertyDamageType.Energy, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_ENERGY")},
                {IP_CONST_DAMAGETYPE_FIRE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_FIRE")},
                {IP_CONST_DAMAGETYPE_MAGICAL, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_MAGICAL")},
                {IP_CONST_DAMAGETYPE_NEGATIVE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_NEGATIVE")},
                {IP_CONST_DAMAGETYPE_PIERCING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_PIERCING")},
                {IP_CONST_DAMAGETYPE_POSITIVE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_POSITIVE")},
                {IP_CONST_DAMAGETYPE_SLASHING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_SLASHING")},
                {IP_CONST_DAMAGETYPE_SONIC, new StoredItemPropertyDetail("PENALTY_ORIGINAL_IMMUNITY_SONIC")},
            };
        }

        private static Dictionary<int, StoredItemPropertyDetail> BuildDamageResistanceItemPropertiesContainer()
        {
            return new Dictionary<int, StoredItemPropertyDetail>
            {
                { IP_CONST_DAMAGETYPE_ACID, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_ACID")},
                { (int)CustomItemPropertyDamageType.Ballistic, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_BALLISTIC")},
                { IP_CONST_DAMAGETYPE_BLUDGEONING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_BLUDGEONING")},
                { (int)CustomItemPropertyDamageType.Bullet, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_BULLET")},
                { IP_CONST_DAMAGETYPE_COLD, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_COLD")},
                { IP_CONST_DAMAGETYPE_DIVINE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_DIVINE")},
                { IP_CONST_DAMAGETYPE_ELECTRICAL, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_ELECTRICAL")},
                { (int)CustomItemPropertyDamageType.Energy, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_ENERGY")},
                { IP_CONST_DAMAGETYPE_FIRE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_FIRE")},
                { IP_CONST_DAMAGETYPE_MAGICAL, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_MAGICAL")},
                { IP_CONST_DAMAGETYPE_NEGATIVE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_NEGATIVE")},
                { IP_CONST_DAMAGETYPE_PIERCING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_PIERCING")},
                { IP_CONST_DAMAGETYPE_POSITIVE, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_POSITIVE")},
                { IP_CONST_DAMAGETYPE_SLASHING, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_SLASHING")},
                { IP_CONST_DAMAGETYPE_SONIC, new StoredItemPropertyDetail("PENALTY_ORIGINAL_RESISTANCE_SONIC")},
            };

        }


        /// <summary>
        /// Adjusts stats on an item if the player's skill rank is lower than the recommended level on the item.
        /// These penalties should be removed with the RemoveEquipmentPenalties method.
        /// </summary>
        /// <param name="player">The player whose skill rank we're checking</param>
        /// <param name="item">The item whose stats will be adjusted.</param>
        private static void ApplyEquipmentPenalties(NWPlayer player, NWItem item)
        {
            // Identify whether this item has a skill type. If it doesn't, exit early.
            SkillType skill = ItemService.GetSkillTypeForItem(item);
            if (skill == SkillType.Unknown) return;

            // Determine the delta between player's skill and the item's recommended level.
            int rank = GetPCSkillRank(player, skill);
            int delta = item.RecommendedLevel - rank;

            // Player meets or exceeds recommended level. Exit early.
            if (delta <= 0) return;

            List<ItemProperty> ipsToApply = new List<ItemProperty>();

            // Attributes
            int str = 0, dex = 0, con = 0, wis = 0, @int = 0, cha = 0;
            // Attack Bonus / Enhancement Bonus
            int ab = 0, eb = 0;
            var immunities = BuildImmunityItemPropertiesContainer();
            var resistances = BuildDamageResistanceItemPropertiesContainer();

            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                int subType = _.GetItemPropertySubType(ip);
                int value = _.GetItemPropertyCostTableValue(ip);

                if (type == ITEM_PROPERTY_ABILITY_BONUS)
                {
                    switch (subType)
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
                    switch (subType)
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
                else if (type == ITEM_PROPERTY_IMMUNITY_DAMAGE_TYPE)
                {
                    var immunity = immunities[subType];
                    immunity.Amount += value;

                    // Mark the original value as a local variable on the item.
                    item.SetLocalInt(immunity.VariableName, immunity.Amount);

                    // Calculate the new value (minimum of 1).
                    int newImmunity = 1 + (immunity.Amount - delta * 5);
                    if (newImmunity < 1) newImmunity = 1;
                    
                    if (newImmunity > immunity.Amount) newImmunity = immunity.Amount;

                    // We have the amount but we need to find the corresponding ID in the 2DA.
                    // Check our cached 2DA data for this value.
                    int costTableID = Cached2DAService.ImmunityCosts.Single(x => x.Value == newImmunity).Key;

                    // Unpack the IP and adjust its value.
                    var unpacked = NWNXItemProperty.UnpackIP(ip);
                    unpacked.CostTableValue = costTableID;

                    // Add it to the list for later application. We don't want to do this right now, for fear of an infinite loop.
                    var packed = NWNXItemProperty.PackIP(unpacked);
                    ipsToApply.Add(packed);

                    // Remove this version of the item property.
                    RemoveItemProperty(item, ip);
                }
                else if (type == ITEM_PROPERTY_DAMAGE_RESISTANCE)
                {
                    // Damage Resistance is an all-or-nothing property.
                    // If player's skill doesn't meet minimum, we strip it entirely.
                    var resistance = resistances[subType];
                    resistance.Amount += value;

                    // Mark the original value as a local variable on the item.
                    item.SetLocalInt(resistance.VariableName, resistance.Amount);

                    // Remove the item property.
                    RemoveItemProperty(item, ip);
                }
                else if (type == ITEM_PROPERTY_DAMAGE_REDUCTION)
                {
                    item.SetLocalInt("PENALTY_ORIGINAL_DR_PLUS_ID", subType);
                    item.SetLocalInt("PENALTY_ORIGINAL_DR_AMOUNT_ID", value);

                    // +1's ID is 0 so we don't need to offset by 1 here.
                    int newPlus = subType - (delta / 3);
                    if (newPlus < 0) newPlus = 0;

                    // Reduce soak amount.
                    int newDR = value - (delta / 5);
                    if (newDR < 1) newDR = 1;

                    // Add the modified item property to the list for later application.
                    ItemProperty newIP = _.ItemPropertyDamageReduction(newPlus, newDR);
                    ipsToApply.Add(newIP);

                    RemoveItemProperty(item, ip);
                }
            }

            // Apply penalties only if total value is greater than 0. Penalties don't scale.

            // Ability scores, AB, and EB receive an additional item property which reduces stats.
            // The original property is left unaffected.
            if (str > 0)
            {
                int newStr = 1 + delta / 5;
                if (newStr > str) newStr = str;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_STRENGTH, newStr);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (dex > 0)
            {
                int newDex = 1 + delta / 5;
                if (newDex > dex) newDex = dex;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_DEXTERITY, newDex);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (con > 0)
            {
                int newCon = 1 + delta / 5;
                if (newCon > con) newCon = con;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CONSTITUTION, newCon);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (@int > 0)
            {
                int newInt = 1 + delta / 5;
                if (newInt > @int) newInt = @int;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_INTELLIGENCE, newInt);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (wis > 0)
            {
                int newWis = 1 + delta / 5;
                if (newWis > wis) newWis = wis;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_WISDOM, newWis);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (cha > 0)
            {
                int newCha = 1 + delta / 5;
                if (newCha > cha) newCha = cha;

                ItemProperty ip = _.ItemPropertyDecreaseAbility(ABILITY_CHARISMA, newCha);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (ab > 0)
            {
                int newAB = 1 + delta / 5;
                if (newAB > ab) newAB = ab;

                ItemProperty ip = _.ItemPropertyAttackPenalty(newAB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }
            if (eb > 0)
            {
                int newEB = 1 + delta / 5;
                if (newEB > eb) newEB = eb;

                ItemProperty ip = _.ItemPropertyEnhancementPenalty(newEB);
                ip = _.TagItemProperty(ip, IPEquipmentPenaltyTag);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }

            // Apply all item properties that are waiting.
            foreach (var ip in ipsToApply)
            {
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }

        /// <summary>
        /// Removes temporary item properties which have been tagged as penalties.
        /// </summary>
        /// <param name="item">The item to remove penalties from.</param>
        private static void RemoveEquipmentPenalties(NWItem item)
        {
            var ipsToApply = new List<ItemProperty>();
            var immunities = BuildImmunityItemPropertiesContainer();
            var resistances = BuildDamageResistanceItemPropertiesContainer();

            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                // Remove any temporary item properties with a matching penalty tag.
                string tag = _.GetItemPropertyTag(ip);
                if (tag == IPEquipmentPenaltyTag)
                {
                    _.RemoveItemProperty(item, ip);
                }
                // Immunity properties get their value set back to original.
                else if (type == ITEM_PROPERTY_IMMUNITY_DAMAGE_TYPE)
                {
                    // Take the existing IP, modify it, then put it in the list for later addition to the item.
                    // We can't directly modify the item property on the item, so we use this as a workaround.
                    int subType = _.GetItemPropertySubType(ip);
                    string varName = immunities[subType].VariableName;
                    int costTableID = item.GetLocalInt(varName);

                    if (costTableID > 0)
                    {
                        // Unpack the IP, modify the value back to original, then add it to the list to be applied later.
                        // Remove this version of the IP.
                        var unpacked = NWNXItemProperty.UnpackIP(ip);
                        unpacked.CostTableValue = costTableID;
                        var packed = NWNXItemProperty.PackIP(unpacked);
                        ipsToApply.Add(packed);

                        _.RemoveItemProperty(item, ip);

                        item.DeleteLocalInt(varName);
                    }
                }
                else if (type == ITEM_PROPERTY_DAMAGE_REDUCTION)
                {
                    int plusID = item.GetLocalInt("PENALTY_ORIGINAL_DR_PLUS_ID");
                    int amountID = item.GetLocalInt("PENALTY_ORIGINAL_DR_AMOUNT_ID");
                    if(plusID > 0 && amountID > 0)
                    {
                        ItemProperty newIP = ItemPropertyDamageReduction(plusID, amountID);
                        ipsToApply.Add(newIP);

                        _.RemoveItemProperty(item, ip);

                        item.DeleteLocalInt("PENALTY_ORIGINAL_DR_PLUS_ID");
                        item.DeleteLocalInt("PENALTY_ORIGINAL_DR_AMOUNT_ID");
                    }
                }
            }

            // Re-add resistance item properties to the item.
            foreach (var resistance in resistances)
            {
                string varName = resistance.Value.VariableName;
                int costTableID = item.GetLocalInt(varName);
                if (costTableID > 0)
                {
                    ItemProperty ip = ItemPropertyDamageResistance(resistance.Key, costTableID);
                    ipsToApply.Add(ip);
                }

                item.DeleteLocalInt(varName);
            }

            // Reapply the item properties with the original values now.
            foreach (var ip in ipsToApply)
            {
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }

    }
}
