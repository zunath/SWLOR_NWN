using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Tests.Feature;

public class RifleSupportAnimationCatalogTests
{
    private static readonly Type Catalog = typeof(WeaponFit).Assembly.GetType(
        "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.RifleSupportAnimationCatalog")!;
    private static readonly RifleSupportPoses Poses = new("rf267_101_i", "rf267_101_r", "rf267_101_s");
    private static object Load(params (string Right, string Left, int Model, RifleSupportPoses Poses)[] rows) =>
        Catalog.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke(new object[] { rows });
    private static RifleSupportPoses Resolve(object catalog, string right, string left, int model) =>
        (RifleSupportPoses)Catalog.GetMethod("Resolve", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(catalog, new object[] { right, left, model });

    [Test]
    public void MixedHandStylesAndRenderedAliasesResolveIndependentlyByWeaponModel()
    {
        var catalog = Load(("pfo0_handr52267", "pfo0_handl260", 101, Poses));
        Resolve(catalog, "PFO0_HANDR52267", "PFO0_HANDL260", 101).Should().Be(Poses);
        Resolve(catalog, "pfo0_handr52267", "pfo0_handl267", 101).Should().BeNull();
        Resolve(catalog, "pfo0_handr52267", "pfo0_handl260", 102).Should().BeNull();
    }

    [Test]
    public void CaseVariantsResolveButCannotCreateDuplicateSelections()
    {
        var catalog = Load(("PFO0_HANDR267", "PFO0_HANDL267", 101, Poses));
        Resolve(catalog, "pfo0_handr267", "pfo0_handl267", 101).Should().Be(Poses);
        Action duplicate = () => Load(("pfo0_handr267", "pfo0_handl267", 101, Poses),
            ("PFO0_HANDR267", "PFO0_HANDL267", 101, Poses));
        duplicate.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }

    [TestCase("pfo0_handl267", "pfo0_handl267", 101)]
    [TestCase("pfo0_handr267", "pfo0_handr267", 101)]
    [TestCase("pfo0_handr267", "pfh0_handl267", 101)]
    [TestCase("pfo2_handr267", "pfo2_handl267", 101)]
    [TestCase("pfo0_handr65536", "pfo0_handl267", 101)]
    [TestCase("pfo0_handr267", "pfo0_handl000", 101)]
    [TestCase("pfo0_handr267", "pfo0_handl267", 0)]
    public void InvalidOrCrossBodySelectionsAreRejected(string right, string left, int model)
    {
        Action load = () => Load((right, left, model, Poses));
        load.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }

    [TestCase("xbowr")]
    [TestCase("rf_invalid_name_is_too_long")]
    [TestCase("rf-invalid")]
    [TestCase("rf_untyped")]
    [TestCase("rf267_101_r")]
    [TestCase("rf123456789_i")]
    public void ClipNamesMustBeQualifiedNativeRifleResources(string name)
    {
        Action load = () => Load(("pfo0_handr267", "pfo0_handl267", 101, new RifleSupportPoses(name, Poses.Ready, Poses.Shot)));
        load.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }
}
