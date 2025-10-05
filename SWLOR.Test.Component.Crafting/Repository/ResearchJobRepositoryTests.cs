using NSubstitute;
using SWLOR.Component.Crafting.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Crafting.Repository
{
    [TestFixture]
    public class ResearchJobRepositoryTests
    {
        private IDatabaseService _db;
        private ResearchJobRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new ResearchJobRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnResearchJob_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedJob = new ResearchJob { Id = id, PlayerId = "player-123" };
            _db.Get<ResearchJob>(id).Returns(expectedJob);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJob));
            _db.Received(1).Get<ResearchJob>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<ResearchJob>(id).Returns((ResearchJob)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<ResearchJob>(id);
        }

        [Test]
        public void GetByParentPropertyId_ShouldReturnResearchJobs_WhenFound()
        {
            // Arrange
            var parentPropertyId = "property-123";
            var expectedJobs = new[] { new ResearchJob { ParentPropertyId = parentPropertyId } };
            var query = Arg.Any<DBQuery<ResearchJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetByParentPropertyId(parentPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<ResearchJob>>());
        }

        [Test]
        public void GetByPlayerId_ShouldReturnResearchJobs_WhenFound()
        {
            // Arrange
            var playerId = "player-123";
            var expectedJobs = new[] { new ResearchJob { PlayerId = playerId } };
            var query = Arg.Any<DBQuery<ResearchJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetByPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<ResearchJob>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllResearchJobs()
        {
            // Arrange
            var expectedJobs = new[] { new ResearchJob(), new ResearchJob() };
            var query = Arg.Any<DBQuery<ResearchJob>>();
            _db.Search(query).Returns(expectedJobs);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedJobs));
            _db.Received(1).Search(Arg.Any<DBQuery<ResearchJob>>());
        }

        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var job = new ResearchJob { Id = "test-id" };

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
            _db.Received(1).Delete<ResearchJob>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<ResearchJob>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<ResearchJob>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<ResearchJob>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<ResearchJob>(id);
        }
    }
}
