using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Character.ValueObjects
{
    [TestFixture]
    public class RacialAppearanceTests
    {
        [Test]
        public void RacialAppearance_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var racialAppearance = new RacialAppearance();

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(1));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(2));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(0));
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Human));
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.0f));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(1));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(1));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(1));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(1));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(1));
        }

        [Test]
        public void RacialAppearance_WithHeadId_ShouldStoreHeadIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(5));
        }

        [Test]
        public void RacialAppearance_WithSkinColorId_ShouldStoreSkinColorIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.SkinColorId = 3;

            // Assert
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(3));
        }

        [Test]
        public void RacialAppearance_WithHairColorId_ShouldStoreHairColorIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HairColorId = 4;

            // Assert
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(4));
        }

        [Test]
        public void RacialAppearance_WithAppearanceType_ShouldStoreAppearanceTypeCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.AppearanceType = AppearanceType.Twilek;

            // Assert
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Twilek));
        }

        [Test]
        public void RacialAppearance_WithScale_ShouldStoreScaleCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.Scale = 1.5f;

            // Assert
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.5f));
        }

        [Test]
        public void RacialAppearance_WithNeckId_ShouldStoreNeckIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.NeckId = 2;

            // Assert
            Assert.That(racialAppearance.NeckId, Is.EqualTo(2));
        }

        [Test]
        public void RacialAppearance_WithTorsoId_ShouldStoreTorsoIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.TorsoId = 3;

            // Assert
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(3));
        }

        [Test]
        public void RacialAppearance_WithPelvisId_ShouldStorePelvisIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.PelvisId = 4;

            // Assert
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(4));
        }

        [Test]
        public void RacialAppearance_WithRightBicepId_ShouldStoreRightBicepIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightBicepId = 5;

            // Assert
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(5));
        }

        [Test]
        public void RacialAppearance_WithRightForearmId_ShouldStoreRightForearmIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightForearmId = 6;

            // Assert
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(6));
        }

        [Test]
        public void RacialAppearance_WithRightHandId_ShouldStoreRightHandIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightHandId = 7;

            // Assert
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(7));
        }

        [Test]
        public void RacialAppearance_WithRightThighId_ShouldStoreRightThighIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightThighId = 8;

            // Assert
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(8));
        }

        [Test]
        public void RacialAppearance_WithRightShinId_ShouldStoreRightShinIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightShinId = 9;

            // Assert
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(9));
        }

        [Test]
        public void RacialAppearance_WithRightFootId_ShouldStoreRightFootIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.RightFootId = 10;

            // Assert
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(10));
        }

        [Test]
        public void RacialAppearance_WithLeftBicepId_ShouldStoreLeftBicepIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftBicepId = 11;

            // Assert
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(11));
        }

        [Test]
        public void RacialAppearance_WithLeftForearmId_ShouldStoreLeftForearmIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftForearmId = 12;

            // Assert
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(12));
        }

        [Test]
        public void RacialAppearance_WithLeftHandId_ShouldStoreLeftHandIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftHandId = 13;

            // Assert
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(13));
        }

        [Test]
        public void RacialAppearance_WithLeftThighId_ShouldStoreLeftThighIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftThighId = 14;

            // Assert
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(14));
        }

        [Test]
        public void RacialAppearance_WithLeftShinId_ShouldStoreLeftShinIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftShinId = 15;

            // Assert
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(15));
        }

        [Test]
        public void RacialAppearance_WithLeftFootId_ShouldStoreLeftFootIdCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.LeftFootId = 16;

            // Assert
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(16));
        }

        [Test]
        public void RacialAppearance_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;
            racialAppearance.SkinColorId = 3;
            racialAppearance.HairColorId = 4;
            racialAppearance.AppearanceType = AppearanceType.Twilek;
            racialAppearance.Scale = 1.5f;
            racialAppearance.NeckId = 2;
            racialAppearance.TorsoId = 3;
            racialAppearance.PelvisId = 4;
            racialAppearance.RightBicepId = 5;
            racialAppearance.RightForearmId = 6;
            racialAppearance.RightHandId = 7;
            racialAppearance.RightThighId = 8;
            racialAppearance.RightShinId = 9;
            racialAppearance.RightFootId = 10;
            racialAppearance.LeftBicepId = 11;
            racialAppearance.LeftForearmId = 12;
            racialAppearance.LeftHandId = 13;
            racialAppearance.LeftThighId = 14;
            racialAppearance.LeftShinId = 15;
            racialAppearance.LeftFootId = 16;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(5));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(3));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(4));
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Twilek));
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.5f));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(2));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(3));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(4));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(5));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(6));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(7));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(8));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(9));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(10));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(11));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(12));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(13));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(14));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(15));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(16));
        }

        [Test]
        public void RacialAppearance_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 0;
            racialAppearance.SkinColorId = 0;
            racialAppearance.HairColorId = 0;
            racialAppearance.Scale = 0.0f;
            racialAppearance.NeckId = 0;
            racialAppearance.TorsoId = 0;
            racialAppearance.PelvisId = 0;
            racialAppearance.RightBicepId = 0;
            racialAppearance.RightForearmId = 0;
            racialAppearance.RightHandId = 0;
            racialAppearance.RightThighId = 0;
            racialAppearance.RightShinId = 0;
            racialAppearance.RightFootId = 0;
            racialAppearance.LeftBicepId = 0;
            racialAppearance.LeftForearmId = 0;
            racialAppearance.LeftHandId = 0;
            racialAppearance.LeftThighId = 0;
            racialAppearance.LeftShinId = 0;
            racialAppearance.LeftFootId = 0;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(0));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(0));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(0));
            Assert.That(racialAppearance.Scale, Is.EqualTo(0.0f));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(0));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(0));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(0));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(0));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(0));
        }

        [Test]
        public void RacialAppearance_WithNegativeValues_ShouldStoreNegativeValues()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = -1;
            racialAppearance.SkinColorId = -2;
            racialAppearance.HairColorId = -3;
            racialAppearance.Scale = -1.0f;
            racialAppearance.NeckId = -4;
            racialAppearance.TorsoId = -5;
            racialAppearance.PelvisId = -6;
            racialAppearance.RightBicepId = -7;
            racialAppearance.RightForearmId = -8;
            racialAppearance.RightHandId = -9;
            racialAppearance.RightThighId = -10;
            racialAppearance.RightShinId = -11;
            racialAppearance.RightFootId = -12;
            racialAppearance.LeftBicepId = -13;
            racialAppearance.LeftForearmId = -14;
            racialAppearance.LeftHandId = -15;
            racialAppearance.LeftThighId = -16;
            racialAppearance.LeftShinId = -17;
            racialAppearance.LeftFootId = -18;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(-1));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(-2));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(-3));
            Assert.That(racialAppearance.Scale, Is.EqualTo(-1.0f));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(-4));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(-5));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(-6));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(-7));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(-8));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(-9));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(-10));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(-11));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(-12));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(-13));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(-14));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(-15));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(-16));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(-17));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(-18));
        }

        [Test]
        public void RacialAppearance_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 1000;
            racialAppearance.SkinColorId = 2000;
            racialAppearance.HairColorId = 3000;
            racialAppearance.Scale = 10.0f;
            racialAppearance.NeckId = 4000;
            racialAppearance.TorsoId = 5000;
            racialAppearance.PelvisId = 6000;
            racialAppearance.RightBicepId = 7000;
            racialAppearance.RightForearmId = 8000;
            racialAppearance.RightHandId = 9000;
            racialAppearance.RightThighId = 10000;
            racialAppearance.RightShinId = 11000;
            racialAppearance.RightFootId = 12000;
            racialAppearance.LeftBicepId = 13000;
            racialAppearance.LeftForearmId = 14000;
            racialAppearance.LeftHandId = 15000;
            racialAppearance.LeftThighId = 16000;
            racialAppearance.LeftShinId = 17000;
            racialAppearance.LeftFootId = 18000;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(1000));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(2000));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(3000));
            Assert.That(racialAppearance.Scale, Is.EqualTo(10.0f));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(4000));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(5000));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(6000));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(7000));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(8000));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(9000));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(10000));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(11000));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(12000));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(13000));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(14000));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(15000));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(16000));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(17000));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(18000));
        }

        [Test]
        public void RacialAppearance_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = int.MaxValue;
            racialAppearance.SkinColorId = int.MaxValue;
            racialAppearance.HairColorId = int.MaxValue;
            racialAppearance.Scale = float.MaxValue;
            racialAppearance.NeckId = int.MaxValue;
            racialAppearance.TorsoId = int.MaxValue;
            racialAppearance.PelvisId = int.MaxValue;
            racialAppearance.RightBicepId = int.MaxValue;
            racialAppearance.RightForearmId = int.MaxValue;
            racialAppearance.RightHandId = int.MaxValue;
            racialAppearance.RightThighId = int.MaxValue;
            racialAppearance.RightShinId = int.MaxValue;
            racialAppearance.RightFootId = int.MaxValue;
            racialAppearance.LeftBicepId = int.MaxValue;
            racialAppearance.LeftForearmId = int.MaxValue;
            racialAppearance.LeftHandId = int.MaxValue;
            racialAppearance.LeftThighId = int.MaxValue;
            racialAppearance.LeftShinId = int.MaxValue;
            racialAppearance.LeftFootId = int.MaxValue;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.Scale, Is.EqualTo(float.MaxValue));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(int.MaxValue));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void RacialAppearance_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = int.MinValue;
            racialAppearance.SkinColorId = int.MinValue;
            racialAppearance.HairColorId = int.MinValue;
            racialAppearance.Scale = float.MinValue;
            racialAppearance.NeckId = int.MinValue;
            racialAppearance.TorsoId = int.MinValue;
            racialAppearance.PelvisId = int.MinValue;
            racialAppearance.RightBicepId = int.MinValue;
            racialAppearance.RightForearmId = int.MinValue;
            racialAppearance.RightHandId = int.MinValue;
            racialAppearance.RightThighId = int.MinValue;
            racialAppearance.RightShinId = int.MinValue;
            racialAppearance.RightFootId = int.MinValue;
            racialAppearance.LeftBicepId = int.MinValue;
            racialAppearance.LeftForearmId = int.MinValue;
            racialAppearance.LeftHandId = int.MinValue;
            racialAppearance.LeftThighId = int.MinValue;
            racialAppearance.LeftShinId = int.MinValue;
            racialAppearance.LeftFootId = int.MinValue;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.SkinColorId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.HairColorId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.Scale, Is.EqualTo(float.MinValue));
            Assert.That(racialAppearance.NeckId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.TorsoId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.PelvisId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightBicepId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightForearmId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightHandId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightThighId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightShinId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.RightFootId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftBicepId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftForearmId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftHandId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftThighId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftShinId, Is.EqualTo(int.MinValue));
            Assert.That(racialAppearance.LeftFootId, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void RacialAppearance_WithAllAppearanceTypes_ShouldStoreAllAppearanceTypes()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act & Assert
            racialAppearance.AppearanceType = AppearanceType.Human;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Human));

            racialAppearance.AppearanceType = AppearanceType.Twilek;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Twilek));

            racialAppearance.AppearanceType = AppearanceType.Zabrak;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Zabrak));

            racialAppearance.AppearanceType = AppearanceType.Rodian;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Rodian));

            racialAppearance.AppearanceType = AppearanceType.MonCalamari;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.MonCalamari));

            racialAppearance.AppearanceType = AppearanceType.Wookiee;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Wookiee));

            racialAppearance.AppearanceType = AppearanceType.Bothan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Bothan));

            racialAppearance.AppearanceType = AppearanceType.Trandoshan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Trandoshan));

            racialAppearance.AppearanceType = AppearanceType.Chiss;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Chiss));

            racialAppearance.AppearanceType = AppearanceType.Cathar;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Cathar));

            racialAppearance.AppearanceType = AppearanceType.KelDor;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.KelDor));

            racialAppearance.AppearanceType = AppearanceType.Mirialan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Mirialan));

            racialAppearance.AppearanceType = AppearanceType.Nautolan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Nautolan));

            racialAppearance.AppearanceType = AppearanceType.Chiss;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Chiss));

            racialAppearance.AppearanceType = AppearanceType.Togruta;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Togruta));

            racialAppearance.AppearanceType = AppearanceType.Cyborg;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Cyborg));

            racialAppearance.AppearanceType = AppearanceType.Chiss;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Chiss));

            racialAppearance.AppearanceType = AppearanceType.KelDor;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.KelDor));

            racialAppearance.AppearanceType = AppearanceType.Mirialan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Mirialan));

            racialAppearance.AppearanceType = AppearanceType.Nautolan;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Nautolan));

            racialAppearance.AppearanceType = AppearanceType.Chiss;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Chiss));

            racialAppearance.AppearanceType = AppearanceType.Togruta;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Togruta));

            racialAppearance.AppearanceType = AppearanceType.Cyborg;
            Assert.That(racialAppearance.AppearanceType, Is.EqualTo(AppearanceType.Cyborg));
        }

        [Test]
        public void RacialAppearance_WithSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();
            racialAppearance.HeadId = 5;
            racialAppearance.SkinColorId = 3;
            racialAppearance.HairColorId = 4;
            racialAppearance.AppearanceType = AppearanceType.Twilek;
            racialAppearance.Scale = 1.5f;
            racialAppearance.NeckId = 2;
            racialAppearance.TorsoId = 3;
            racialAppearance.PelvisId = 4;
            racialAppearance.RightBicepId = 5;
            racialAppearance.RightForearmId = 6;
            racialAppearance.RightHandId = 7;
            racialAppearance.RightThighId = 8;
            racialAppearance.RightShinId = 9;
            racialAppearance.RightFootId = 10;
            racialAppearance.LeftBicepId = 11;
            racialAppearance.LeftForearmId = 12;
            racialAppearance.LeftHandId = 13;
            racialAppearance.LeftThighId = 14;
            racialAppearance.LeftShinId = 15;
            racialAppearance.LeftFootId = 16;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(racialAppearance);
            var deserializedAppearance = System.Text.Json.JsonSerializer.Deserialize<RacialAppearance>(json);

            // Assert
            Assert.That(deserializedAppearance, Is.Not.Null);
            Assert.That(deserializedAppearance.HeadId, Is.EqualTo(racialAppearance.HeadId));
            Assert.That(deserializedAppearance.SkinColorId, Is.EqualTo(racialAppearance.SkinColorId));
            Assert.That(deserializedAppearance.HairColorId, Is.EqualTo(racialAppearance.HairColorId));
            Assert.That(deserializedAppearance.AppearanceType, Is.EqualTo(racialAppearance.AppearanceType));
            Assert.That(deserializedAppearance.Scale, Is.EqualTo(racialAppearance.Scale));
            Assert.That(deserializedAppearance.NeckId, Is.EqualTo(racialAppearance.NeckId));
            Assert.That(deserializedAppearance.TorsoId, Is.EqualTo(racialAppearance.TorsoId));
            Assert.That(deserializedAppearance.PelvisId, Is.EqualTo(racialAppearance.PelvisId));
            Assert.That(deserializedAppearance.RightBicepId, Is.EqualTo(racialAppearance.RightBicepId));
            Assert.That(deserializedAppearance.RightForearmId, Is.EqualTo(racialAppearance.RightForearmId));
            Assert.That(deserializedAppearance.RightHandId, Is.EqualTo(racialAppearance.RightHandId));
            Assert.That(deserializedAppearance.RightThighId, Is.EqualTo(racialAppearance.RightThighId));
            Assert.That(deserializedAppearance.RightShinId, Is.EqualTo(racialAppearance.RightShinId));
            Assert.That(deserializedAppearance.RightFootId, Is.EqualTo(racialAppearance.RightFootId));
            Assert.That(deserializedAppearance.LeftBicepId, Is.EqualTo(racialAppearance.LeftBicepId));
            Assert.That(deserializedAppearance.LeftForearmId, Is.EqualTo(racialAppearance.LeftForearmId));
            Assert.That(deserializedAppearance.LeftHandId, Is.EqualTo(racialAppearance.LeftHandId));
            Assert.That(deserializedAppearance.LeftThighId, Is.EqualTo(racialAppearance.LeftThighId));
            Assert.That(deserializedAppearance.LeftShinId, Is.EqualTo(racialAppearance.LeftShinId));
            Assert.That(deserializedAppearance.LeftFootId, Is.EqualTo(racialAppearance.LeftFootId));
        }

        [Test]
        public void RacialAppearance_WithEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var racialAppearance1 = new RacialAppearance();
            var racialAppearance2 = new RacialAppearance();
            racialAppearance1.HeadId = 5;
            racialAppearance2.HeadId = 5;

            // Act & Assert
            Assert.That(racialAppearance1.HeadId, Is.EqualTo(racialAppearance2.HeadId));
        }

        [Test]
        public void RacialAppearance_WithInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var racialAppearance1 = new RacialAppearance();
            var racialAppearance2 = new RacialAppearance();
            racialAppearance1.HeadId = 5;
            racialAppearance2.HeadId = 10;

            // Act & Assert
            Assert.That(racialAppearance1.HeadId, Is.Not.EqualTo(racialAppearance2.HeadId));
        }

        [Test]
        public void RacialAppearance_WithToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            var result = racialAppearance.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void RacialAppearance_WithGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            var type = racialAppearance.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(RacialAppearance)));
        }

        [Test]
        public void RacialAppearance_WithHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            var hashCode = racialAppearance.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }

        [Test]
        public void RacialAppearance_WithScaleIncrement_ShouldIncrementScale()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.Scale = 1.0f;
            racialAppearance.Scale += 0.5f;

            // Assert
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.5f));
        }

        [Test]
        public void RacialAppearance_WithScaleDecrement_ShouldDecrementScale()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.Scale = 2.0f;
            racialAppearance.Scale -= 0.5f;

            // Assert
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.5f));
        }

        [Test]
        public void RacialAppearance_WithScaleMultiplication_ShouldMultiplyScale()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.Scale = 1.0f;
            racialAppearance.Scale *= 2.0f;

            // Assert
            Assert.That(racialAppearance.Scale, Is.EqualTo(2.0f));
        }

        [Test]
        public void RacialAppearance_WithScaleDivision_ShouldDivideScale()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.Scale = 2.0f;
            racialAppearance.Scale /= 2.0f;

            // Assert
            Assert.That(racialAppearance.Scale, Is.EqualTo(1.0f));
        }

        [Test]
        public void RacialAppearance_WithIdIncrement_ShouldIncrementId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;
            racialAppearance.HeadId++;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(6));
        }

        [Test]
        public void RacialAppearance_WithIdDecrement_ShouldDecrementId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;
            racialAppearance.HeadId--;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(4));
        }

        [Test]
        public void RacialAppearance_WithIdAddition_ShouldAddToId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;
            racialAppearance.HeadId += 3;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(8));
        }

        [Test]
        public void RacialAppearance_WithIdSubtraction_ShouldSubtractFromId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 10;
            racialAppearance.HeadId -= 3;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(7));
        }

        [Test]
        public void RacialAppearance_WithIdMultiplication_ShouldMultiplyId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 5;
            racialAppearance.HeadId *= 3;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(15));
        }

        [Test]
        public void RacialAppearance_WithIdDivision_ShouldDivideId()
        {
            // Arrange
            var racialAppearance = new RacialAppearance();

            // Act
            racialAppearance.HeadId = 15;
            racialAppearance.HeadId /= 3;

            // Assert
            Assert.That(racialAppearance.HeadId, Is.EqualTo(5));
        }
    }
}
