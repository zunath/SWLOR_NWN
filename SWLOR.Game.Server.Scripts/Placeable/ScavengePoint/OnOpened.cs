using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Scripts.Placeable.ScavengePoint
{
    public class OnOpened: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable point = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            if (!oPC.IsPlayer) return;

            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);
            const int baseChanceToFullyHarvest = 50;
            
            bool hasBeenSearched = point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1;
            if (hasBeenSearched)
            {
                oPC.SendMessage("There's nothing left to harvest here...");
                return;
            }


            if (!oPC.IsPlayer && !oPC.IsDM) return;
            int rank = SkillService.GetPCSkillRank(oPC, SkillType.Scavenging);
            int lootTableID = point.GetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID");
            int level = point.GetLocalInt("SCAVENGE_POINT_LEVEL");
            int delta = level - rank;

            if (delta > 8)
            {
                oPC.SendMessage("You aren't skilled enough to scavenge through this. (Required Level: " + (level - 8) + ")");
                oPC.AssignCommand(() => _.ActionInteractObject(point.Object));
                return;
            }

            int dc = 6 + delta;
            if (dc <= 4) dc = 4;
            int searchAttempts = 1 + CalculateSearchAttempts(oPC);

            int luck = PerkService.GetCreaturePerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
            if (RandomService.Random(100) + 1 <= luck / 2)
            {
                dc--;
            }

            oPC.AssignCommand(() => _.ActionPlayAnimation(_.ANIMATION_LOOPING_GET_LOW, 1.0f, 2.0f));

            for (int attempt = 1; attempt <= searchAttempts; attempt++)
            {
                int roll = RandomService.Random(20) + 1;
                if (roll >= dc)
                {
                    oPC.FloatingText(ColorTokenService.SkillCheck("Search: *success*: (" + roll + " vs. DC: " + dc + ")"));
                    ItemVO spawnItem = LootService.PickRandomItemFromLootTable(lootTableID);

                    if (spawnItem == null)
                    {
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(spawnItem.Resref) && spawnItem.Quantity > 0)
                    {
                        _.CreateItemOnObject(spawnItem.Resref, point.Object, spawnItem.Quantity);
                    }

                    float xp = SkillService.CalculateRegisteredSkillLevelAdjustedXP(200, level, rank);
                    SkillService.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                else
                {
                    oPC.FloatingText(ColorTokenService.SkillCheck("Search: *failure*: (" + roll + " vs. DC: " + dc + ")"));

                    float xp = SkillService.CalculateRegisteredSkillLevelAdjustedXP(50, level, rank);
                    SkillService.GiveSkillXP(oPC, SkillType.Scavenging, (int)xp);
                }
                dc += RandomService.Random(3) + 1;
            }
            
            // Chance to destroy the scavenge point.
            int chanceToFullyHarvest = baseChanceToFullyHarvest - (PerkService.GetCreaturePerkLevel(oPC, PerkType.CarefulScavenger) * 5);
            
            if (chanceToFullyHarvest <= 5) chanceToFullyHarvest = 5;

            point.SetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED", 1);
            oPC.SendMessage("This resource has been fully harvested...");

            point.SetLocalInt("SCAVENGE_POINT_DESPAWN_TICKS", 30);
        }


        private int CalculateSearchAttempts(NWPlayer oPC)
        {
            int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.ScavengingExpert);

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

            if (RandomService.Random(100) + 1 <= attempt1Chance)
            {
                numberOfSearches++;
            }
            if (RandomService.Random(100) + 1 <= attempt2Chance)
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
