using NSubstitute;
using SWLOR.Component.Character.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Character.Repository
{
    [TestFixture]
    public class PlayerNoteRepositoryTests
    {
        private PlayerNoteRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new PlayerNoteRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnPlayerNote()
        {
            // Arrange
            var noteId = "test-note-id";
            var expectedNote = new PlayerNote { Id = noteId, PlayerId = "test-player", Text = "Test note" };
            _databaseService.Get<PlayerNote>(noteId).Returns(expectedNote);

            // Act
            var result = _repository.GetById(noteId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNote));
            _databaseService.Received(1).Get<PlayerNote>(noteId);
        }

        [Test]
        public void GetByPlayerId_WithValidPlayerId_ShouldReturnPlayerNotes()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedNotes = new List<PlayerNote>
            {
                new PlayerNote { Id = "note1", PlayerId = playerId, Text = "Note 1" },
                new PlayerNote { Id = "note2", PlayerId = playerId, Text = "Note 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetByPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerNote>>());
        }

        [Test]
        public void GetByName_WithValidName_ShouldReturnPlayerNotes()
        {
            // Arrange
            var name = "test-note-name";
            var expectedNotes = new List<PlayerNote>
            {
                new PlayerNote { Id = "note1", Name = name, Text = "Note 1" },
                new PlayerNote { Id = "note2", Name = name, Text = "Note 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetByName(name);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerNote>>());
        }

        [Test]
        public void GetDMNotes_ShouldReturnDMNotes()
        {
            // Arrange
            var expectedNotes = new List<PlayerNote>
            {
                new PlayerNote { Id = "note1", PlayerId = "player1", Text = "DM Note 1", IsDMNote = true },
                new PlayerNote { Id = "note2", PlayerId = "player2", Text = "DM Note 2", IsDMNote = true }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetDMNotes();

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerNote>>());
        }

        [Test]
        public void GetDMNotesByCreatorName_WithValidCreatorName_ShouldReturnDMNotes()
        {
            // Arrange
            var creatorName = "test-dm";
            var expectedNotes = new List<PlayerNote>
            {
                new PlayerNote { Id = "note1", PlayerId = "player1", Text = "DM Note 1", IsDMNote = true, DMCreatorName = creatorName },
                new PlayerNote { Id = "note2", PlayerId = "player2", Text = "DM Note 2", IsDMNote = true, DMCreatorName = creatorName }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetDMNotesByCreatorName(creatorName);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerNote>>());
        }

        [Test]
        public void GetDMNotesByCreatorCDKey_WithValidCreatorCDKey_ShouldReturnDMNotes()
        {
            // Arrange
            var creatorCDKey = "test-cd-key";
            var expectedNotes = new List<PlayerNote>
            {
                new PlayerNote { Id = "note1", PlayerId = "player1", Text = "DM Note 1", IsDMNote = true, DMCreatorCDKey = creatorCDKey },
                new PlayerNote { Id = "note2", PlayerId = "player2", Text = "DM Note 2", IsDMNote = true, DMCreatorCDKey = creatorCDKey }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetDMNotesByCreatorCDKey(creatorCDKey);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerNote>>());
        }

        [Test]
        public void Save_WithValidPlayerNote_ShouldCallDatabaseSet()
        {
            // Arrange
            var playerNote = new PlayerNote { Id = "test-note", PlayerId = "test-player", Text = "Test note" };

            // Act
            _repository.Save(playerNote);

            // Assert
            _databaseService.Received(1).Set(playerNote);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var noteId = "test-note-id";

            // Act
            _repository.Delete(noteId);

            // Assert
            _databaseService.Received(1).Delete<PlayerNote>(noteId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var noteId = "test-note-id";
            _databaseService.Exists<PlayerNote>(noteId).Returns(true);

            // Act
            var result = _repository.Exists(noteId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<PlayerNote>(noteId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var noteId = "non-existent-note";
            _databaseService.Exists<PlayerNote>(noteId).Returns(false);

            // Act
            var result = _repository.Exists(noteId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<PlayerNote>(noteId);
        }
    }
}
