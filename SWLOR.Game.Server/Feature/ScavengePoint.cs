using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature
{
    public static class ScavengePoint
    {
        /// <summary>
        /// 
        /// </summary>
        [NWNEventHandler("scav_opened")]
        public static void OnOpened()
        {
            var user = GetLastOpenedBy();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var placeable = OBJECT_SELF;
            if (GetLocalBool(placeable, "FULLY_HARVESTED"))
            {
                SendMessageToPC(user, "This object has already been searched through.");
                return;
            }
            
            var scavengingLevel = Perk.GetEffectivePerkLevel(user, PerkType.Scavenging);
            var hardLookLevel = Perk.GetEffectivePerkLevel(user, PerkType.HardLook);
            var requiredLevel = GetLocalInt(placeable, "SCAVENGE_POINT_LEVEL");
            var lootTableName = GetLocalString(placeable, "SCAVENGE_POINT_LOOT_TABLE_NAME");

            // Loot table doesn't exist.
            if (!Loot.LootTableExists(lootTableName))
            {
                SendMessageToPC(user, $"The assigned loot table '{lootTableName}' does not exist. Please inform an admin that this scavenge site is broken.");
                AssignCommand(user, () => ActionInteractObject(placeable));
                return;
            }
            
            // Perk level isn't high enough.
            if (scavengingLevel < requiredLevel)
            {
                SendMessageToPC(user, $"You aren't skilled enough to scavenge through this. (Required level: {requiredLevel})");
                AssignCommand(user, () => ActionInteractObject(placeable));
                SetLocalBool(placeable, "DO_NOT_DESTROY", true);
                return;
            }

            var attempts = 1;

            // Chance for a second attempt based on the hard look perk level and the user's Perception modifier.
            if (Random.D100(1) <= (hardLookLevel * 10 + GetAbilityModifier(AbilityType.Perception, user) * 5))
            {
                attempts++;
            }

            AssignCommand(user, () => ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 2.0f));

            var lootTable = Loot.GetLootTableByName(lootTableName);
            var dc = 6;
            var xp = 0;

            var playerId = GetObjectUUID(user);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbSkill = dbPlayer.Skills[SkillType.Gathering];
            var scavLevel = 10 * requiredLevel;            
            var delta = scavLevel - dbSkill.Rank;
            var deltaXP = Skill.GetDeltaXP(delta);
            var treasureHunterLevel = Perk.GetEffectivePerkLevel(user, PerkType.TreasureHunter);
            var creditFinderLevel = Perk.GetEffectivePerkLevel(user, PerkType.CreditFinder);
            var creditPercentIncrease = creditFinderLevel * 0.2f;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                var roll = Random.D20(1);

                if (roll + GetAbilityModifier(AbilityType.Perception, user) >= dc)
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *success*: ({roll} + {GetAbilityModifier(AbilityType.Perception, user)} vs. DC: {dc})"), user, false);

                    var item = lootTable.GetRandomItem(treasureHunterLevel);
                    var quantity = Random.Next(item.MaxQuantity) + 1;

                    if (item.Resref == "nw_it_gold001")
                    {
                        quantity += (int)(quantity * creditPercentIncrease);
                    }

                    CreateItemOnObject(item.Resref, placeable, quantity);
                    xp = deltaXP;
                }
                else
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *failure*: ({roll} + {GetAbilityModifier(AbilityType.Perception, user)} vs DC: {dc})"), user, false);
                    xp = deltaXP / 4;
                }

                dc += Random.D3(1);
                Skill.GiveSkillXP(user, SkillType.Gathering, xp, false, false);
            }

            SetLocalBool(placeable, "FULLY_HARVESTED", true);
        }

        /// <summary>
        /// When an item is added to a scavenge point, return it to the user.
        /// When an item is removed from a scavenge point, if there are no more items in the inventory, destroy the placeable.
        /// </summary>
        [NWNEventHandler("scav_disturbed")]
        public static void OnDisturbed()
        {
            var user = GetLastDisturbed();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();
            var placeable = OBJECT_SELF;

            if (type == DisturbType.Added)
            {
                Item.ReturnItem(user, item);
            }
            else
            {
                var firstItem = GetFirstItemInInventory(placeable);
                if (!GetIsObjectValid(firstItem))
                {
                    // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                    // Use EffectDeath to ensure that we trigger death processing.
                    SetPlotFlag(placeable, false);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), placeable);
                }
            }

        }

        /// <summary>
        /// When a scavenge site is closed by a player, if there are no more items in the inventory, destroy the scavenge point.
        /// </summary>
        [NWNEventHandler("scav_closed")]
        public static void OnClosed()
        {
            var placeable = OBJECT_SELF;
            var user = GetLastClosedBy();

            if (!GetIsPC(user) || GetIsDM(user)) return;

            // In case the user is not skilled enough to scavenge this resource, we don't want to destroy it. 
            if (GetLocalBool(placeable, "DO_NOT_DESTROY"))
            {
                DeleteLocalBool(placeable, "DO_NOT_DESTROY");
                return;
            }

            var firstItem = GetFirstItemInInventory(placeable);
            if (!GetIsObjectValid(firstItem))
            {
                // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                // Use EffectDeath to ensure that we trigger death processing.
                SetPlotFlag(placeable, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), placeable);
            }

        }
    }
}
