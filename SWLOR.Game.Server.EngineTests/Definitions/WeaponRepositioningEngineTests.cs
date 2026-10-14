using System;
using System.Linq;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector2 = System.Numerics.Vector2;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponRepositioningEngineTests
{
    [EngineTest("Twin Pistol cosmetic muzzle pool is bounded non-blocking and cleaned on release or unpair", Category = "TwinPistolPresentation", TimeoutSeconds = 30f)]
    public static async Task CosmeticAnchorsAreNonBlockingAndBounded(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        var target = ctx.SpawnCreature("civilian", 5f);
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        await ctx.DelaySecondsAsync(1f);
        Location destination = null;
        var groundAnchor = OBJECT_INVALID;
        var groundMissAnchor = OBJECT_INVALID;
        var callbacks = 0;
        var arrived = false;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            destination = FindDestination(ctx, actor, 3f);
            var middle = NumericsVector3.Lerp(GetPosition(actor), GetPositionFromLocation(destination), .5f);
            groundAnchor = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MuzzleBlueprint,
                Location(ctx.Arena, middle, 0f));
            ctx.Track(groundAnchor);
            groundMissAnchor = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MissBlueprint,
                Location(ctx.Arena, NumericsVector3.Lerp(GetPosition(actor), GetPositionFromLocation(destination), .7f), 0f));
            ctx.Track(groundMissAnchor);
            SetTag(groundMissAnchor, "miss_collision_probe");
        });
        await ctx.WaitFrameAsync();
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(GetIsObjectValid(groundAnchor), "The runtime has the dedicated collision-free muzzle blueprint");
                ctx.Assert(GetIsObjectValid(groundMissAnchor), "The runtime has the stock invisible miss-endpoint blueprint");
                ctx.AssertEqual(32093, ObjectPlugin.GetAppearance(groundAnchor),
                    "The actual source uses its allocated custom placeable row");
                ctx.AssertEqual(479, ObjectPlugin.GetAppearance(groundMissAnchor),
                    "The actual miss endpoint uses the verified stock invisible model");
                ctx.AssertEqual(string.Empty, WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f),
                    "Both source and stock invisible miss endpoint preserve the native direct movement corridor at ground level");
                PairedRangedPresentation.Prepare(actor);
                PairedRangedPresentation.Prepare(actor);
                WeaponRepositioning.Begin(actor, destination, (success, _) => { callbacks++; arrived = success; });
            });
            await ctx.WaitUntilAsync(() => callbacks > 0, 5f, "normal collision movement through source and stock miss anchors");
            ctx.Assert(arrived, "The normal movement action crosses both cosmetic placeables successfully");
            await ctx.DelaySecondsAsync(.3f);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                var targetHP = GetCurrentHitPoints(target);
                for (var enemy = 0; enemy < 3; enemy++)
                {
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, main, true);
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, companion, false);
                }
                var launchedSources = GetMuzzleSources(ctx.Arena)
                    .Where(source => GetPosition(source).Z - GetPosition(actor).Z > 1f)
                    .ToDictionary(source => source, GetPosition);
                ctx.AssertEqual(6, launchedSources.Count,
                    "Both hands against three targets keep six independent in-flight source anchors");
                var radians = GetFacing(actor) * MathF.PI / 180f;
                var right = new NumericsVector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
                ctx.AssertEqual(3, launchedSources.Values.Count(position =>
                        NumericsVector3.Dot(position - GetPosition(actor), right) > .1f),
                    "Three active main-hand shots originate from the right barrel");
                ctx.AssertEqual(3, launchedSources.Values.Count(position =>
                        NumericsVector3.Dot(position - GetPosition(actor), right) < -.1f),
                    "Three active companion shots originate from the left barrel, including misses");
                for (var shot = 0; shot < 32; shot++)
                    PairedRangedPresentation.OnCompanionLaunch(actor, target, shot % 2 == 0);
                foreach (var source in launchedSources)
                    ctx.Assert(NumericsVector3.Distance(source.Value, GetPosition(source.Key)) < .001f,
                        "Pool saturation cannot move an already launched projectile source");
                ctx.AssertEqual(targetHP, GetCurrentHitPoints(target), "Cosmetic launches never deal additional damage");
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount + PairedRangedPresentation.MissAnchorCount, CountMuzzles(ctx.Arena),
                    "Repeated preparation and saturated hit/miss launches retain one bounded actor-owned pool");
                PairedRangedPresentation.Release(actor);
            });
            await ctx.WaitUntilAsync(() => CountMuzzles(ctx.Arena) == 0, 2f, "explicit actor release to remove its owned pool");
            ctx.Assert(GetIsObjectValid(groundAnchor), "Pool cleanup leaves an unrelated collision-test anchor alone");
            ctx.Assert(GetIsObjectValid(groundMissAnchor), "Pool cleanup leaves the unrelated stock miss anchor alone");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                PairedRangedPresentation.Prepare(actor);
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount + PairedRangedPresentation.MissAnchorCount, CountMuzzles(ctx.Arena), "A fresh pool can be warmed after release");
                CreaturePlugin.RunUnequip(actor, companion);
                PairedRangedPresentation.RemoveAbandonedAnchors();
            });
            await ctx.WaitUntilAsync(() => CountMuzzles(ctx.Arena) == 0, 2f, "unpaired actor cleanup to remove all abandoned anchors");
        }
        finally { PairedRangedPresentation.Release(actor); }
    }

    private static uint[] GetMuzzleSources(uint area)
    {
        var sources = new System.Collections.Generic.List<uint>();
        for (var obj = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Placeable))
            if (GetTag(obj) == PairedRangedPresentation.MuzzleTag && GetResRef(obj) == PairedRangedPresentation.MuzzleBlueprint)
                sources.Add(obj);
        return sources.ToArray();
    }

    private static int CountMuzzles(uint area)
    {
        var count = 0;
        for (var obj = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Placeable))
            if (GetTag(obj) == PairedRangedPresentation.MuzzleTag) count++;
        return count;
    }

    [EngineTest("Twin Pistol free-ground movement confirms arrival once and shares a lockout", Category = "TwinPistolMovement", TimeoutSeconds = 30f)]
    public static async Task FreeGroundMovement(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        await ctx.DelaySecondsAsync(1f);
        var callbacks = 0;
        var arrived = false;
        var distance = 0f;
        var locked = string.Empty;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            var destination = FindDestination(ctx, actor, 3f);
            ctx.AssertEqual(string.Empty, WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f),
                "An open direct path includes its own actor without reporting self-collision");
            WeaponRepositioning.Begin(actor, destination, (success, moved) => { callbacks++; arrived = success; distance = moved; });
            locked = WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f);
        });
        await ctx.WaitUntilAsync(() => callbacks > 0, 5f, "normal collision movement to finish");
        ctx.Assert(arrived, "Shooting may proceed only after the engine confirms arrival");
        ctx.Assert(distance >= 2f && distance <= 3.35f, $"Actual movement remains in the captured segment ({distance:0.00}m)");
        ctx.Assert(locked.Contains("two seconds"), "A second movement ability shares the same lockout");
        WeaponRepositioning.Cancel(actor);
        await ctx.DelaySecondsAsync(0.3f);
        ctx.AssertEqual(1, callbacks, "Arrival, polling and cancellation cannot deliver the callback twice");
    }

    [EngineTest("Twin Pistol movement rejects roots and body collision before payment", Category = "TwinPistolMovement", TimeoutSeconds = 30f)]
    public static async Task RootsAndCollision(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        await ctx.DelaySecondsAsync(1f);
        Location destination = null;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            destination = FindDestination(ctx, actor, 4f);
            var root = EffectCutsceneImmobilize();
            ApplyEffectToObject(DurationType.Temporary, root, actor, 30f);
            ctx.Assert(WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f).Contains("rooted"),
                "An immobilized actor cannot relocate");
            RemoveEffect(actor, root);
            var middle = NumericsVector3.Lerp(GetPosition(actor), GetPositionFromLocation(destination), 0.5f);
            var blocker = CreateObject(ObjectType.Creature, "civilian", Location(ctx.Arena, middle, 0));
            ctx.Track(blocker);
            ctx.Assert(GetIsObjectValid(blocker), "Create a live collision blocker");
        });
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var collisionError = WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f);
            ctx.Assert(collisionError.Contains("blocked"),
                $"GetPathExists cannot allow a direct movement corridor through another creature; validation: '{collisionError}'");
        });
    }

    [EngineTest("Twin Pistol cancelled movement cannot report arrival or reset its lockout", Category = "TwinPistolMovement", TimeoutSeconds = 30f)]
    public static async Task Cancellation(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        await ctx.DelaySecondsAsync(1f);
        var callbacks = 0;
        var arrived = true;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            var destination = FindDestination(ctx, actor, 4f);
            WeaponRepositioning.Begin(actor, destination, (success, _) => { callbacks++; arrived = success; });
            WeaponRepositioning.Cancel(actor);
            ctx.Assert(WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f).Contains("two seconds"),
                "Cancellation does not refresh the shared movement cooldown");
        });
        await ctx.DelaySecondsAsync(0.5f);
        ctx.AssertEqual(1, callbacks, "Cancelled movement completes once");
        ctx.Assert(!arrived, "Cancelled movement earns neither impacts nor movement rewards");
    }

    private static Location FindDestination(EngineTestContext ctx, uint actor, float distance)
    {
        var origin = GetPosition(actor);
        for (var direction = 0; direction < 16; direction++)
        {
            var angle = direction * MathF.PI / 8;
            var position = origin + new NumericsVector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0);
            if (position.X < 0 || position.Y < 0) continue;
            position.Z = GetGroundHeight(Location(ctx.Arena, position, 0));
            var destination = Location(ctx.Arena, position, 0);
            if (string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), destination, 2f, 6f))) return destination;
        }
        ctx.Assert(false, "The arena must provide one collision-safe direct movement segment");
        return GetLocation(actor);
    }
}
