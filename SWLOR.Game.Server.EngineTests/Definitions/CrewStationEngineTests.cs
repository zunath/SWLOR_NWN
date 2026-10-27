using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Crew stations (plan section 10) through the live module pipeline: a station module fires from the ship only
    /// for the crew member at its seat, only at their seat-skill rank, and credits them in their seat's skill. Two
    /// headless players stand in for the pilot (whose creature is the ship) and the crew member. Boarding itself
    /// moves the crew member into the space area and attaches their camera, which needs a real client (release pass).
    /// </summary>
    public static class CrewStationEngineTests
    {
        private const string EnemyResref = "nw_rat001";

        // On a Large hull with one Low station slot, Low slot 3 is hotbar slot 13, an Engineer slot.
        private const int EngineerSlot = 13;

        private static ShipStatus LargeShipWithEngineerRepairer() => new()
        {
            Size = ShipSize.Large,
            Hull = 10,
            MaxHull = 1000,
            Capacitor = 1000,
            MaxCapacitor = 1000,
            SlotTags = new Dictionary<int, ShipSlotTag>
            {
                [1] = ShipSlotTag.Helm,
                [11] = ShipSlotTag.Helm,
                [EngineerSlot] = ShipSlotTag.Engineer,
            },
            LowPowerModules =
            {
                [3] = new ShipStatus.ShipStatusModule
                {
                    ItemTag = "hull_rep_b",
                    RequiredSkills = new Dictionary<SkillType, int> { [SkillType.Piloting] = 10 },
                },
            },
        };

        [EngineTest("A crew member works their station's module from the ship; the pilot cannot", Category = "CrewStations", TimeoutSeconds = 60f)]
        public static async Task CrewWorkTheirStationFromTheShip(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            using var crew = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var shipId = "engine-crew-station/" + pilot.Id;
            pilot.Update(p => p.ActiveShipId = shipId);
            crew.Update(p =>
            {
                p.Skills[SkillType.Engineering].Rank = 0;
                p.Skills[SkillType.Engineering].XP = 0;
            });
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, Status = LargeShipWithEngineerRepairer() });

            var stationFeat = (FeatType)((int)FeatType.ShipModule1 - 1 + EngineerSlot);
            int Hull() => DB.Get<PlayerShip>(shipId).Status.Hull;

            try
            {
                var fighting = ctx.SpawnCreature(EnemyResref, 6f);
                await ctx.WaitFrameAsync();
                Enmity.ModifyEnmity(pilot.Creature, fighting, 100);

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => Space.ActivateShipModule(pilot.Creature, stationFeat));
                ctx.AssertEqual(10, Hull(), "hull after the pilot presses an Engineer slot");

                await ctx.ExecuteInCreatureContextAsync(crew.Creature, () => Space.ActivateShipModule(crew.Creature, stationFeat));
                ctx.AssertEqual(10, Hull(), "hull after a crew member with no seat presses it");

                ctx.Assert(SpaceCrew.AssignSeat(pilot.Creature, ShipCrewSeat.Engineer, crew.Creature), "the Engineer seat is free");
                ctx.Assert(SpaceCrew.IsSeatCrewed(pilot.Creature, Space.GetShipStatus(pilot.Creature), EngineerSlot), "the seat is crewed");
                ctx.AssertEqual(0.0, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), EngineerSlot),
                    "a station module above the crew member's Engineering rank works nothing, its passives included");

                await ctx.ExecuteInCreatureContextAsync(crew.Creature, () => Space.ActivateShipModule(crew.Creature, stationFeat));
                ctx.AssertEqual(10, Hull(), "hull after an Engineer below the module's rank presses it");

                crew.Update(p => p.Skills[SkillType.Engineering].Rank = 10);
                ctx.AssertEqual(1.0, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), EngineerSlot),
                    "a player at the module's rank works it at 100%");

                await ctx.ExecuteInCreatureContextAsync(crew.Creature, () => Space.ActivateShipModule(crew.Creature, stationFeat));
                Space.TurnOffShipToggles(pilot.Creature);
                ctx.AssertEqual(17, Hull(), "hull after the Engineer's Basic Hull Repairer cycle (7 at 100%)");

                ctx.Assert(CombatPoint.HasCombatPoints(crew.Creature, fighting),
                    "the crew member earns the combat point for their station's repair");
                ctx.Assert(!CombatPoint.HasCombatPoints(pilot.Creature, fighting),
                    "the pilot earns nothing for the crew member's repair");

                SpaceCrew.ReleaseSeat(crew.Creature);
                await ctx.ExecuteInCreatureContextAsync(crew.Creature, () => Space.ActivateShipModule(crew.Creature, stationFeat));
                Space.TurnOffShipToggles(pilot.Creature);
                ctx.AssertEqual(17, Hull(), "hull after the crew member leaves the seat and presses it");
            }
            finally
            {
                SpaceCrew.ReleaseSeat(crew.Creature);
                Space.TurnOffShipToggles(pilot.Creature);
                AbilityCooldownVisual.ClearAllRecastDelays(pilot.Creature);
                AbilityCooldownVisual.ClearAllRecastDelays(crew.Creature);
                DB.Delete<PlayerShip>(shipId);
            }
        }
    }
}
