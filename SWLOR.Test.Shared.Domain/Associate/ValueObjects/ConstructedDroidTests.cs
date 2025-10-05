using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class ConstructedDroidTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var droid = new ConstructedDroid();

            // Assert
            Assert.That(droid.Name, Is.EqualTo(string.Empty));
            Assert.That(droid.SerializedCPU, Is.EqualTo(string.Empty));
            Assert.That(droid.SerializedHead, Is.EqualTo(string.Empty));
            Assert.That(droid.SerializedBody, Is.EqualTo(string.Empty));
            Assert.That(droid.SerializedArms, Is.EqualTo(string.Empty));
            Assert.That(droid.SerializedLegs, Is.EqualTo(string.Empty));
            Assert.That(droid.PortraitId, Is.EqualTo(-1));
            Assert.That(droid.SoundSetId, Is.EqualTo(-1));
            Assert.That(droid.AppearanceParts, Is.Not.Null);
            Assert.That(droid.AppearanceParts, Is.Empty);
            Assert.That(droid.LearnedPerks, Is.Not.Null);
            Assert.That(droid.LearnedPerks, Is.Empty);
            Assert.That(droid.ActivePerks, Is.Not.Null);
            Assert.That(droid.ActivePerks, Is.Empty);
            Assert.That(droid.EquippedItems, Is.Not.Null);
            Assert.That(droid.EquippedItems, Is.Empty);
            Assert.That(droid.Inventory, Is.Not.Null);
            Assert.That(droid.Inventory, Is.Empty);
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var droid = new ConstructedDroid();
            var perk = new DroidPerk();

            // Act
            droid.Name = "Test Droid";
            droid.SerializedCPU = "cpu_data";
            droid.SerializedHead = "head_data";
            droid.SerializedBody = "body_data";
            droid.SerializedArms = "arms_data";
            droid.SerializedLegs = "legs_data";
            droid.PortraitId = 123;
            droid.SoundSetId = 456;
            droid.AppearanceParts.Add(CreaturePartType.Head, 1);
            droid.LearnedPerks.Add(perk);
            droid.ActivePerks.Add(perk);
            droid.EquippedItems.Add(InventorySlotType.Head, "item1");
            droid.Inventory.Add("slot1", "item2");

            // Assert
            Assert.That(droid.Name, Is.EqualTo("Test Droid"));
            Assert.That(droid.SerializedCPU, Is.EqualTo("cpu_data"));
            Assert.That(droid.SerializedHead, Is.EqualTo("head_data"));
            Assert.That(droid.SerializedBody, Is.EqualTo("body_data"));
            Assert.That(droid.SerializedArms, Is.EqualTo("arms_data"));
            Assert.That(droid.SerializedLegs, Is.EqualTo("legs_data"));
            Assert.That(droid.PortraitId, Is.EqualTo(123));
            Assert.That(droid.SoundSetId, Is.EqualTo(456));
            Assert.That(droid.AppearanceParts.Count, Is.EqualTo(1));
            Assert.That(droid.LearnedPerks.Count, Is.EqualTo(1));
            Assert.That(droid.ActivePerks.Count, Is.EqualTo(1));
            Assert.That(droid.EquippedItems.Count, Is.EqualTo(1));
            Assert.That(droid.Inventory.Count, Is.EqualTo(1));
        }

        [Test]
        public void AppearanceParts_ShouldBeMutable()
        {
            // Arrange
            var droid = new ConstructedDroid();

            // Act
            droid.AppearanceParts.Add(CreaturePartType.Head, 1);
            droid.AppearanceParts.Add(CreaturePartType.Torso, 2);

            // Assert
            Assert.That(droid.AppearanceParts.Count, Is.EqualTo(2));
            Assert.That(droid.AppearanceParts[CreaturePartType.Head], Is.EqualTo(1));
            Assert.That(droid.AppearanceParts[CreaturePartType.Torso], Is.EqualTo(2));
        }

        [Test]
        public void LearnedPerks_ShouldBeMutable()
        {
            // Arrange
            var droid = new ConstructedDroid();
            var perk1 = new DroidPerk();
            var perk2 = new DroidPerk();

            // Act
            droid.LearnedPerks.Add(perk1);
            droid.LearnedPerks.Add(perk2);

            // Assert
            Assert.That(droid.LearnedPerks.Count, Is.EqualTo(2));
            Assert.That(droid.LearnedPerks, Contains.Item(perk1));
            Assert.That(droid.LearnedPerks, Contains.Item(perk2));
        }

        [Test]
        public void ActivePerks_ShouldBeMutable()
        {
            // Arrange
            var droid = new ConstructedDroid();
            var perk1 = new DroidPerk();
            var perk2 = new DroidPerk();

            // Act
            droid.ActivePerks.Add(perk1);
            droid.ActivePerks.Add(perk2);

            // Assert
            Assert.That(droid.ActivePerks.Count, Is.EqualTo(2));
            Assert.That(droid.ActivePerks, Contains.Item(perk1));
            Assert.That(droid.ActivePerks, Contains.Item(perk2));
        }

        [Test]
        public void EquippedItems_ShouldBeMutable()
        {
            // Arrange
            var droid = new ConstructedDroid();

            // Act
            droid.EquippedItems.Add(InventorySlotType.Head, "helmet");
            droid.EquippedItems.Add(InventorySlotType.Chest, "armor");

            // Assert
            Assert.That(droid.EquippedItems.Count, Is.EqualTo(2));
            Assert.That(droid.EquippedItems[InventorySlotType.Head], Is.EqualTo("helmet"));
            Assert.That(droid.EquippedItems[InventorySlotType.Chest], Is.EqualTo("armor"));
        }

        [Test]
        public void Inventory_ShouldBeMutable()
        {
            // Arrange
            var droid = new ConstructedDroid();

            // Act
            droid.Inventory.Add("slot1", "item1");
            droid.Inventory.Add("slot2", "item2");

            // Assert
            Assert.That(droid.Inventory.Count, Is.EqualTo(2));
            Assert.That(droid.Inventory["slot1"], Is.EqualTo("item1"));
            Assert.That(droid.Inventory["slot2"], Is.EqualTo("item2"));
        }
    }
}
