using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponGripPlacementCatalogTests
{
    private static readonly Assembly Server = typeof(WeaponFit).Assembly;
    private static readonly Type Catalog = Server.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripPlacementCatalog")!;
    private static readonly Type Row = Server.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripPlacementRow")!;
    private static readonly Type Hold = Server.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripHold")!;

    private static WeaponGripFit Fit(float x) => new(new FitVector(x, 0, 0), FitVector.Zero, 1);

    private static object LoadRows(Array rows) =>
        Catalog.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke(new object[] { rows });

    private static object Load(params (string Hand, int Model, string Hold, WeaponGripFit Fit)[] entries)
    {
        var rows = Array.CreateInstance(Row, entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            rows.SetValue(Activator.CreateInstance(Row, entry.Hand, entry.Model, Enum.Parse(Hold, entry.Hold), entry.Fit), i);
        }
        return LoadRows(rows);
    }

    private static Array ParseColumns(string models, params string[] columns)
    {
        var values = (System.Collections.IEnumerable)Row.GetMethod("FromColumns", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { "pmz0_handr32308", Enum.Parse(Hold, "Pistol"), models, columns })!;
        var parsed = values.Cast<object>().ToArray();
        var rows = Array.CreateInstance(Row, parsed.Length);
        for (var i = 0; i < parsed.Length; i++) rows.SetValue(parsed[i], i);
        return rows;
    }

    private static (bool Found, WeaponGripFit Fit) Lookup(object catalog, string hand, int model, string hold)
    {
        object[] args = { hand, model, Enum.Parse(Hold, hold), null! };
        var found = (bool)Catalog.GetMethod("TryGet", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(catalog, args)!;
        return (found, (WeaponGripFit)args[3]);
    }

    [Test]
    public void ModelHoldAndHandSelectIndependentPlacements()
    {
        var catalog = Load(
            ("pmz0_handr32308", 301, "Pistol", Fit(.001f)),
            ("pmz0_handl32308", 301, "Pistol", Fit(-.001f)),
            ("pmz0_handr32308", 122, "Pistol", Fit(.002f)),
            ("pmz0_handr32308", 301, "RifleIdle", Fit(.003f)),
            ("pmz0_handr32308", 301, "RifleReady", Fit(.004f)));

        Lookup(catalog, "PMZ0_HANDR32308", 301, "Pistol").Should().Be((true, Fit(.001f)));
        Lookup(catalog, "pmz0_handl32308", 301, "Pistol").Should().Be((true, Fit(-.001f)));
        Lookup(catalog, "pmz0_handr32308", 122, "Pistol").Should().Be((true, Fit(.002f)));
        Lookup(catalog, "pmz0_handr32308", 301, "RifleIdle").Should().Be((true, Fit(.003f)));
        Lookup(catalog, "pmz0_handr32308", 301, "RifleReady").Should().Be((true, Fit(.004f)));
        Lookup(catalog, "pmz0_handr32308", 125, "Pistol").Should().Be((false, WeaponGripFit.Identity));
    }

    [TestCase("pmz0_handl32308", 301, "RifleReady", 1f)]
    [TestCase("pmz2_handr32308", 301, "Pistol", 1f)]
    [TestCase("pmz0_handr32308", 0, "Pistol", 1f)]
    [TestCase("pmz0_handr32308", 301, "Pistol", .8f)]
    [TestCase("pmz0_handr32308", 301, "Pistol", float.NaN)]
    public void InvalidRowsCannotIntroduceWrongRolesOrResizedWeapons(string hand, int model, string hold, float scale)
    {
        Action load = () => Load((hand, model, hold, new WeaponGripFit(FitVector.Zero, FitVector.Zero, scale)));
        load.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }

    [Test]
    public void ConflictingRowsCannotSilentlyReplaceAPlacement()
    {
        Action load = () => Load(("pmz0_handr32308", 301, "Pistol", Fit(.001f)),
            ("PMZ0_HANDR32308", 301, "Pistol", Fit(.002f)));
        load.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }

    [Test]
    public void CompactRowsRetainModelOrderAndBroadcastConstantRotations()
    {
        var catalog = LoadRows(ParseColumns("301,122", ".001,.002", "-.04,-.02", ".02,.03", "0", "0", "0"));
        Lookup(catalog, "pmz0_handr32308", 301, "Pistol").Should().Be((true,
            new WeaponGripFit(new FitVector(.001f, -.04f, .02f), FitVector.Zero, 1)));
        Lookup(catalog, "pmz0_handr32308", 122, "Pistol").Should().Be((true,
            new WeaponGripFit(new FitVector(.002f, -.02f, .03f), FitVector.Zero, 1)));
    }

    [Test]
    public void CompactRowsRejectMisalignedCoordinates()
    {
        Action parse = () => ParseColumns("301,122", ".001,.002,.003", "0", "0", "0", "0", "0");
        parse.Should().Throw<ArgumentException>();
    }
}
