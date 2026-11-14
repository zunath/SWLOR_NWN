using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A planet service with the planet cache the module-load handler builds, for tests that need the planets or the
/// area-to-planet rules without a server. The engine is a fake with no areas, so loading registers no area.
/// </summary>
public static class LoadedPlanets
{
    private const uint SomeArea = 4242;

    /// <summary>A new planet service whose cache was filled by <see cref="IPlanetService.CacheData"/>.</summary>
    public static PlanetService Create()
    {
        var planets = new PlanetService();
        using (NWScriptRuntime.Override(new FakeNWScript()))
        {
            planets.CacheData();
        }

        return planets;
    }

    /// <summary>
    /// The planet <see cref="IPlanetService.GetPlanetType"/> answers for an area the fake engine describes with this
    /// name, resref and space flag. An area with a name that no planet claims and no resref gives
    /// <see cref="PlanetType.Invalid"/>, so passing only a name asks for the name rule alone.
    /// </summary>
    public static PlanetType Resolve(IPlanetService planets, string areaName, string areaResref = "", bool isSpace = false)
    {
        var engine = new FakeNWScript();
        engine.SetReturn("GetIsObjectValid", true);
        engine.SetReturn("GetName", areaName);
        engine.SetReturn("GetResRef", areaResref);
        engine.SetReturn("GetLocalBool", isSpace);

        using (NWScriptRuntime.Override(engine))
        {
            return planets.GetPlanetType(SomeArea);
        }
    }
}
