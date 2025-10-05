using NSubstitute;
using SWLOR.Component.Associate.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Test.Component.Associate.Repository
{
    [TestFixture]
    public class IncubationJobRepositoryTests
    {
        private IDatabaseService _db;
        private IncubationJobRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new IncubationJobRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnIncubationJob_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedJob = new IncubationJob { Id = id, PlayerId = "player-123" };
            _db.Get<IncubationJob>(id).Returns(expectedJob);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJob));
            _db.Received(1).Get<IncubationJob>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<IncubationJob>(id).Returns((IncubationJob)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<IncubationJob>(id);
        }

        [Test]
        public void GetByParentPropertyId_ShouldReturnIncubationJobs_WhenFound()
        {
            // Arrange
            var parentPropertyId = "property-123";
            var expectedJobs = new[] { new IncubationJob { ParentPropertyId = parentPropertyId } };
            var query = Arg.Any<DBQuery<IncubationJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetByParentPropertyId(parentPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<IncubationJob>>());
        }

        [Test]
        public void GetByPlayerId_ShouldReturnIncubationJobs_WhenFound()
        {
            // Arrange
            var playerId = "player-123";
            var expectedJobs = new[] { new IncubationJob { PlayerId = playerId } };
            var query = Arg.Any<DBQuery<IncubationJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetByPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<IncubationJob>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllIncubationJobs()
        {
            // Arrange
            var expectedJobs = new[] { new IncubationJob(), new IncubationJob() };
            var query = Arg.Any<DBQuery<IncubationJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<IncubationJob>>());
        }

        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var job = new IncubationJob { Id = "test-id" };

            // Act
            _repository.Save(job);

            // Assert
            _db.Received(1).Set(job);
        }

        [Test]
        public void Delete_ShouldCallDelete()
        {
            // Arrange
            var id = "test-id";

            // Act
            _repository.Delete(id);

            // Assert
            _db.Received(1).Delete<IncubationJob>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<IncubationJob>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<IncubationJob>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<IncubationJob>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<IncubationJob>(id);
        }
    }
}
