using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Properties.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Properties.Repository
{
    [TestFixture]
    public class ElectionRepositoryTests
    {
        private ElectionRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new ElectionRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnElection()
        {
            // Arrange
            var electionId = "test-election-id";
            var expectedElection = new Election { Id = electionId, PropertyId = "test-property" };
            _databaseService.Get<Election>(electionId).Returns(expectedElection);

            // Act
            var result = _repository.GetById(electionId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedElection));
            _databaseService.Received(1).Get<Election>(electionId);
        }

        [Test]
        public void GetByPropertyId_WithValidPropertyId_ShouldReturnElections()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedElections = new List<Election> { new Election { Id = "election1", PropertyId = propertyId } };
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(expectedElections);

            // Act
            var result = _repository.GetByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedElections));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }

        [Test]
        public void GetByPropertyId_WithNoMatchingElection_ShouldReturnEmptyCollection()
        {
            // Arrange
            var propertyId = "test-property-id";
            var searchResults = new List<Election>();
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(searchResults);

            // Act
            var result = _repository.GetByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(searchResults));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }

        [Test]
        public void Save_WithValidElection_ShouldCallDatabaseSet()
        {
            // Arrange
            var election = new Election { Id = "test-election", PropertyId = "test-property" };

            // Act
            _repository.Save(election);

            // Assert
            _databaseService.Received(1).Set(election);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var electionId = "test-election-id";

            // Act
            _repository.Delete(electionId);

            // Assert
            _databaseService.Received(1).Delete<Election>(electionId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var electionId = "test-election-id";
            _databaseService.Exists<Election>(electionId).Returns(true);

            // Act
            var result = _repository.Exists(electionId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<Election>(electionId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var electionId = "non-existent-election";
            _databaseService.Exists<Election>(electionId).Returns(false);

            // Act
            var result = _repository.Exists(electionId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<Election>(electionId);
        }

        [Test]
        public void GetByStage_WithValidStage_ShouldReturnElections()
        {
            // Arrange
            var stage = ElectionStageType.Registration;
            var expectedElections = new List<Election>
            {
                new Election { Id = "election1", Stage = stage },
                new Election { Id = "election2", Stage = stage }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(expectedElections);

            // Act
            var result = _repository.GetByStage(stage);

            // Assert
            Assert.That(result, Is.EqualTo(expectedElections));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }

        [Test]
        public void GetByPropertyIdAndStage_WithValidPropertyIdAndStage_ShouldReturnElections()
        {
            // Arrange
            var propertyId = "test-property-id";
            var stage = ElectionStageType.Voting;
            var expectedElections = new List<Election>
            {
                new Election { Id = "election1", PropertyId = propertyId, Stage = stage }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(expectedElections);

            // Act
            var result = _repository.GetByPropertyIdAndStage(propertyId, stage);

            // Assert
            Assert.That(result, Is.EqualTo(expectedElections));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }

        [Test]
        public void GetSingleByPropertyId_WithValidPropertyId_ShouldReturnSingleElection()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedElection = new Election { Id = "election1", PropertyId = propertyId };
            var searchResults = new List<Election> { expectedElection };
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(searchResults);

            // Act
            var result = _repository.GetSingleByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedElection));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }

        [Test]
        public void GetSingleByPropertyId_WithNoMatchingElection_ShouldReturnNull()
        {
            // Arrange
            var propertyId = "test-property-id";
            var searchResults = new List<Election>();
            _databaseService.Search(Arg.Any<IDBQuery<Election>>()).Returns(searchResults);

            // Act
            var result = _repository.GetSingleByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.Null);
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Election>>());
        }
    }
}
