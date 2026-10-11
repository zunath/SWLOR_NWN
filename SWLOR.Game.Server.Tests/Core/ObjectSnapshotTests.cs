using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

public class ObjectSnapshotTests
{
    [Test]
    public void NestedSearchDuringGameplayDoesNotResetOuterSearch()
    {
        uint cursor = 0;
        uint First() => cursor = 1;
        uint Next() => ++cursor <= 3 ? cursor : 0;
        var processed = new List<uint>();

        foreach (var obj in ObjectSnapshot.Capture(First, Next, obj => obj != 0))
        {
            processed.Add(obj);
            ObjectSnapshot.Capture(First, Next, obj => obj != 0);
        }

        processed.Should().Equal(1u, 2u, 3u);
    }

    [Test]
    public void RepeatedNativeObjectStopsTheSearchWithADiagnostic()
    {
        var objects = new Queue<uint>(new[] { 2u, 1u });
        Action capture = () => ObjectSnapshot.Capture(() => 1, objects.Dequeue, _ => true);

        capture.Should().Throw<InvalidOperationException>().WithMessage("*00000001*advancing*");
        objects.Should().BeEmpty();
    }

    [Test]
    public void EmptySearchDoesNotRequestAnotherObject()
    {
        ObjectSnapshot.Capture(() => 0, () => throw new Exception("Unexpected next"), obj => obj != 0)
            .Should().BeEmpty();
    }
}
