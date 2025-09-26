using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class StripMinerModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private ILootService LootService => _serviceProvider.GetRequiredService<ILootService>();
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();

        public StripMinerModuleDefinition(IDatabaseService db, IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            StripMiner("cap_minlas", "Strip Miner", "Strip Miner");

            return _builder.Build();
        }

        private const string Mined = "BEING_MINED";

        private void StripMiner(string itemTag, string name, string shortName)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_087")
                .Type(ShipModuleType.StripMiner)
                .MaxDistance(10f)
                .ValidTargetType(ObjectType.Placeable)
                .Description("Mines a target asteroid over 18 seconds, retrieving an extremely large number of ores up to the asteroid's tier. Your ship is immobile while this happens.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, 5)
                .Capacitor(30)
                .Recast(18)
                .CapitalClassModule()
                .ValidationAction((_, _, target, _, _) =>
                {
                    var lootTableId = GetLocalString(target, "STRIPMINE_LOOT_TABLE_ID");
                    if (string.IsNullOrWhiteSpace(lootTableId))
                    {
                        return "Only asteroids may be targeted with this module.";
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

                    ApplyEffectToObject(DurationType.Temporary, EffectEntangle(), activator, 18f);

                    AssignCommand(activator, () =>
                    {
                        var beam = EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest);
                        ApplyEffectToObject(DurationType.Temporary, beam, target, 18f);
                    });

                    DelayCommand(18f, () =>
                    {
                        var industrialBonus = SpaceService.GetShipStatus(activator).Industrial;

                        var amountToMine = 1 + PerkService.GetPerkLevel(activator, PerkType.StarshipMining) + (int)(industrialBonus / 6) + (moduleBonus / 6);

                        SetPlotFlag(target, false);
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                        SendMessageToPC(activator, $"{GetName(target)} has been depleted by your strip miner.");

                        LootService.SpawnLoot(target, activator, "LOOT_TABLE_");

                        var lootTableId = GetLocalString(target, "STRIPMINE_LOOT_TABLE_ID");
                        var lootTable = LootService.GetLootTableByName(lootTableId);

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
