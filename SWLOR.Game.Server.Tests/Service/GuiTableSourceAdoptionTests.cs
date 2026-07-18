using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Ratchet guarding the GuiTableSource migration.
///
/// A ViewModel that builds several GuiBindingList instances by hand and appends to them
/// in lockstep has nothing keeping those lists the same length; a missed Add in one branch
/// silently misaligns every column to its right. GuiTableSource collapses that into one
/// row-DTO list so the columns cannot drift.
///
/// Every table-shaped ViewModel has been migrated. This test stops new ones from
/// reintroducing the hand-synced pattern: a ViewModel that constructs two or more
/// GuiBindingList instances must either use GuiTableSource, or be listed in
/// NonTabularViewModels below with a reason.
///
/// See SWLOR.Game.Server/Feature/GuiDefinition/Component/GuiTable.cs and
/// SWLOR.Game.Server/Readmes/Builders.md.
/// </summary>
public class GuiTableSourceAdoptionTests
{
    /// <summary>
    /// ViewModels that build multiple GuiBindingList instances without being a lockstep
    /// table. Add an entry here only when the lists are genuinely independent — differing
    /// lengths, built in separate loops, or mutated one row at a time by user actions.
    /// If the lists are appended together once per row, migrate to GuiTableSource instead.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> NonTabularViewModels =
        new Dictionary<string, string>
        {
            ["DebugNuiGalleryViewModel.cs"] =
                "Widget gallery: deliberately exercises raw NUI widgets, including the " +
                "hand-rolled list shapes, as boot-time layout canaries.",
            ["RefineryViewModel.cs"] =
                "Input/output lists are grown and shrunk one row at a time by separate user " +
                "callbacks; they are never rebuilt together from a source collection.",
            ["CreatureManagerViewModel.cs"] =
                "Builds one bound list per method (creature names, then pagination pages); " +
                "the two are unrelated and differ in length.",
        };

    [Test]
    public void TableShapedViewModels_UseGuiTableSourceRatherThanHandSyncedLists()
    {
        var viewModelDirectory = new DirectoryInfo(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel"));

        viewModelDirectory.Exists.Should().BeTrue(
            $"the ViewModel directory should exist at {viewModelDirectory.FullName}");

        var offenders = new List<string>();

        foreach (var file in viewModelDirectory.GetFiles("*ViewModel.cs").OrderBy(f => f.Name))
        {
            var source = File.ReadAllText(file.FullName);
            var bindingListCount = Regex.Matches(source, @"new\s+GuiBindingList\s*<").Count;

            if (bindingListCount < 2)
                continue;

            if (source.Contains("GuiTableSource"))
                continue;

            if (NonTabularViewModels.ContainsKey(file.Name))
                continue;

            offenders.Add($"{file.Name} (constructs {bindingListCount} GuiBindingList instances)");
        }

        offenders.Should().BeEmpty(
            "these ViewModels build parallel GuiBindingList columns by hand, which lets the " +
            "columns drift out of length-sync. Use GuiTableSource (see SkillsViewModel for the " +
            "reference pattern), or if the lists are genuinely not a lockstep table, add the " +
            "file to NonTabularViewModels in this test with a reason. Offenders: " +
            string.Join(", ", offenders));
    }

    [Test]
    public void NonTabularAllowList_DoesNotNameFilesThatNoLongerExistOrHaveMigrated()
    {
        var viewModelDirectory = new DirectoryInfo(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel"));

        var stale = new List<string>();

        foreach (var (fileName, _) in NonTabularViewModels)
        {
            var path = Path.Combine(viewModelDirectory.FullName, fileName);

            if (!File.Exists(path))
            {
                stale.Add($"{fileName} (no longer exists)");
                continue;
            }

            var source = File.ReadAllText(path);

            if (source.Contains("GuiTableSource"))
                stale.Add($"{fileName} (now uses GuiTableSource)");
            else if (Regex.Matches(source, @"new\s+GuiBindingList\s*<").Count < 2)
                stale.Add($"{fileName} (no longer builds multiple GuiBindingList instances)");
        }

        stale.Should().BeEmpty(
            "the NonTabularViewModels allow-list should shrink as windows migrate. Remove these " +
            "entries: " + string.Join(", ", stale));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
