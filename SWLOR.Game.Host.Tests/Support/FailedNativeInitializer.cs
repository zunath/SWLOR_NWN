using System.Runtime.CompilerServices;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// NWN.Native's P/Invoke class (<c>NWN.Native.API.NWNXLibPINVOKE</c>) cannot initialize in a test process, which has no
/// NWNX library. The runtime keeps the <see cref="TypeInitializationException"/> for the life of the process and throws
/// the same object again at every later use, recording the stack of that throw in it, so it holds the code, and through
/// it the load context, of the last generation that called NWN.Native. On the server the class initializes and nothing
/// is kept. Touching the class from the test's own code moves that stack trace out of every generation.
/// </summary>
internal static class FailedNativeInitializer
{
    private static readonly Type NativeInterop =
        typeof(NWN.Native.API.NWNXLib).Assembly.GetType("NWN.Native.API.NWNXLibPINVOKE", throwOnError: true)!;

    /// <summary>Runs the failed class initializer again from this frame.</summary>
    public static void Retouch()
    {
        try
        {
            RuntimeHelpers.RunClassConstructor(NativeInterop.TypeHandle);
        }
        catch (TypeInitializationException)
        {
            // Expected in a test process; the point is the stack trace the runtime now records.
        }
    }
}
