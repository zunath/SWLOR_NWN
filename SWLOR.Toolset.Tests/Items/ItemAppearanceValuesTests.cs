using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests.Items
{
    [TestFixture]
    public class ItemAppearanceValuesTests
    {
        [Test]
        public void ExtendedFieldIsAuthoritativeWhenPresent()
        {
            var store = Store(
                """
                "ModelPart1": { "type": "byte", "value": 255 },
                "xModelPart1": { "type": "word", "value": 270 }
                """);

            ItemAppearanceValues.Read(store.Item, "ModelPart1").Should().Be(270);
        }

        [Test]
        public void ValueAboveByteRangeUsesTheExtendedFieldWithoutOverflowingThePrimary()
        {
            var store = Store(
                """
                "ArmorPart_LBicep": { "type": "byte", "value": 1 }
                """);

            ItemAppearanceValues.Write(store, "ArmorPart_LBicep", 270);

            store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_LBicep").Should().Be(byte.MaxValue);
            store.GetInteger(BehaviorFieldStorage.Field, "xArmorPart_LBice").Should().Be(270);
            ItemAppearanceValues.Read(store.Item, "ArmorPart_LBicep").Should().Be(270);
        }

        [Test]
        public void ExistingExtendedFieldStaysSynchronizedWhenValueReturnsToByteRange()
        {
            var store = Store(
                """
                "ModelPart3": { "type": "byte", "value": 255 },
                "xModelPart3": { "type": "word", "value": 259 }
                """);

            ItemAppearanceValues.Write(store, "ModelPart3", 42);

            store.GetInteger(BehaviorFieldStorage.Field, "ModelPart3").Should().Be(42);
            store.GetInteger(BehaviorFieldStorage.Field, "xModelPart3").Should().Be(42);
            ItemAppearanceValues.Read(store.Item, "ModelPart3").Should().Be(42);
        }

        private static ItemValueStore Store(string fields) =>
            new(JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                $$"""
                {
                  "__data_type": "UTI ",
                  {{fields}}
                }
                """)).Root);
    }
}
