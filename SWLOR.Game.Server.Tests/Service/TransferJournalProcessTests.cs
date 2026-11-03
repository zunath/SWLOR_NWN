using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A journaled transfer across a restart (Player City Revamp, farming and the Civic Exchange). A fake store stops the transfer after any
/// number of its writes, as a restart would, drops what the character file had not saved, lets the target change before the character's
/// next login, and then settles the login, which can itself be stopped after any write. However it goes, every item ends in exactly one
/// place: the character's inventory, the target, or the character's overflow store.
/// </summary>
public class TransferJournalProcessTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private const string PlayerId = "player";
    private const string Kind = "test.hopper";
    private const int Total = 5;
    private const int Moved = 3;

    /// <summary>Thrown by the fake store at the write a restart stops the transfer at.</summary>
    private sealed class RestartException : Exception
    {
    }

    /// <summary>What happens to the target between the restart and the character's next login.</summary>
    public enum Meanwhile
    {
        NothingChanges,
        TheTargetFillsUp,
        TheTargetIsDeleted
    }

    /// <summary>
    /// The character (live, and as the last save left it), a target that holds a count of the items and the receipts it saved, the
    /// entries and the character's overflow store, in memory. It counts every write and stops (throws) at the one a restart stops it at.
    /// A restart drops everything the character file did not save.
    /// </summary>
    private sealed class FakeStore : ITransferStore
    {
        public const int Capacity = 4;

        public FakeStore(TransferDirection direction)
        {
            if (direction == TransferDirection.Deposit)
            {
                Items = Total;
                FileItems = Total;
            }
            else
            {
                Target = new Hopper { Count = Total };
            }

            if (direction == TransferDirection.Deposit)
                Target = new Hopper { Count = 0 };
        }

        /// <summary>The target record: what it holds and the receipts saved with it.</summary>
        public sealed class Hopper
        {
            public int Count { get; set; }
            public HashSet<string> Receipts { get; } = new();
        }

        public Hopper Target { get; set; }

        /// <summary>Items other people put into the target while the character was away. They are not the character's.</summary>
        public int Foreign { get; set; }

        public Dictionary<string, TransferEntry> Entries { get; } = new();

        public Dictionary<string, int> Overflow { get; } = new();

        public int Items { get; private set; }

        public HashSet<string> Marks { get; private set; } = new();

        public int FileItems { get; private set; }

        public HashSet<string> FileMarks { get; private set; } = new();

        /// <summary>How many items the inventory has room for. The rest of a withdrawal goes to overflow.</summary>
        public int Room { get; set; } = int.MaxValue;

        public bool SaveFails { get; set; }

        public List<string> Writes { get; } = new();

        public int? StopAfterWrites { get; set; }

        private void Write(string what)
        {
            if (StopAfterWrites.HasValue && Writes.Count >= StopAfterWrites.Value)
                throw new RestartException();

            Writes.Add(what);
        }

        private void SaveCharacter()
        {
            FileItems = Items;
            FileMarks = new HashSet<string>(Marks);
        }

        /// <summary>The server stops: the character comes back as the file holds it. The entries, the target and the overflow store are in the database and stay.</summary>
        public void Restart()
        {
            Items = FileItems;
            Marks = new HashSet<string>(FileMarks);
            StopAfterWrites = null;
            Writes.Clear();
        }

        public void SavePending(TransferEntry entry)
        {
            Write("save");
            Entries[entry.Id] = entry;
        }

        public void DeletePending(string entryId)
        {
            Write("delete");
            Entries.Remove(entryId);
        }

        public bool TakeAndMark(TransferEntry entry)
        {
            // The items leave the inventory before the file is saved (native destruction waits for the end of the script, so the real
            // store parks them in a holding container first), and the file is saved with the mark after. A stop between the two
            // leaves the file as it was: the items in it and no mark.
            Write("park");
            if (Items < entry.Items.Sum(item => item.Count))
                return false;

            Items -= entry.Items.Sum(item => item.Count);
            Write("mark");
            Marks.Add(entry.Id);
            if (SaveFails)
            {
                Items += entry.Items.Sum(item => item.Count);
                Marks.Remove(entry.Id);
                return false;
            }

            SaveCharacter();
            return true;
        }

        public bool GiveAndMark(TransferEntry entry)
        {
            var count = entry.Items.Sum(item => item.Count);
            var placed = Math.Min(count, Room - Items);
            if (placed < count)
            {
                // The part that does not fit is written to overflow before the file is saved.
                Write("overflow-tail");
                Overflow[TransferJournalRules.GetOverflowRecordId(entry.Id, 0, 0)] = count - placed;
            }

            Write("give");
            Items += placed;
            Marks.Add(entry.Id);
            if (SaveFails)
            {
                Items -= placed;
                Marks.Remove(entry.Id);
                return false;
            }

            SaveCharacter();
            return true;
        }

        public void ClearMark(string entryId)
        {
            Write("clear");

            // The cleared mark is not saved at once: the file keeps the mark until its next save.
            Marks.Remove(entryId);
        }

        public bool IsApplied(TransferEntry entry) => Target != null && Target.Receipts.Contains(entry.Id);

        public bool CanApply(TransferEntry entry) => Target != null && Target.Count + entry.Items.Sum(item => item.Count) <= Capacity;

        public void Apply(TransferEntry entry)
        {
            Write("apply");
            if (Target == null || Target.Receipts.Contains(entry.Id))
                return;

            if (entry.Direction == TransferDirection.Deposit)
                Target.Count += entry.Items.Sum(item => item.Count);
            else
                Target.Count = 0;

            Target.Receipts.Add(entry.Id);
        }

        public void MarkApplied(TransferEntry entry)
        {
            Write("applied");
            entry.AppliedUtc = Now;
            Entries[entry.Id] = entry;
        }

        public void ToOverflow(TransferEntry entry)
        {
            Write("overflow");
            for (var index = 0; index < entry.Items.Count; index++)
            {
                // The same ids as the part GiveAndMark wrote, so a repeat replaces it.
                Overflow[TransferJournalRules.GetOverflowRecordId(entry.Id, index, 0)] = entry.Items[index].Count;
            }
        }

        public int InTarget => Target?.Count ?? 0;

        public int InOverflow => Overflow.Values.Sum();
    }

    private static TransferEntry NewEntry(TransferDirection direction, int count = Moved)
    {
        var entry = TransferJournalRules.Create(PlayerId, Kind, direction, "hopper", "Your feed", Now);
        entry.Items.Add(TransferJournalRules.Plain("wheat", "Wheat", count));
        return entry;
    }

    private static void SettleLogin(FakeStore store)
    {
        TransferJournalProcess.SettleAtLogin(
            store,
            store.Marks.ToList(),
            store.Entries.Values.Where(entry => entry.PlayerId == PlayerId).ToList());
    }

    private static void ApplyMeanwhile(FakeStore store, Meanwhile meanwhile)
    {
        switch (meanwhile)
        {
            case Meanwhile.TheTargetFillsUp:
                store.Foreign = FakeStore.Capacity - store.Target.Count;
                store.Target.Count = FakeStore.Capacity;
                break;
            case Meanwhile.TheTargetIsDeleted:
                store.Target = null;
                break;
        }
    }

    private static void AssertOneCopyOfEverything(FakeStore store, int expected)
    {
        (store.Items + store.InOverflow + store.InTarget - store.Foreign).Should().Be(expected,
            $"every item is in one place: inventory {store.Items}, overflow {store.InOverflow}, target {store.InTarget}");
        store.Entries.Should().BeEmpty("no entry is left to settle again");
        store.Marks.Should().BeEmpty("no mark is left to settle again");
        store.Items.Should().Be(store.FileItems, "the character file was saved last");
    }

    private const int DepositWrites = 7;
    private const int WithdrawWrites = 6;

    #region Deposits

    [Test]
    public void ADepositThatFinishes_LeavesTheItemsInTheTargetAndNothingToSettle()
    {
        var store = new FakeStore(TransferDirection.Deposit);

        var settlement = TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));

        settlement.Should().Be(TransferSettlement.Apply);
        store.Writes.Should().Equal("save", "park", "mark", "apply", "applied", "delete", "clear");
        store.InTarget.Should().Be(Moved);
        store.Items.Should().Be(Total - Moved);
        store.Entries.Should().BeEmpty();
        store.Marks.Should().BeEmpty();
    }

    [Test]
    public void ADeposit_TakesTheItemsAndSavesTheCharacter_BeforeTheTargetIsTouched()
    {
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 3 };

        var act = () => TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));

        act.Should().Throw<RestartException>();
        store.InTarget.Should().Be(0, "the target is only touched after the file holds the mark");
        store.FileItems.Should().Be(Total - Moved, "the saved file no longer holds the items");
        store.FileMarks.Should().HaveCount(1);
    }

    [Test]
    public void ADepositStoppedAfterTheItemsLeftTheInventory_ButBeforeTheFileWasSaved_KeepsThemInTheFile_AndTheLoginDropsTheEntry()
    {
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 2 };

        var act = () => TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));

        act.Should().Throw<RestartException>();
        store.Writes.Should().Equal("save", "park");
        store.Items.Should().Be(Total - Moved, "the items are already out of the live inventory");
        store.FileItems.Should().Be(Total, "the saved file still holds them");
        store.FileMarks.Should().BeEmpty("no mark names an entry whose items the file still holds");

        store.Restart();
        SettleLogin(store);

        store.Items.Should().Be(Total, "the character comes back with the items");
        store.InTarget.Should().Be(0);
        store.InOverflow.Should().Be(0);
        store.Entries.Should().BeEmpty("the entry with no mark is dropped");
        store.Marks.Should().BeEmpty();
    }

    [Test]
    public void ADepositWhoseItemsCouldNotBeTaken_ChangesNothing_AndWritesNoMark()
    {
        var store = new FakeStore(TransferDirection.Deposit);

        var settlement = TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit, Total + 1));

        settlement.Should().Be(TransferSettlement.Declined);
        store.Writes.Should().Equal("save", "park", "delete");
        store.FileMarks.Should().BeEmpty();
        store.FileItems.Should().Be(Total);
    }

    /// <summary>
    /// Every write a deposit can be stopped before, with every change to the target that still means something: a target deleted before
    /// the deposit was applied took nothing of the character's with it. A login that is itself stopped after any of its first writes, or not.
    /// </summary>
    private static IEnumerable<TestCaseData> DepositRestarts()
    {
        for (var stop = 0; stop < DepositWrites; stop++)
        {
            foreach (var meanwhile in Enum.GetValues<Meanwhile>().Where(meanwhile => meanwhile != Meanwhile.TheTargetIsDeleted || stop <= 3))
            {
                for (var settleStop = -1; settleStop <= 3; settleStop++)
                {
                    yield return new TestCaseData(stop, meanwhile, settleStop).SetName($"Deposit_Stop{stop}_{meanwhile}_LoginStop{settleStop}");
                }
            }
        }
    }

    [TestCaseSource(nameof(DepositRestarts))]
    public void ADepositStoppedAtAnyWrite_LeavesEveryItemInOnePlace_AfterTheLogin(int stopAfterWrites, Meanwhile meanwhile, int settleStopAfterWrites)
    {
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = stopAfterWrites };
        var entry = NewEntry(TransferDirection.Deposit);
        try
        {
            TransferJournalProcess.Deposit(store, entry);
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, meanwhile);

        if (settleStopAfterWrites >= 0)
        {
            store.StopAfterWrites = settleStopAfterWrites;
            try
            {
                SettleLogin(store);
            }
            catch (RestartException)
            {
            }

            store.Restart();
        }

        SettleLogin(store);

        AssertOneCopyOfEverything(store, Total);
    }

    [Test]
    public void ADepositTheTargetCanNoLongerTake_GoesToOverflow()
    {
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 3 };
        try
        {
            TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, Meanwhile.TheTargetFillsUp);
        SettleLogin(store);

        store.InOverflow.Should().Be(Moved);
        store.InTarget.Should().Be(FakeStore.Capacity);
        store.Items.Should().Be(Total - Moved);
    }

    [Test]
    public void ADepositTheTargetAlreadyHolds_IsNotAppliedAgain()
    {
        // The restart came after the target saved the deposit and before the entry went.
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 5 };
        try
        {
            TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        SettleLogin(store);

        store.InTarget.Should().Be(Moved, "the receipt stops a second copy");
        store.Items.Should().Be(Total - Moved);
        store.Entries.Should().BeEmpty();
    }

    [Test]
    public void ADepositWithNoMark_NeverLeftTheCharacter_AndTheEntryGoes()
    {
        // The restart came after the entry was written and before the character file was saved without the items.
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 1 };
        try
        {
            TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        SettleLogin(store);

        store.Items.Should().Be(Total);
        store.InTarget.Should().Be(0);
        store.InOverflow.Should().Be(0);
        store.Entries.Should().BeEmpty();
    }

    [Test]
    public void ADepositTheCharacterCannotAfford_ChangesNothing()
    {
        var store = new FakeStore(TransferDirection.Deposit);

        var settlement = TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit, Total + 1));

        settlement.Should().Be(TransferSettlement.Declined);
        store.Items.Should().Be(Total);
        store.InTarget.Should().Be(0);
        store.Entries.Should().BeEmpty("the entry is withdrawn");
        store.Marks.Should().BeEmpty();
    }

    [Test]
    public void ADepositWhoseCharacterFileCannotBeSaved_ChangesNothing()
    {
        var store = new FakeStore(TransferDirection.Deposit) { SaveFails = true };

        var settlement = TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));

        settlement.Should().Be(TransferSettlement.Declined);
        store.Items.Should().Be(Total);
        store.InTarget.Should().Be(0);
        store.Entries.Should().BeEmpty();
        store.Marks.Should().BeEmpty();
    }

    #endregion

    #region Withdrawals

    [Test]
    public void AWithdrawalThatFinishes_LeavesTheItemsWithTheCharacterAndNothingToSettle()
    {
        var store = new FakeStore(TransferDirection.Withdraw);

        var settlement = TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));

        settlement.Should().Be(TransferSettlement.Forget);
        store.Writes.Should().Equal("save", "apply", "applied", "give", "delete", "clear");
        store.InTarget.Should().Be(0);
        store.Items.Should().Be(Total);
        store.Entries.Should().BeEmpty();
        store.Marks.Should().BeEmpty();
    }

    [Test]
    public void AWithdrawal_ChangesTheTargetWithItsReceipt_BeforeTheCharacterGetsTheItems()
    {
        var store = new FakeStore(TransferDirection.Withdraw) { StopAfterWrites = 3 };

        var act = () => TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));

        act.Should().Throw<RestartException>();
        store.InTarget.Should().Be(0, "the target gave the items up");
        store.FileItems.Should().Be(0, "the character file does not hold them yet");
        store.Entries.Values.Single().AppliedUtc.Should().NotBeNull("the entry already says the target gave them up");
    }

    /// <summary>
    /// Every write a withdrawal can be stopped before, with the inventory roomy or nearly full. A target deleted between the restart and
    /// the login only means something once it had given its items up: before that, they went with it. The one instant not covered is a stop
    /// after the target's write and before the entry is saved as applied, with the target (and its receipt) deleted before the login.
    /// </summary>
    private static IEnumerable<TestCaseData> WithdrawalRestarts()
    {
        for (var stop = 0; stop <= WithdrawWrites; stop++)
        {
            foreach (var room in new[] { int.MaxValue, 2 })
            {
                foreach (var meanwhile in new[] { Meanwhile.NothingChanges, Meanwhile.TheTargetIsDeleted }.Where(meanwhile => meanwhile != Meanwhile.TheTargetIsDeleted || stop >= 3))
                {
                    for (var settleStop = -1; settleStop <= 3; settleStop++)
                    {
                        yield return new TestCaseData(stop, room, meanwhile, settleStop).SetName($"Withdrawal_Stop{stop}_Room{(room == int.MaxValue ? "Plenty" : room.ToString())}_{meanwhile}_LoginStop{settleStop}");
                    }
                }
            }
        }
    }

    [TestCaseSource(nameof(WithdrawalRestarts))]
    public void AWithdrawalStoppedAtAnyWrite_LeavesEveryItemInOnePlace_AfterTheLogin(int stopAfterWrites, int room, Meanwhile meanwhile, int settleStopAfterWrites)
    {
        var store = new FakeStore(TransferDirection.Withdraw) { StopAfterWrites = stopAfterWrites, Room = room };
        try
        {
            TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, meanwhile);

        if (settleStopAfterWrites >= 0)
        {
            store.StopAfterWrites = settleStopAfterWrites;
            try
            {
                SettleLogin(store);
            }
            catch (RestartException)
            {
            }

            store.Restart();
        }

        SettleLogin(store);

        // The target held Total and gave it all up, or never did: either way nothing is duplicated or lost.
        (store.Items + store.InOverflow + store.InTarget).Should().Be(Total);
        store.Entries.Should().BeEmpty();
        store.Marks.Should().BeEmpty();
        store.Items.Should().Be(store.FileItems, "the character file was saved last");
    }

    [Test]
    public void AWithdrawalThatPutItemsInOverflowBeforeTheRestart_DoesNotWriteThemTwice()
    {
        // Room for 2 of 5: the other 3 are written to overflow, then the restart comes before the character file is saved.
        var store = new FakeStore(TransferDirection.Withdraw) { Room = 2, StopAfterWrites = 4 };
        try
        {
            TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));
        }
        catch (RestartException)
        {
        }

        store.InOverflow.Should().Be(3);
        store.Restart();
        SettleLogin(store);

        store.Items.Should().Be(0, "the file never held them");
        store.InOverflow.Should().Be(Total, "the same record is replaced with all five");
        store.InTarget.Should().Be(0);
    }

    [Test]
    public void AWithdrawalWhoseTargetNeverChanged_IsDropped()
    {
        // The restart came after the entry was written and before the target gave anything up.
        var store = new FakeStore(TransferDirection.Withdraw) { StopAfterWrites = 1 };
        try
        {
            TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        SettleLogin(store);

        store.InTarget.Should().Be(Total);
        store.Items.Should().Be(0);
        store.InOverflow.Should().Be(0);
        store.Entries.Should().BeEmpty();
    }

    [Test]
    public void AWithdrawalTheTargetGaveUp_AfterTheTargetWasDeleted_StillReachesTheCharacter()
    {
        // The restart came after the entry was saved as applied, and the target (with its receipts) is deleted before the login.
        var store = new FakeStore(TransferDirection.Withdraw) { StopAfterWrites = 3 };
        try
        {
            TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, Meanwhile.TheTargetIsDeleted);
        SettleLogin(store);

        store.InOverflow.Should().Be(Total, "the entry's own applied mark does not depend on the target");
        store.Entries.Should().BeEmpty();
    }

    [Test]
    public void AWithdrawalWhoseCharacterFileCannotBeSaved_GoesToOverflow()
    {
        var store = new FakeStore(TransferDirection.Withdraw) { SaveFails = true };

        var settlement = TransferJournalProcess.Withdraw(store, NewEntry(TransferDirection.Withdraw, Total));

        settlement.Should().Be(TransferSettlement.ToOverflow);
        store.Items.Should().Be(0);
        store.InOverflow.Should().Be(Total);
        store.InTarget.Should().Be(0);
        store.Entries.Should().BeEmpty();
        store.Marks.Should().BeEmpty();
    }

    #endregion

    #region Marks without entries

    [Test]
    public void AMarkWithNoEntry_IsClearedAtLogin()
    {
        // A transfer that finished left its mark in the file because the cleared mark was not saved before the restart.
        var store = new FakeStore(TransferDirection.Deposit);
        store.Marks.Add("finished");

        TransferJournalProcess.SettleAtLogin(store, store.Marks.ToList(), new List<TransferEntry>());

        store.Marks.Should().BeEmpty();
    }

    [Test]
    public void SettlingTwice_ChangesNothingTheSecondTime()
    {
        var store = new FakeStore(TransferDirection.Deposit) { StopAfterWrites = 4 };
        try
        {
            TransferJournalProcess.Deposit(store, NewEntry(TransferDirection.Deposit));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        SettleLogin(store);
        var items = store.Items;
        var inTarget = store.InTarget;
        var inOverflow = store.InOverflow;
        store.Writes.Clear();

        SettleLogin(store);

        store.Writes.Should().BeEmpty();
        (store.Items, store.InTarget, store.InOverflow).Should().Be((items, inTarget, inOverflow));
    }

    #endregion
}
