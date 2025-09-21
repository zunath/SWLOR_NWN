using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class StripMinerModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IPerkService _perkService;
        private readonly ISpaceService _spaceService;
        private readonly ILootService _lootService;
        private readonly ISkillService _skillService;
        private readonly ShipModuleBuilder _builder = new();

        public StripMinerModuleDefinition(IDatabaseService db, IPerkService perkService, ISpaceService spaceService, ILootService lootService, ISkillService skillService)
        {
            _db = db;
            _perkService = perkService;
            _spaceService = spaceService;
            _lootService = lootService;
            _skillService = skillService;
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
                        var industrialBonus = _spaceService.GetShipStatus(activator).Industrial;

                        var amountToMine = 1 + _perkService.GetPerkLevel(activator, PerkType.StarshipMining) + (int)(industrialBonus / 6) + (moduleBonus / 6);

                        SetPlotFlag(target, false);
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                        SendMessageToPC(activator, $"{GetName(target)} has been depleted by your strip miner.");

                        _lootService.SpawnLoot(target, activator, "LOOT_TABLE_");

                        var lootTableId = GetLocalString(target, "STRIPMINE_LOOT_TABLE_ID");
                        var lootTable = _lootService.GetLootTableByName(lootTableId);

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
                            var xp = _skillService.GetDeltaXP(delta);

                            _skillService.GiveSkillXP(activator, SkillType.Piloting, xp);
                        }
                    });
                    SetLocalBool(target, Mined, false);
                });
        }
    }
}
