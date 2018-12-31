using System;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;
using System.Linq;

namespace SWLOR.Game.Server.Placeable.ScavengePoint
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IResourceService _resource;
        private readonly IColorTokenService _color;
        private readonly ILootService _loot;
        private readonly IFarmingService _farming;
        private readonly IPlayerStatService _playerStat;

        public OnOpened(INWScript script,
            IBiowareXP2 biowareXP2,
            ISkillService skill,
            IPerkService perk,
            IRandomService random,
            IResourceService resource,
            IColorTokenService color,
            ILootService loot,
            IFarmingService farming,
            IPlayerStatService playerStat)
        {
            _ = script;
			_biowareXP2 = biowareXP2;
            _skill = skill;
            _perk = perk;
            _random = random;
			_resource = resource;
            _color = color;
            _loot = loot;
            _farming = farming;
            _playerStat = playerStat;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable point = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            if (!oPC.IsPlayer) return false;

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
            int rank = _skill.GetPCSkillRank(oPC, SkillType.Scavenging);
            int lootTableID = point.GetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID");
            int level = point.GetLocalInt("SCAVENGE_POINT_LEVEL");
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
                        NWItem resource = _.CreateItemOnObject(spawnItem.Resref, point.Object, spawnItem.Quantity);

                        var componentIP = resource.ItemProperties.FirstOrDefault(x => _.GetItemPropertyType(x) == (int)CustomItemPropertyType.ComponentType);                        
                        if (componentIP != null)
                        {
                            // Add properties to the item based on Scavenging skill.  Similar logic to the resource harvester.
                            var chance = _random.Random(1, 100) + _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
                            ResourceQuality quality;

                            if (chance < 50) quality = ResourceQuality.Low;
                            else if (chance < 75) quality = ResourceQuality.Normal;
                            else if (chance < 95) quality = ResourceQuality.High;
                            else quality = ResourceQuality.VeryHigh;

                            int ipBonusChance = _resource.CalculateChanceForComponentBonus(oPC, (level / 10 + 1), quality, true);

                            if (_random.Random(1, 100) <= ipBonusChance)
                            {
                                var ip = _resource.GetRandomComponentBonusIP(ResourceQuality.Normal);
                                _biowareXP2.IPSafeAddItemProperty(resource, ip.Item1, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);

                                switch (ip.Item2)
                                {
                                    case 0:
                                        resource.Name = _color.Green(resource.Name);
                                        break;
                                    case 1:
                                        resource.Name = _color.Blue(resource.Name);
                                        break;
                                    case 2:
                                        resource.Name = _color.Purple(resource.Name);
                                        break;
                                    case 3:
                                        resource.Name = _color.Orange(resource.Name);
                                        break;
                                }
                            }
                        }
                    }

                    float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(200, level, rank);
                    _skill.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                else
                {
                    oPC.FloatingText(_color.SkillCheck("Search: *failure*: (" + roll + " vs. DC: " + dc + ")"));

                    float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(50, level, rank);
                    _skill.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                dc += _random.Random(3) + 1;
            }
            
            // Chance to destroy the scavenge point.
            int chanceToFullyHarvest = baseChanceToFullyHarvest - (_perk.GetPCPerkLevel(oPC, PerkType.CarefulScavenger) * 5);
            string growingPlantID = point.GetLocalString("GROWING_PLANT_ID");
            if (!string.IsNullOrWhiteSpace(growingPlantID))
            {
                Data.Entity.GrowingPlant growingPlant = _farming.GetGrowingPlantByID(new Guid(growingPlantID));
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

            int background = oPC.Class1;
            if (background == (int)BackgroundType.Scavenger)
                numberOfSearches++;

            return numberOfSearches;
        }
    }
}
