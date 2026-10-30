using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Clients draw a native weapon bolt from a fixed point at the hands and ignore the broadcast origin,
/// which is well behind a cannon's barrel. Cannon bolts are drawn instead as MIRVs from invisible
/// anchors waiting at the muzzle, the same carrier the Twin Pistol companion uses.
/// </summary>
public static class CannonMuzzleAnchors
{
    // A three-bolt burst every attack; four sources cover a burst still in flight when the next starts.
    public const int AnchorCount = 4;
    public const string ShotSound = "cb_sh_blstrfire3";
    // Measured in the live client: the barrel end of the 1.245 m cannon models in the rifle firing pose.
    public static readonly NumericsVector3 MuzzleOffset = new(.1f, 1.25f, 1.65f);
    private const long ReplicationWarmupMilliseconds = 250;
    // Freshly moved anchors draw from their previous spot, so a source only fires once it has settled.
    private const float ParkToleranceMeters = .05f;
    private static readonly Dictionary<uint, (uint[] Sources, long[] BusyUntil, long ReadyAt, uint Area)> Anchors = new();
    private static readonly Dictionary<uint, (NumericsVector3 Position, float Facing)> LastPoses = new();

    public static bool IsCannonWielder(uint actor) =>
        GetIsObjectValid(actor) && GetObjectType(actor) == ObjectType.Creature && !GetIsDead(actor) &&
        CannonBurstPresentation.IsBurstWeapon(GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, actor)));

    /// <summary>Attack results the client draws as a hit rather than as a miss beside the target.</summary>
    public static bool IsHitResult(byte result) => result is 1 or 3 or 7 or 10;

    private static NumericsVector3 Muzzle(uint actor) =>
        PairedRangedPresentation.ResolveMuzzle(GetPosition(actor), GetFacing(actor), MuzzleOffset,
            PairedRangedPresentation.VisualScale(actor));

    public static void Prepare(uint actor)
    {
        if (!IsCannonWielder(actor)) return;
        var area = GetArea(actor);
        if (!GetIsObjectValid(area)) return;
        if (Anchors.TryGetValue(actor, out var existing) && existing.Area == area &&
            Array.TrueForAll(existing.Sources, GetIsObjectValid)) return;
        Release(actor);
        var sources = new uint[AnchorCount];
        var muzzle = Muzzle(actor);
        for (var index = 0; index < sources.Length; index++)
        {
            sources[index] = CreateObject(ObjectType.Placeable, PairedRangedPresentation.MuzzleBlueprint,
                Location(area, muzzle, 0f), false, PairedRangedPresentation.MuzzleTag);
            if (GetIsObjectValid(sources[index])) continue;
            DestroyAnchors(sources);
            return;
        }
        Anchors[actor] = (sources, new long[AnchorCount], Environment.TickCount64 + ReplicationWarmupMilliseconds, area);
        LastPoses[actor] = (GetPosition(actor), GetFacing(actor));
    }

    public static void Release(uint actor)
    {
        LastPoses.Remove(actor);
        if (!Anchors.Remove(actor, out var pool)) return;
        DestroyAnchors(pool.Sources);
    }

    /// <summary>True when the actor's anchors exist and have reached clients, so a burst can use them.</summary>
    public static bool IsReady(uint actor) =>
        Anchors.TryGetValue(actor, out var pool) && pool.Area == GetArea(actor) &&
        pool.ReadyAt <= Environment.TickCount64 && Array.TrueForAll(pool.Sources, GetIsObjectValid);

    /// <summary>Draws one bolt from a free muzzle anchor. Returns false when none is free.</summary>
    public static bool Launch(uint actor, uint target, bool hit)
    {
        if (!IsReady(actor) || !GetIsObjectValid(target) || GetArea(target) != GetArea(actor)) return false;
        var pool = Anchors[actor];
        var muzzle = Muzzle(actor);
        var slot = PairedRangedPresentation.LeaseProjectileAnchor(pool.BusyUntil, Environment.TickCount64,
            NumericsVector3.Distance(muzzle, GetPosition(target)), false);
        if (slot < 0) return false;
        var source = pool.Sources[slot];
        MoveIfNeeded(source, muzzle);
        AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant,
            EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2, !hit), target));
        AssignCommand(actor, () => PlaySound(ShotSound));
        return true;
    }

    private static void MoveIfNeeded(uint anchor, NumericsVector3 point)
    {
        if (GetIsObjectValid(anchor) && NumericsVector3.Distance(GetPosition(anchor), point) > ParkToleranceMeters)
            ObjectPlugin.SetPosition(anchor, point);
    }

    /// <summary>Moves every free source to the muzzle once the actor has held the same pose for one check.</summary>
    private static void ParkSettledActors()
    {
        var now = Environment.TickCount64;
        foreach (var actor in new List<uint>(Anchors.Keys))
        {
            if (!GetIsObjectValid(actor)) continue;
            var position = GetPosition(actor);
            var facing = GetFacing(actor);
            if (LastPoses.TryGetValue(actor, out var last) &&
                NumericsVector3.Distance(last.Position, position) < ParkToleranceMeters &&
                MathF.Abs(last.Facing - facing) < 2f && Anchors[actor].Area == GetArea(actor))
            {
                var pool = Anchors[actor];
                var muzzle = Muzzle(actor);
                for (var slot = 0; slot < pool.Sources.Length; slot++)
                    if (pool.BusyUntil[slot] <= now) MoveIfNeeded(pool.Sources[slot], muzzle);
            }
            LastPoses[actor] = (position, facing);
        }
    }

    private static void DestroyAnchors(IEnumerable<uint> anchors)
    {
        foreach (var anchor in anchors)
            if (anchor != 0 && GetIsObjectValid(anchor)) DestroyObject(anchor);
    }

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void StartParking() =>
        Scheduler.ScheduleRepeating(ParkSettledActors, TimeSpan.FromMilliseconds(500));

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnEquipmentChanged()
    {
        var actor = OBJECT_SELF;
        Scheduler.Schedule(() => AssignCommand(GetModule(), () =>
        {
            if (IsCannonWielder(actor)) Prepare(actor);
            else Release(actor);
        }), TimeSpan.FromMilliseconds(100));
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
            if (!IsCannonWielder(actor) || GetArea(actor) != Anchors[actor].Area)
                Release(actor);
    }
}
