using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

[TestFixture]
[NonParallelizable]
public class TileMagicServiceTests
{
    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private readonly TileMagicService _service = new();

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void ChangeAreaGroundTiles_DrawsOneTilePerColumnAndRowIncludingTheEdge()
    {
        _service.ChangeAreaGroundTiles(100, default(TileMagicType), 2, 3);

        _engine.CountOf("CreateObject").Should().Be(12);
        _engine.CountOf("SetPlotFlag").Should().Be(12);
        _engine.CountOf("ApplyEffectToObject").Should().Be(12);
    }

    [Test]
    public void ChangeAreaGroundTiles_WholeArea_UsesTheAreaSize()
    {
        _engine.SetReturn("GetAreaSize", 1);

        _service.ChangeAreaGroundTiles(100, default(TileMagicType));

        _engine.CountOf("CreateObject").Should().Be(4);
    }
}
