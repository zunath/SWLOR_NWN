// SPDX-License-Identifier: MIT

using NUnit.Framework;

namespace SWLOR.NWN.Formats.Corpus.Tests;

/// <summary>
/// Skips every licensed-corpus test on machines that do not have the licensed assets (an NWN:EE
/// install and an initialized SWLOR_Haks checkout), so an unfiltered
/// <c>dotnet test SWLOR.Game.Server.sln</c> stays green on ordinary developer and CI machines.
/// Corpus-evidence runs must not skip silently: <c>tools\UpdateNwnFormatsCorpusBaseline.ps1</c>
/// sets <c>SWLOR_REQUIRE_LICENSED_CORPUS=1</c>, which turns missing assets into a hard failure.
/// </summary>
[SetUpFixture]
public sealed class CorpusAvailabilityGate
{
    [OneTimeSetUp]
    public void RequireOrSkip()
    {
        string? missing = null;
        try
        {
            _ = LicensedCorpus.HaksRoot;
            _ = LicensedCorpus.InstallRoot;
        }
        catch (DirectoryNotFoundException exception)
        {
            missing = exception.Message;
        }

        if (missing == null)
            return;

        if (Environment.GetEnvironmentVariable("SWLOR_REQUIRE_LICENSED_CORPUS") == "1")
        {
            throw new InvalidOperationException(
                $"SWLOR_REQUIRE_LICENSED_CORPUS=1 but the licensed corpus is unavailable: {missing}");
        }

        Assert.Ignore($"Licensed corpus assets are not present on this machine: {missing}");
    }
}
