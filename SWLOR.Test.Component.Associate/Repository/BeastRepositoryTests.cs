using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Associate.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Associate.Repository
{
    [TestFixture]
    public class BeastRepositoryTests
    {
        private IDatabaseService _db;
        private BeastRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new BeastRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnBeast_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedBeast = new Beast { Id = id, Name = "Test Beast" };
            _db.Get<Beast>(id).Returns(expectedBeast);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBeast));
            _db.Received(1).Get<Beast>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<Beast>(id).Returns((Beast)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<Beast>(id);
        }

        [Test]
        public void GetByOwnerPlayerId_ShouldReturnBeasts_WhenFound()
        {
            // Arrange
            var ownerPlayerId = "player-123";
            var expectedBeasts = new[] { new Beast { OwnerPlayerId = ownerPlayerId } };
            var query = Arg.Any<DBQuery<Beast>>();
            _db.Search(query).Returns(expectedBeasts);

            // Act
            var result = _repository.GetByOwnerPlayerId(ownerPlayerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBeasts));
            _db.Received(1).Search(Arg.Any<DBQuery<Beast>>());
        }

        [Test]
        public void GetByName_ShouldReturnBeasts_WhenFound()
        {
            // Arrange
            var name = "Test Beast";
            var expectedBeasts = new[] { new Beast { Name = name } };
            var query = Arg.Any<DBQuery<Beast>>();
            _db.Search(query).Returns(expectedBeasts);

            // Act
            var result = _repository.GetByName(name);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBeasts));
            _db.Received(1).Search(Arg.Any<DBQuery<Beast>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllBeasts()
        {
            // Arrange
            var expectedBeasts = new[] { new Beast(), new Beast() };
            var query = Arg.Any<DBQuery<Beast>>();
            _db.Search(query).Returns(expectedBeasts);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedBeasts));
            _db.Received(1).Search(Arg.Any<DBQuery<Beast>>());
        }

        [Test]
        public void GetCountByOwnerPlayerId_ShouldReturnCount()
        {
            // Arrange
            var ownerPlayerId = "player-123";
            var expectedCount = 2L;
            var query = Arg.Any<DBQuery<Beast>>();
            _db.SearchCount(query).Returns(expectedCount);

            // Act
            var result = _repository.GetCountByOwnerPlayerId(ownerPlayerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _db.Received(1).SearchCount(Arg.Any<DBQuery<Beast>>());
        }

        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var beast = new Beast { Id = "test-id" };

            // Act
            _repository.Save(beast);

            // Assert
            _db.Received(1).Set(beast);
        }

        [Test]
        public void Delete_ShouldCallDelete()
        {
            // Arrange
            var id = "test-id";

            // Act
            _repository.Delete(id);

            // Assert
            _db.Received(1).Delete<Beast>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<Beast>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<Beast>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<Beast>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<Beast>(id);
        }
    }
}
