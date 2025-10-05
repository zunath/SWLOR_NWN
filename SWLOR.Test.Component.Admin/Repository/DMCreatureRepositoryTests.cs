using NSubstitute;
using SWLOR.Component.Admin.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Admin.Repository
{
    [TestFixture]
    public class DMCreatureRepositoryTests
    {
        private IDatabaseService _db;
        private DMCreatureRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new DMCreatureRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnDMCreature_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedCreature = new DMCreature { Id = id, Name = "Test Creature" };
            _db.Get<DMCreature>(id).Returns(expectedCreature);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCreature));
            _db.Received(1).Get<DMCreature>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<DMCreature>(id).Returns((DMCreature)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<DMCreature>(id);
        }

        [Test]
        public void GetByName_ShouldReturnDMCreatures_WhenFound()
        {
            // Arrange
            var name = "Test Creature";
            var expectedCreatures = new[] { new DMCreature { Name = name } };
            var query = Arg.Any<DBQuery<DMCreature>>();
            _db.Search(query).Returns(expectedCreatures);

            // Act
            var result = _repository.GetByName(name);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCreatures));
            _db.Received(1).Search(Arg.Any<DBQuery<DMCreature>>());
        }

        [Test]
        public void GetByTag_ShouldReturnDMCreatures_WhenFound()
        {
            // Arrange
            var tag = "test-tag";
            var expectedCreatures = new[] { new DMCreature { Tag = tag } };
            var query = Arg.Any<DBQuery<DMCreature>>();
            _db.Search(query).Returns(expectedCreatures);

            // Act
            var result = _repository.GetByTag(tag);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCreatures));
            _db.Received(1).Search(Arg.Any<DBQuery<DMCreature>>());
        }

        [Test]
        public void GetAllOrderedByName_ShouldReturnAllDMCreaturesOrdered()
        {
            // Arrange
            var expectedCreatures = new[] { new DMCreature(), new DMCreature() };
            var query = Arg.Any<DBQuery<DMCreature>>();
            _db.Search(query).Returns(expectedCreatures);

            // Act
            var result = _repository.GetAllOrderedByName();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCreatures));
            _db.Received(1).Search(Arg.Any<DBQuery<DMCreature>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllDMCreatures()
        {
            // Arrange
            var expectedCreatures = new[] { new DMCreature(), new DMCreature() };
            var query = Arg.Any<DBQuery<DMCreature>>();
            _db.Search(query).Returns(expectedCreatures);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCreatures));
            _db.Received(1).Search(Arg.Any<DBQuery<DMCreature>>());
        }

        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var creature = new DMCreature { Id = "test-id" };

            // Act
            _repository.Save(creature);

            // Assert
            _db.Received(1).Set(creature);
        }

        [Test]
        public void Delete_ShouldCallDelete()
        {
            // Arrange
            var id = "test-id";

            // Act
            _repository.Delete(id);

            // Assert
            _db.Received(1).Delete<DMCreature>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<DMCreature>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<DMCreature>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<DMCreature>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<DMCreature>(id);
        }
    }
}
