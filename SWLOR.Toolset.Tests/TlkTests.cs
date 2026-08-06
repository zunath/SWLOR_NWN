using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="TlkJsonFile"/> and <see cref="TlkService"/>: known entries from
    /// sw_tlk.tlk.json come back verbatim, and the 16777216 custom/base strref boundary resolves
    /// to the custom TLK above the boundary and returns null below it when no base dialog.tlk was
    /// supplied (no NWN install is assumed present in this test environment).
    /// </summary>
    public class TlkTests
    {
        private static string HaksDirectory
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (Directory.Exists(candidate))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository SWLOR_Haks directory from the test context.");
            }
        }

        private static string SwTlkJsonPath => Path.Combine(HaksDirectory, "sw_tlk", "sw_tlk.tlk.json");

        [Test]
        public void TlkJsonFile_Load_ParsesLanguageAndKnownEntries()
        {
            var tlk = TlkJsonFile.Load(SwTlkJsonPath);

            tlk.Language.Should().Be(0);

            // From SWLOR_Haks/sw_tlk/sw_tlk.tlk.json:
            //   id 0 -> "Bad Strref", id 1 -> "Tough", id 2 -> "Tough Heroes"
            tlk.GetText(0).Should().Be("Bad Strref");
            tlk.GetText(1).Should().Be("Tough");
            tlk.GetText(2).Should().Be("Tough Heroes");
        }

        [Test]
        public void TlkJsonFile_GetText_UnknownId_ReturnsNull()
        {
            var tlk = TlkJsonFile.Load(SwTlkJsonPath);

            tlk.GetText(-1).Should().BeNull();
            tlk.GetText(int.MaxValue).Should().BeNull();
        }

        [Test]
        public void TlkJsonFile_EntryIds_AreSparse()
        {
            var tlk = TlkJsonFile.Load(SwTlkJsonPath);

            // The corpus's max entry id is far larger than its entry count, confirming ids are
            // not contiguous from 0 - a plain array-by-index store would waste memory/be wrong.
            tlk.Count.Should().BeGreaterThan(1000);
            tlk.GetText(192552).Should().NotBeNull();
        }

        [Test]
        public void TlkService_CustomStrref_ResolvesAboveBoundary()
        {
            var service = TlkService.Load(SwTlkJsonPath);

            // 16777216 + 1 -> custom entry id 1 -> "Tough"
            service.GetString(TlkService.CustomTlkBase + 1).Should().Be("Tough");
            service.GetCustomText(1).Should().Be("Tough");
        }

        [Test]
        public void TlkService_BaseStrref_ReturnsNull_WhenNoBaseTlkProvided()
        {
            var service = TlkService.Load(SwTlkJsonPath);

            // Below the 16777216 boundary with no dialog.tlk supplied -> null, not a throw.
            service.GetString(0).Should().BeNull();
            service.GetString(TlkService.CustomTlkBase - 1).Should().BeNull();
        }

        [Test]
        public void TlkService_UnreadableOptionalBase_DoesNotHideCustomText()
        {
            var service = TlkService.LoadWithOptionalBase(
                SwTlkJsonPath,
                Path.Combine(Path.GetTempPath(), "missing-dialog-" + Guid.NewGuid().ToString("N") + ".tlk"),
                out var warning);

            service.GetString(TlkService.CustomTlkBase + 1).Should().Be("Tough");
            service.GetString(1).Should().BeNull();
            warning.Should().Contain("optional base-game dialog.tlk");
        }

        [Test]
        public void TlkService_DeferredLoad_DoesNotReadFilesUntilFirstLookup()
        {
            var missingCustom = Path.Combine(
                Path.GetTempPath(), "missing-custom-" + Guid.NewGuid().ToString("N") + ".json");

            var service = TlkService.LoadDeferredWithOptionalBase(missingCustom, baseTlkPath: null);

            Action firstLookup = () => service.GetCustomText(0);
            firstLookup.Should().Throw<FileNotFoundException>(
                "constructing the service must not put TLK parsing on the interactive startup path");
        }
    }
}
