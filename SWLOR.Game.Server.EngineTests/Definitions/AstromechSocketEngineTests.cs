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
    /// The astromech socket through the live module pipeline: from the socket a crew member works the Pilot's repair
    /// module on their own Engineering rank and is credited for it, and never a weapon; with the socket empty the
    /// module is still the Pilot's. Two headless players stand in for the pilot (whose creature is the ship) and the
    /// astromech. Taking the socket checks the body frame and moves the astromech into space, which needs a real
    /// client (AstromechPlayableSpeciesPlan.md, Phase 4 results).
    /// </summary>
    public static class AstromechSocketEngineTests
    {
        private const string EnemyResref = "nw_rat001";

        // A Small hull has no station slots: High slot 1 is hotbar slot 1 and Low slot 3 is hotbar slot 13, both Helm.
        private const int WeaponSlot = 1;
        private const int RepairerSlot = 13;

        // The Mk VIII Hull Repairer restores 8, scaled to the hull's size (ShipShotBuilder.ScaleSupport).
        private static readonly int FullRepair = ShipShotBuilder.ScaleSupport(8, ShipSize.Small);

        private static ShipStatus SmallShipWithHelmRepairer() => new()
        {
            Size = ShipSize.Small,
            Hull = 10,
            MaxHull = 1000,
            Capacitor = 1000,
            MaxCapacitor = 1000,
            SlotTags = new Dictionary<int, ShipSlotTag>
            {
                [WeaponSlot] = ShipSlotTag.Helm,
                [11] = ShipSlotTag.Helm,
                [RepairerSlot] = ShipSlotTag.Helm,
            },
            HighPowerModules =
            {
                [1] = new ShipStatus.ShipStatusModule
                {
                    ItemTag = "sm_laser_00",
                    RequiredSkills = new Dictionary<SkillType, int>(),
                },
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

        [EngineTest("An astromech in the socket works the pilot's repairer on Engineering, and no weapon", Category = "CrewStations", TimeoutSeconds = 60f)]
        public static async Task TheSocketWorksThePilotsRepairer(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            using var astromech = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var shipId = "engine-astromech-socket/" + pilot.Id;
            pilot.Update(p => p.ActiveShipId = shipId);
            astromech.Update(p =>
            {
                p.Skills[SkillType.Engineering].Rank = 0;
                p.Skills[SkillType.Engineering].XP = 0;
            });
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, Status = SmallShipWithHelmRepairer() });

            var repairerFeat = (FeatType)((int)FeatType.ShipModule1 - 1 + RepairerSlot);
            var weaponFeat = (FeatType)((int)FeatType.ShipModule1 - 1 + WeaponSlot);
            int Hull() => DB.Get<PlayerShip>(shipId).Status.Hull;

            try
            {
                var fighting = ctx.SpawnCreature(EnemyResref, 6f);
                await ctx.WaitFrameAsync();
                Enmity.ModifyEnmity(pilot.Creature, fighting, 100);

                ctx.AssertEqual(1.0, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), RepairerSlot),
                    "with the socket empty the repairer is a Helm module at full effect");

                await ctx.ExecuteInCreatureContextAsync(astromech.Creature, () => Space.ActivateShipModule(astromech.Creature, repairerFeat));
                ctx.AssertEqual(10, Hull(), "hull after a player with no seat presses the repairer");

                ctx.Assert(SpaceCrew.AssignSeat(pilot.Creature, ShipCrewSeat.AstromechSocket, astromech.Creature), "the socket is free");
                ctx.Assert(!SpaceCrew.IsSeatCrewed(pilot.Creature, Space.GetShipStatus(pilot.Creature), RepairerSlot),
                    "the socket owns no slot: the repairer stays the Pilot's");
                ctx.AssertEqual(1.0, Space.GetStationSlotEffectiveness(pilot.Creature, Space.GetShipStatus(pilot.Creature), RepairerSlot),
                    "a plugged-in astromech takes nothing from the Pilot's module");

                await ctx.ExecuteInCreatureContextAsync(astromech.Creature, () => Space.ActivateShipModule(astromech.Creature, repairerFeat));
                ctx.AssertEqual(10, Hull(), "hull after an astromech below the module's rank in Engineering presses it");

                astromech.Update(p => p.Skills[SkillType.Engineering].Rank = 10);
                await ctx.ExecuteInCreatureContextAsync(astromech.Creature, () => Space.ActivateShipModule(astromech.Creature, weaponFeat));
                ctx.Assert(!Space.IsShipToggleOn(Space.GetShipStatus(pilot.Creature), WeaponSlot), "the socket never works a weapon");

                await ctx.ExecuteInCreatureContextAsync(astromech.Creature, () => Space.ActivateShipModule(astromech.Creature, repairerFeat));
                Space.TurnOffShipToggles(pilot.Creature);
                ctx.AssertEqual(10 + FullRepair, Hull(), "hull after the astromech's Hull Repairer cycle (8, scaled to the Small hull)");

                ctx.Assert(CombatPoint.HasCombatPoints(astromech.Creature, fighting),
                    "the astromech earns the combat point for the repair it works");
                ctx.Assert(!CombatPoint.HasCombatPoints(pilot.Creature, fighting),
                    "the pilot earns nothing for the astromech's repair");

                SpaceCrew.ReleaseSeat(astromech.Creature);
                await ctx.ExecuteInCreatureContextAsync(astromech.Creature, () => Space.ActivateShipModule(astromech.Creature, repairerFeat));
                Space.TurnOffShipToggles(pilot.Creature);
                ctx.AssertEqual(10 + FullRepair, Hull(), "hull after the astromech unplugs and presses it");
            }
            finally
            {
                SpaceCrew.ReleaseSeat(astromech.Creature);
                Space.TurnOffShipToggles(pilot.Creature);
                AbilityCooldownVisual.ClearAllRecastDelays(pilot.Creature);
                AbilityCooldownVisual.ClearAllRecastDelays(astromech.Creature);
                DB.Delete<PlayerShip>(shipId);
            }
        }
    }
}
