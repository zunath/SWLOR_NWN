using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceNPCShipRangeEngineTests
    {
        // t1interceptor fits only a Laser Cannon (40m); t1cargo is a registered ship to shoot at.
        private const string AttackerResref = "t1interceptor";
        private const string TargetResref = "t1cargo";
        private const float WeaponRange = 40f;
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
            var (attacker, target) = await SpawnShipsAsync(ctx, 55f, immobilizeAttacker: true);
            var distance = GetDistanceBetween(attacker, target);
            ctx.Assert(distance > WeaponRange + 5f, $"setup places the target beyond weapon range ({distance:0.0}m)");

            var damaged = await WaitForDamageAsync(ctx, target, 12f);
            ctx.Assert(!damaged, $"target {distance:0.0}m away takes no ship damage");
        }

        [EngineTest("NPC ship closes on an enmity target beyond weapon range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task ClosesBeyondWeaponRange(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnShipsAsync(ctx, 55f, immobilizeAttacker: false);
            var startDistance = GetDistanceBetween(attacker, target);
            var startPosition = GetPosition(attacker);
            ctx.Assert(startDistance > WeaponRange + 5f, "setup places the target beyond weapon range");

            await ctx.WaitUntilAsync(() => GetDistanceBetween(attacker, target) <= WeaponRange, 20f,
                $"the attacker to move within weapon range (start {startDistance:0.0}m, now {GetDistanceBetween(attacker, target):0.0}m, moved {Vector3Distance(startPosition, GetPosition(attacker)):0.0}m)");
            var damaged = await WaitForDamageAsync(ctx, target, 20f);
            ctx.Assert(damaged, "target takes ship damage once the attacker is in range");
        }

        [EngineTest("NPC bomber skips its torpedo against a Medium ship and still fires its missile", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task BomberSkipsTorpedoAgainstMediumShip(EngineTestContext ctx)
        {
            // The torpedo only targets Large and Capital ships, so against a Medium cargo ship only the missile fires.
            var (attacker, target) = await SpawnShipsAsync(ctx, "t1bomber", TargetResref, 35f, immobilizeAttacker: true);
            ctx.AssertEqual(ShipSize.Medium, Space.GetShipStatus(target).Size, "target hull size");
            var torpedo = Space.GetShipStatus(attacker).HighPowerModules.Values.Single(x => x.ItemTag == "sm_torp_00");
            var initialRecast = torpedo.RecastTime;

            var damaged = await WaitForDamageAsync(ctx, target, 20f);
            ctx.Assert(damaged, "target takes missile damage");
            ctx.Assert(torpedo.RecastTime == initialRecast, "the torpedo never fires at a Medium ship");
        }

        [EngineTest("NPC bomber fires its torpedo at a capital ship in range", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task BomberFiresTorpedo(EngineTestContext ctx)
        {
            // The torpedo must fire before the bomber repositions; its recast starts only on activation.
            var (attacker, target) = await SpawnShipsAsync(ctx, "t5bomber", "mandocap2", 8f, immobilizeAttacker: false);
            ctx.Assert(Space.GetShipStatus(target).CapitalShip, "target is a capital ship");
            var torpedo = Space.GetShipStatus(attacker).HighPowerModules.Values.Single(x => x.ItemTag == "sm_torp_00");
            var initialRecast = torpedo.RecastTime;
            var startDistance = GetDistanceBetween(attacker, target);

            try
            {
                await ctx.WaitUntilAsync(() => torpedo.RecastTime != initialRecast, 10f,
                    $"the torpedo to activate (start {startDistance:0.0}m, now {GetDistanceBetween(attacker, target):0.0}m)");
            }
            catch (EngineTestAssertionException ex)
            {
                var status = Space.GetShipStatus(attacker);
                throw new EngineTestAssertionException($"{ex.Message} [role={Space.GetNpcRole(attacker)} enmity={Enmity.GetEnmityTable(attacker).Count} " +
                    $"highModules={string.Join(",", status.HighPowerModules.Select(x => x.Key + ":" + x.Value.ItemTag + (Space.IsShipToggleOn(status, x.Key) ? "(on)" : "")))} " +
                    $"capacitor={status.Capacitor}/{status.MaxCapacitor} dead={GetIsDead(attacker)} targetCapital={Space.GetShipStatus(target).CapitalShip}]");
            }
        }

        [EngineTest("NPC capital turns its turbolasers on against its enmity target", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task CapitalTurnsOnTurbolasers(EngineTestContext ctx)
        {
            // mandocap2 fits two turbolasers in High slots 1 and 2; the AI switches them on against the top enmity target.
            var (attacker, target) = await SpawnShipsAsync(ctx, "mandocap2", TargetResref, 35f, immobilizeAttacker: true);
            var status = Space.GetShipStatus(attacker);
            ctx.Assert(status.HighPowerModules[1].ItemTag == "sm_turbo_40" && status.HighPowerModules[2].ItemTag == "sm_turbo_40",
                "mandocap2 fits turbolasers in its first two High slots");

            await ctx.WaitUntilAsync(() => Space.IsShipToggleOn(status, 1) && Space.IsShipToggleOn(status, 2), 10f,
                $"both turbolasers to switch on (target {GetDistanceBetween(attacker, target):0.0}m away)");
            var damaged = await WaitForDamageAsync(ctx, target, 15f);
            ctx.Assert(damaged, "target takes turbolaser damage");
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
