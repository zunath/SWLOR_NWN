using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

public class CuHeadWhitelistTests
{
    public static IEnumerable<TestCaseData> OriginalTestCatalog()
    {
        yield return new TestCaseData(typeof(CatharRacialAppearanceDefinition), "Female", new[] { 19, 20, 21, 22, 23, 24 }).SetName("CatharRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(ChissRacialAppearanceDefinition), "Female", new[] { 279, 280, 281, 282, 283, 284, 285, 286, 289, 290, 310, 311, 312, 313, 316, 317, 318, 319, 320, 321, 322, 323, 327 }).SetName("ChissRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(ChissRacialAppearanceDefinition), "Male", new[] { 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 334, 335 }).SetName("ChissRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(EchaniRacialAppearanceDefinition), "Female", new[] { 279, 280, 281, 282, 283, 284, 285, 286, 289, 290, 310, 311, 312, 313, 316, 317, 318, 319, 320, 321, 322, 323, 327 }).SetName("EchaniRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(EchaniRacialAppearanceDefinition), "Male", new[] { 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 334, 335 }).SetName("EchaniRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(HumanRacialAppearanceDefinition), "Female", new[] { 279, 280, 281, 282, 283, 284, 285, 286, 289, 290, 310, 311, 312, 313, 316, 317, 318, 319, 320, 321, 322, 323, 327 }).SetName("HumanRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(HumanRacialAppearanceDefinition), "Male", new[] { 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 334, 335 }).SetName("HumanRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(MirialanRacialAppearanceDefinition), "Female", new[] { 279, 280, 281, 282, 283, 284, 285, 286, 289, 290, 310, 311, 312, 313, 316, 317, 318, 319, 320, 321, 322, 323, 327 }).SetName("MirialanRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(MirialanRacialAppearanceDefinition), "Male", new[] { 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 334, 335 }).SetName("MirialanRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(MonCalamariRacialAppearanceDefinition), "Female", new[] { 324, 325, 330 }).SetName("MonCalamariRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(MonCalamariRacialAppearanceDefinition), "Male", new[] { 331, 332 }).SetName("MonCalamariRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(NautolanRacialAppearanceDefinition), "Male", new[] { 337 }).SetName("NautolanRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(TogrutaRacialAppearanceDefinition), "Female", new[] { 219 }).SetName("TogrutaRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(TrandoshanRacialAppearanceDefinition), "Female", new[] { 326, 329 }).SetName("TrandoshanRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(TrandoshanRacialAppearanceDefinition), "Male", new[] { 333, 338 }).SetName("TrandoshanRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(TwilekRacialAppearanceDefinition), "Male", new[] { 339 }).SetName("TwilekRacialAppearanceDefinition_Male_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(ZabrakRacialAppearanceDefinition), "Female", new[] { 315 }).SetName("ZabrakRacialAppearanceDefinition_Female_RetainsCuHeadIds");
        yield return new TestCaseData(typeof(ZabrakRacialAppearanceDefinition), "Male", new[] { 336 }).SetName("ZabrakRacialAppearanceDefinition_Male_RetainsCuHeadIds");
    }

    [Test]
    public void MaleLyrenHeadIsOnlySelectableForMaleNautolans()
    {
        var definition = new NautolanRacialAppearanceDefinition();
        definition.MaleHeads.Should().Contain(337);
        definition.FemaleHeads.Should().NotContain(328);
        definition.FemaleHeads.Should().Contain(new[] { 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 257, 278 });
    }

    [TestCaseSource(nameof(OriginalTestCatalog))]
    public void RacialDefinitionsRetainThePreviouslyDeployedCuHeads(Type definitionType, string sex, int[] expected)
    {
        var definition = Activator.CreateInstance(definitionType)!;
        var actual = (int[])definitionType.GetProperty(sex + "Heads")!.GetValue(definition)!;
        actual.Should().Contain(expected);
        actual.Should().OnlyHaveUniqueItems();
    }
}
