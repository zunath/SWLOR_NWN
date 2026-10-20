using NUnit.Framework;

namespace SWLOR.Toolset.Tests.Support;

/// <summary>
/// Some tests make a file operation fail by holding the file open without sharing, or check that a
/// lease blocks a competing writer. Only Windows enforces share modes. Unix file locks are advisory,
/// so another handle can still write, rename, or delete the file there.
/// </summary>
internal static class WindowsFileSharing
{
    public static bool IsEnforced => OperatingSystem.IsWindows();

    public static void Require()
    {
        if (!IsEnforced)
            Assert.Ignore("Relies on Windows file share modes, which Unix does not enforce.");
    }
}
