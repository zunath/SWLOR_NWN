using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A creature made by New Creature can actually be seen.
    /// </summary>
    /// <remarks>
    /// The template picks appearance 6, a <c>MODELTYPE P</c> dynamic human, which has no model of its own
    /// and is assembled from per-part fields. Those were never written, so the parts list came out empty,
    /// <c>BlueprintModelResolver</c> returned no model, and the creature was invisible - and unrepairable,
    /// because the editor schema exposes no part fields to add by hand.
    /// </remarks>
    [TestFixture]
    public class NewCreatureAppearanceTests
    {
        private static JsonElement NewCreature()
        {
            var content = BlueprintTemplateFactory.CreateFileContent(ResourceType.Utc, "probe_creature", "Probe");
            return JsonDocument.Parse(Encoding.UTF8.GetString(content)).RootElement;
        }

        private static int Value(JsonElement root, string field) =>
            root.GetProperty(field).GetProperty("value").GetInt32();

        /// <summary>
        /// Base item 0 is the short sword, a <c>ModelType=2</c> composite weapon assembled from three
        /// parts. They were never written, so they read back as part 0, which has no model - and
        /// <c>UtiSchema</c> exposes only the base-item selector, so the appearance could not be repaired
        /// in the toolset either. Same failure as the creature above, different blueprint type.
        /// </summary>
        [Test]
        public void ANewItemGetsItsThreeWeaponModelParts()
        {
            var content = BlueprintTemplateFactory.CreateFileContent(ResourceType.Uti, "probe_item", "Probe");
            var root = JsonDocument.Parse(Encoding.UTF8.GetString(content)).RootElement;

            root.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(0, "the template picks the short sword");

            foreach (var part in new[] { "ModelPart1", "ModelPart2", "ModelPart3" })
            {
                root.GetProperty(part).GetProperty("value").GetInt32()
                    .Should().Be(1, $"{part} 0 has no model at all");
            }
        }

        [Test]
        public void ItStillUsesTheDynamicHumanAppearance()
        {
            Value(NewCreature(), "Appearance_Type").Should().Be(6);
        }

        [Test]
        public void EveryStructuralBodyPartIsPresent()
        {
            var root = NewCreature();

            foreach (var part in new[]
                     {
                         "BodyPart_LBicep", "BodyPart_LFArm", "BodyPart_LFoot", "BodyPart_LHand",
                         "BodyPart_LShin", "BodyPart_LThigh", "BodyPart_Neck", "BodyPart_Pelvis",
                         "BodyPart_RBicep", "BodyPart_RFArm", "BodyPart_RHand", "BodyPart_RShin",
                         "BodyPart_RThigh", "BodyPart_Torso"
                     })
            {
                Value(root, part).Should().Be(1, $"{part} is needed to assemble the model");
            }

            Value(root, "Appearance_Head").Should().Be(1);
            Value(root, "ArmorPart_RFoot").Should().Be(1);
        }

        [Test]
        public void TheAccessorySlotsStartEmpty()
        {
            var root = NewCreature();

            // Belt and shoulders are optional pieces; the module's own creatures leave them at 0.
            Value(root, "BodyPart_Belt").Should().Be(0);
            Value(root, "BodyPart_LShoul").Should().Be(0);
            Value(root, "BodyPart_RShoul").Should().Be(0);
        }

        [Test]
        public void ColoursGetANeutralStartingValue()
        {
            var root = NewCreature();

            foreach (var colour in new[] { "Color_Hair", "Color_Skin", "Color_Tattoo1", "Color_Tattoo2" })
                Value(root, colour).Should().Be(1);
        }

        [Test]
        public void ThePartSetMatchesWhatTheModulesOwnCreaturesCarry()
        {
            // The authority for "which fields does appearance 6 need" is the checked-in corpus, not this
            // test's own list - so compare against a real one rather than restating the expectation.
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "utc");
            if (!Directory.Exists(directory))
                Assert.Ignore("The module's creature blueprints are not present in this checkout.");

            var sample = Directory.EnumerateFiles(directory, "*.utc.json")
                .Select(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement)
                .FirstOrDefault(root =>
                    root.TryGetProperty("Appearance_Type", out var a) &&
                    a.GetProperty("value").GetInt32() == 6 &&
                    root.TryGetProperty("BodyPart_Torso", out _));

            if (sample.ValueKind == JsonValueKind.Undefined)
                Assert.Ignore("No appearance-6 creature was found to compare against.");

            var created = NewCreature();
            var missing = sample.EnumerateObject()
                .Select(p => p.Name)
                .Where(n => n.StartsWith("BodyPart_") || n.StartsWith("Color_") ||
                            n == "Appearance_Head" || n == "ArmorPart_RFoot")
                .Where(n => !created.TryGetProperty(n, out _))
                .ToList();

            missing.Should().BeEmpty("a new creature needs every part field a real one has");
        }
    }
}
