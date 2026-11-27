using System;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// How many player files the current thread is loading right now: the player file load hook counts up around the
/// engine's load and the saved inventory hook reads it, so saved item stacks stay intact while a player file loads. The
/// count lives here, beside the trampolines, because the engine thread never exits and a thread-static value of a game
/// build would outlive the build.
/// </summary>
public static class PlayerFileLoadDepth
{
    [ThreadStatic] private static int _depth;

    /// <summary>How many player files this thread is loading right now.</summary>
    public static int Current => _depth;

    /// <summary>A player file starts loading on this thread.</summary>
    public static void Enter() => _depth++;

    /// <summary>A player file this thread was loading is done.</summary>
    public static void Exit() => _depth--;
}
