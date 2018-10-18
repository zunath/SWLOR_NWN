using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Placeable.ScavengePoint
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;
        private readonly ILootService _loot;
        private readonly IFarmingService _farming;
        private readonly IPlayerStatService _playerStat;

        public OnOpened(INWScript script,
            ISkillService skill,
            IPerkService perk,
            IRandomService random,
            IColorTokenService color,
            ILootService loot,
            IFarmingService farming,
            IPlayerStatService playerStat)
        {
            _ = script;
            _skill = skill;
            _perk = perk;
            _random = random;
            _color = color;
            _loot = loot;
            _farming = farming;
            _playerStat = playerStat;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable point = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);
            const int baseChanceToFullyHarvest = 50;
            bool alwaysDestroys = point.GetLocalInt("SCAVENGE_POINT_ALWAYS_DESTROYS") == 1;
            
            bool hasBeenSearched = point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1;
            if (hasBeenSearched)
            {
                oPC.SendMessage("There's nothing left to harvest here...");
                return true;
            }

            // Not fully harvested but the timer hasn't counted down yet.
            int refillTick = point.GetLocalInt("SCAVENGE_POINT_REFILL_TICKS");
            if (refillTick > 0)
            {
                oPC.SendMessage("You couldn't find anything new here. Check back later...");
                return true;
            }

            if (!oPC.IsPlayer && !oPC.IsDM) return false;
            PCSkill pcSkill = _skill.GetPCSkill(oPC, SkillType.Scavenging);
            if (pcSkill == null) return false;

            int lootTableID = point.GetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID");
            int level = point.GetLocalInt("SCAVENGE_POINT_LEVEL");
            int rank = pcSkill.Rank;
            int delta = level - rank;

            if (delta > 8)
            {
                oPC.SendMessage("You aren't skilled enough to scavenge through this. (Required Level: " + (level - 8) + ")");
                oPC.AssignCommand(() => _.ActionInteractObject(point.Object));
                return true;
            }

            int dc = 6 + delta;
            if (dc <= 4) dc = 4;
            int searchAttempts = 1 + CalculateSearchAttempts(oPC);

            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck / 2)
            {
                dc--;
            }

            oPC.AssignCommand(() => _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_GET_LOW, 1.0f, 2.0f));

            for (int attempt = 1; attempt <= searchAttempts; attempt++)
            {
                int roll = _random.Random(20) + 1;
                if (roll >= dc)
                {
                    oPC.FloatingText(_color.SkillCheck("Search: *success*: (" + roll + " vs. DC: " + dc + ")"));
                    ItemVO spawnItem = _loot.PickRandomItemFromLootTable(lootTableID);

                    if (spawnItem == null)
                    {
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(spawnItem.Resref) && spawnItem.Quantity > 0)
                    {
                        _.CreateItemOnObject(spawnItem.Resref, point.Object, spawnItem.Quantity);
                    }

                    float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(50, level, rank);
                    _skill.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                else
                {
                    oPC.FloatingText(_color.SkillCheck("Search: *failure*: (" + roll + " vs. DC: " + dc + ")"));

                    float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(10, level, rank);
                    _skill.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                dc += _random.Random(3) + 1;
            }
            
            // Chance to destroy the scavenge point.
            int chanceToFullyHarvest = baseChanceToFullyHarvest - (_perk.GetPCPerkLevel(oPC, PerkType.CarefulScavenger) * 5);
            int growingPlantID = point.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID > 0)
            {
                Data.Entities.GrowingPlant growingPlant = _farming.GetGrowingPlantByID(growingPlantID);
                chanceToFullyHarvest = chanceToFullyHarvest - (growingPlant.LongevityBonus);
            }

            if (chanceToFullyHarvest <= 5) chanceToFullyHarvest = 5;

            if (alwaysDestroys || _random.Random(100) + 1 <= chanceToFullyHarvest)
            {
                point.SetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED", 1);
                oPC.SendMessage("This resource has been fully harvested...");
            }
            // Otherwise the scavenge point will be refilled in 10-20 minutes.
            else
            {
                point.SetLocalInt("SCAVENGE_POINT_REFILL_TICKS", 100 + _random.Random(100));
            }

            point.SetLocalInt("SCAVENGE_POINT_DESPAWN_TICKS", 30);

            return true;
        }


        private int CalculateSearchAttempts(NWPlayer oPC)
        {
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.ScavengingExpert);

            int numberOfSearches = 0;
            int attempt1Chance = 0;
            int attempt2Chance = 0;

            switch (perkLevel)
            {
                case 1: attempt1Chance = 10; break;
                case 2: attempt1Chance = 20; break;
                case 3: attempt1Chance = 30; break;
                case 4: attempt1Chance = 40; break;
                case 5: attempt1Chance = 50; break;

                case 6:
                    attempt1Chance = 50;
                    attempt2Chance = 10;
                    break;
                case 7:
                    attempt1Chance = 50;
                    attempt2Chance = 20;
                    break;
                case 8:
                    attempt1Chance = 50;
                    attempt2Chance = 30;
                    break;
                case 9:
                    attempt1Chance = 50;
                    attempt2Chance = 40;
                    break;
                case 10:
                    attempt1Chance = 50;
                    attempt2Chance = 50;
                    break;
            }

            if (_random.Random(100) + 1 <= attempt1Chance)
            {
                numberOfSearches++;
            }
            if (_random.Random(100) + 1 <= attempt2Chance)
            {
                numberOfSearches++;
            }

            return numberOfSearches;
        }
    }
}
