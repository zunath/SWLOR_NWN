namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// What the last call through a trampoline reached: a stand-in engine function (by its entry point index) or a sink
/// method, with the arguments it received. Writing it allocates nothing, so the allocation tests can use it too. The
/// tests of this assembly run one at a time (the trampolines are process-wide).
/// </summary>
internal static class CallLog
{
    public static readonly long[] Args = new long[8];

    /// <summary>The entry point whose stand-in engine function ran last, or -1.</summary>
    public static int Original { get; private set; } = -1;

    /// <summary>The sink method that ran last, or null.</summary>
    public static string? SinkMethod { get; private set; }

    /// <summary>The engine function the sink received (<c>original</c>), or 0 for a sink that gets none.</summary>
    public static nint SinkOriginal { get; private set; }

    public static int ArgCount { get; private set; }

    /// <summary>Every call recorded since <see cref="Reset"/>.</summary>
    public static int Calls { get; private set; }

    public static void Reset()
    {
        Original = -1;
        SinkMethod = null;
        SinkOriginal = 0;
        ArgCount = 0;
        Calls = 0;
        Array.Clear(Args);
    }

    public static void EngineFunction(int entryPoint, int count, long a0 = 0, long a1 = 0, long a2 = 0, long a3 = 0,
        long a4 = 0, long a5 = 0, long a6 = 0, long a7 = 0)
    {
        Original = entryPoint;
        Store(count, a0, a1, a2, a3, a4, a5, a6, a7);
    }

    public static void Sink(string method, nint original, int count, long a0 = 0, long a1 = 0, long a2 = 0, long a3 = 0,
        long a4 = 0, long a5 = 0, long a6 = 0, long a7 = 0)
    {
        SinkMethod = method;
        SinkOriginal = original;
        Store(count, a0, a1, a2, a3, a4, a5, a6, a7);
    }

    public static long[] RecordedArgs() => Args.Take(ArgCount).ToArray();

    private static void Store(int count, long a0, long a1, long a2, long a3, long a4, long a5, long a6, long a7)
    {
        Calls++;
        ArgCount = count;
        Args[0] = a0;
        Args[1] = a1;
        Args[2] = a2;
        Args[3] = a3;
        Args[4] = a4;
        Args[5] = a5;
        Args[6] = a6;
        Args[7] = a7;
    }
}
