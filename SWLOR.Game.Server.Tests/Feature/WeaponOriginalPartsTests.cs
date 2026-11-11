using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Rare weapons such as the Muster Line Edge come with parts the shared appearance editor lists lack. Each
/// weapon keeps its own part as an extra choice; other weapons of the type never see it.
/// </summary>
public class WeaponOriginalPartsTests
{
    private static readonly int[] Shared = { 101, 105, 501, 505 };

    [Test]
    public void APartMissingFromTheSharedListBecomesTheWeaponsOwn()
    {
        WeaponOriginalParts.Resolve(0, 502, Shared).Should().Be(502);
    }

    [Test]
    public void ASharedPartIsNotRecorded()
    {
        WeaponOriginalParts.Resolve(0, 505, Shared).Should().Be(0);
        WeaponOriginalParts.Resolve(0, 0, Shared).Should().Be(0);
    }

    [Test]
    public void TheRecordedPartSurvivesARestyle()
    {
        // After the player picks a shared part, the weapon can still switch back to its own.
        WeaponOriginalParts.Resolve(502, 101, Shared).Should().Be(502);
        WeaponOriginalParts.GetOptions(Shared, 502).Should().Equal(101, 105, 501, 502, 505);
    }

    [Test]
    public void AWeaponWithoutItsOwnPartGetsTheSharedListUnchanged()
    {
        WeaponOriginalParts.GetOptions(Shared, 0).Should().BeSameAs(Shared);
        WeaponOriginalParts.GetOptions(Shared, 505).Should().BeSameAs(Shared);
    }

    [Test]
    public void OnlyTheRareWeaponTypesKeepTheirOwnParts()
    {
        // Other types, such as the training sabers, refuse a weapon whose part is missing from their lists.
        var editor = typeof(AppearanceEditorViewModel);
        editor.GetMethod("LoadWeaponAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
        var loaded = (IDictionary)editor.GetField("_weaponAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        loaded.Keys.Cast<BaseItem>().Where(type => ((WeaponAppearanceBaseDefinition)loaded[type]!).KeepsOwnParts)
            .Should().BeEquivalentTo(new[]
            {
                BaseItem.Longsword, BaseItem.GreatSword, BaseItem.Dagger,
                BaseItem.Pistol, BaseItem.Sling, BaseItem.LegacyPistol
            });
    }

    private static IEnumerable<TestCaseData> RareParts()
    {
        // The vibroblade (longsword) definition is internal to the server assembly.
        var longsword = (WeaponAppearanceBaseDefinition)Activator.CreateInstance(
            typeof(WeaponAppearanceBaseDefinition).Assembly.GetType(
                "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.LongswordAppearanceDefinition", throwOnError: true)!,
            nonPublic: true)!;
        yield return Case("Muster Line Edge", longsword, AppearanceWeapon.Middle, 502, "wswls_m_025");
        yield return Case("Muster Line Edge", longsword, AppearanceWeapon.Bottom, 109, "wswls_b_091");
        yield return Case("Slag Cleaver", new GreatSwordAppearanceDefinition(), AppearanceWeapon.Middle, 802, "wswgs_m_028");
        yield return Case("Slag Cleaver", new GreatSwordAppearanceDefinition(), AppearanceWeapon.Bottom, 802, "wswgs_b_028");
        yield return Case("Lockdown Vibroknife", new DaggerAppearanceDefinition(), AppearanceWeapon.Middle, 701, "wswdg_m_017");
        yield return Case("Lockdown Vibroknife", new DaggerAppearanceDefinition(), AppearanceWeapon.Bottom, 701, "wswdg_b_017");
        yield return Case("Dead Man's Blaster", new PistolAppearanceDefinition(), AppearanceWeapon.Middle, 219, "wbwsh_m_192");
    }

    private static TestCaseData Case(string weapon, WeaponAppearanceBaseDefinition appearance, AppearanceWeapon part, int partId, string pictureKey) =>
        new TestCaseData(appearance, part, partId, pictureKey).SetArgDisplayNames(weapon, part.ToString(), partId.ToString());

    [TestCaseSource(nameof(RareParts))]
    public void RareLooksStayOffTheSharedListsButHaveANameForTheirOwnWeapon(
        WeaponAppearanceBaseDefinition appearance, AppearanceWeapon part, int partId, string pictureKey)
    {
        var shared = part switch
        {
            AppearanceWeapon.Middle => appearance.MiddleParts,
            AppearanceWeapon.Bottom => appearance.BottomParts,
            _ => appearance.TopParts
        };
        shared.Should().NotContain(partId, "only the weapon that came with it may use it");
        AppearancePartNames.All.Should().ContainKey(pictureKey, "the picker labels the weapon's own part");
    }
}
