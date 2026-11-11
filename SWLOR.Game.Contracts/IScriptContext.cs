using System;

namespace SWLOR.Game.Contracts;

/// <summary>
/// How game code enters the NWScript virtual machine from a native hook or the engine thread: run an action with a
/// given object as <c>OBJECT_SELF</c> and read where the VM stands. The host builds it on the package's
/// <c>CoreFunctionHandler</c>; the game's own <c>ScriptExecutor</c> does the same job today.
/// </summary>
public interface IScriptContext
{
    /// <summary>The object scripts currently run as (<c>OBJECT_SELF</c>); the invalid object id when none does.</summary>
    uint ObjectSelf { get; }

    /// <summary>The VM's recursion level; -1 while no script runs.</summary>
    int RecursionLevel { get; }

    /// <summary>The VM run-time stack pointer. It must be the same before and after <see cref="Execute"/>.</summary>
    int StackPointer { get; }

    /// <summary>
    /// Runs <paramref name="action"/> inside a script context whose <c>OBJECT_SELF</c> is <paramref name="objectSelf"/>,
    /// then restores the previous one.
    /// </summary>
    /// <param name="action">The work to run.</param>
    /// <param name="objectSelf">The object id to run as.</param>
    /// <param name="scriptEventId">The script event id the VM script frame records, or 0.</param>
    void Execute(Action action, uint objectSelf, int scriptEventId);

    /// <summary>The same as <see cref="Execute(Action, uint, int)"/>, returning what <paramref name="action"/> returns.</summary>
    T Execute<T>(Func<T> action, uint objectSelf, int scriptEventId);
}
