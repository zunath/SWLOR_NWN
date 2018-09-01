using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnDamaged: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly IDataContext _db;
        private readonly IDurabilityService _durability;

        public OnDamaged(
            INWScript script,
            IRandomService random,
            IColorTokenService color,
            IPerkService perk,
            ISkillService skill,
            IDataContext db,
            IDurabilityService durability)
        {
            _ = script;
            _random = random;
            _color = color;
            _perk = perk;
            _skill = skill;
            _db = db;
            _durability = durability;
        }


        public bool Run(params object[] args)
        {
            NWPlaceable resource = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDamager(resource.Object));
            if (oPC.GetLocalInt("NOT_USING_CORRECT_WEAPON") == 1)
            {
                oPC.DeleteLocalInt("NOT_USING_CORRECT_WEAPON");
                return true;
            }

            PlayerCharacter pcEntity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);

            NWItem oWeapon = NWItem.Wrap(_.GetLastWeaponUsed(oPC.Object));
            Location location = oPC.Location;
            string resourceItemResref = resource.GetLocalString("RESOURCE_RESREF");
            int activityID = resource.GetLocalInt("RESOURCE_ACTIVITY");
            string resourceName = resource.GetLocalString("RESOURCE_NAME");
            int resourceCount = resource.GetLocalInt("RESOURCE_COUNT");
            int difficultyRating = resource.GetLocalInt("RESOURCE_DIFFICULTY_RATING");
            int weaponChanceBonus;
            SkillType skillType;
            int perkChanceBonus;
            int secondResourceChance;
            int durabilityChanceReduction = 0;
            int hasteChance;
            int lucky = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            bool hasBaggerPerk;

            if (activityID == 1) // 1 = Logging
            {
                weaponChanceBonus = oWeapon.LoggingBonus;
                if (weaponChanceBonus > 0)
                {
                    weaponChanceBonus += _perk.GetPCPerkLevel(oPC, PerkType.LoggingAxeExpert) * 5;
                    durabilityChanceReduction = _perk.GetPCPerkLevel(oPC, PerkType.LoggingAxeExpert) * 10 + lucky;
                }

                skillType = SkillType.Logging;
                perkChanceBonus = _perk.GetPCPerkLevel(oPC, PerkType.Lumberjack) * 5 + lucky;
                secondResourceChance = _perk.GetPCPerkLevel(oPC, PerkType.PrecisionLogging) * 10;
                hasteChance = _perk.GetPCPerkLevel(oPC, PerkType.SpeedyLogger) * 10 + lucky;

                if (pcEntity.BackgroundID == (int)BackgroundType.Lumberjack)
                {
                    hasteChance += 10;
                }

                hasBaggerPerk = _perk.GetPCPerkLevel(oPC, PerkType.WoodBagger) > 0;
            }
            else if (activityID == 2) // Mining
            {
                weaponChanceBonus = oWeapon.MiningBonus;
                if (weaponChanceBonus > 0)
                {
                    weaponChanceBonus += _perk.GetPCPerkLevel(oPC, PerkType.PickaxeExpert) * 5;
                    durabilityChanceReduction = _perk.GetPCPerkLevel(oPC, PerkType.PickaxeExpert) * 10 + lucky;
                }
                skillType = SkillType.Mining;
                perkChanceBonus = _perk.GetPCPerkLevel(oPC, PerkType.Miner) * 5 + lucky;
                secondResourceChance = _perk.GetPCPerkLevel(oPC, PerkType.PrecisionMining) * 10;
                hasteChance = _perk.GetPCPerkLevel(oPC, PerkType.SpeedyMiner) * 10 + lucky;

                if (pcEntity.BackgroundID == (int)BackgroundType.Miner)
                {
                    hasteChance += 10;
                }

                hasBaggerPerk = _perk.GetPCPerkLevel(oPC, PerkType.OreBagger) > 0;
            }
            else return false;
            PCSkill skill = _skill.GetPCSkillByID(oPC.GlobalID, (int)skillType);
            int durabilityLossChance = 100 - durabilityChanceReduction;
            if (_random.Random(100) <= durabilityLossChance)
            {
                _durability.RunItemDecay(oPC, oWeapon);
            }

            int baseChance = 10;
            int chance = baseChance + weaponChanceBonus;
            chance += CalculateSuccessChanceDeltaModifier(difficultyRating, skill.Rank);
            chance += perkChanceBonus;

            bool givePityItem = false;
            if (chance > 0)
            {
                if (_random.Random(100) + 1 <= hasteChance)
                {
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectHaste(), oPC.Object, 8.0f);
                }

                // Give an item if the player hasn't gotten anything after 6-8 attempts.
                int attemptFailureCount = oPC.GetLocalInt("RESOURCE_ATTEMPT_FAILURE_COUNT") + 1;
                NWObject failureResource = NWObject.Wrap(oPC.GetLocalObject("RESOURCE_ATTEMPT_FAILURE_OBJECT"));

                if (!failureResource.IsValid || !Equals(failureResource, resource))
                {
                    failureResource = resource;
                    attemptFailureCount = 1;
                }

                int pityItemChance = 0;
                if (attemptFailureCount == 6) pityItemChance = 60;
                else if (attemptFailureCount == 7) pityItemChance = 80;
                else if (attemptFailureCount >= 8) pityItemChance = 100;

                if (_random.Random(100) + 1 <= pityItemChance)
                {
                    givePityItem = true;
                    attemptFailureCount = 0;
                }

                oPC.SetLocalInt("RESOURCE_ATTEMPT_FAILURE_COUNT", attemptFailureCount);
                oPC.SetLocalObject("RESOURCE_ATTEMPT_FAILURE_OBJECT", failureResource.Object);
            }

            if (chance <= 0)
            {
                oPC.FloatingText("You do not have enough skill to harvest this resource...");
            }
            else if (_random.Random(100) <= chance || givePityItem)
            {
                if (hasBaggerPerk)
                {
                    _.CreateItemOnObject(resourceItemResref, oPC.Object);
                }
                else
                {
                    _.CreateObject(NWScript.OBJECT_TYPE_ITEM, resourceItemResref, location);
                }


                oPC.FloatingText("You break off some " + resourceName + ".");
                resource.SetLocalInt("RESOURCE_COUNT", --resourceCount);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(10000), resource.Object);

                if (_random.Random(100) + 1 <= secondResourceChance)
                {
                    oPC.FloatingText("You break off a second piece.");

                    if (hasBaggerPerk)
                    {
                        _.CreateItemOnObject(resourceItemResref, oPC.Object);
                    }
                    else
                    {
                        _.CreateObject(NWScript.OBJECT_TYPE_ITEM, resourceItemResref, location);
                    }
                }

                float deltaModifier = CalculateXPDeltaModifier(difficultyRating, skill.Rank);
                float baseXP = (100 + _random.Random(20)) * deltaModifier;
                int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(baseXP, oWeapon.RecommendedLevel, skill.Rank);
                _skill.GiveSkillXP(oPC, skillType, xp);

                oPC.DeleteLocalInt("RESOURCE_ATTEMPT_FAILURE_COUNT");
                oPC.DeleteLocalObject("RESOURCE_ATTEMPT_FAILURE_OBJECT");
            }

            if (resourceCount <= 0)
            {
                SpawnSeed(resource, oPC);

                NWObject prop = NWObject.Wrap(resource.GetLocalObject("RESOURCE_PROP_OBJ"));
                if (prop.IsValid)
                {
                    prop.Destroy();
                }
                resource.Destroy();
            }
            return true;
        }

        private void SpawnSeed(NWObject objSelf, NWPlayer oPC)
        {
            Location location = objSelf.Location;
            string resourceSeedResref = objSelf.GetLocalString("RESOURCE_SEED_RESREF");

            if (!string.IsNullOrWhiteSpace(resourceSeedResref))
            {
                _.CreateObject(NWScript.OBJECT_TYPE_ITEM, resourceSeedResref, location);

                int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.SeedSearcher);
                if (perkLevel <= 0) return;

                if (_random.Random(100) + 1 <= perkLevel * 10)
                {
                    _.CreateObject(NWScript.OBJECT_TYPE_ITEM, resourceSeedResref, location);
                }
            }

        }

        private float CalculateXPDeltaModifier(int difficultyRating, int skillRank)
        {
            int delta = difficultyRating - skillRank;
            float deltaModifier;

            if (delta >= 9) deltaModifier = 1.9f;
            else if (delta >= 8) deltaModifier = 1.8f;
            else if (delta >= 7) deltaModifier = 1.7f;
            else if (delta >= 6) deltaModifier = 1.6f;
            else if (delta >= 5) deltaModifier = 1.5f;
            else if (delta >= 4) deltaModifier = 1.4f;
            else if (delta >= 3) deltaModifier = 1.3f;
            else if (delta >= 2) deltaModifier = 1.2f;
            else if (delta >= 1) deltaModifier = 1.1f;

            else if (delta <= -10) deltaModifier = 0.0f;
            else if (delta <= -9) deltaModifier = 0.1f;
            else if (delta <= -8) deltaModifier = 0.2f;
            else if (delta <= -7) deltaModifier = 0.3f;
            else if (delta <= -6) deltaModifier = 0.4f;
            else if (delta <= -5) deltaModifier = 0.5f;
            else if (delta <= -4) deltaModifier = 0.6f;
            else if (delta <= -3) deltaModifier = 0.7f;
            else if (delta <= -2) deltaModifier = 0.8f;
            else if (delta <= -1) deltaModifier = 0.9f;
            else deltaModifier = 1.0f;

            return deltaModifier;
        }

        private int CalculateSuccessChanceDeltaModifier(int difficultyRating, int skillRank)
        {
            int delta = difficultyRating - skillRank;
            int chanceModifier;

            if (delta >= 10) chanceModifier = -999;
            else if (delta >= 9) chanceModifier = -100;
            else if (delta >= 8) chanceModifier = -90;
            else if (delta >= 7) chanceModifier = -80;
            else if (delta >= 6) chanceModifier = -70;
            else if (delta >= 5) chanceModifier = -60;
            else if (delta >= 4) chanceModifier = -40;
            else if (delta >= 3) chanceModifier = -30;
            else if (delta >= 2) chanceModifier = -10;
            else if (delta >= 1) chanceModifier = -5;

            else if (delta <= -10) chanceModifier = 90;
            else if (delta <= -9) chanceModifier = 80;
            else if (delta <= -8) chanceModifier = 75;
            else if (delta <= -7) chanceModifier = 70;
            else if (delta <= -6) chanceModifier = 60;
            else if (delta <= -5) chanceModifier = 40;
            else if (delta <= -4) chanceModifier = 30;
            else if (delta <= -3) chanceModifier = 20;
            else if (delta <= -2) chanceModifier = 10;
            else if (delta <= -1) chanceModifier = 5;
            else chanceModifier = 0;

            return chanceModifier;
        }
    }
}
