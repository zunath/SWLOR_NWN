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
    public const string MuzzleTag = "paired_pistol_muzzle";
    // Rush, the other two instant recast groups, a finishing capstone and a pending
    // three-shot ordinary batch can overlap fifteen cosmetic projectiles.
    public const int SourceAnchorCount = 16;
    private const long ReplicationWarmupMilliseconds = 250;
    // Clients apply a moved anchor late and draw a bolt sent in the same frame from its previous
    // spot, such as where the actor stood earlier. Free anchors therefore wait at the hand they
    // serve (even slots at the main muzzle, odd slots at the companion muzzle) before they fire.
    private const float ParkToleranceMeters = .05f;
    private static readonly Dictionary<uint, (uint[] Sources, long[] BusyUntil, long ReadyAt, uint Area)> Anchors = new();
    private static readonly Dictionary<uint, (NumericsVector3 Position, float Facing)> LastPoses = new();
    private static readonly Dictionary<uint, int> LaunchCounts = new();

    // The authored firing pose and registered 11/201/11 pistol appearance define
    // this approximation; the server cannot evaluate client skeletal attachments.
    public static readonly NumericsVector3 MainMuzzleOffset = new(.23f, .82f, 1.52f);
    public static readonly NumericsVector3 CompanionMuzzleOffset = new(-.23f, .82f, 1.52f);

    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, float scale = 1f) =>
        ResolveMuzzle(origin, facingDegrees, true, scale);

    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, bool companion, float scale = 1f) =>
        ResolveMuzzle(origin, facingDegrees, companion ? CompanionMuzzleOffset : MainMuzzleOffset, scale);

    /// <summary>Places a body-relative offset (right, forward, up) at the actor's position and facing.</summary>
    public static NumericsVector3 ResolveMuzzle(NumericsVector3 origin, float facingDegrees, NumericsVector3 offset, float scale)
    {
        var radians = facingDegrees * MathF.PI / 180f;
        var forward = new NumericsVector3(MathF.Cos(radians), MathF.Sin(radians), 0f);
        var right = new NumericsVector3(MathF.Sin(radians), -MathF.Cos(radians), 0f);
        return origin + (right * offset.X + forward * offset.Y + NumericsVector3.UnitZ * offset.Z) * scale;
    }

    public static float FlightSeconds(float distance) => Math.Max(.02f, distance / 50f);

    public static bool ShouldPresentAbilityWeapon(bool activePair, uint weapon, uint main, uint companion) =>
        activePair && weapon != OBJECT_INVALID && (weapon == main || weapon == companion);

    public static bool IsCompanionSlot(int slot) => slot % 2 == 1;

    public static NumericsVector3 ResolveSlotMuzzle(NumericsVector3 origin, float facingDegrees, int slot, float scale = 1f) =>
        ResolveMuzzle(origin, facingDegrees, IsCompanionSlot(slot), scale);

    /// <summary>
    /// Each source stays fixed until its projectile has arrived. A free slot already waiting at
    /// the firing hand is preferred; any other free slot has to move in the launch frame.
    /// </summary>
    public static int LeaseProjectileAnchor(long[] busyUntil, long now, float distance, bool companion = true)
    {
        var slot = -1;
        for (var index = 0; index < busyUntil.Length && slot < 0; index++)
            if (busyUntil[index] <= now && IsCompanionSlot(index) == companion) slot = index;
        if (slot < 0) slot = Array.FindIndex(busyUntil, until => until <= now);
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
            Array.TrueForAll(existing.Sources, GetIsObjectValid)) return;
        Release(actor);
        var sources = new uint[SourceAnchorCount];
        var position = GetPosition(actor);
        var facing = GetFacing(actor);
        var scale = VisualScale(actor);
        for (var index = 0; index < sources.Length; index++)
        {
            sources[index] = CreateObject(ObjectType.Placeable, MuzzleBlueprint,
                Location(area, ResolveSlotMuzzle(position, facing, index, scale), 0f), false, MuzzleTag);
            if (GetIsObjectValid(sources[index])) continue;
            DestroyAnchors(sources);
            return;
        }
        Anchors[actor] = (sources, new long[SourceAnchorCount],
            Environment.TickCount64 + ReplicationWarmupMilliseconds, area);
        LastPoses[actor] = (position, facing);
    }

    /// <summary>Bolts presented for the actor since its pool was prepared.</summary>
    public static int GetLaunchCount(uint actor) => LaunchCounts.GetValueOrDefault(actor);

    public static void Release(uint actor)
    {
        LastPoses.Remove(actor);
        LaunchCounts.Remove(actor);
        if (!Anchors.Remove(actor, out var pool)) return;
        DestroyAnchors(pool.Sources);
    }

    /// <summary>Moves every free source to the muzzle its slot serves, ahead of the shots that use it.</summary>
    public static void Park(uint actor)
    {
        if (!Anchors.TryGetValue(actor, out var pool) || pool.Area != GetArea(actor)) return;
        var now = Environment.TickCount64;
        var position = GetPosition(actor);
        var facing = GetFacing(actor);
        var scale = VisualScale(actor);
        for (var slot = 0; slot < pool.Sources.Length; slot++)
            if (pool.BusyUntil[slot] <= now)
                MoveIfNeeded(pool.Sources[slot], ResolveSlotMuzzle(position, facing, slot, scale));
    }

    private static void MoveIfNeeded(uint anchor, NumericsVector3 point)
    {
        if (GetIsObjectValid(anchor) && NumericsVector3.Distance(GetPosition(anchor), point) > ParkToleranceMeters)
            ObjectPlugin.SetPosition(anchor, point);
    }

    public static float VisualScale(uint actor)
    {
        var scale = GetObjectVisualTransform(actor, ScriptVisualTransform.Scale);
        if (!float.IsFinite(scale) || scale <= 0f) scale = 1f;
        if (float.TryParse(Get2DAString("appearance", "HEIGHT", (int)GetAppearanceType(actor)),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out var height) && height > 0f)
            scale *= height / 2f;
        return scale;
    }

    /// <summary>Parks an actor's anchors once it has held the same position and facing for one check.</summary>
    private static void ParkSettledActors()
    {
        foreach (var actor in new List<uint>(Anchors.Keys))
        {
            if (!GetIsObjectValid(actor)) continue;
            var position = GetPosition(actor);
            var facing = GetFacing(actor);
            if (LastPoses.TryGetValue(actor, out var last) &&
                NumericsVector3.Distance(last.Position, position) < ParkToleranceMeters &&
                MathF.Abs(last.Facing - facing) < 2f)
                Park(actor);
            LastPoses[actor] = (position, facing);
        }
    }

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void StartParking() =>
        Scheduler.ScheduleRepeating(ParkSettledActors, TimeSpan.FromMilliseconds(500));

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
        var muzzle = ResolveMuzzle(GetPosition(actor), GetFacing(actor), companion, VisualScale(actor));
        var destination = GetPosition(target) + NumericsVector3.UnitZ;
        var remaining = pool.ReadyAt - Environment.TickCount64;
        if (remaining > 0)
        {
            // Newly created invisible objects must reach the client before a MIRV can
            // refer to them. Normal equip/enter warmup completes before the first shot.
            AssignCommand(GetModule(), () => DelayCommand(remaining / 1000f, () =>
            {
                if (Anchors.TryGetValue(actor, out var current) && ReferenceEquals(current.Sources, pool.Sources) &&
                    current.ReadyAt == pool.ReadyAt) Launch(actor, target, hit, companion, muzzle, destination, main, offhand);
            }));
            return;
        }
        Launch(actor, target, hit, companion, muzzle, destination, main, offhand);
    }

    private static void Launch(uint actor, uint target, bool hit, bool companionHand, NumericsVector3 muzzle,
        NumericsVector3 destination, uint main, uint companion)
    {
        if (!GetIsObjectValid(actor) || !GetIsObjectValid(target) ||
            !Anchors.TryGetValue(actor, out var pool) || pool.Area != GetArea(actor) ||
            GetArea(actor) != GetArea(target) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0 ||
            !TwinPistolEquipment.IsActivePair(actor) ||
            GetItemInSlot(InventorySlot.RightHand, actor) != main ||
            GetItemInSlot(InventorySlot.LeftHand, actor) != companion) return;
        // Lease sources for hits too. Moving a shared source in the same frame can retarget
        // the other hand's MIRV or a previous target of the same volley before clients see it.
        var slot = LeaseProjectileAnchor(pool.BusyUntil, Environment.TickCount64,
            NumericsVector3.Distance(muzzle, destination), companionHand);
        if (slot < 0) return;
        var source = pool.Sources[slot];
        if (!GetIsObjectValid(source)) return;
        MoveIfNeeded(source, muzzle);
        // The reference-backed MIRV route uses the anchor as its visual creator, and the engine's
        // miss variant picks a point beside the target. Combat credit and impact timing belong to
        // the separate attack resolver.
        AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant,
            EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2, !hit), target));
        LaunchCounts[actor] = GetLaunchCount(actor) + 1;
        AssignCommand(actor, () => PlaySound(PistolAnimationRemap.PistolShotSound));
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
