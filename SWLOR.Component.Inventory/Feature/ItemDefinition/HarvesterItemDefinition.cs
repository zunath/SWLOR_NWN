using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Inventory;
using SWLOR.Shared.Events.Events.World;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class HarvesterItemDefinition: IItemListDefinition
    {
        private readonly IRandomService _random;
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public HarvesterItemDefinition(IRandomService random, ILogger logger, IDatabaseService db, IServiceProvider serviceProvider, IEventAggregator eventAggregator)
        {
            _random = random;
            _logger = logger;
            _db = db;
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
            
            // Initialize lazy services
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _lootService = new Lazy<ILootService>(() => _serviceProvider.GetRequiredService<ILootService>());
            _skillService = new Lazy<ISkillService>(() => _serviceProvider.GetRequiredService<ISkillService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<ILootService> _lootService;
        private readonly Lazy<ISkillService> _skillService;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _perkService.Value;
        private ILootService LootService => _lootService.Value;
        private ISkillService SkillService => _skillService.Value;

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Harvester("harvest_r_old", 0);
            Harvester("harvest_r_b", 1);
            Harvester("harvest_r_1", 2);
            Harvester("harvest_r_2", 3);
            Harvester("harvest_r_3", 4);
            Harvester("harvest_r_4", 5);

            return Builder.Build();
        }

        /// <summary>
        /// Whenever a resource despawns, if it has an associated prop placeable, destroy it from the game world.
        /// </summary>
        [ScriptHandler<OnSpawnDespawn>]
        public void CleanupResourcePropPlaceables()
        {
            var resource = OBJECT_SELF;
            DestroyProp(resource);
        }

        private static void DestroyProp(uint resource)
        {
            var prop = GetLocalObject(resource, "RESOURCE_PROP_OBJ");
            if (GetIsObjectValid(prop))
            {
                SetPlotFlag(prop, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), prop);
            }
        }

        private void Harvester(string tag, int requiredLevel)
        {
            Builder.Create(tag)
                .Delay(5f)
                .PlaysAnimation(AnimationType.LoopingGetMid)
                .UserFacesTarget()
                .MaxDistance(3.0f)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var perkLevel = PerkService.GetPerkLevel(user, PerkType.Harvesting);

                    if (perkLevel < requiredLevel)
                    {
                        return $"Your Harvesting perk level is too low to use this harvester. (Required: {requiredLevel})";
                    }

                    var lootTableName = GetLocalString(target, "HARVESTING_LOOT_TABLE");
                    if (string.IsNullOrWhiteSpace(lootTableName))
                    {
                        return "This harvester cannot be used on that target.";
                    }

                    if (!LootService.LootTableExists(lootTableName))
                    {
                        _logger.Write<ErrorLogGroup>($"Loot table '{lootTableName}' assigned to harvesting object '{GetName(target)}' does not exist.");
                        return $"ERROR: Harvesting loot table misconfigured. Please use /bug to report this issue.";
                    }

                    var harvesterLevel = requiredLevel < 1 ? 1 : requiredLevel;
                    var resourceLevel = GetLocalInt(target, "HARVESTER_REQUIRED_LEVEL");
                    if (resourceLevel > harvesterLevel)
                    {
                        return $"A level {resourceLevel} harvester or higher is required for this resource.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "You lose your target.");
                        return;
                    }

                    var lootTableName = GetLocalString(target, "HARVESTING_LOOT_TABLE");
                    var lootTable = LootService.GetLootTableByName(lootTableName);
                    var loot = lootTable.GetRandomItem();
                    var resourceLevel = GetLocalInt(target, "HARVESTER_REQUIRED_LEVEL");

                    var resourceCount = GetLocalInt(target, "RESOURCE_COUNT");

                    if (resourceCount <= 0)
                    {
                        resourceCount = _random.D4(1);
                    }

                    resourceCount--;

                    var itemsGathered = 1; // Track number of items gathered
                    CreateItemOnObject(loot.Resref, user);

                    // Additional loot tables - these adhere to standard loot table rules.
                    LootService.SpawnLoot(target, user, "LOOT_TABLE_");

                    // Check against the user's Might; create a second item if they are 
                    // strong.  This is 'free' and does not count towards the limit in the resource point.
                    if (d100() <= 5 * GetAbilityModifier(AbilityType.Might, user) * 5)
                    {
                        loot = lootTable.GetRandomItem();
                        CreateItemOnObject(loot.Resref, user);
                        itemsGathered++; // Increment for the second item
                    }

                    if (resourceCount <= 0)
                    {
                        // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                        // Use EffectDeath to ensure that we trigger death processing.
                        SetPlotFlag(target, false);
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                        DestroyProp(target);
                    }
                    else
                    {
                        SetLocalInt(target, "RESOURCE_COUNT", resourceCount);
                    }

                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Summon_Monster_3), GetLocation(target));

                    if (GetIsPC(user) && !GetIsDM(user))
                    {
                        var playerId = GetObjectUUID(user);
                        var dbPlayer = _db.Get<Player>(playerId);
                        var dbSkill = dbPlayer.Skills[SkillType.Gathering];
                        var veinLevel = 10 * (resourceLevel - 1) + 5;
                        var delta = veinLevel - dbSkill.Rank;
                        var deltaXP = SkillService.GetDeltaXP(delta);

                        // Give XP for each item gathered
                        SkillService.GiveSkillXP(user, SkillType.Gathering, deltaXP * itemsGathered, false, false);
                    }

                    _eventAggregator.Publish(new OnHarvesterUsed(), user);
                });
        }
    }
}
