using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The NPC ship role behaviors on spawned role ships: each fights a Defender cargo ship and the test watches
    /// for the role's action.
    /// </summary>
    public static class SpaceNpcRoleEngineTests
    {
        private const string TargetResref = "t1cargo";
        private const string SpaceAreaResref = "prefab_space";
        private const int FirstMidSlot = 21;

        [EngineTest("NPC tank switches on its Shield Hardener below half hull", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task TankHardensBelowHalfHull(EngineTestContext ctx)
        {
            var (area, tank, _) = await SpawnFightAsync(ctx, "t1gunship");
            var status = Space.GetShipStatus(tank);
            ctx.Assert(status.MidPowerModules[1].ItemTag.StartsWith("sm_hard_"), "the gunship fits a Shield Hardener");
            ctx.Assert(!Space.IsShipToggleOn(status, FirstMidSlot), "the hardener starts off");

            status.Hull = status.MaxHull * 2 / 5;
            await ctx.WaitUntilAsync(() => Space.IsShipToggleOn(status, FirstMidSlot), 10f, "the hardener to switch on");
        }

        [EngineTest("NPC logistics ship repairs a damaged ally", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task LogisticsRepairsAnAlly(EngineTestContext ctx)
        {
            var (area, logistics, _) = await SpawnFightAsync(ctx, "t1logistics");
            var ally = SpawnShip(ctx, area, "t1fighter", 66f, StandardFaction.Hostile);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ally) != null, 5f, "the ally to register");
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), ally);

            var allyStatus = Space.GetShipStatus(ally);
            allyStatus.Hull = allyStatus.MaxHull / 2;
            var damaged = allyStatus.Hull;
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ally).Hull > damaged, 15f,
                $"the ally's hull to rise from {damaged} (logistics {GetDistanceBetween(logistics, ally):0.0}m away)");
        }

        [EngineTest("NPC EW ship jams its target", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task ElectronicWarfareJams(EngineTestContext ctx)
        {
            var (_, _, target) = await SpawnFightAsync(ctx, "t1ewar");
            await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<FlashStatusEffect>(target), 10f, "the target to be jammed");
        }

        [EngineTest("NPC carrier launches its tier's fighter", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task CarrierLaunchesAWave(EngineTestContext ctx)
        {
            var (area, carrier, target) = await SpawnFightAsync(ctx, "t1carrier");
            var seen = new System.Collections.Generic.HashSet<string>();
            try
            {
                await ctx.WaitUntilAsync(() =>
                {
                    for (var o = GetFirstObjectInArea(area); GetIsObjectValid(o); o = GetNextObjectInArea(area))
                        seen.Add($"{GetTag(o)}/{GetResRef(o)}/{GetObjectType(o)}/dead={GetIsDead(o)}");
                    return CountInArea(area, "t1fighter") >= 1;
                }, 10f, "a fighter to launch");
            }
            catch (EngineTestAssertionException ex)
            {
                var states = (System.Collections.IDictionary)typeof(Space).GetField("_npcRoleStates", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                var state = states.Contains(carrier) ? (SWLOR.Game.Server.Service.SpaceService.ShipNpcRoleState)states[carrier] : null;
                throw new EngineTestAssertionException($"{ex.Message} [tag={GetTag(carrier)} role={Space.GetNpcRole(carrier)} state={(state == null ? "none" : $"waves {state.WavesLaunched} last {state.LastWave:o} fighters {state.Fighters.Count}")} " +
                    $"status={(Space.GetShipStatus(carrier) != null)} dist={GetDistanceBetween(carrier, target):0.0} enemy={GetIsEnemy(target, carrier)} dead={GetIsDead(carrier)} enmity={Enmity.GetEnmityTable(carrier).Count} fightersAny={CountInArea(area, "t1fighter")} seen={string.Join(";", seen)}]");
            }

            DestroyObject(carrier);
            await ctx.WaitUntilAsync(() => CountInArea(area, "t1fighter") == 0, 10f, "the fighter to leave with the carrier");
        }

        [EngineTest("NPC minelayer drops a mine", Category = "SpaceNPCAI", TimeoutSeconds = 45f)]
        public static async Task MinelayerDropsAMine(EngineTestContext ctx)
        {
            var (area, _, _) = await SpawnFightAsync(ctx, "t1minelayer");
            await ctx.WaitUntilAsync(() => CountInArea(area, "spacemine") >= 1, 10f, "a mine to appear");
        }

        private static int CountInArea(uint area, string tag)
        {
            var count = 0;
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) == tag && !GetIsDead(obj))
                    count++;
            }

            return count;
        }

        private static async Task<(uint Area, uint Ship, uint Target)> SpawnFightAsync(EngineTestContext ctx, string resref)
        {
            var area = ctx.CreateInstancedArea(SpaceAreaResref);
            var ship = SpawnShip(ctx, area, resref, 60f, StandardFaction.Hostile);
            var target = SpawnShip(ctx, area, TargetResref, 80f, StandardFaction.Defender);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(ship) != null && Space.GetShipStatus(target) != null, 5f,
                "both ships to register as NPC ships");

            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), ship);
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), target);
            await ctx.ExecuteInCreatureContextAsync(ship, () => Enmity.ModifyEnmity(target, ship, 100));

            return (area, ship, target);
        }

        private static uint SpawnShip(EngineTestContext ctx, uint area, string resref, float x, StandardFaction faction)
        {
            var ship = CreateObject(ObjectType.Creature, resref, Location(area, new System.Numerics.Vector3(x, 80f, 0f), 0f));
            ctx.Assert(GetIsObjectValid(ship), $"Failed to spawn ship '{resref}'.");
            ChangeToStandardFaction(ship, faction);
            ctx.Track(ship);

            return ship;
        }
    }
}