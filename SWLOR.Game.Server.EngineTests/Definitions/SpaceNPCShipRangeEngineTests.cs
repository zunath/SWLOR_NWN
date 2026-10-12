using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceNPCShipRangeEngineTests
    {
        // t1interceptor fits only Combat Laser B (30m); t1cargo is a registered ship to shoot at.
        private const string AttackerResref = "t1interceptor";
        private const string TargetResref = "t1cargo";
        private const float WeaponRange = 30f;
        private const string SpaceAreaResref = "prefab_space";

        [EngineTest("NPC ship fires on an enmity target inside weapon range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task FiresInsideWeaponRange(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnShipsAsync(ctx, 20f, immobilizeAttacker: true);

            var damaged = await WaitForDamageAsync(ctx, target, 20f);
            ctx.Assert(damaged, $"target {GetDistanceBetween(attacker, target):0.0}m away takes ship damage");
        }

        [EngineTest("NPC ship holds fire on an enmity target beyond weapon range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task HoldsFireBeyondWeaponRange(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnShipsAsync(ctx, 45f, immobilizeAttacker: true);
            var distance = GetDistanceBetween(attacker, target);
            ctx.Assert(distance > WeaponRange + 5f, $"setup places the target beyond weapon range ({distance:0.0}m)");

            var damaged = await WaitForDamageAsync(ctx, target, 12f);
            ctx.Assert(!damaged, $"target {distance:0.0}m away takes no ship damage");
        }

        [EngineTest("NPC ship closes on an enmity target beyond weapon range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task ClosesBeyondWeaponRange(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnShipsAsync(ctx, 45f, immobilizeAttacker: false);
            var startDistance = GetDistanceBetween(attacker, target);
            var startPosition = GetPosition(attacker);
            ctx.Assert(startDistance > WeaponRange + 5f, "setup places the target beyond weapon range");

            await ctx.WaitUntilAsync(() => GetDistanceBetween(attacker, target) <= WeaponRange, 20f,
                $"the attacker to move within weapon range (start {startDistance:0.0}m, now {GetDistanceBetween(attacker, target):0.0}m, moved {Vector3Distance(startPosition, GetPosition(attacker)):0.0}m)");
            var damaged = await WaitForDamageAsync(ctx, target, 20f);
            ctx.Assert(damaged, "target takes ship damage once the attacker is in range");
        }

        [EngineTest("NPC ship closes past weapons that cannot target the current ship", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task ClosesPastIneligibleWeapons(EngineTestContext ctx)
        {
            // mandocap2's 50m Assault Concussion Missile only targets capital ships; its batteries reach 20m.
            var (attacker, target) = await SpawnShipsAsync(ctx, "mandocap2", TargetResref, 35f, immobilizeAttacker: false);
            ctx.Assert(!Space.GetShipStatus(target).CapitalShip, "target is not a capital ship");

            await ctx.WaitUntilAsync(() => GetDistanceBetween(attacker, target) <= 20f, 20f,
                $"the attacker to move within battery range (now {GetDistanceBetween(attacker, target):0.0}m)");
            var damaged = await WaitForDamageAsync(ctx, target, 20f);
            ctx.Assert(damaged, "target takes ship damage once the batteries are in range");
        }

        [EngineTest("NPC bomber fires its proton bomb at a capital ship in bomb range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task BomberFiresProtonBomb(EngineTestContext ctx)
        {
            // The 10m proton bomb must fire before the bomber repositions; its recast starts only on activation.
            var (attacker, target) = await SpawnShipsAsync(ctx, "t5bomber", "mandocap2", 8f, immobilizeAttacker: false);
            ctx.Assert(Space.GetShipStatus(target).CapitalShip, "target is a capital ship");
            var bomb = Space.GetShipStatus(attacker).HighPowerModules.Values.Single(x => x.ItemTag == "protbomblnch");
            var initialRecast = bomb.RecastTime;
            var startDistance = GetDistanceBetween(attacker, target);

            await ctx.WaitUntilAsync(() => bomb.RecastTime != initialRecast, 10f,
                $"the proton bomb to activate (start {startDistance:0.0}m, now {GetDistanceBetween(attacker, target):0.0}m)");
        }

        private static Task<(uint Attacker, uint Target)> SpawnShipsAsync(EngineTestContext ctx, float separation, bool immobilizeAttacker)
            => SpawnShipsAsync(ctx, AttackerResref, TargetResref, separation, immobilizeAttacker);

        private static async Task<(uint Attacker, uint Target)> SpawnShipsAsync(
            EngineTestContext ctx,
            string attackerResref,
            string targetResref,
            float separation,
            bool immobilizeAttacker)
        {
            // An empty 16x16 space area gives an unobstructed straight path between the ships.
            var area = ctx.CreateInstancedArea(SpaceAreaResref);
            var attacker = SpawnShip(ctx, area, attackerResref, 60f);
            var target = SpawnShip(ctx, area, targetResref, 60f + separation);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(attacker) != null && Space.GetShipStatus(target) != null, 5f,
                $"both ships to register as NPC ships (attacker valid={GetIsObjectValid(attacker)}, target valid={GetIsObjectValid(target)})");

            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), target);
            if (immobilizeAttacker)
                ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), attacker);

            await ctx.ExecuteInCreatureContextAsync(attacker, () => Enmity.ModifyEnmity(target, attacker, 100));
            ctx.AssertEqual(target, Enmity.GetHighestEnmityTarget(attacker), "attacker's highest enmity target");

            return (attacker, target);
        }

        private static uint SpawnShip(EngineTestContext ctx, uint area, string resref, float x)
        {
            var ship = CreateObject(ObjectType.Creature, resref, Location(area, new System.Numerics.Vector3(x, 80f, 0f), 0f));
            ctx.Assert(GetIsObjectValid(ship), $"Failed to spawn ship '{resref}'.");
            ChangeToStandardFaction(ship, StandardFaction.Defender);
            ctx.Track(ship);

            return ship;
        }

        private static float Vector3Distance(System.Numerics.Vector3 a, System.Numerics.Vector3 b)
            => System.Numerics.Vector3.Distance(a, b);

        private static async Task<bool> WaitForDamageAsync(EngineTestContext ctx, uint target, float seconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < deadline)
            {
                var status = Space.GetShipStatus(target);
                if (status == null || status.Shield < status.MaxShield || status.Hull < status.MaxHull)
                    return true;

                await ctx.WaitFrameAsync();
            }

            return false;
        }
    }
}
