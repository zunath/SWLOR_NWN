using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceNPCShipLeashEngineTests
    {
        [EngineTest("NPC ship leashes like ground enemies: stops firing, restores, and ignores damage", Category = "SpaceNPCAI", TimeoutSeconds = 60f)]
        public static async Task ShipFollowsCombatLeash(EngineTestContext ctx)
        {
            // The combat leash only runs while an enemy player shares the area, and the headless
            // player fixture can only enter the default arena.
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
            var ship = ctx.SpawnCreature("t1interceptor", 20f, 15f);
            var target = ctx.SpawnCreature("t1cargo", 35f, 15f);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ship) != null && Space.GetShipStatus(target) != null, 5f,
                "both ships to register as NPC ships");

            ctx.MakeHostile(ship);
            ctx.Assert(GetIsEnemy(player.Creature, ship), "player is an enemy of the ship");
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), target);
            ctx.Assert(GetDistanceBetween(ship, target) <= 40f, $"target starts inside laser range ({GetDistanceBetween(ship, target):0.0}m)");

            // Home far from both ships puts the target outside the leash radius.
            var home = Location(ctx.Arena, new System.Numerics.Vector3(1f, 1f, 0f), 0f);
            ctx.Assert(GetDistanceBetweenLocations(home, GetLocation(target)) > 55f,
                $"target is outside the leash radius ({GetDistanceBetweenLocations(home, GetLocation(target)):0.0}m from home)");
            SetLocalLocation(ship, "HOME_LOCATION", home);
            AI.SetAIFlag(ship, AI.GetAIFlag(ship) | AIFlag.ReturnHome);

            var shipStatus = Space.GetShipStatus(ship);
            shipStatus.Hull = shipStatus.MaxHull / 2;
            shipStatus.Shield = 0;

            await ctx.ExecuteInCreatureContextAsync(ship, () => Enmity.ModifyEnmity(target, ship, 100));
            await ctx.WaitUntilAsync(() => AI.IsLeashEvading(ship), 10f, "the ship to start its combat leash");
            await ctx.WaitUntilAsync(() => shipStatus.Hull == shipStatus.MaxHull && shipStatus.Shield == shipStatus.MaxShield, 3f,
                $"the leashed ship to restore its shield and hull (shield {shipStatus.Shield}/{shipStatus.MaxShield}, hull {shipStatus.Hull}/{shipStatus.MaxHull})");

            await ctx.ExecuteInCreatureContextAsync(target, () => Space.ApplyShipDamage(target, ship, 50));
            ctx.AssertEqual(shipStatus.MaxShield, shipStatus.Shield, "leashed ship's shield after taking damage");

            // Delayed salvos route through ApplyShipDamage after activation, so a leashed attacker's damage must be dropped.
            var targetStatus = Space.GetShipStatus(target);
            var shieldBeforeSalvo = targetStatus.Shield;
            var hullBeforeSalvo = targetStatus.Hull;
            ctx.Assert(AI.IsLeashEvading(ship), "ship is still returning home");
            await ctx.ExecuteInCreatureContextAsync(ship, () =>
            {
                Space.ApplyShipDamage(ship, target, 50);
                Space.ApplyHullDamage(ship, target, 50);
            });
            ctx.Assert(targetStatus.Shield >= shieldBeforeSalvo && targetStatus.Hull >= hullBeforeSalvo,
                $"a leashed ship's delayed damage is dropped (shield {targetStatus.Shield}/{shieldBeforeSalvo}, hull {targetStatus.Hull}/{hullBeforeSalvo})");
            ctx.Assert(!Space.CanShipFire(ship), "a leashed ship's repeating salvos stop");

            var previousShield = targetStatus.Shield;
            var previousHull = targetStatus.Hull;
            var deadline = DateTime.UtcNow.AddSeconds(5f);
            while (DateTime.UtcNow < deadline && AI.IsLeashEvading(ship))
            {
                await ctx.WaitFrameAsync();
                ctx.Assert(targetStatus.Shield >= previousShield && targetStatus.Hull >= previousHull,
                    "the leashed ship does not damage its former target");
                previousShield = targetStatus.Shield;
                previousHull = targetStatus.Hull;
            }
        }
    }
}
