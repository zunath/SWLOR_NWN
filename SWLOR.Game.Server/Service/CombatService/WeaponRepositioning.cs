using System.Collections.Generic;
using System.Linq;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector2 = System.Numerics.Vector2;
using NWN.Native.API;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Service.CombatService;

/// <summary>Short weapon movement through the engine's normal collision and movement actions.</summary>
public static unsafe class WeaponRepositioning
{
    private static readonly Dictionary<uint, WeaponRepositioningOperation> _moving = new();
    private static readonly Dictionary<uint, long> _lockouts = new();
    private const int MovementLockoutMilliseconds = 2000;
    private const int ArrivalTimeoutMilliseconds = 3000;
    /// <summary>A move counts as arrived this close to its destination.</summary>
    public const float ArrivalTolerance = 0.35f;
    // The native move cycles these base run clips; the weapon carry layers would pin the arms.
    private static readonly string[] RunAnimations = { "run", "runfl", "runfr" };
    private static readonly string[] RunCarryLayers = { "run_swordr", "run_swordl", "run_bowl", "run_shieldl" };
    /// <summary>Tags the temporary ghost effect that lets a pass-through move ignore creature collision.</summary>
    public const string PassThroughEffectTag = "WEAPON_REPOSITION_PASS_THROUGH";

    public static bool IsMoving(uint actor) => _moving.ContainsKey(actor);

    /// <param name="passThroughCreatures">
    /// Creatures along the path do not block the move, which travels as a cutscene ghost; it must still
    /// end clear of every creature body. Walls, placeables, doors and walkmesh limits apply either way.
    /// </param>
    public static string Validate(uint actor, Location origin, Location destination, float minimumDistance, float maximumDistance,
        bool passThroughCreatures = false)
    {
        if (!GetIsObjectValid(actor) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0)
            return "You cannot reposition now.";
        if (IsMovementBlocked(actor)) return "You cannot reposition while rooted or incapacitated.";
        if (IsMoving(actor) || _lockouts.TryGetValue(actor, out var until) && Environment.TickCount64 < until)
            return "Wait two seconds between movement abilities.";

        var area = GetArea(actor);
        if (area != GetAreaFromLocation(origin) || area != GetAreaFromLocation(destination))
            return "The destination must be in your current area.";
        var start = GetPositionFromLocation(origin);
        var end = GetPositionFromLocation(destination);
        var geometryError = ValidateGeometry(start, end, minimumDistance, maximumDistance);
        if (!string.IsNullOrEmpty(geometryError)) return geometryError;
        if (NumericsVector3.Distance(GetPosition(actor), start) > 0.5f) return "Your starting position changed.";

        var personalSpace = Math.Max(0.1f, CreaturePlugin.GetPersonalSpace(actor));
        var height = Math.Max(0.5f, CreaturePlugin.GetHeight(actor));
        var nativeArea = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(area)?.AsNWSArea();
        // Unlike GetPathExists, this engine query includes closed doors and placeable collision.
        if (nativeArea == null || nativeArea.TestDirectLine(start.X, start.Y, end.X, end.Y, personalSpace, height, 0) != 1)
            return "The direct movement path is blocked.";
        if (passThroughCreatures)
        {
            if (IsEndpointOccupied(actor, end)) return "The destination is occupied by another creature.";
        }
        else
        {
            // The native query can ignore friendly creatures. Explicitly preserve their occupied space too.
            for (var other = GetFirstObjectInArea(area); GetIsObjectValid(other); other = GetNextObjectInArea(area))
            {
                if (other == actor || GetObjectType(other) != ObjectType.Creature || GetIsDead(other)) continue;
                var otherPosition = GetPosition(other);
                if (Math.Abs(otherPosition.Z - start.Z) > Math.Max(height, CreaturePlugin.GetHeight(other))) continue;
                var otherRadius = Math.Max(0.1f, CreaturePlugin.GetCreaturePersonalSpace(other));
                if (IntersectsOccupiedCircle(start, end, otherPosition, personalSpace + otherRadius))
                    return "The direct movement path is blocked by another creature.";
            }
        }
        var depth = Math.Max(64, GetAreaSize(Dimension.Width, area) * GetAreaSize(Dimension.Height, area));
        if (!AreaPlugin.GetPathExists(area, start, end, depth)) return "The destination is unreachable.";

        var lastGround = GetGroundHeight(origin);
        var steps = Math.Max(1, (int)Math.Ceiling(NumericsVector2.Distance(new(start.X, start.Y), new(end.X, end.Y)) / 0.25f));
        for (var index = 1; index <= steps; index++)
        {
            var point = NumericsVector3.Lerp(start, end, index / (float)steps);
            var location = Location(area, point, 0f);
            var material = GetSurfaceMaterial(location);
            if (material < 0 || !int.TryParse(Get2DAString("surfacemat", "Walk", material), out var walkable) || walkable != 1)
                return "The destination must follow walkable ground.";
            var ground = GetGroundHeight(location);
            if (!float.IsFinite(ground) || Math.Abs(ground - lastGround) > 0.75f)
                return "The movement path crosses an unsafe elevation change.";
            lastGround = ground;
        }
        if (Math.Abs(end.Z - lastGround) > 0.75f) return "The destination must be on the ground.";

        // TestDirectLine already validates the full body corridor independently of script context.
        // Add the visual ray only when the caller already has the actor's synchronous context.
        if (OBJECT_SELF == actor && !LineOfSightVector(start + new NumericsVector3(0, 0, 0.5f), end + new NumericsVector3(0, 0, 0.5f)))
            return "The direct movement path is blocked.";
        if (CrossesTransition(area, start, end, steps)) return "Movement abilities cannot cross area transitions.";
        return string.Empty;
    }

    public static string ValidateGeometry(NumericsVector3 origin, NumericsVector3 destination, float minimumDistance, float maximumDistance)
    {
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || !float.IsFinite(origin.Z) ||
            !float.IsFinite(destination.X) || !float.IsFinite(destination.Y) || !float.IsFinite(destination.Z) ||
            origin.X < 0 || origin.Y < 0 || destination.X < 0 || destination.Y < 0 ||
            !float.IsFinite(minimumDistance) || !float.IsFinite(maximumDistance) ||
            minimumDistance < 0 || maximumDistance < minimumDistance || maximumDistance > 6f)
            return "Invalid movement destination.";
        var distance = NumericsVector2.Distance(new(origin.X, origin.Y), new(destination.X, destination.Y));
        return distance + 0.001f < minimumDistance || distance > maximumDistance + 0.001f
            ? $"Choose a destination between {minimumDistance:0.#} and {maximumDistance:0.#} meters away."
            : string.Empty;
    }

    /// <summary>True when standing at <paramref name="end"/> would overlap another living creature's body.</summary>
    public static bool IsEndpointOccupied(uint actor, NumericsVector3 end)
    {
        var area = GetArea(actor);
        var personalSpace = Math.Max(0.1f, CreaturePlugin.GetPersonalSpace(actor));
        var height = Math.Max(0.5f, CreaturePlugin.GetHeight(actor));
        for (var other = GetFirstObjectInArea(area); GetIsObjectValid(other); other = GetNextObjectInArea(area))
        {
            if (other == actor || GetObjectType(other) != ObjectType.Creature || GetIsDead(other)) continue;
            var otherPosition = GetPosition(other);
            if (Math.Abs(otherPosition.Z - end.Z) > Math.Max(height, CreaturePlugin.GetHeight(other))) continue;
            var otherRadius = Math.Max(0.1f, CreaturePlugin.GetCreaturePersonalSpace(other));
            if (NumericsVector2.Distance(new(end.X, end.Y), new(otherPosition.X, otherPosition.Y)) < personalSpace + otherRadius)
                return true;
        }
        return false;
    }

    public static bool IntersectsOccupiedCircle(NumericsVector3 origin, NumericsVector3 destination, NumericsVector3 body, float radius)
    {
        var start = new NumericsVector2(origin.X, origin.Y);
        var segment = new NumericsVector2(destination.X, destination.Y) - start;
        var position = new NumericsVector2(body.X, body.Y);
        var projection = NumericsVector2.Dot(position - start, segment);
        // Allow moving away from an already overlapping body, never through it.
        if (projection <= 0 && NumericsVector2.Distance(start, position) < radius) return false;
        var fraction = segment.LengthSquared() > 0 ? Math.Clamp(projection / segment.LengthSquared(), 0f, 1f) : 0f;
        return NumericsVector2.Distance(position, start + fraction * segment) < radius;
    }

    /// <param name="travelAnimation">Authored clip shown instead of running for this move, or null for the native run.</param>
    /// <param name="passThroughCreatures">Travel as a cutscene ghost so creatures on the path do not block it.</param>
    public static void Begin(uint actor, Location destination, Action<bool, float> onArrival, string travelAnimation = null,
        bool passThroughCreatures = false)
    {
        var validation = Validate(actor, GetLocation(actor), destination, 0f, 6f, passThroughCreatures);
        if (!string.IsNullOrEmpty(validation)) { onArrival(false, 0f); return; }
        var operation = new WeaponRepositioningOperation
        {
            Actor = actor, Area = GetArea(actor), Origin = GetPosition(actor), Destination = GetPositionFromLocation(destination),
            MainHand = GetItemInSlot(InventorySlot.RightHand, actor), OffHand = GetItemInSlot(InventorySlot.LeftHand, actor),
            Started = Environment.TickCount64, OnArrival = onArrival, LastPosition = GetPosition(actor),
            TravelAnimation = travelAnimation, PassesThroughCreatures = passThroughCreatures,
        };
        _moving[actor] = operation;
        foreach (var expired in _lockouts.Where(pair => pair.Value <= operation.Started && !_moving.ContainsKey(pair.Key))
                     .Select(pair => pair.Key).ToArray()) _lockouts.Remove(expired);
        _lockouts[actor] = operation.Started + MovementLockoutMilliseconds;
        AssignCommand(actor, () =>
        {
            // AssignCommand may run after a stop/equipment callback cancelled this operation.
            if (!_moving.TryGetValue(actor, out var current) || current.Id != operation.Id) return;
            ClearAllActions();
            ApplyTravelAnimation(operation);
            ApplyPassThrough(operation);
            ActionMoveToLocation(Location(operation.Area, operation.Destination, GetFacing(actor)), true);
            operation.ActionGroup = GetActionGroup(actor);
            ActionDoCommand(() => Finish(operation, IsCurrentAndValid(operation) && IsInsideCapturedCorridor(operation) &&
                NumericsVector3.Distance(GetPosition(actor), operation.Destination) <= ArrivalTolerance, false));
        });
        DelayCommand(0.1f, () => Poll(operation));
    }

    public static void Cancel(uint actor)
    {
        if (_moving.TryGetValue(actor, out var operation)) Finish(operation, false, true);
    }

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit()
    {
        var actor = GetExitingObject();
        Cancel(actor);
        _lockouts.Remove(actor);
    }

    // A destroyed mover never arrives: report the failed arrival so the ability releases its deferred
    // stamina context, without touching the destroyed object.
    [NWNEventHandler(ScriptName.OnObjectDestroyed)]
    public static void OnDestroyed()
    {
        var actor = OBJECT_SELF;
        _lockouts.Remove(actor);
        if (_moving.Remove(actor, out var operation)) operation.OnArrival(false, 0f);
    }

    private static void Poll(WeaponRepositioningOperation operation)
    {
        if (!_moving.TryGetValue(operation.Actor, out var current) || current.Id != operation.Id) return;
        if (!IsCurrentAndValid(operation)) { Finish(operation, false, true); return; }
        if (!IsInsideCapturedCorridor(operation)) { Finish(operation, false, true); return; }
        var elapsed = Environment.TickCount64 - operation.Started;
        if (elapsed >= ArrivalTimeoutMilliseconds) { Finish(operation, false, true); return; }
        var action = GetCurrentAction(operation.Actor);
        var group = GetActionGroup(operation.Actor);
        if (action == ActionType.MoveToPoint)
        {
            if (operation.ActionGroup.HasValue && group != operation.ActionGroup) { Finish(operation, false, false); return; }
            operation.ActionGroup = group;
        }
        else if (elapsed > 250 && NumericsVector3.Distance(GetPosition(operation.Actor), operation.Destination) > ArrivalTolerance)
        {
            Finish(operation, false, true);
            return;
        }
        DelayCommand(0.1f, () => Poll(operation));
    }

    private static bool IsCurrentAndValid(WeaponRepositioningOperation operation) =>
        _moving.TryGetValue(operation.Actor, out var current) && current.Id == operation.Id &&
        GetIsObjectValid(operation.Actor) && !GetIsDead(operation.Actor) && GetCurrentHitPoints(operation.Actor) > 0 &&
        GetArea(operation.Actor) == operation.Area && !IsMovementBlocked(operation.Actor) &&
        GetItemInSlot(InventorySlot.RightHand, operation.Actor) == operation.MainHand &&
        GetItemInSlot(InventorySlot.LeftHand, operation.Actor) == operation.OffHand;

    private static void Finish(WeaponRepositioningOperation operation, bool arrived, bool stopOwnedMove)
    {
        if (!_moving.TryGetValue(operation.Actor, out var current) || current.Id != operation.Id) return;
        _moving.Remove(operation.Actor);
        RestoreTravelAnimation(operation);
        RemovePassThrough(operation.Actor);
        if (stopOwnedMove && GetIsObjectValid(operation.Actor) && operation.ActionGroup.HasValue &&
            GetActionGroup(operation.Actor) == operation.ActionGroup)
            AssignCommand(operation.Actor, () =>
            {
                if (GetActionGroup(operation.Actor) == operation.ActionGroup) ClearAllActions();
            });
        var distance = GetIsObjectValid(operation.Actor) ? NumericsVector2.Distance(
            new(operation.Origin.X, operation.Origin.Y), new(GetPosition(operation.Actor).X, GetPosition(operation.Actor).Y)) : 0f;
        operation.OnArrival(arrived, distance);
    }

    private static void ApplyTravelAnimation(WeaponRepositioningOperation operation)
    {
        if (string.IsNullOrEmpty(operation.TravelAnimation)) return;
        foreach (var name in RunAnimations) ReplaceObjectAnimation(operation.Actor, name, operation.TravelAnimation);
        foreach (var name in RunCarryLayers) ReplaceObjectAnimation(operation.Actor, name, NamedAnimationPlayback.NoHoldName);
    }

    private static void RestoreTravelAnimation(WeaponRepositioningOperation operation)
    {
        if (string.IsNullOrEmpty(operation.TravelAnimation) || !GetIsObjectValid(operation.Actor)) return;
        foreach (var name in RunAnimations) ReplaceObjectAnimation(operation.Actor, name);
        foreach (var name in RunCarryLayers) ReplaceObjectAnimation(operation.Actor, name);
    }

    private static void ApplyPassThrough(WeaponRepositioningOperation operation)
    {
        if (!operation.PassesThroughCreatures) return;
        // The arrival timeout ends the move well before this lapses; Finish removes it on every outcome.
        var ghost = TagEffect(SupernaturalEffect(EffectCutsceneGhost()), PassThroughEffectTag);
        ApplyEffectToObject(DurationType.Temporary, ghost, operation.Actor, ArrivalTimeoutMilliseconds / 1000f + 1f);
    }

    private static void RemovePassThrough(uint actor)
    {
        if (!GetIsObjectValid(actor)) return;
        for (var effect = GetFirstEffect(actor); GetIsEffectValid(effect); effect = GetNextEffect(actor))
            if (GetEffectTag(effect) == PassThroughEffectTag) RemoveEffect(actor, effect);
    }

    private static bool IsInsideCapturedCorridor(WeaponRepositioningOperation operation)
    {
        var position = GetPosition(operation.Actor);
        operation.DistanceTravelled += NumericsVector2.Distance(new(operation.LastPosition.X, operation.LastPosition.Y), new(position.X, position.Y));
        operation.LastPosition = position;
        var origin = new NumericsVector2(operation.Origin.X, operation.Origin.Y);
        var destination = new NumericsVector2(operation.Destination.X, operation.Destination.Y);
        var current = new NumericsVector2(position.X, position.Y);
        var segment = destination - origin;
        var fraction = segment.LengthSquared() > 0 ? Math.Clamp(NumericsVector2.Dot(current - origin, segment) / segment.LengthSquared(), 0f, 1f) : 0f;
        return operation.DistanceTravelled <= 6.35f && NumericsVector2.Distance(current, origin + segment * fraction) <= 0.75f;
    }

    private static ushort? GetActionGroup(uint actor)
    {
        var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor)?.AsNWSCreature();
        return creature == null || creature.m_lQueuedActions.IsEmpty() != 0 ? null : creature.m_lQueuedActions.GetHead().m_nGroupActionId;
    }

    private static bool IsMovementBlocked(uint actor)
    {
        if (StatusEffect.HasStatusEffectCategory(actor, StatusEffectCategory.Incapacitating)) return true;
        for (var effect = GetFirstEffect(actor); GetIsEffectValid(effect); effect = GetNextEffect(actor))
            if (GetEffectType(effect) is EffectTypeScript.CutsceneImmobilize or EffectTypeScript.Entangle or
                EffectTypeScript.Paralyze or EffectTypeScript.CutsceneParalyze or EffectTypeScript.Stunned or
                EffectTypeScript.Sleep or EffectTypeScript.Petrify or EffectTypeScript.Dazed) return true;
        return false;
    }

    private static bool CrossesTransition(uint area, NumericsVector3 start, NumericsVector3 end, int steps)
    {
        for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
        {
            var type = GetObjectType(obj);
            if (type is not (ObjectType.Trigger or ObjectType.Door) || !GetIsObjectValid(GetTransitionTarget(obj))) continue;
            for (var index = 0; index <= steps; index++)
            {
                var point = NumericsVector3.Lerp(start, end, index / (float)steps);
                if (type == ObjectType.Trigger ? ObjectPlugin.GetPositionIsInTrigger(obj, point) : NumericsVector3.Distance(point, GetPosition(obj)) < 1f)
                    return true;
            }
        }
        return false;
    }
}
