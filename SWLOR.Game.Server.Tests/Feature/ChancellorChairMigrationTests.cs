using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ChancellorChairMigrationTests
{
    [TestCase(90f, 10f, 9.85f)]
    [TestCase(270f, 10f, 10.15f)]
    [TestCase(0f, 9.85f, 10f)]
    [TestCase(180f, 10.15f, 10f)]
    public void PlacedChairsMoveTowardTheirSeatFront(float facing, float x, float y)
    {
        var structure = Structure(StructureType.ChairChancellor, facing);

        ChancellorChairMigration.Migrate(structure).Should().BeTrue();

        var position = structure.Positions[PropertyLocationType.StaticPosition];
        position.X.Should().BeApproximately(x, 0.0001f);
        position.Y.Should().BeApproximately(y, 0.0001f);
        position.Orientation.Should().Be(facing);
    }

    [Test]
    public void OtherStructuresStayPut()
    {
        var structure = Structure(StructureType.StatueTwilek, 90f);

        ChancellorChairMigration.Migrate(structure).Should().BeFalse();

        structure.Positions[PropertyLocationType.StaticPosition].Y.Should().Be(10f);
    }

    [Test]
    public void UnplacedChairsAreSkipped()
    {
        var structure = new WorldProperty { StructureType = StructureType.ChairChancellor };

        ChancellorChairMigration.Migrate(structure).Should().BeFalse();
    }

    private static WorldProperty Structure(StructureType type, float facing)
    {
        var structure = new WorldProperty { StructureType = type };
        structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation { X = 10f, Y = 10f, Orientation = facing };
        return structure;
    }
}
