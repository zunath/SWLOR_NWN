using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature
{
    public class ScavengePoint
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly ISkillService _skillService;
        private readonly IItemService _itemService;
        private readonly ILootService _lootService;

        public ScavengePoint(IDatabaseService db, IRandomService random, IPerkService perkService, ISkillService skillService, IItemService itemService, ILootService lootService)
        {
            _db = db;
            _random = random;
            _perkService = perkService;
            _skillService = skillService;
            _itemService = itemService;
            _lootService = lootService;
        }
        /// <summary>
        /// 
        /// </summary>
        [ScriptHandler(ScriptName.OnScavengeOpened)]
        public void OnOpened()
        {
            OnOpenedInternal();
        }

        private void OnOpenedInternal()
        {
            var user = GetLastOpenedBy();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var placeable = OBJECT_SELF;
            if (GetLocalBool(placeable, "FULLY_HARVESTED"))
            {
                SendMessageToPC(user, "This object has already been searched through.");
                return;
            }
            
            var scavengingLevel = _perkService.GetPerkLevel(user, PerkType.Scavenging);
            var hardLookLevel = _perkService.GetPerkLevel(user, PerkType.HardLook);
            var requiredLevel = GetLocalInt(placeable, "SCAVENGE_POINT_LEVEL");
            var lootTableName = GetLocalString(placeable, "SCAVENGE_POINT_LOOT_TABLE_NAME");

            // Loot table doesn't exist.
            if (!_lootService.LootTableExists(lootTableName))
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
            if (_random.D100(1) <= (hardLookLevel * 10 + GetAbilityModifier(AbilityType.Perception, user) * 5))
            {
                attempts++;
            }

            AssignCommand(user, () => ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 2.0f));

            var lootTable = _lootService.GetLootTableByName(lootTableName);
            var dc = 6;
            var xp = 0;

            var playerId = GetObjectUUID(user);
            var dbPlayer = _db.Get<Player>(playerId);
            var dbSkill = dbPlayer.Skills[SkillType.Gathering];
            var scavLevel = 10 * requiredLevel;            
            var delta = scavLevel - dbSkill.Rank;
            var deltaXP = _skillService.GetDeltaXP(delta);
            var treasureHunterLevel = _perkService.GetPerkLevel(user, PerkType.TreasureHunter);
            var creditFinderLevel = _perkService.GetPerkLevel(user, PerkType.CreditFinder);
            var creditPercentIncrease = creditFinderLevel * 0.2f;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                var roll = _random.D20(1);

                if (roll + GetAbilityModifier(AbilityType.Perception, user) >= dc)
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *success*: ({roll} + {GetAbilityModifier(AbilityType.Perception, user)} vs. DC: {dc})"), user, false);

                    var item = lootTable.GetRandomItem(treasureHunterLevel);
                    var quantity = _random.Next(item.MaxQuantity) + 1;

                    if (item.Resref == "nw_it_gold001")
                    {
                        quantity += (int)(quantity * creditPercentIncrease);
                    }

                    CreateItemOnObject(item.Resref, placeable, quantity);
                    xp = deltaXP;

                    _lootService.SpawnLoot(placeable, user, "LOOT_TABLE_");
                }
                else
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *failure*: ({roll} + {GetAbilityModifier(AbilityType.Perception, user)} vs DC: {dc})"), user, false);
                    xp = deltaXP / 4;
                }

                dc += _random.D3(1);
                _skillService.GiveSkillXP(user, SkillType.Gathering, xp, false, false);
            }

            SetLocalBool(placeable, "FULLY_HARVESTED", true);
        }

        /// <summary>
        /// When an item is added to a scavenge point, return it to the user.
        /// When an item is removed from a scavenge point, if there are no more items in the inventory, destroy the placeable.
        /// </summary>
        [ScriptHandler(ScriptName.OnScavengeDisturbed)]
        public void OnDisturbed()
        {
            OnDisturbedInternal();
        }

        private void OnDisturbedInternal()
        {
            var user = GetLastDisturbed();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();
            var placeable = OBJECT_SELF;

            if (type == DisturbType.Added)
            {
                _itemService.ReturnItem(user, item);
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
        [ScriptHandler(ScriptName.OnScavengeClosed)]
        public void OnClosed()
        {
            OnClosedInternal();
        }

        private void OnClosedInternal()
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
