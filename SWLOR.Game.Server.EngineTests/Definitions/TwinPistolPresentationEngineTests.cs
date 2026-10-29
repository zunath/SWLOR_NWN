using System;
using System.Linq;
using NumericsVector3 = System.Numerics.Vector3;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolPresentationEngineTests
{
    [EngineTest("Twin Pistol cosmetic muzzle pool is bounded and cleaned on release or unpair", Category = "TwinPistolPresentation", TimeoutSeconds = 30f)]
    public static async Task CosmeticAnchorsAreBounded(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        var target = ctx.SpawnCreature("civilian", 5f);
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        await ctx.DelaySecondsAsync(1f);
        var groundAnchor = OBJECT_INVALID;
        var groundMissAnchor = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            groundAnchor = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MuzzleBlueprint,
                Location(ctx.Arena, GetPosition(actor) + new NumericsVector3(1.5f, 0f, 0f), 0f));
            ctx.Track(groundAnchor);
            groundMissAnchor = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MissBlueprint,
                Location(ctx.Arena, GetPosition(actor) + new NumericsVector3(2f, 0f, 0f), 0f));
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
                PairedRangedPresentation.Prepare(actor);
                PairedRangedPresentation.Prepare(actor);
            });
            await ctx.DelaySecondsAsync(.3f);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                var targetHP = GetCurrentHitPoints(target);
                for (var shot = 0; shot < 3; shot++)
                {
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, main, true);
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, companion, false);
                }
                var launchedSources = GetMuzzleSources(ctx.Arena)
                    .Where(source => GetPosition(source).Z - GetPosition(actor).Z > 1f)
                    .ToDictionary(source => source, GetPosition);
                ctx.AssertEqual(6, launchedSources.Count,
                    "Both hands over three volleys keep six independent in-flight source anchors");
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
            ctx.Assert(GetIsObjectValid(groundAnchor), "Pool cleanup leaves an unrelated anchor alone");
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
}
