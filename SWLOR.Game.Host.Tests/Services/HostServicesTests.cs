using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Services;

namespace SWLOR.Game.Host.Tests.Services;

[TestFixture]
public class HostServicesTests
{
    [Test]
    public void A_service_resolves_by_the_type_it_was_added_under_and_anything_else_is_null()
    {
        var callbacks = new HostCallbacks();
        var services = new HostServices().Add<IHostCallbacks>(callbacks);

        services.Resolve(typeof(IHostCallbacks)).Should().BeSameAs(callbacks);
        services.Resolve(typeof(HostCallbacks)).Should().BeNull();
        services.Resolve(typeof(IRedisConnectionSource)).Should().BeNull();
    }

    [Test]
    public void A_type_is_added_once_and_null_is_refused()
    {
        var services = new HostServices().Add<IHostCallbacks>(new HostCallbacks());

        var again = () => services.Add<IHostCallbacks>(new HostCallbacks());
        var nothing = () => services.Add<IHostCallbacks>(null!);
        var resolveNothing = () => services.Resolve(null!);

        again.Should().Throw<ArgumentException>();
        nothing.Should().Throw<ArgumentNullException>();
        resolveNothing.Should().Throw<ArgumentNullException>();
    }
}
