using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShipSupportCombatPointEngineTests
    {
        private const string EnemyResref = "nw_rat001";
        private const int TemporaryHP = 5000;

        /// <summary>
        /// A pilot who only supports never tags an enemy with a weapon, so each repair or capacitor
        /// refill must credit the enemies fighting the supported ship for Piloting XP when they die.
        /// </summary>
        [EngineTest("Ship repairs and refills on an ally earn Piloting XP from the ally's enemies", Category = "SupportXP", TimeoutSeconds = 60f)]
        public static async Task RepairsEarnXPFromAllyEnemies(EngineTestContext ctx)
        {
            using var repairer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var ally = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), ally.Creature, 3600f);

            var repairerShipId = "engine-repair-xp/" + repairer.Id;
            var allyShipId = "engine-repair-xp/" + ally.Id;
            var repairerStatus = new ShipStatus { Hull = 100, MaxHull = 100, Capacitor = 1000, MaxCapacitor = 1000 };
            repairerStatus.HighPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "hull_rep_b" };
            repairerStatus.HighPowerModules[2] = new ShipStatus.ShipStatusModule { ItemTag = "shld_rep_b" };
            repairerStatus.HighPowerModules[3] = new ShipStatus.ShipStatusModule { ItemTag = "repairfield" };
            repairerStatus.HighPowerModules[4] = new ShipStatus.ShipStatusModule { ItemTag = "bulwarkgen" };
            repairerStatus.HighPowerModules[5] = new ShipStatus.ShipStatusModule { ItemTag = "cap_inject1" };
            repairer.Update(p => p.ActiveShipId = repairerShipId);
            ally.Update(p => p.ActiveShipId = allyShipId);
            DB.Set(new PlayerShip { Id = repairerShipId, OwnerPlayerId = repairer.Id, Status = repairerStatus });
            DB.Set(new PlayerShip { Id = allyShipId, OwnerPlayerId = ally.Id, Status = new ShipStatus { Hull = 10, MaxHull = 1000, Shield = 10, MaxShield = 1000, Capacitor = 10, MaxCapacitor = 1000 } });
            SetLocalObject(repairer.Creature, "SPACE_TARGET", ally.Creature);
            await ctx.ExecuteInCreatureContextAsync(repairer.Creature, () => CreateItemOnObject("ship_fuelcapsule", repairer.Creature));

            try
            {
                var modules = new[]
                {
                    (Feat: FeatType.ShipModule1, Name: "Hull Repairer"),
                    (Feat: FeatType.ShipModule2, Name: "Shield Repairer"),
                    (Feat: FeatType.ShipModule3, Name: "Repair Field Generator"),
                    (Feat: FeatType.ShipModule4, Name: "Bulwark Shield Generator"),
                    (Feat: FeatType.ShipModule5, Name: "Hypermatter Injector"),
                };

                foreach (var (feat, name) in modules)
                {
                    repairer.Update(p =>
                    {
                        p.Skills[SkillType.Piloting].Rank = 0;
                        p.Skills[SkillType.Piloting].XP = 0;
                        p.XPDebt = 0;
                        p.DMXPBonus = 0;
                    });

                    var fighting = ctx.SpawnCreature(EnemyResref, 6f);
                    var aggroOnly = ctx.SpawnCreature(EnemyResref, 6f, 2f);
                    var idle = ctx.SpawnCreature(EnemyResref, 6f, -2f);
                    await ctx.WaitFrameAsync();
                    Enmity.ModifyEnmity(ally.Creature, fighting, 100);
                    Enmity.ModifyProximityEnmity(ally.Creature, aggroOnly, 1);

                    var before = DB.Get<PlayerShip>(allyShipId).Status;
                    var hullBefore = before.Hull;
                    var shieldBefore = before.Shield;
                    var capacitorBefore = before.Capacitor;
                    await ctx.ExecuteInCreatureContextAsync(repairer.Creature, () => Space.ActivateShipModule(repairer.Creature, feat));
                    // Repairers are toggles; keep each one to its first cycle.
                    Space.TurnOffShipToggles(repairer.Creature);
                    var after = DB.Get<PlayerShip>(allyShipId).Status;
                    ctx.Assert(after.Hull > hullBefore || after.Shield > shieldBefore || after.Capacitor > capacitorBefore,
                        $"{name} restores the ally's ship");

                    ctx.Assert(CombatPoint.HasCombatPoints(repairer.Creature, fighting),
                        $"{name} credits the enemy fighting the repaired ally");
                    ctx.Assert(!CombatPoint.HasCombatPoints(repairer.Creature, aggroOnly),
                        $"{name} does not credit an enemy holding only aggro-proximity enmity");
                    ctx.Assert(!CombatPoint.HasCombatPoints(repairer.Creature, idle),
                        $"{name} does not credit an enemy outside the fight");

                    await Kill(ctx, fighting, repairer.Creature);
                    var piloting = DB.Get<Player>(repairer.Id).Skills[SkillType.Piloting];
                    ctx.Assert(piloting.Rank > 0 || piloting.XP > 0,
                        $"{name} earns Piloting XP when the ally's enemy dies");

                    foreach (var leftover in new[] { aggroOnly, idle })
                    {
                        Enmity.ClearEnmityTable(leftover);
                        DestroyObject(leftover);
                    }
                    await ctx.WaitUntilAsync(() => !GetIsObjectValid(aggroOnly) && !GetIsObjectValid(idle), 5f, "leftover enemy despawn");

                    // Every module shares a two-second global recast.
                    await ctx.DelaySecondsAsync(2.1f);
                }
            }
            finally
            {
                AbilityCooldownVisual.ClearAllRecastDelays(repairer.Creature);
                DB.Delete<PlayerShip>(repairerShipId);
                DB.Delete<PlayerShip>(allyShipId);
            }
        }

        /// <summary>
        /// A repair with no fight in progress must not create combat credit.
        /// </summary>
        [EngineTest("Ship repairs outside combat earn no combat points", Category = "SupportXP", TimeoutSeconds = 30f)]
        public static async Task RepairsOutsideCombatEarnNothing(EngineTestContext ctx)
        {
            using var repairer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var ally = await PlayerAbilityFixture.CreateAsync(ctx, 3f);

            var repairerShipId = "engine-repair-idle/" + repairer.Id;
            var allyShipId = "engine-repair-idle/" + ally.Id;
            var repairerStatus = new ShipStatus { Hull = 100, MaxHull = 100, Capacitor = 1000, MaxCapacitor = 1000 };
            repairerStatus.HighPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "hull_rep_b" };
            repairer.Update(p => p.ActiveShipId = repairerShipId);
            ally.Update(p => p.ActiveShipId = allyShipId);
            DB.Set(new PlayerShip { Id = repairerShipId, OwnerPlayerId = repairer.Id, Status = repairerStatus });
            DB.Set(new PlayerShip { Id = allyShipId, OwnerPlayerId = ally.Id, Status = new ShipStatus { Hull = 10, MaxHull = 1000 } });
            SetLocalObject(repairer.Creature, "SPACE_TARGET", ally.Creature);

            try
            {
                var bystander = ctx.SpawnCreature(EnemyResref, 6f);
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(repairer.Creature, () => Space.ActivateShipModule(repairer.Creature, FeatType.ShipModule1));
                Space.TurnOffShipToggles(repairer.Creature);

                ctx.Assert(DB.Get<PlayerShip>(allyShipId).Status.Hull > 10, "the hull repair lands on the ally");
                ctx.Assert(!CombatPoint.HasCombatPoints(repairer.Creature, bystander), "a repair outside combat credits no nearby creature");
                ctx.AssertEqual(-1, CombatPoint.GetTaggedCreatureCount(repairer.Creature), "a repair outside combat tags nothing");
            }
            finally
            {
                AbilityCooldownVisual.ClearAllRecastDelays(repairer.Creature);
                DB.Delete<PlayerShip>(repairerShipId);
                DB.Delete<PlayerShip>(allyShipId);
            }
        }

        /// <summary>
        /// Capital E-War draws fire from allies by taunting nearby enemy ships, so a pilot who only
        /// taunts must be credited for the ships it taunted and nothing else.
        /// </summary>
        [EngineTest("Capital E-War credits the enemy ships it taunts", Category = "SupportXP", TimeoutSeconds = 60f)]
        public static async Task EwarCreditsTauntedShips(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            pilot.Update(p =>
            {
                p.Skills[SkillType.Piloting].Rank = 0;
                p.Skills[SkillType.Piloting].XP = 0;
                p.XPDebt = 0;
                p.DMXPBonus = 0;
            });

            var shipId = "engine-ewar-xp/" + pilot.Id;
            var status = new ShipStatus { Hull = 100, MaxHull = 100, Capacitor = 1000, MaxCapacitor = 1000 };
            status.HighPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "cap_ewar" };
            pilot.Update(p => p.ActiveShipId = shipId);
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = pilot.Id, Status = status });
            SetLocalObject(pilot.Creature, "SPACE_TARGET", pilot.Creature);

            // Enemy ships are registered when real space encounters spawn; register the fixtures directly.
            var npcShips = (Dictionary<uint, ShipStatus>)typeof(Space)
                .GetField("_shipNPCs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var hostileShip = await SpawnHeldCreatureAsync(ctx, 6f, 0f, true);
            var hostileNonShip = await SpawnHeldCreatureAsync(ctx, 6f, 3f, true);
            var neutralShip = await SpawnHeldCreatureAsync(ctx, 6f, -3f, false);
            npcShips[hostileShip] = new ShipStatus { Hull = 100, MaxHull = 100 };
            npcShips[neutralShip] = new ShipStatus { Hull = 100, MaxHull = 100 };

            try
            {
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => Space.ActivateShipModule(pilot.Creature, FeatType.ShipModule1));
                Space.TurnOffShipToggles(pilot.Creature);

                ctx.Assert(CombatPoint.HasCombatPoints(pilot.Creature, hostileShip), "E-War credits the enemy ship it taunts");
                ctx.Assert(!CombatPoint.HasCombatPoints(pilot.Creature, hostileNonShip), "E-War does not credit a hostile creature it cannot taunt");
                ctx.Assert(!CombatPoint.HasCombatPoints(pilot.Creature, neutralShip), "E-War does not credit a ship that is not an enemy");

                await Kill(ctx, hostileShip, pilot.Creature);
                var piloting = DB.Get<Player>(pilot.Id).Skills[SkillType.Piloting];
                ctx.Assert(piloting.Rank > 0 || piloting.XP > 0, "E-War earns Piloting XP when the taunted ship dies");
            }
            finally
            {
                npcShips.Remove(hostileShip);
                npcShips.Remove(neutralShip);
                AbilityCooldownVisual.ClearAllRecastDelays(pilot.Creature);
                DB.Delete<PlayerShip>(shipId);
            }
        }

        /// <summary>
        /// Paralyzed creatures keep their enmity but cannot act, so the space AI cannot move them away.
        /// </summary>
        private static async Task<uint> SpawnHeldCreatureAsync(EngineTestContext ctx, float xOffset, float yOffset, bool hostile)
        {
            var creature = ctx.SpawnCreature(EnemyResref, xOffset, yOffset);
            if (hostile)
                ctx.MakeHostile(creature);
            await ctx.WaitFrameAsync();
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 600f);
            return creature;
        }

        private static async Task Kill(EngineTestContext ctx, uint npc, uint player)
        {
            await ctx.ExecuteInCreatureContextAsync(npc, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), npc));
            await ctx.WaitUntilAsync(() => !CombatPoint.HasCombatPoints(player, npc), 5f, "kill XP payout and cleanup");
        }
    }
}
