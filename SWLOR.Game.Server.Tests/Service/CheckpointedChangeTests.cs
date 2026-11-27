using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A change to a character that must reach the character file before a database write (Player City Revamp). A save that fails must
/// leave the character as it was and report false, so the database side never goes on for something the saved file still holds.
/// </summary>
public class CheckpointedChangeTests
{
    /// <summary>The character's inventory and mark as the live game sees them, and as the file last saved them.</summary>
    private sealed class FakeCharacter
    {
        public int Items { get; private set; } = 5;
        public string Mark { get; private set; } = string.Empty;
        public int FileItems { get; private set; } = 5;
        public string FileMark { get; private set; } = string.Empty;
        public bool SaveFails { get; set; }
        public int Destroyed { get; private set; }
        public List<string> Calls { get; } = new();

        public bool Take(int count, string mark, bool succeeds = true)
        {
            Calls.Add("take");
            if (!succeeds)
                return false;

            Items -= count;
            Mark = mark;
            return true;
        }

        public bool Save()
        {
            Calls.Add("save");
            if (SaveFails)
                return false;

            FileItems = Items;
            FileMark = Mark;
            return true;
        }

        public void Undo(int count)
        {
            Calls.Add("undo");
            Items += count;
            Mark = string.Empty;
        }

        public void Commit()
        {
            Calls.Add("commit");
            Destroyed++;
        }
    }

    [Test]
    public void ASavedChange_IsKept_AndCommittedOnceTheFileHoldsIt()
    {
        var character = new FakeCharacter();

        var result = CheckpointedChange.Run(() => character.Take(3, "donation"), character.Save, () => character.Undo(3), character.Commit);

        result.Should().BeTrue();
        character.Calls.Should().Equal("take", "save", "commit");
        character.FileItems.Should().Be(2);
        character.FileMark.Should().Be("donation");
    }

    [Test]
    public void AFailedSave_PutsTheCharacterBack_AndReportsFalse_SoTheDatabaseSideDoesNotGoOn()
    {
        var character = new FakeCharacter { SaveFails = true };

        var result = CheckpointedChange.Run(() => character.Take(3, "donation"), character.Save, () => character.Undo(3), character.Commit);

        result.Should().BeFalse();
        character.Calls.Should().Equal("take", "save", "undo");
        character.Items.Should().Be(5, "what was taken is back");
        character.Mark.Should().BeEmpty("the mark is taken off");
        character.FileItems.Should().Be(5);
        character.FileMark.Should().BeEmpty();
        character.Destroyed.Should().Be(0, "nothing is destroyed for a change the file does not hold");
    }

    [Test]
    public void AChangeThatCannotBeMade_IsUndone_WithoutSavingTheFile()
    {
        var character = new FakeCharacter();

        var result = CheckpointedChange.Run(() => character.Take(3, "donation", succeeds: false), character.Save, () => character.Undo(0), character.Commit);

        result.Should().BeFalse();
        character.Calls.Should().Equal("take", "undo");
        character.Destroyed.Should().Be(0);
    }

    [Test]
    public void AChangeThatThrows_IsUndone_AndTheExceptionIsNotSwallowed()
    {
        var character = new FakeCharacter();

        var act = () => CheckpointedChange.Run(
            () =>
            {
                character.Take(3, "donation");
                throw new InvalidOperationException("broken");
            },
            character.Save,
            () => character.Undo(3),
            character.Commit);

        act.Should().Throw<InvalidOperationException>();
        character.Calls.Should().Equal("take", "undo");
        character.Items.Should().Be(5);
    }

    [Test]
    public void ACommitThatThrows_IsNotUndone_BecauseTheFileAlreadyHoldsTheChange()
    {
        var character = new FakeCharacter();

        var act = () => CheckpointedChange.Run(() => character.Take(3, "donation"), character.Save, () => character.Undo(3), () => throw new InvalidOperationException("broken"));

        act.Should().Throw<InvalidOperationException>();
        character.Calls.Should().Equal("take", "save");
        character.FileItems.Should().Be(2);
    }

    [Test]
    public void ATakenChangeWithNoCommit_NeedsNone()
    {
        var character = new FakeCharacter();

        CheckpointedChange.Run(() => character.Take(1, "mark"), character.Save, () => character.Undo(1)).Should().BeTrue();

        character.Calls.Should().Equal("take", "save");
    }
}
