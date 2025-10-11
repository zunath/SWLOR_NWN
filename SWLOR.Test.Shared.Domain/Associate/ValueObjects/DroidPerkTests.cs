using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class DroidPerkTests
    {
        [Test]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var perk = new DroidPerk();

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.Invalid));
            Assert.That(perk.Level, Is.EqualTo(0));
        }

    }
}
