using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class MoreNWNXEventsTests
    {
        [Test]
        public void OnBarterEndAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnBarterEndAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnBarterEndAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBarterEndBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnBarterEndBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnBarterEndBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBarterStartAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnBarterStartAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnBarterStartAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBarterStartBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnBarterStartBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnBarterStartBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarDawn_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarDawn();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarDawn));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarDay_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarDay();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarDay));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarDusk_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarDusk();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarDusk));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarHour_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarHour();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarHour));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarMonth_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarMonth();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarMonth));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCalendarYear_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnCalendarYear();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCalendarYear));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnEffectAppliedAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnEffectAppliedAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnEffectAppliedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnEffectAppliedBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnEffectAppliedBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnEffectAppliedBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnEffectRemovedAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnEffectRemovedAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnEffectRemovedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnEffectRemovedBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnEffectRemovedBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnEffectRemovedBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnHealAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnHealAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnHealAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnHealBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnHealBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnHealBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnHealerKitAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnHealerKitAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnHealerKitAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnHealerKitBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnHealerKitBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnHealerKitBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnInventoryOpenAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnInventoryOpenAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnInventoryOpenAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnInventoryOpenBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnInventoryOpenBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnInventoryOpenBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnLevelDownAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnLevelDownAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnLevelDownAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnLevelDownBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnLevelDownBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnLevelDownBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnLevelUpAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnLevelUpAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnLevelUpAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnLevelUpBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnLevelUpBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnLevelUpBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPolymorphAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnPolymorphAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPolymorphAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPolymorphBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnPolymorphBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPolymorphBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUnpolymorphAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnUnpolymorphAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUnpolymorphAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUnpolymorphBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnUnpolymorphBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUnpolymorphBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnQuickchatAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnQuickchatAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnQuickchatAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnQuickchatBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnQuickchatBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnQuickchatBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnStartCombatRoundAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnStartCombatRoundAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnStartCombatRoundAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnStartCombatRoundBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnStartCombatRoundBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnStartCombatRoundBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnValidateItemEquipAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnValidateItemEquipAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnValidateItemEquipAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnValidateItemEquipBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnValidateItemEquipBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnValidateItemEquipBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnValidateUseItemAfter_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnValidateUseItemAfter();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnValidateUseItemAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnValidateUseItemBefore_ShouldHaveCorrectScript()
        {
            var eventInstance = new OnValidateUseItemBefore();
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnValidateUseItemBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}

