using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;
using ScriptVisualTransform = SWLOR.NWN.API.NWScript.Enum.ObjectVisualTransform;

namespace SWLOR.Game.Server.Native;

/// <summary>Cosmetic pistol launches; never resolves damage, ammunition, traits or progression.</summary>
public static class PairedRangedPresentation
{
    public const string MuzzleBlueprint = "tp_muzzle";
    public const string MissBlueprint = "tp_miss";
    public const string MuzzleTag = "paired_pistol_muzzle";
    // Rush, the other two instant recast groups, a finishing capstone and a pending
    // three-shot ordinary batch can overlap fifteen cosmetic projectiles.
    public const int SourceAnchorCount = 16;
    public const int MissAnchorCount = 16;
    private const long ReplicationWarmupMilliseconds = 250;
    private static readonly Dictionary<uint, (uint[] Sources, uint[] MissTargets, long[] BusyUntil,
        long ReadyAt, uint Area)> Anchors = new();

    // The authored firing pose and registered 11/201/11 pistol appearance define
    // this approximation; the server cannot evaluate client skeletal attachments.
    public static readonly NumericsVector3 MainMuzzleOffset = new(.23f, .82f, 1.52f);
    public static readonly NumericsVector3 CompanionMuzzleOffset = new(-.23f, .82f, 1.52f);

    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, float scale = 1f) =>
        ResolveMuzzle(origin, facingDegrees, true, scale);

    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, bool companion, float scale = 1f)
    {
        var offset = companion ? CompanionMuzzleOffset : MainMuzzleOffset;
        var radians = facingDegrees * MathF.PI / 180f;
        var forward = new NumericsVector3(MathF.Cos(radians), MathF.Sin(radians), 0f);
        var right = new NumericsVector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
        return origin + (right * offset.X + forward * offset.Y + NumericsVector3.UnitZ * offset.Z) * scale;
    }

    public static float FlightSeconds(float distance) => Math.Max(.02f, distance / 50f);

    public static bool ShouldPresentAbilityWeapon(bool activePair, uint weapon, uint main, uint companion) =>
        activePair && weapon != OBJECT_INVALID && (weapon == main || weapon == companion);

    public static NumericsVector3 ResolveMissDestination(NumericsVector3 muzzle, NumericsVector3 targetPosition)
    {
        var destination = targetPosition + NumericsVector3.UnitZ;
        var direction = destination - muzzle;
        var lateral = new NumericsVector3(direction.Y, -direction.X, 0f);
        return destination + (lateral.LengthSquared() > .0001f
            ? NumericsVector3.Normalize(lateral) : NumericsVector3.UnitX) * 1.5f;
    }

    /// <summary>Each source and its optional miss endpoint stay fixed until their projectile has arrived.</summary>
    public static int LeaseProjectileAnchor(long[] busyUntil, long now, float distance)
    {
        var slot = Array.FindIndex(busyUntil, until => until <= now);
        if (slot >= 0)
            busyUntil[slot] = now + (long)Math.Ceiling(FlightSeconds(distance) * 1000f) + 100;
        return slot;
    }

    /// <summary>Warm invisible client-known anchors when a valid pair is equipped.</summary>
    public static void Prepare(uint actor)
    {
        if (!GetIsObjectValid(actor) || GetObjectType(actor) != ObjectType.Creature ||
            GetIsDead(actor) || !TwinPistolEquipment.IsActivePair(actor)) return;
        var area = GetArea(actor);
        if (!GetIsObjectValid(area)) return;
        if (Anchors.TryGetValue(actor, out var existing) && existing.Area == area &&
            Array.TrueForAll(existing.Sources, GetIsObjectValid) && Array.TrueForAll(existing.MissTargets, GetIsObjectValid)) return;
        Release(actor);
        var sources = new uint[SourceAnchorCount];
        var targets = new uint[MissAnchorCount];
        for (var index = 0; index < sources.Length; index++)
        {
            sources[index] = CreateAnchor(actor);
            targets[index] = CreateAnchor(actor, MissBlueprint);
            if (GetIsObjectValid(sources[index]) && GetIsObjectValid(targets[index])) continue;
            DestroyAnchors(sources);
            DestroyAnchors(targets);
            return;
        }
        Anchors[actor] = (sources, targets, new long[SourceAnchorCount],
            Environment.TickCount64 + ReplicationWarmupMilliseconds, area);
    }

    public static void Release(uint actor)
    {
        if (!Anchors.Remove(actor, out var pool)) return;
        DestroyAnchors(pool.Sources);
        DestroyAnchors(pool.MissTargets);
    }

    public static void OnCompanionLaunch(uint actor, uint target, bool hit) => OnLaunch(actor, target, hit, true);

    /// <summary>Active abilities supply both visual hands; ordinary attacks retain their native main projectile.</summary>
    public static void OnAbilityWeaponLaunch(uint actor, uint target, uint weapon, bool hit)
    {
        var activePair = TwinPistolEquipment.IsActivePair(actor);
        var main = GetItemInSlot(InventorySlot.RightHand, actor);
        var companion = GetItemInSlot(InventorySlot.LeftHand, actor);
        if (ShouldPresentAbilityWeapon(activePair, weapon, main, companion))
            OnLaunch(actor, target, hit, weapon == companion);
    }

    private static void OnLaunch(uint actor, uint target, bool hit, bool companion)
    {
        if (!GetIsObjectValid(actor) || !GetIsObjectValid(target) || GetArea(actor) != GetArea(target)) return;
        Prepare(actor);
        if (!Anchors.TryGetValue(actor, out var pool)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, actor);
        var offhand = GetItemInSlot(InventorySlot.LeftHand, actor);
        var scale = GetObjectVisualTransform(actor, ScriptVisualTransform.Scale);
        if (!float.IsFinite(scale) || scale <= 0f) scale = 1f;
        if (float.TryParse(Get2DAString("appearance", "HEIGHT", (int)GetAppearanceType(actor)),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out var height) && height > 0f)
            scale *= height / 2f;
        var muzzle = ResolveMuzzle(GetPosition(actor), GetFacing(actor), companion, scale);
        var destination = hit ? GetPosition(target) + NumericsVector3.UnitZ
            : ResolveMissDestination(muzzle, GetPosition(target));
        var remaining = pool.ReadyAt - Environment.TickCount64;
        if (remaining > 0)
        {
            // Newly created invisible objects must reach the client before a MIRV can
            // refer to them. Normal equip/enter warmup completes before the first shot.
            AssignCommand(GetModule(), () => DelayCommand(remaining / 1000f, () =>
            {
                if (Anchors.TryGetValue(actor, out var current) && ReferenceEquals(current.Sources, pool.Sources) &&
                    current.ReadyAt == pool.ReadyAt) Launch(actor, target, hit, muzzle, destination, main, offhand);
            }));
            return;
        }
        Launch(actor, target, hit, muzzle, destination, main, offhand);
    }

    private static void Launch(uint actor, uint target, bool hit, NumericsVector3 muzzle, NumericsVector3 destination,
        uint main, uint companion)
    {
        if (!GetIsObjectValid(actor) || !GetIsObjectValid(target) ||
            !Anchors.TryGetValue(actor, out var pool) || pool.Area != GetArea(actor) ||
            GetArea(actor) != GetArea(target) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0 ||
            !TwinPistolEquipment.IsActivePair(actor) ||
            GetItemInSlot(InventorySlot.RightHand, actor) != main ||
            GetItemInSlot(InventorySlot.LeftHand, actor) != companion) return;
        // Lease sources for hits too. Moving a shared source in the same frame can retarget
        // the other hand's MIRV or a previous target in Pistol Rush before clients see it.
        var slot = LeaseProjectileAnchor(pool.BusyUntil, Environment.TickCount64,
            NumericsVector3.Distance(muzzle, destination));
        if (slot < 0) return;
        var source = pool.Sources[slot];
        if (!GetIsObjectValid(source)) return;
        var visualTarget = target;
        if (!hit)
        {
            visualTarget = pool.MissTargets[slot];
            if (!GetIsObjectValid(visualTarget)) return;
            ObjectPlugin.SetPosition(visualTarget, destination);
        }
        ObjectPlugin.SetPosition(source, muzzle);
        // The reference-backed MIRV route uses the anchor as its visual creator.
        // Combat credit and impact timing belong to the separate attack resolver.
        AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant,
            EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2), visualTarget));
        AssignCommand(actor, () => PlaySound(PistolAnimationRemap.PistolShotSound));
    }

    private static uint CreateAnchor(uint actor, string blueprint = MuzzleBlueprint)
    {
        return CreateObject(ObjectType.Placeable, blueprint, GetLocation(actor), false, MuzzleTag);
    }

    private static void DestroyAnchors(IEnumerable<uint> anchors)
    {
        foreach (var anchor in anchors)
            if (anchor != 0 && GetIsObjectValid(anchor)) DestroyObject(anchor);
    }

    [NWNEventHandler(ScriptName.OnAreaEnter)]
    public static void OnAreaEnter() => Prepare(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnAreaExit)]
    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => Release(GetExitingObject());

    [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
    public static void OnCreatureDeath() => Release(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void OnPlayerDeath() => Release(GetLastPlayerDied());

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void RemoveAbandonedAnchors()
    {
        foreach (var actor in new List<uint>(Anchors.Keys))
            if (!GetIsObjectValid(actor) || GetIsDead(actor) ||
                GetArea(actor) != Anchors[actor].Area || !TwinPistolEquipment.IsActivePair(actor))
                Release(actor);
    }
}
