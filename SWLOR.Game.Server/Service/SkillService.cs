using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Data.SqlResults;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Skill;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class SkillService : ISkillService
    {
        private const float PrimaryIncrease = 0.2f;
        private const float SecondaryIncrease = 0.1f;
        private const float TertiaryIncrease = 0.05f;
        private const int MaxAttributeBonus = 70;
        private const string IPWeaponPenaltyTag = "SKILL_PENALTY_WEAPON_ITEM_PROPERTY";
        private const string IPEquipmentPenaltyTag = "SKILL_PENALTY_EQUIPMENT_ITEM_PROPERTY";


        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IEnmityService _enmity;
        private readonly AppState _state;

        public SkillService(IDataContext db,
            INWScript script,
            IRandomService random,
            INWNXCreature nwnxCreature,
            IPerkService perk,
            IBiowareXP2 biowareXP2,
            IEnmityService enmity,
            AppState state)
        {
            _db = db;
            _ = script;
            _random = random;
            _nwnxCreature = nwnxCreature;
            _perk = perk;
            _biowareXP2 = biowareXP2;
            _enmity = enmity;
            _state = state;
        }

        public int SkillCap => 500;

        public void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false)
        {
            if (!player.IsPlayer) return;
            if (!player.IsInitializedAsPlayer) return;

            PlayerCharacter pcEntity = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            List<PCSkill> skills = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID && x.Skill.IsActive).ToList();
            float strBonus = 0.0f;
            float dexBonus = 0.0f;
            float conBonus = 0.0f;
            float intBonus = 0.0f;
            float wisBonus = 0.0f;
            float chaBonus = 0.0f;

            foreach (PCSkill pcSkill in skills)
            {
                Skill skill = pcSkill.Skill;
                CustomAttribute primary = (CustomAttribute)skill.Primary;
                CustomAttribute secondary = (CustomAttribute)skill.Secondary;
                CustomAttribute tertiary = (CustomAttribute)skill.Tertiary;

                // Primary Bonuses
                if (primary == CustomAttribute.STR) strBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.DEX) dexBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.CON) conBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.INT) intBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.WIS) wisBonus += PrimaryIncrease * pcSkill.Rank;
                else if (primary == CustomAttribute.CHA) chaBonus += PrimaryIncrease * pcSkill.Rank;

                // Secondary Bonuses
                if (secondary == CustomAttribute.STR) strBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.DEX) dexBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.CON) conBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.INT) intBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.WIS) wisBonus += SecondaryIncrease * pcSkill.Rank;
                else if (secondary == CustomAttribute.CHA) chaBonus += SecondaryIncrease * pcSkill.Rank;

                // Tertiary Bonuses
                if (tertiary == CustomAttribute.STR) strBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.DEX) dexBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.CON) conBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.INT) intBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.WIS) wisBonus += TertiaryIncrease * pcSkill.Rank;
                else if (tertiary == CustomAttribute.CHA) chaBonus += TertiaryIncrease * pcSkill.Rank;
            }

            // Check caps.
            if (strBonus > MaxAttributeBonus) strBonus = MaxAttributeBonus;
            if (dexBonus > MaxAttributeBonus) dexBonus = MaxAttributeBonus;
            if (conBonus > MaxAttributeBonus) conBonus = MaxAttributeBonus;
            if (intBonus > MaxAttributeBonus) intBonus = MaxAttributeBonus;
            if (wisBonus > MaxAttributeBonus) wisBonus = MaxAttributeBonus;
            if (chaBonus > MaxAttributeBonus) chaBonus = MaxAttributeBonus;
            
            // Apply attributes
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_STRENGTH, (int)strBonus + pcEntity.STRBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_DEXTERITY, (int)dexBonus + pcEntity.DEXBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CONSTITUTION, (int)conBonus + pcEntity.CONBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_INTELLIGENCE, (int)intBonus + pcEntity.INTBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_WISDOM, (int)wisBonus + pcEntity.WISBase);
            _nwnxCreature.SetRawAbilityScore(player, ABILITY_CHARISMA, (int)chaBonus + pcEntity.CHABase);

            // Apply AC
            int ac = player.CalculateEffectiveArmorClass(ignoreItem);
            _nwnxCreature.SetBaseAC(player, ac);

            // Apply BAB
            int bab = CalculateBAB(player, ignoreItem);
            _nwnxCreature.SetBaseAttackBonus(player, bab);


            int equippedItemHPBonus = 0;
            int equippedItemFPBonus = 0;

            for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
            {
                NWItem item = NWItem.Wrap(_.GetItemInSlot(slot, player.Object));
                if (item.Equals(ignoreItem)) continue;

                equippedItemHPBonus += item.HPBonus;
                equippedItemFPBonus += item.FPBonus;
            }

            // Apply HP
            int hp = 30 + player.ConstitutionModifier * 5;
            hp += _perk.GetPCPerkLevel(player, PerkType.Health) * 5;
            hp += equippedItemHPBonus;

            if (hp > 255) hp = 255;
            if (hp < 20) hp = 20;
            _nwnxCreature.SetMaxHitPointsByLevel(player, 1, hp);
            if (player.CurrentHP > player.MaxHP)
            {
                int amount = player.CurrentHP - player.MaxHP;
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, player.Object);
            }

            // Apply FP
            int fp = 20;
            fp += (player.IntelligenceModifier + player.WisdomModifier + player.CharismaModifier) * 5;
            fp += _perk.GetPCPerkLevel(player, PerkType.FP) * 5;
            fp += equippedItemFPBonus;

            if (fp < 0) fp = 0;
            pcEntity.MaxFP = fp;

            if (isInitialization)
                pcEntity.CurrentFP = pcEntity.MaxFP;

            _db.SaveChanges();
        }

        public void RegisterPCToAllCombatTargetsForSkill(NWPlayer player, SkillType skillType)
        {
            int skillID = (int)skillType;
            if (!player.IsPlayer) return;
            if (skillID <= 0) return;

            List<NWPlayer> members = player.GetPartyMembers();

            int nth = 1;
            NWCreature creature = NWCreature.Wrap(_.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, player.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0));
            while (creature.IsValid)
            {
                if (_.GetDistanceBetween(player.Object, creature.Object) > 20.0f) break;

                // Check NPC's enmity table 
                EnmityTable enmityTable = _enmity.GetEnmityTable(creature);
                foreach (var member in members)
                {
                    if (enmityTable.ContainsKey(member.GlobalID))
                    {
                        RegisterPCToNPCForSkill(player, creature, skillID);
                        break;
                    }
                }

                nth++;
                creature = NWCreature.Wrap(_.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, player.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0));
            }
        }

        public void GiveSkillXP(NWPlayer oPC, SkillType skill, int xp)
        {
            GiveSkillXP(oPC, (int)skill, xp);
        }

        public void GiveSkillXP(NWPlayer oPC, int skillID, int xp)
        {
            if (skillID <= 0 || xp <= 0 || !oPC.IsPlayer) return;

            xp = (int)(xp + xp * oPC.ResidencyBonus);
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

                if (player.TotalSPAcquired < SkillCap)
                {
                    player.UnallocatedSP++;
                    player.TotalSPAcquired++;
                }

                skill.Rank++;
                oPC.FloatingText("Your " + skill.Skill.Name + " skill level increased!");
                req = _db.SkillXPRequirements.Single(x => x.SkillID == skillID && x.Rank == skill.Rank);

                // Reapply skill penalties on a skill level up.
                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(slot, oPC.Object));
                    RemoveWeaponPenalties(item);
                    ApplyWeaponPenalties(oPC, NWItem.Wrap(new Object()));
                    RemoveEquipmentPenalties(item);
                    ApplyEquipmentPenalties(oPC, NWItem.Wrap(new Object()));
                }
            }

            _db.SaveChanges();

            // Update player and apply stat changes only if a level up occurred.
            if (originalRank != skill.Rank)
            {
                ApplyStatChanges(oPC, null);
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
            return _db.PCSkills.Where(x => x.PlayerID == playerID).Sum(x => x.Rank);
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
                
                // Grant XP based on points acquired during combat.
                foreach (Tuple<int, PlayerSkillPointTracker> skreg in skillRegs)
                {
                    int skillID = skreg.Item1;
                    int skillRank = GetPCSkillByID(preg.Player.GlobalID, skillID).Rank;

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
                    NWItem item = NWItem.Wrap(_.GetItemInSlot(slot, preg.Player.Object));
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
            NWPlayer oPC = NWPlayer.Wrap(_.GetExitingObject());
            RemovePlayerFromRegistrations(oPC);
        }

        public void OnModuleEnter()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetEnteringObject());
            if (oPC.IsPlayer)
            {
                _db.StoredProcedure("InsertAllPCSkillsByID",
                    new SqlParameter("PlayerID", oPC.GlobalID));
                ForceEquipFistGlove(oPC);
            }
        }

        public void OnModuleClientLeave()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetExitingObject());
            RemovePlayerFromRegistrations(oPC);
        }

        public void OnModuleItemEquipped()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetPCItemLastEquippedBy());
            if (!oPC.IsInitializedAsPlayer) return; // Players who log in for the first time don't have an ID yet.

            NWItem oItem = NWItem.Wrap(_.GetPCItemLastEquipped());
            ApplyStatChanges(oPC, null);
            ApplyWeaponPenalties(oPC, oItem);
            ApplyEquipmentPenalties(oPC, oItem);
        }

        public void OnModuleItemUnequipped()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetPCItemLastUnequippedBy());
            NWItem oItem = NWItem.Wrap(_.GetPCItemLastUnequipped());
            HandleGlovesUnequipEvent();
            ApplyStatChanges(oPC, oItem);
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
            oPC.DelayCommand(() =>
            {
                if (!oPC.Arms.IsValid)
                {
                    oPC.ClearAllActions();
                    NWItem glove = NWItem.Wrap(_.CreateItemOnObject("fist", oPC.Object));
                    glove.SetLocalInt("UNBREAKABLE", 1);

                    oPC.AssignCommand(() => _.ActionEquipItem(glove.Object, INVENTORY_SLOT_ARMS));
                }
            }, 1.0f);
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
            NWPlayer oPC = NWPlayer.Wrap(_.GetPCItemLastUnequippedBy());
            NWItem oItem = NWItem.Wrap(_.GetPCItemLastUnequipped());
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

            ApplyStatChanges(oPC, null);
            return true;
        }

        public SkillType GetSkillTypeForItem(NWItem item)
        {
            SkillType skillType = SkillType.Unknown;
            int type = item.BaseItemType;
            int[] oneHandedTypes = 
            {
                BASE_ITEM_BASTARDSWORD,
                BASE_ITEM_BATTLEAXE,
                BASE_ITEM_CLUB,
                BASE_ITEM_DAGGER,
                BASE_ITEM_HANDAXE,
                BASE_ITEM_KAMA,
                BASE_ITEM_KATANA,
                BASE_ITEM_KUKRI,
                BASE_ITEM_LIGHTFLAIL,
                BASE_ITEM_LIGHTHAMMER,
                BASE_ITEM_LIGHTMACE,
                BASE_ITEM_LONGSWORD,
                BASE_ITEM_RAPIER,
                BASE_ITEM_SCIMITAR,
                BASE_ITEM_SHORTSPEAR,
                BASE_ITEM_SHORTSWORD,
                BASE_ITEM_SICKLE,
                BASE_ITEM_WHIP,
                CustomBaseItemType.Lightsaber
            };

            int[] twoHandedTypes = 
            {
                BASE_ITEM_DIREMACE,
                BASE_ITEM_DWARVENWARAXE,
                BASE_ITEM_GREATAXE,
                BASE_ITEM_GREATSWORD,
                BASE_ITEM_HALBERD,
                BASE_ITEM_HEAVYFLAIL,
                BASE_ITEM_MORNINGSTAR,
                BASE_ITEM_QUARTERSTAFF,
                BASE_ITEM_SCYTHE,
                BASE_ITEM_TRIDENT,
                BASE_ITEM_WARHAMMER
            };

            int[] twinBladeTypes = 
            {
                BASE_ITEM_DOUBLEAXE,
                BASE_ITEM_TWOBLADEDSWORD,
                CustomBaseItemType.Saberstaff
            };

            int[] martialArtsTypes = 
            {
                BASE_ITEM_BRACER,
                BASE_ITEM_GLOVES
            };

            int[] firearmTypes = 
            {
                BASE_ITEM_HEAVYCROSSBOW,
                BASE_ITEM_LIGHTCROSSBOW,
                BASE_ITEM_LONGBOW,
                BASE_ITEM_SHORTBOW,
                BASE_ITEM_ARROW,
                BASE_ITEM_BOLT
            };

            int[] throwingTypes =
            {
                BASE_ITEM_GRENADE,
                BASE_ITEM_SHURIKEN,
                BASE_ITEM_SLING,
                BASE_ITEM_THROWINGAXE,
                BASE_ITEM_BULLET,
                BASE_ITEM_DART
            };

            int[] shieldTypes =
            {
                BASE_ITEM_SMALLSHIELD,
                BASE_ITEM_LARGESHIELD,
                BASE_ITEM_TOWERSHIELD
            };

            if (oneHandedTypes.Contains(type)) skillType = SkillType.OneHanded;
            else if (twoHandedTypes.Contains(type)) skillType = SkillType.TwoHanded;
            else if (twinBladeTypes.Contains(type)) skillType = SkillType.TwinBlades;
            else if (martialArtsTypes.Contains(type)) skillType = SkillType.MartialArts;
            else if (firearmTypes.Contains(type)) skillType = SkillType.Firearms;
            else if (throwingTypes.Contains(type)) skillType = SkillType.Throwing;
            else if (item.CustomItemType == CustomItemType.HeavyArmor) skillType = SkillType.HeavyArmor;
            else if (item.CustomItemType == CustomItemType.LightArmor) skillType = SkillType.LightArmor;
            else if (item.CustomItemType == CustomItemType.ForceArmor) skillType = SkillType.ForceArmor;
            else if (shieldTypes.Contains(type)) skillType = SkillType.Shields;

            return skillType;
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
            NWItem oSpellOrigin = NWItem.Wrap(_.GetSpellCastItem());
            NWCreature oTarget = NWCreature.Wrap(_.GetSpellTargetObject());

            SkillType skillType = GetSkillTypeForItem(oSpellOrigin);

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

        public void RegisterPCToNPCForSkill(NWPlayer pc, NWCreature npc, int skillID)
        {
            if (!pc.IsPlayer || !pc.IsValid) return;
            if (npc.IsPlayer || npc.IsDM || !npc.IsValid) return;
            if (skillID <= 0) return;

            PCSkill pcSkill = GetPCSkill(pc, skillID);
            if (pcSkill == null) return;

            CreatureSkillRegistration reg = GetCreatureSkillRegistration(npc.GlobalID);
            reg.AddSkillRegistrationPoint(pc, skillID, pcSkill.Rank, pcSkill.Rank);
        }

        public void RegisterPCToNPCForSkill(NWPlayer pc, NWCreature npc, SkillType skillType)
        {
            RegisterPCToNPCForSkill(pc, npc, (int)skillType);
        }

        public void RegisterPCToAllCombatTargetsForSkill(NWPlayer pc, int skillID)
        {
            if (!pc.IsPlayer || skillID <= 0) return;

            List<NWCreature> members = new List<NWCreature>();

            Object member = _.GetFirstFactionMember(pc.Object);
            while (_.GetIsObjectValid(member) == TRUE)
            {
                members.Add(NWCreature.Wrap(member));
                member = _.GetNextFactionMember(pc.Object);
            }

            int nth = 1;
            NWCreature creature = NWCreature.Wrap(_.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, pc.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0));
            while (creature.IsValid)
            {
                if (_.GetDistanceBetween(pc.Object, creature.Object) > 20.0f) break;

                NWCreature target = NWCreature.Wrap(_.GetAttackTarget(creature.Object));
                if (target.IsValid && members.Contains(target))
                {
                    if (target.IsValid && target.Area.Equals(pc.Area))
                    {
                        RegisterPCToNPCForSkill(pc, creature, skillID);
                    }
                }

                nth++;
                creature = NWCreature.Wrap(_.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, 1, pc.Object, nth, CREATURE_TYPE_PLAYER_CHAR, 0));
            }
        }

        private int CalculateBAB(NWPlayer oPC, NWItem ignoreItem)
        {
            NWItem weapon = oPC.RightHand;

            // The unequip event fires before the item is actually unequipped, so we need
            // to have additional checks to make sure we're not getting the weapon that's about to be
            // unequipped.
            if (weapon.Equals(ignoreItem))
            {
                weapon = null;
                NWItem offHand = oPC.LeftHand;

                if (offHand.CustomItemType == CustomItemType.Vibroblade ||
                   offHand.CustomItemType == CustomItemType.FinesseVibroblade ||
                   offHand.CustomItemType == CustomItemType.Baton ||
                   offHand.CustomItemType == CustomItemType.HeavyVibroblade ||
                   offHand.CustomItemType == CustomItemType.Saberstaff ||
                   offHand.CustomItemType == CustomItemType.Polearm ||
                   offHand.CustomItemType == CustomItemType.TwinBlade ||
                   offHand.CustomItemType == CustomItemType.MartialArtWeapon ||
                   offHand.CustomItemType == CustomItemType.BlasterPistol ||
                   offHand.CustomItemType == CustomItemType.BlasterRifle ||
                   offHand.CustomItemType == CustomItemType.Throwing)
                {
                    weapon = offHand;
                }
            }

            if (weapon == null || !weapon.IsValid)
            {
                weapon = oPC.Arms;
            }
            if (!weapon.IsValid) return 0;

            SkillType itemSkill = GetSkillTypeForItem(weapon);
            if (itemSkill == SkillType.Unknown ||
                itemSkill == SkillType.LightArmor ||
                itemSkill == SkillType.HeavyArmor ||
                itemSkill == SkillType.ForceArmor ||
                itemSkill == SkillType.Shields) return 0;

            int weaponSkillID = (int)itemSkill;
            PCSkill skill = GetPCSkill(oPC, weaponSkillID);
            if (skill == null) return 0;
            int skillBAB = skill.Rank / 10;
            int perkBAB = 0;
            int backgroundBAB = 0;
            BackgroundType background = (BackgroundType) oPC.Class1;
            bool receivesBackgroundBonus = false;

            // Apply increased BAB if player is using a weapon for which they have a proficiency.
            PerkType proficiencyPerk = PerkType.Unknown;
            SkillType proficiencySkill = SkillType.Unknown;
            switch (weapon.CustomItemType)
            {
                case CustomItemType.Vibroblade:
                    proficiencyPerk = PerkType.VibrobladeProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    break;
                case CustomItemType.FinesseVibroblade:
                    proficiencyPerk = PerkType.FinesseVibrobladeProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    receivesBackgroundBonus = background == BackgroundType.Duelist;
                    break;
                case CustomItemType.Baton:
                    proficiencyPerk = PerkType.BatonProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    receivesBackgroundBonus = background == BackgroundType.SecurityOfficer;
                    break;
                case CustomItemType.HeavyVibroblade:
                    proficiencyPerk = PerkType.HeavyVibrobladeProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    receivesBackgroundBonus = background == BackgroundType.Soldier;
                    break;
                case CustomItemType.Saberstaff:
                    proficiencyPerk = PerkType.SaberstaffProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    receivesBackgroundBonus = background == BackgroundType.Sentinel || background == BackgroundType.Assassin;
                    break;
                case CustomItemType.Polearm:
                    proficiencyPerk = PerkType.PolearmProficiency;
                    proficiencySkill = SkillType.TwoHanded;
                    break;
                case CustomItemType.TwinBlade:
                    proficiencyPerk = PerkType.TwinVibrobladeProficiency;
                    proficiencySkill = SkillType.TwinBlades;
                    receivesBackgroundBonus = background == BackgroundType.Berserker;
                    break;
                case CustomItemType.MartialArtWeapon:
                    proficiencyPerk = PerkType.MartialArtsProficiency;
                    proficiencySkill = SkillType.MartialArts;
                    receivesBackgroundBonus = background == BackgroundType.TerasKasi;
                    break;
                case CustomItemType.BlasterPistol:
                    proficiencyPerk = PerkType.BlasterPistolProficiency;
                    proficiencySkill = SkillType.Firearms;
                    receivesBackgroundBonus = background == BackgroundType.Smuggler;
                    break;
                case CustomItemType.BlasterRifle:
                    proficiencyPerk = PerkType.BlasterRifleProficiency;
                    proficiencySkill = SkillType.Firearms;
                    receivesBackgroundBonus = background == BackgroundType.Sharpshooter;
                    break;
                case CustomItemType.Throwing:
                    proficiencyPerk = PerkType.ThrowingProficiency;
                    proficiencySkill = SkillType.Throwing;
                    break;
                case CustomItemType.Lightsaber:
                    proficiencyPerk = PerkType.LightsaberProficiency;
                    proficiencySkill = SkillType.OneHanded;
                    receivesBackgroundBonus = background == BackgroundType.Guardian || background == BackgroundType.Warrior;
                    break;
            }

            if (proficiencyPerk != PerkType.Unknown &&
                proficiencySkill != SkillType.Unknown)
            {
                perkBAB += _perk.GetPCPerkLevel(oPC, proficiencyPerk);
            }

            if (receivesBackgroundBonus)
            {
                backgroundBAB = 2;
            }

            int equipmentBAB = 0;
            for (int x = 0; x < NUM_INVENTORY_SLOTS; x++)
            {
                NWItem equipped = NWItem.Wrap(_.GetItemInSlot(x, oPC.Object));

                int itemLevel = equipped.RecommendedLevel;
                SkillType equippedSkill = GetSkillTypeForItem(equipped);
                int rank = GetPCSkill(oPC, equippedSkill).Rank;
                int delta = itemLevel - rank; // -20
                int itemBAB = equipped.BaseAttackBonus;

                if (delta >= 1) itemBAB--;
                if(delta > 0) itemBAB = itemBAB - delta / 5;

                if (itemBAB <= 0) itemBAB = 0;

                equipmentBAB += itemBAB;
            }

            return 1 + skillBAB + perkBAB + equipmentBAB + backgroundBAB; // Note: Always add 1 to BAB. 0 will cause a crash in NWNX.
        }

        private void ApplyWeaponPenalties(NWPlayer oPC, NWItem oItem)
        {
            SkillType skillType = GetSkillTypeForItem(oItem);

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
            SkillType skillType = GetSkillTypeForItem(oItem);
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
            SkillType skill = GetSkillTypeForItem(oItem);
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
