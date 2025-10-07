using SWLOR.NWN.API.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.AI.Enums;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.AI.Service
{
    [TestFixture]
    public class AIServiceTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void TestFrameworkSetup_Works()
        {
            // Arrange
            var creature = OBJECT_INVALID + 1u;

            // Act & Assert - This verifies NWScript mocking is working
            NWScript.SetLocalInt(creature, "test", 42);
            var result = NWScript.GetLocalInt(creature, "test");

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void AIFlagType_EnumValues_AreAccessible()
        {
            // Test that we can access the AI flag enum values
            var randomWalk = AIFlagType.RandomWalk;
            var returnHome = AIFlagType.ReturnHome;
            var combined = randomWalk | returnHome;

            Assert.That(combined, Is.EqualTo(AIFlagType.RandomWalk | AIFlagType.ReturnHome));
        }
    }
}