using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class ShipMaterialReclamationTests
{
    [Test] public void MissingProvenance_NeverInventsMaterials()
    {
        var ledger=new SpaceEconomyLedger();ShipMaterialReclamation.Apply(ledger,"item","com_laser_b",null).Should().BeFalse();ledger.MaterialRecovery.Should().BeEmpty();
    }
    [Test] public void VerifiedReclamation_PreservesFractionsAndCannotPayAnIdentityTwice()
    {
        var row=SpaceEconomyCatalog.Default.LegacyRefunds.Values.First(x=>x.Fraction>0);
        var source=new ShipItemProvenance {Resref=row.Resref,RecipeMaterials=new(row.Components),EnhancementGrade=100,EnhancementMaterials=new(){["ref_tilarium"]=3}};
        var ledger=new SpaceEconomyLedger();ShipMaterialReclamation.Apply(ledger,"item",row.Resref,source).Should().BeTrue();
        var total=ledger.MaterialRecovery.Values.Sum();total.Should().BeApproximately(row.Components.Values.Sum()*row.Fraction+1.2,1e-8);
        ShipMaterialReclamation.Apply(ledger,"item",row.Resref,source).Should().BeTrue();ledger.MaterialRecovery.Values.Sum().Should().Be(total);
        ShipMaterialReclamation.Apply(ledger,"other",row.Resref,new(){Resref="wrong"}).Should().BeFalse();
    }
}
