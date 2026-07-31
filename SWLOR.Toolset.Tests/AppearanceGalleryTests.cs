using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Editors.Appearance;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The searchable appearance grid, shared by every editor that picks one.
    /// </summary>
    /// <remarks>
    /// Exercised without a <c>ThumbnailService</c>: the grid must page, filter, and record a pick
    /// whether or not previews are available, because that is exactly the state it is in for the
    /// first second after a tab opens and permanently when the game data did not resolve.
    /// </remarks>
    [TestFixture]
    public class AppearanceGalleryTests
    {
        [Test]
        public void OnlyTheFirstPageIsPublishedUntilSomethingAsksForMore()
        {
            var section = Section(Options(500), out _);

            section.Tiles.Should().HaveCount(48);
            section.CanLoadMore.Should().BeTrue();
            section.MatchSummary.Should().Be("48 of 500 appearances");

            section.LoadMoreCommand.Execute(null);
            section.Tiles.Should().HaveCount(96);

            for (var page = 0; page < 20; page++)
                section.LoadMoreCommand.Execute(null);

            section.Tiles.Should().HaveCount(500);
            section.CanLoadMore.Should().BeFalse();
            section.MatchSummary.Should().Be("500 appearances");
        }

        [Test]
        public void ClearingTheSearchTakesEffectAtOnceRatherThanWaitingOutTheDebounce()
        {
            // Emptying the box is a search being abandoned, not one being typed. Waiting leaves the
            // old results sitting there looking like the filter stuck.
            var section = Section(Options(200), out _);

            section.Query = "   ";
            section.Tiles.Should().HaveCount(48);
            section.MatchSummary.Should().Be("48 of 200 appearances");
        }

        [Test]
        public void PickingATileRecordsTheEditAndMovesTheCurrentMarker()
        {
            var section = Section(Options(100), out var applied);

            var target = section.Tiles[7];
            section.Highlighted = target;

            applied.Should().ContainSingle().Which.Key.Should().Be(target.Option.Key);
            target.IsCurrent.Should().BeTrue();
            section.Tiles.Where(tile => tile.IsCurrent).Should().ContainSingle();
        }

        [Test]
        public void ARefusedPickPutsTheGridBackWhereItWas()
        {
            var options = Options(100);
            var section = new AppearanceGallerySectionViewModel(
                options,
                thumbnails: null,
                currentKey: () => "0",
                apply: _ => false,
                noun: "appearance");

            section.Highlighted = section.Tiles[9];

            section.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("0");
            section.Highlighted.Should().BeNull("a refused edit clears the highlight it came from");
        }

        [Test]
        public void PickingWhatIsAlreadyStoredIsNotAnEdit()
        {
            var section = Section(Options(20), out var applied);

            section.Highlighted = section.Tiles.Single(tile => tile.Option.Key == "0");

            applied.Should().BeEmpty();
        }

        [Test]
        public void AStoredRowTheTableDoesNotHaveIsReportedRatherThanHidden()
        {
            var unknown = new AppearanceGallerySectionViewModel(
                Options(10),
                thumbnails: null,
                currentKey: () => "9999",
                apply: _ => true,
                noun: "appearance");

            unknown.CurrentIsUnknown.Should().BeTrue();
            unknown.CurrentDescription.Should().Contain("9999");

            var known = Section(Options(10), out _);
            known.CurrentIsUnknown.Should().BeFalse();
            known.CurrentDescription.Should().Contain("Appearance 0");
        }

        [Test]
        public void TheTileFallsBackToALetterUntilItsPictureArrives()
        {
            var section = Section(Options(3), out _);

            section.Tiles[0].Preview.Should().BeNull("no thumbnail service was supplied");
            section.Tiles[0].Glyph.Should().Be("A");
            section.Tiles[0].HasDetail.Should().BeTrue();
        }

        [Test]
        public void DisposingCancelsAPendingSearchRatherThanLettingItFire()
        {
            var section = Section(Options(50), out _);

            section.Query = "Appearance 4";
            section.Dispose();

            // Disposing twice is a no-op, which matters because the editor disposes on close and
            // the tab may already have been torn down.
            section.Dispose();
        }

        [Test]
        public void OneGridServesEveryEditorThatPicksAnAppearance()
        {
            // The door editor and the creature editor draw the same control. They had arrived at
            // the same design separately, and the creature editor had not arrived at it at all.
            var doorView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "DoorEditorView.axaml"));
            var blueprintView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "BlueprintEditorView.axaml"));
            var creatureView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            doorView.Should().Contain("<appearance:AppearanceGalleryView");
            blueprintView.Should().Contain("appearance:AppearanceGallerySectionViewModel");
            creatureView.Should().Contain("<appearance:AppearanceGalleryView");
            creatureView.Should().Contain("<behaviors:BehaviorRowView />",
                "creature equipment reuses the shared progressive choice control");
            var itemView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "ItemEditorView.axaml"));
            itemView.Should().Contain("<items:PaletteColorPickerView");
            creatureView.Should().Contain("<items:PaletteColorPickerView",
                "creature colors reuse the item editor's established palette control");
            creatureView.Should().Contain("<TabItem Header=\"Equipment\">");
            creatureView.Should().Contain("SelectedItem=\"{Binding EquipmentSlots.SelectedSlot, Mode=TwoWay}\"",
                "equipment reuses the merchant editor's focused rail/work-pane interaction");

            Directory.Exists(Path.Combine(
                    CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Appearance"))
                .Should().BeTrue();
            File.Exists(Path.Combine(
                    CorpusLocator.RepositoryRoot,
                    "SWLOR.Toolset", "Editors", "Doors", "DoorAppearanceSectionViewModel.cs"))
                .Should().BeFalse("the door editor uses the shared grid now");
        }

        private static AppearanceGallerySectionViewModel Section(
            IReadOnlyList<AppearanceOption> options,
            out List<AppearanceOption> applied)
        {
            var picks = new List<AppearanceOption>();
            var current = "0";
            var section = new AppearanceGallerySectionViewModel(
                options,
                thumbnails: null,
                currentKey: () => current,
                apply: option =>
                {
                    picks.Add(option);
                    current = option.Key;
                    return true;
                },
                noun: "appearance");

            applied = picks;
            return section;
        }

        private static IReadOnlyList<AppearanceOption> Options(int count) =>
            Enumerable.Range(0, count)
                .Select(index => new AppearanceOption(
                    index.ToString(),
                    $"Appearance {index}",
                    $"row {index} · label_{index}",
                    CreatureAppearanceId: index))
                .ToList();
    }
}
