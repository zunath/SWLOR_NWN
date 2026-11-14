using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Keyboard movement sends drive paths that start at the client's own position. When a teleport lands while the
/// player holds a movement key, the client can keep driving from where it stood before the jump, and the engine
/// accepts those starts and pulls the creature back. After a teleport, a drive path that starts far from the
/// creature's server position starts from the server position instead, until the client sends a path from the
/// server position. The decision rules live in <see cref="StaleDriveAfterJump"/>; this sink holds the per-creature
/// watches.
/// </summary>
public sealed unsafe class StaleDriveAfterJumpSink : IStaleDriveAfterJumpSink
{
    private readonly Dictionary<uint, StaleDriveState> _states = new();

    public uint JumpToPoint(void* creature, void* node, nint original)
    {
        float fromX = 0f, fromY = 0f;
        var captured = false;
        try
        {
            var before = CNWSCreature.FromPointer(creature).m_vPosition;
            fromX = before.x;
            fromY = before.y;
            captured = true;
        }
        catch
        {
            // Never let a managed failure escape into the engine.
        }

        var result = ((delegate* unmanaged<void*, void*, uint>)original)(creature, node);

        if (!captured)
            return result;

        try
        {
            var native = CNWSCreature.FromPointer(creature);
            var after = native.m_vPosition;
            if (StaleDriveAfterJump.IsTeleport(fromX, fromY, after.x, after.y))
            {
                var now = Environment.TickCount64;
                foreach (var expired in _states.Where(entry => StaleDriveAfterJump.IsExpired(entry.Value, now))
                             .Select(entry => entry.Key).ToList())
                    _states.Remove(expired);
                _states[native.m_idSelf] = new StaleDriveState { JumpedAt = now };
            }
        }
        catch
        {
            // Never let a managed failure escape into the engine.
        }

        return result;
    }

    public int AddDriveAction(void* creature, ushort groupId, void* pathStart, int bearing,
        int clientPathNumber, int driveFlags, int wayPointsToGenerate, nint original)
    {
        try
        {
            var native = CNWSCreature.FromPointer(creature);
            var id = native.m_idSelf;
            if (_states.TryGetValue(id, out var state))
            {
                var start = (float*)pathStart;
                var server = native.m_vPosition;
                var gap = StaleDriveAfterJump.HorizontalDistance(start[0], start[1], server.x, server.y);
                switch (StaleDriveAfterJump.Decide(ref state, Environment.TickCount64, gap, clientPathNumber))
                {
                    case StaleDriveDecision.StartFromServer:
                        start[0] = server.x;
                        start[1] = server.y;
                        start[2] = server.z;
                        _states[id] = state;
                        break;
                    case StaleDriveDecision.Release:
                        _states.Remove(id);
                        break;
                }
            }
        }
        catch
        {
            // Never let a managed failure escape into the engine.
        }

        return ((delegate* unmanaged<void*, ushort, void*, int, int, int, int, int>)original)(creature, groupId, pathStart, bearing, clientPathNumber, driveFlags, wayPointsToGenerate);
    }
}
