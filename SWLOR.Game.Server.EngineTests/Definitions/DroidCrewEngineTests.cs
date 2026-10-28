using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Droid crew and power routing (plan section 10) through the live module pipeline. A headless player stands in
    /// for the pilot (whose creature is the ship) and a second one for the Engineer. A droid seated at the Engineer
    /// station runs the hull repairer from the droid crew loop at its tier's effectiveness; a tier too low for the
    /// module leaves it off. Posting through the dialog and the controller item need a real client (release pass).
    /// </summary>
    public static class DroidCrewEngineTests
    {
        // On a Large hull with one Low station slot, Low slot 3 is hotbar slot 13, an Engineer slot.
        private const int EngineerSlot = 13;

        private static ShipStatus LargeShipWithEngineerRepairer() => new()
        {
            Size = ShipSize.Large,
            Hull = 10,
            MaxHull = 1000,
            Shield = 100,
            MaxShield = 100,
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
                    ItemTag = "sm_hrep_35",
                    RequiredSkills = new Dictionary<SkillType, int> { [SkillType.Piloting] = 10 },
                },
            },
        };

        private static ShipDroidCrew EngineerDroid(int tier) => new()
        {
            Seat = ShipCrewSeat.Engineer, OwnerPlayerId = "engine-droid-owner", DroidItemId = "engine-droid-" + tier, Name = "R5", Tier = tier,
        };

        [EngineTest("A droid at the Engineer station runs the hull repairer at its tier's effectiveness", Category = "DroidCrew", TimeoutSeconds = 60f)]
        public static async Task DroidRunsTheEngineerRepairer(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = "engine-droid-crew/" + pilot.Id;
            pilot.Update(p => p.ActiveShipId = shipId);
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, Status = LargeShipWithEngineerRepairer() });
            int Hull() => DB.Get<PlayerShip>(shipId).Status.Hull;

            try
            {
                ctx.Assert(SpaceCrew.AssignDroidSeat(pilot.Creature, EngineerDroid(1)), "the Engineer seat takes a droid");
                ctx.AssertEqual(0.0, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), EngineerSlot),
                    "a tier-1 droid (rank 5) cannot work a rank-10 module");
                await ctx.DelaySecondsAsync(2.5f);
                ctx.AssertEqual(10, Hull(), "hull while a droid below the module's rank holds the seat");
                SpaceCrew.ClearDroidCrew(pilot.Creature);

                ctx.Assert(SpaceCrew.AssignDroidSeat(pilot.Creature, EngineerDroid(5)), "the Engineer seat takes a tier-5 droid");
                ctx.AssertEqual(0.75, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), EngineerSlot),
                    "a tier-5 droid works an Engineer slot at 75%");

                await ctx.WaitUntilAsync(() => Hull() > 10, 5f, "the droid switches the hull repairer on");
                Space.TurnOffShipToggles(pilot.Creature);
                SpaceCrew.ClearDroidCrew(pilot.Creature);
                ctx.AssertEqual(16, Hull(), "hull after the droid's Hull Repairer cycle (8 at 75%)");
                ctx.Assert(!SpaceCrew.IsSeatWorked(pilot.Creature, ShipCrewSeat.Engineer), "the seat is free again");
            }
            finally
            {
                SpaceCrew.ClearDroidCrew(pilot.Creature);
                Space.TurnOffShipToggles(pilot.Creature);
                DB.Delete<PlayerShip>(shipId);
            }
        }

        [EngineTest("Only the Engineer routes power, at most every six seconds, and the routing changes shield regen", Category = "DroidCrew", TimeoutSeconds = 30f)]
        public static async Task EngineerRoutesPower(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            using var crew = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var shipId = "engine-power-routing/" + pilot.Id;
            pilot.Update(p => p.ActiveShipId = shipId);
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, Status = LargeShipWithEngineerRepairer() });
            ShipStatus Status() => DB.Get<PlayerShip>(shipId).Status;

            try
            {
                ctx.Assert(!Space.SetPowerRouting(crew.Creature, ShipPowerRouting.Shields), "nobody routes power from outside the Engineer seat");
                ctx.Assert(SpaceCrew.AssignSeat(pilot.Creature, ShipCrewSeat.Engineer, crew.Creature), "the Engineer seat is free");

                var balancedRegen = Space.GetShipShieldRegenPerSecond(pilot.Creature, Status());
                ctx.Assert(Space.SetPowerRouting(crew.Creature, ShipPowerRouting.Shields), "the Engineer routes power to the shields");
                ctx.AssertEqual(ShipPowerRouting.Shields, Status().PowerRouting, "the routing is saved on the ship");
                var shieldsRegen = Space.GetShipShieldRegenPerSecond(pilot.Creature, Status());
                ctx.Assert(System.Math.Abs(shieldsRegen - balancedRegen * 1.15) < 1e-6, $"shield regen rises by 15% ({balancedRegen} -> {shieldsRegen})");

                ctx.Assert(!Space.SetPowerRouting(crew.Creature, ShipPowerRouting.Weapons), "the routing cannot change again within six seconds");
                ctx.Assert(!Space.SetPowerRouting(pilot.Creature, ShipPowerRouting.Weapons), "the pilot cannot route power");
                ctx.AssertEqual(ShipPowerRouting.Shields, Status().PowerRouting, "refusals leave the routing alone");
            }
            finally
            {
                SpaceCrew.ReleaseSeat(crew.Creature);
                DB.Delete<PlayerShip>(shipId);
            }
        }
    }
}
