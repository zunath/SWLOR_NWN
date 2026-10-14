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

/// <summary>Cosmetic companion launch; never resolves damage, ammunition, traits or progression.</summary>
public static class PairedRangedPresentation
{
    public const string MuzzleBlueprint = "tp_muzzle";
    public const string MissBlueprint = "tp_miss";
    public const string MuzzleTag = "paired_pistol_muzzle";
    public const int MissAnchorCount = 8;
    private const long ReplicationWarmupMilliseconds = 250;
    private static readonly Dictionary<uint, (uint Source, uint[] MissTargets, long[] BusyUntil,
        long ReadyAt, uint Area)> Anchors = new();

    // The authored firing pose and registered 11/201/11 pistol appearance define
    // this approximation; the server cannot evaluate client skeletal attachments.
    public static readonly NumericsVector3 CompanionMuzzleOffset = new(-.23f, .82f, 1.52f);

    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, float scale = 1f)
    {
        var radians = facingDegrees * MathF.PI / 180f;
        var forward = new NumericsVector3(MathF.Cos(radians), MathF.Sin(radians), 0f);
        var right = new NumericsVector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
        return origin + (right * CompanionMuzzleOffset.X + forward * CompanionMuzzleOffset.Y +
                         NumericsVector3.UnitZ * CompanionMuzzleOffset.Z) * scale;
    }

    public static float FlightSeconds(float distance) => Math.Max(.02f, distance / 50f);

    /// <summary>Warm invisible client-known anchors when a valid pair is equipped.</summary>
    public static void Prepare(uint actor)
    {
        if (!GetIsObjectValid(actor) || GetObjectType(actor) != ObjectType.Creature ||
            GetIsDead(actor) || !TwinPistolEquipment.IsActivePair(actor)) return;
        var area = GetArea(actor);
        if (!GetIsObjectValid(area)) return;
        if (Anchors.TryGetValue(actor, out var existing) && existing.Area == area &&
            GetIsObjectValid(existing.Source) && Array.TrueForAll(existing.MissTargets, GetIsObjectValid)) return;
        Release(actor);
        var source = CreateAnchor(actor);
        if (!GetIsObjectValid(source)) return;
        var targets = new uint[MissAnchorCount];
        for (var index = 0; index < targets.Length; index++)
        {
            targets[index] = CreateAnchor(actor, MissBlueprint);
            if (GetIsObjectValid(targets[index])) continue;
            DestroyObject(source);
            foreach (var target in targets)
                if (target != 0 && GetIsObjectValid(target)) DestroyObject(target);
            return;
        }
        Anchors[actor] = (source, targets, new long[MissAnchorCount],
            Environment.TickCount64 + ReplicationWarmupMilliseconds, area);
    }

    public static void Release(uint actor)
    {
        if (!Anchors.Remove(actor, out var pool)) return;
        if (GetIsObjectValid(pool.Source)) DestroyObject(pool.Source);
        foreach (var target in pool.MissTargets)
            if (GetIsObjectValid(target)) DestroyObject(target);
    }

    public static void OnCompanionLaunch(uint actor, uint target, bool hit)
    {
        if (!GetIsObjectValid(actor) || !GetIsObjectValid(target) || GetArea(actor) != GetArea(target)) return;
        Prepare(actor);
        if (!Anchors.TryGetValue(actor, out var pool)) return;
        var remaining = pool.ReadyAt - Environment.TickCount64;
        if (remaining > 0)
        {
            // Newly created invisible objects must reach the client before a MIRV can
            // refer to them. Normal equip/enter warmup completes before the first shot.
            AssignCommand(GetModule(), () => DelayCommand(remaining / 1000f, () =>
            {
                if (Anchors.TryGetValue(actor, out var current) && current.Source == pool.Source &&
                    current.ReadyAt == pool.ReadyAt) Launch(actor, target, hit);
            }));
            return;
        }
        Launch(actor, target, hit);
    }

    private static void Launch(uint actor, uint target, bool hit)
    {
        if (!GetIsObjectValid(actor) || !GetIsObjectValid(target) ||
            !Anchors.TryGetValue(actor, out var pool) || pool.Area != GetArea(actor) ||
            GetArea(actor) != GetArea(target) || !GetIsObjectValid(pool.Source)) return;
        var scale = GetObjectVisualTransform(actor, ScriptVisualTransform.Scale);
        if (!float.IsFinite(scale) || scale <= 0f) scale = 1f;
        if (float.TryParse(Get2DAString("appearance", "HEIGHT", (int)GetAppearanceType(actor)),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out var height) && height > 0f)
            scale *= height / 2f;
        var muzzle = ResolveMuzzle(GetPosition(actor), GetFacing(actor), scale);
        var destination = GetPosition(target) + NumericsVector3.UnitZ;
        var visualTarget = target;
        if (!hit)
        {
            var direction = destination - muzzle;
            var lateral = new NumericsVector3(direction.Y, -direction.X, 0f);
            if (lateral.LengthSquared() > .0001f) destination += NumericsVector3.Normalize(lateral) * 1.5f;
            var now = Environment.TickCount64;
            var slot = Array.FindIndex(pool.BusyUntil, until => until <= now);
            // Cosmetic saturation cannot retarget an existing projectile or create
            // unbounded objects. Eight leased endpoints cover simultaneous Rush shots.
            if (slot < 0) return;
            visualTarget = pool.MissTargets[slot];
            if (!GetIsObjectValid(visualTarget)) return;
            pool.BusyUntil[slot] = now + (long)Math.Ceiling(FlightSeconds(
                NumericsVector3.Distance(muzzle, destination)) * 1000f) + 100;
            ObjectPlugin.SetPosition(visualTarget, destination);
        }
        ObjectPlugin.SetPosition(pool.Source, muzzle);
        // The reference-backed MIRV route uses the anchor as its visual creator.
        // Combat credit and impact timing belong to the separate attack resolver.
        AssignCommand(pool.Source, () => ApplyEffectToObject(DurationType.Instant,
            EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2), visualTarget));
        AssignCommand(actor, () => PlaySound(PistolAnimationRemap.PistolShotSound));
    }

    private static uint CreateAnchor(uint actor, string blueprint = MuzzleBlueprint)
    {
        return CreateObject(ObjectType.Placeable, blueprint, GetLocation(actor), false, MuzzleTag);
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
