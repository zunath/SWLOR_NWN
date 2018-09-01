using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Tests.Service
{
    public class BackgroundServiceTests
    {
        private IDataContext _db;
        private INWScript _;
        private IPerkService _perk;
        private ISkillService _skill;
        private INWNXCreature _nwnxCreature;

        [SetUp]
        public void SetUp()
        {
            IQueryable<Background> backgrounds =
                new List<Background>
                {
                    new Background{BackgroundID = 1, Bonuses = "+1 STR", Description = "MyBackground1", IsActive = true, Name = "Background 1"},
                    new Background{BackgroundID = 2, Bonuses = "None", Description = "MyBackground2", IsActive = true, Name = "Background 2"},
                    new Background{BackgroundID = 3, Bonuses = "+1 DEX", Description = "MyBackground3", IsActive = true, Name = "Background 3"},
                    new Background{BackgroundID = 4, Bonuses = "+2 STR", Description = "MyBackground4", IsActive = false, Name = "Background 4"},
                    new Background{BackgroundID = 5, Bonuses = "+1 WIS", Description = "MyBackground5", IsActive = true, Name = "Background 5"},
                    new Background{BackgroundID = 6, Bonuses = "+1 CHA", Description = "MyBackground6", IsActive = false, Name = "Background 6"},
                    new Background{BackgroundID = 7, Bonuses = "+4 CON", Description = "MyBackground7", IsActive = true, Name = "Background 7"},
                }.AsQueryable();


            IDbSet<Background> backgroundDbSet = Substitute.For<IDbSet<Background>>();
            backgroundDbSet.Provider.Returns(backgrounds.Provider);
            backgroundDbSet.Expression.Returns(backgrounds.Expression);
            backgroundDbSet.ElementType.Returns(backgrounds.ElementType);
            backgroundDbSet.GetEnumerator().Returns(backgrounds.GetEnumerator());


            _db = Substitute.For<IDataContext>();
            _db.Backgrounds.Returns(backgroundDbSet);

            _ = Substitute.For<INWScript>();
            _perk = Substitute.For<IPerkService>();
            _skill = Substitute.For<ISkillService>();
            _nwnxCreature = Substitute.For<INWNXCreature>();
        }

        [Test]
        public void BackgroundService_GetActiveBackgrounds_ShouldReturn5Active()
        {
            // Arrange
            var service = new BackgroundService(_db, _, _perk, _skill);

            // Act
            var results = service.GetActiveBackgrounds().ToList();

            // Assert
            Assert.AreEqual(5, results.Count);
        }

        [Test]
        public void BackgroundService_GetActiveBackgrounds_ShouldMatchData()
        {
            // Arrange
            var service = new BackgroundService(_db, _, _perk, _skill);

            // Act
            var results = service.GetActiveBackgrounds().ToList();

            // Assert
            Assert.AreEqual(1, results[0].BackgroundID);
            Assert.AreEqual("+1 STR", results[0].Bonuses);
            Assert.AreEqual("MyBackground1", results[0].Description);
            Assert.AreEqual(true, results[0].IsActive);
            Assert.AreEqual("Background 1", results[0].Name);

            Assert.AreEqual(2, results[1].BackgroundID);
            Assert.AreEqual("None", results[1].Bonuses);
            Assert.AreEqual("MyBackground2", results[1].Description);
            Assert.AreEqual(true, results[1].IsActive);
            Assert.AreEqual("Background 2", results[1].Name);

            Assert.AreEqual(3, results[2].BackgroundID);
            Assert.AreEqual("+1 DEX", results[2].Bonuses);
            Assert.AreEqual("MyBackground3", results[2].Description);
            Assert.AreEqual(true, results[2].IsActive);
            Assert.AreEqual("Background 3", results[2].Name);

            Assert.AreEqual(5, results[3].BackgroundID);
            Assert.AreEqual("+1 WIS", results[3].Bonuses);
            Assert.AreEqual("MyBackground5", results[3].Description);
            Assert.AreEqual(true, results[3].IsActive);
            Assert.AreEqual("Background 5", results[3].Name);

            Assert.AreEqual(7, results[4].BackgroundID);
            Assert.AreEqual("+4 CON", results[4].Bonuses);
            Assert.AreEqual("MyBackground7", results[4].Description);
            Assert.AreEqual(true, results[4].IsActive);
            Assert.AreEqual("Background 7", results[4].Name);
        }

        [Test]
        public void BackgroundService_SetPlayerBackground_IDShouldBe7()
        {
            // Arrange
            var player1 = Substitute.For<NWPlayer>(_, _nwnxCreature);
            player1.GlobalID.Returns("123");

            var player2 = Substitute.For<NWPlayer>();
            player2.GlobalID.Returns("789");

            IQueryable<PlayerCharacter> players =
                new List<PlayerCharacter>
                {
                    new PlayerCharacter { BackgroundID = 0, PlayerID = player1.GlobalID},
                    new PlayerCharacter { BackgroundID = 3, PlayerID = player2.GlobalID},
                }.AsQueryable();

            IDbSet<PlayerCharacter> playerDbSet = Substitute.For<IDbSet<PlayerCharacter>>();
            playerDbSet.Provider.Returns(players.Provider);
            playerDbSet.Expression.Returns(players.Expression);
            playerDbSet.ElementType.Returns(players.ElementType);
            playerDbSet.GetEnumerator().Returns(players.GetEnumerator());

            _db.PlayerCharacters.Returns(playerDbSet);

            var service = new BackgroundService(_db, _, _perk, _skill);
            var backgrounds = service.GetActiveBackgrounds().ToList();
            
            // Act
            service.SetPlayerBackground(player1, backgrounds[4]);
            var dbPlayer1 = _db.PlayerCharacters.Single(x => x.PlayerID == player1.GlobalID);
            var dbPlayer2 = _db.PlayerCharacters.Single(x => x.PlayerID == player2.GlobalID);

            // Assert
            Assert.AreEqual(7, dbPlayer1.BackgroundID);
            Assert.AreEqual(3, dbPlayer2.BackgroundID);
        }
    }
}
