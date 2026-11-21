using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>
/// The settings of every weapon ability, taken before the weapon ability base is split: for each feat every ability detail property and
/// every profile property that differs from the default. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilitySettingsTests
{
    private FacadeContainer _facades = null!;
    private IDisposable _scope = null!;

    [SetUp]
    public void Install()
    {
        _facades = new FacadeContainer();
        _scope = _facades.Install();
    }

    [TearDown]
    public void Remove()
    {
        _scope.Dispose();
        _facades.Dispose();
    }

    [Test]
    public void EveryWeaponAbilityKeepsItsSettings()
    {
        var entries = WeaponAbilityCatalogReader.Build();

        entries.Should().NotBeEmpty();
        WeaponAbilityGolden.Assert("SWLOR.Game.Server.Tests/Fixtures/WeaponAbility/WeaponAbilitySettings.txt", WeaponAbilitySettingsWriter.Write(entries));
    }

    [Test]
    public void TheSettingsAreTheSameWhenBuiltTwice()
    {
        WeaponAbilitySettingsWriter.Write(WeaponAbilityCatalogReader.Build())
            .Should().Be(WeaponAbilitySettingsWriter.Write(WeaponAbilityCatalogReader.Build()));
    }
}
