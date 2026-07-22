using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Verifies the VarTable view: reading real corpus entries (int/float/string), in-place
    /// mutation of an existing entry, and creation of a brand-new entry.
    /// </summary>
    public class VarTableTests
    {
        private static string BfButcherUtcPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "utc", "bf_butcher.utc.json");

        [Test]
        public void BfButcher_KnownIntAndStringEntries_ReadCorrectly()
        {
            var document = UtcDocument.Load(BfButcherUtcPath);

            document.VarTable.GetInt("QUEST_NPC_GROUP_ID").Should().Be(69);
            document.VarTable.GetString("LOOT_TABLE_1").Should().Be("VISCARA_SEWERS_DEPTHS_BUTCHER,100,1");
            document.VarTable.GetString("LOOT_TABLE_2").Should().Be("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES,5,1");

            // A name that isn't present, and a type mismatch, both read as null rather than throwing.
            document.VarTable.GetInt("DOES_NOT_EXIST").Should().BeNull();
            document.VarTable.GetString("QUEST_NPC_GROUP_ID").Should().BeNull();
        }

        [Test]
        public void SetInt_OnExistingEntry_UpdatesInPlaceAndRoundTrips()
        {
            var original = File.ReadAllBytes(BfButcherUtcPath);
            var document = UtcDocument.Parse(original);

            document.VarTable.SetInt("QUEST_NPC_GROUP_ID", 999);
            document.VarTable.GetInt("QUEST_NPC_GROUP_ID").Should().Be(999);

            var written = document.ToBytes();
            var reparsed = UtcDocument.Parse(written);
            reparsed.VarTable.GetInt("QUEST_NPC_GROUP_ID").Should().Be(999);
            // Untouched entries remain intact.
            reparsed.VarTable.GetString("LOOT_TABLE_1").Should().Be("VISCARA_SEWERS_DEPTHS_BUTCHER,100,1");
        }

        [Test]
        public void SetInt_OnMissingEntry_CreatesItWithCorpusShape()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));

            document.VarTable.SetInt("NEW_TEST_VAR", 7);

            var written = document.ToBytes();
            var reparsed = UtcDocument.Parse(written);

            reparsed.VarTable.GetInt("NEW_TEST_VAR").Should().Be(7);

            // The new entry must match the corpus's VarTable entry shape: a struct id, and
            // Name (cexostring) / Type (dword, 1 = int) / Value (int) fields in that order.
            var entries = reparsed.Fields.Get("VarTable").Elements!;
            var newEntry = entries[^1];
            newEntry.RawStructId.Should().NotBeNull();
            newEntry.Entries.Select(e => e.Key).Should().ContainInOrder("Name", "Type", "Value");
            newEntry.Get("Name").Type.Should().Be(GffFieldType.CExoString);
            newEntry.Get("Type").Type.Should().Be(GffFieldType.Dword);
            newEntry.Get("Type").GetUnsignedInteger().Should().Be(1u);
            newEntry.Get("Value").Type.Should().Be(GffFieldType.Int);
        }

        [Test]
        public void SetFloat_AndSetString_OnMissingEntries_RoundTrip()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));

            document.VarTable.SetFloat("NEW_FLOAT_VAR", 12.5f);
            document.VarTable.SetString("NEW_STRING_VAR", "hello");

            var reparsed = UtcDocument.Parse(document.ToBytes());
            reparsed.VarTable.GetFloat("NEW_FLOAT_VAR").Should().Be(12.5f);
            reparsed.VarTable.GetString("NEW_STRING_VAR").Should().Be("hello");
        }

        [Test]
        public void Remove_DeletesTheEntry()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));

            document.VarTable.Remove("LOOT_TABLE_2").Should().BeTrue();
            document.VarTable.GetString("LOOT_TABLE_2").Should().BeNull();
            document.VarTable.Remove("LOOT_TABLE_2").Should().BeFalse();

            var reparsed = UtcDocument.Parse(document.ToBytes());
            reparsed.VarTable.GetString("LOOT_TABLE_2").Should().BeNull();
            reparsed.VarTable.GetInt("QUEST_NPC_GROUP_ID").Should().Be(69);
        }

        [Test]
        public void AddMissingEntry_InSession_IsDirtyAndUndoable()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));
            using var session = new DocumentSession(BfButcherUtcPath, document.Document);

            using (session.Begin("add local"))
                document.VarTable.SetInt("UNDO_NEW_LOCAL", 7);

            session.UndoStack.IsDirty.Should().BeTrue();
            session.UndoStack.Entries.Should().ContainSingle();
            document.VarTable.GetInt("UNDO_NEW_LOCAL").Should().Be(7);

            session.UndoStack.Undo();
            document.VarTable.GetInt("UNDO_NEW_LOCAL").Should().BeNull();
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void RemoveEntry_InSession_IsDirtyAndUndoable()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));
            using var session = new DocumentSession(BfButcherUtcPath, document.Document);

            using (session.Begin("remove local"))
                document.VarTable.Remove("LOOT_TABLE_2").Should().BeTrue();

            session.UndoStack.IsDirty.Should().BeTrue();
            document.VarTable.GetString("LOOT_TABLE_2").Should().BeNull();

            session.UndoStack.Undo();
            document.VarTable.GetString("LOOT_TABLE_2").Should().Be("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES,5,1");
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void ChangeEntryType_InSession_IsDirtyAndUndoable()
        {
            var document = UtcDocument.Parse(File.ReadAllBytes(BfButcherUtcPath));
            using var session = new DocumentSession(BfButcherUtcPath, document.Document);

            using (session.Begin("change local type"))
                document.VarTable.SetString("QUEST_NPC_GROUP_ID", "replacement");

            session.UndoStack.IsDirty.Should().BeTrue();
            document.VarTable.GetString("QUEST_NPC_GROUP_ID").Should().Be("replacement");

            session.UndoStack.Undo();
            document.VarTable.GetInt("QUEST_NPC_GROUP_ID").Should().Be(69);
            session.UndoStack.IsDirty.Should().BeFalse();
        }
    }
}
