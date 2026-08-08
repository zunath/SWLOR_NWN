using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Shell.Views;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What the script-slot picker says about a row.
    /// </summary>
    /// <remarks>
    /// The list used to enumerate <c>.nss</c> only, even though <c>ScriptExists</c> treats a
    /// compiled <c>.ncs</c> with no source as a real executable script - and the module has many.
    /// A slot naming one was reported as pointing at a script that does not exist, and no other slot
    /// could be pointed at it through the browse UI at all. Marked rather than hidden: the builder
    /// can select it, and only opening it is unavailable.
    /// </remarks>
    [TestFixture]
    public class ScriptPickerRowTests
    {
        [Test]
        public void ACompiledOnlyScriptSaysSo()
        {
            var row = new ScriptPickerRow(
                "x2_def_onblocked", "x2_def_onblocked", isInclude: false, usageCount: 3, hasSource: false);

            row.HasSource.Should().BeFalse();
            row.Note.Should().Be("compiled only");
        }

        /// <summary>
        /// "compiled only" outranks the usage count, because it is the fact that changes what the
        /// builder can do with the row.
        /// </summary>
        [Test]
        public void ASourcedScriptKeepsItsUsageCount()
        {
            var row = new ScriptPickerRow("my_script", "my_script", isInclude: false, usageCount: 3);

            row.HasSource.Should().BeTrue();
            row.Note.Should().Be("used by 3");
        }

        [Test]
        public void AnIncludeIsStillLabelledAnInclude()
        {
            var row = new ScriptPickerRow("my_inc", "my_inc", isInclude: true, usageCount: 12);

            row.Note.Should().Be("include");
        }

        [Test]
        public void AnUnusedScriptSaysNothingAtAll()
        {
            var row = new ScriptPickerRow("lonely", "lonely", isInclude: false, usageCount: 0);

            row.Note.Should().BeEmpty();
        }
    }
}
