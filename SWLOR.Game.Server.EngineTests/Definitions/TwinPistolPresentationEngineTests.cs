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
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            groundAnchor = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MuzzleBlueprint,
                Location(ctx.Arena, GetPosition(actor) + new NumericsVector3(1.5f, 0f, 0f), 0f));
            ctx.Track(groundAnchor);
        });
        await ctx.WaitFrameAsync();
        try
        {
            var parked = new System.Collections.Generic.Dictionary<uint, NumericsVector3>();
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(GetIsObjectValid(groundAnchor), "The runtime has the dedicated collision-free muzzle blueprint");
                ctx.AssertEqual(32093, ObjectPlugin.GetAppearance(groundAnchor),
                    "The actual source uses its allocated custom placeable row");
                PairedRangedPresentation.Prepare(actor);
                PairedRangedPresentation.Prepare(actor);
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount, CountMuzzles(ctx.Arena),
                    "Repeated preparation keeps one bounded actor-owned pool");
                var right = Right(actor);
                foreach (var source in GetMuzzleSources(ctx.Arena))
                    parked[source] = GetPosition(source);
                ctx.Assert(parked.Values.All(position => position.Z - GetPosition(actor).Z > 1f),
                    "Every source starts at a muzzle instead of the actor's feet");
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount / 2, parked.Values.Count(position =>
                        NumericsVector3.Dot(position - GetPosition(actor), right) > .1f),
                    "Half of the sources wait at the right barrel");
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount / 2, parked.Values.Count(position =>
                        NumericsVector3.Dot(position - GetPosition(actor), right) < -.1f),
                    "Half of the sources wait at the left barrel");
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
                ctx.Assert(parked.All(source => NumericsVector3.Distance(source.Value, GetPosition(source.Key)) < .001f),
                    "Both hands fire from sources already waiting at their barrels without moving them");

                // Moving the actor leaves the six in-flight sources in place and parks the free ones at the new barrels.
                ObjectPlugin.SetPosition(actor, GetPosition(actor) + new NumericsVector3(0f, 3f, 0f));
                PairedRangedPresentation.Park(actor);
                var right = Right(actor);
                var moved = parked.Where(source => NumericsVector3.Distance(source.Value, GetPosition(source.Key)) > .001f).ToArray();
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount - 6, moved.Length,
                    "Parking moves only the sources that are not carrying a projectile");
                ctx.Assert(moved.All(source => GetPosition(source.Key).Z - GetPosition(actor).Z > 1f &&
                        MathF.Abs(NumericsVector3.Dot(GetPosition(source.Key) - GetPosition(actor), right)) > .1f &&
                        NumericsVector3.Distance(GetPosition(source.Key), GetPosition(actor)) < 2.5f),
                    "Parked sources wait at the moved actor's barrels");

                for (var shot = 0; shot < 32; shot++)
                    PairedRangedPresentation.OnCompanionLaunch(actor, target, shot % 2 == 0);
                ctx.AssertEqual(targetHP, GetCurrentHitPoints(target), "Cosmetic launches never deal additional damage");
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount, CountMuzzles(ctx.Arena),
                    "Saturated hit and miss launches never grow the pool");
                PairedRangedPresentation.Release(actor);
            });
            await ctx.WaitUntilAsync(() => CountMuzzles(ctx.Arena) == 0, 2f, "explicit actor release to remove its owned pool");
            ctx.Assert(GetIsObjectValid(groundAnchor), "Pool cleanup leaves an unrelated anchor alone");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                PairedRangedPresentation.Prepare(actor);
                ctx.AssertEqual(PairedRangedPresentation.SourceAnchorCount, CountMuzzles(ctx.Arena), "A fresh pool can be warmed after release");
                CreaturePlugin.RunUnequip(actor, companion);
                PairedRangedPresentation.RemoveAbandonedAnchors();
            });
            await ctx.WaitUntilAsync(() => CountMuzzles(ctx.Arena) == 0, 2f, "unpaired actor cleanup to remove all abandoned anchors");
        }
        finally { PairedRangedPresentation.Release(actor); }
    }

    private static NumericsVector3 Right(uint actor)
    {
        var radians = GetFacing(actor) * MathF.PI / 180f;
        return new NumericsVector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
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
