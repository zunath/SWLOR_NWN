using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class MiningLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private ILootService LootService => _serviceProvider.GetRequiredService<ILootService>();

        public MiningLaserModuleDefinition(IDatabaseService db, IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MiningLaser("min_laser_b", "Basic Mining Laser", "B. Min. Laser", 1, 5, 6f);
            MiningLaser("min_laser_1", "Mining Laser I", "Min. Laser I", 2, 7, 7f);
            MiningLaser("min_laser_2", "Mining Laser II", "Min. Laser II", 3, 9, 8f);
            MiningLaser("min_laser_3", "Mining Laser III", "Min. Laser III", 4, 11, 9f);
            MiningLaser("min_laser_4", "Mining Laser IV", "Min. Laser IV", 5, 13, 10f);

            return _builder.Build();
        }

        private const string Mined = "BEING_MINED";

        private void MiningLaser(string itemTag, string name, string shortName, int requiredLevel, int capacitor, float recast)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_084")
                .Type(ShipModuleType.MiningLaser)
                .MaxDistance(20f)
                .ValidTargetType(ObjectType.Placeable)
                .Description($"Mines targets up to tier {requiredLevel}.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, requiredLevel)
                .Capacitor(capacitor)
                .Recast(recast)
                .ValidationAction((_, _, target, _, _) =>
                {
                    // Ensure an asteroid ore type has been specified by the builder.
                    var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
                    if (string.IsNullOrWhiteSpace(lootTableId))
                    {
                        return "Only asteroids may be targeted with this module.";
                    }

                    // Ensure required level of the module matches or exceeds tier of asteroid.
                    var tierRequired = GetLocalInt(target, "ASTEROID_TIER");
                    if (requiredLevel < tierRequired)
                    {
                        return "This mining laser is not powerful enough to harvest that asteroid.";
                    }

                    if (GetLocalBool(target, Mined))
                    {
                        return "This asteroid is already being mined.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                {
                    SetLocalBool(target, Mined, true);
                    // Remaining units aren't set - pick a random number to assign.
                    var remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");
                    if (remainingUnits <= 0)
                    {
                        remainingUnits = Random.D4(1) + 2;
                        SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
                    }

                    AssignCommand(activator, () =>
                    {
                        var beam = EffectBeam(VisualEffectType.Vfx_Beam_Holy, activator, BodyNodeType.Chest);
                        ApplyEffectToObject(DurationType.Temporary, beam, target, recast + 0.1f);
                    });

                    // At the end of the process, spawn the ore on the activator and reduce remaining units.
                    DelayCommand(recast + 0.1f, () =>
                    {
                        // Perk & module bonuses. These increase the overall yield of each asteroid.
                        var industrialBonus = SpaceService.GetShipStatus(activator).Industrial;

                        var amountToMine = 1 + PerkService.GetPerkLevel(activator, PerkType.StarshipMining) + (int)(industrialBonus / 4) + (int)(moduleBonus)/6f;

                        // Refresh remaining units (could have changed since the start)
                        remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");

                        // Safety check - if another player pulls all of the ore from the asteroid, give an error message.
                        if (!GetIsObjectValid(target) || remainingUnits <= 0)
                        {
                            SendMessageToPC(activator, "Your target has been fully mined.");
                            return;
                        }

                        remainingUnits -= 1 + (int)amountToMine / 2;

                        // Fully deplete the rock - destroy it.
                        if (remainingUnits <= 0)
                        {
                            // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                            // Use EffectDeath to ensure that we trigger death processing.
                            SetPlotFlag(target, false);
                            ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                            SendMessageToPC(activator, $"{GetName(target)} has been fully mined.");
                        }
                        // Update remaining units.
                        else
                        {
                            SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
                        }

                        LootService.SpawnLoot(target, activator, "LOOT_TABLE_");

                        var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
                        var lootTable = LootService.GetLootTableByName(lootTableId);

                        // Spawn the units.
                        for (var count = 1; count <= amountToMine; count++)
                        {
                            var item = lootTable.GetRandomItem();
                            CreateItemOnObject(item.Resref, activator);
                        }

                        if (GetIsPC(activator) && !GetIsDM(activator) && !GetIsDMPossessed(activator))
                        {
                            var playerId = GetObjectUUID(activator);
                            var dbPlayer = _db.Get<Player>(playerId);
                            var rank = dbPlayer.Skills[SkillType.Piloting].Rank;
                            var asteroidLevel = GetLocalInt(target, "ASTEROID_TIER") * 10;
                            var delta = asteroidLevel - rank;
                            var xp = SkillService.GetDeltaXP(delta);

                            SkillService.GiveSkillXP(activator, SkillType.Piloting, xp);
                        }
                    });
                    SetLocalBool(target, Mined, false);
                });
        }
    }
}



