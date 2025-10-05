using NSubstitute;
using SWLOR.Component.World.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.World.Repository
{
    [TestFixture]
    public class AreaNoteRepositoryTests
    {
        private AreaNoteRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new AreaNoteRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnAreaNote()
        {
            // Arrange
            var noteId = "test-note-id";
            var expectedNote = new AreaNote { Id = noteId, AreaResref = "test-area", PrivateText = "Private note" };
            _databaseService.Get<AreaNote>(noteId).Returns(expectedNote);

            // Act
            var result = _repository.GetById(noteId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNote));
            _databaseService.Received(1).Get<AreaNote>(noteId);
        }

        [Test]
        public void GetByAreaResref_WithValidAreaResref_ShouldReturnAreaNotes()
        {
            // Arrange
            var areaResref = "test-area";
            var expectedNotes = new List<AreaNote>
            {
                new AreaNote { Id = "note1", AreaResref = areaResref, PrivateText = "Note 1" },
                new AreaNote { Id = "note2", AreaResref = areaResref, PrivateText = "Note 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<AreaNote>>()).Returns(expectedNotes);

            // Act
            var result = _repository.GetByAreaResref(areaResref);

            // Assert
            Assert.That(result, Is.EqualTo(expectedNotes));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<AreaNote>>());
        }

        [Test]
        public void Save_WithValidAreaNote_ShouldCallDatabaseSet()
        {
            // Arrange
            var areaNote = new AreaNote { Id = "test-note", AreaResref = "test-area", PrivateText = "Test note" };

            // Act
            _repository.Save(areaNote);

            // Assert
            _databaseService.Received(1).Set(areaNote);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var noteId = "test-note-id";

            // Act
            _repository.Delete(noteId);

            // Assert
            _databaseService.Received(1).Delete<AreaNote>(noteId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var noteId = "test-note-id";
            _databaseService.Exists<AreaNote>(noteId).Returns(true);

            // Act
            var result = _repository.Exists(noteId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<AreaNote>(noteId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var noteId = "non-existent-note";
            _databaseService.Exists<AreaNote>(noteId).Returns(false);

            // Act
            var result = _repository.Exists(noteId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<AreaNote>(noteId);
        }
    }
}
