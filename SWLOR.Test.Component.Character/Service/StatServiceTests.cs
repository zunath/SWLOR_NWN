using NSubstitute;
using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Character.Service
{
    [TestFixture]
    public class StatCalculationServiceTests : TestBase
    {
        private ICharacterStatService _mockCharacterStatService;
        private IWeaponStatService _mockWeaponStatService;
        private IStatusEffectService _mockStatusEffectService;
        private ISkillService _mockSkillService;
        private StatCalculationService _statService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();

            _mockCharacterStatService = Substitute.For<ICharacterStatService>();
            _mockWeaponStatService = Substitute.For<IWeaponStatService>();
            _mockStatusEffectService = Substitute.For<IStatusEffectService>();
            _mockSkillService = Substitute.For<ISkillService>();

            _statService = new StatCalculationService(_mockCharacterStatService, _mockWeaponStatService, _mockStatusEffectService, _mockSkillService);
        }

        [Test]
        public void StatCalculationService_ConstructsSuccessfully()
        {
            // Arrange & Act & Assert
            Assert.That(_statService, Is.Not.Null);
        }
    }
}