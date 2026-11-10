using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The roleplay XP detection model holder: the built-in default until a model is set, and again when it is cleared.</summary>
[TestFixture]
public class RoleplayXPDetectionServiceTests
{
    [Test]
    public void TheBuiltInModelIsUsedUntilOneIsSet()
    {
        var service = new RoleplayXPDetectionService();

        service.GetModel().Should().BeSameAs(OOCDetectionModel.Default);
    }

    [Test]
    public void ASetModelIsReturnedAndClearingItRestoresTheDefault()
    {
        var service = new RoleplayXPDetectionService();
        var loaded = RuntimeHelpers.GetUninitializedObject(typeof(OOCDetectionModel)) as OOCDetectionModel;

        service.SetModel(loaded);
        service.GetModel().Should().BeSameAs(loaded);

        service.SetModel(null);
        service.GetModel().Should().BeSameAs(OOCDetectionModel.Default);
    }
}
