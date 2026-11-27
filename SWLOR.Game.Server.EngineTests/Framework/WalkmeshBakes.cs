using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reads the baked spawn points the server's walkmesh instance holds (the private <c>_walkmeshesByArea</c> table),
    /// as the engine tests read them from the static class before the walkmesh service was converted.
    /// </summary>
    public static class WalkmeshBakes
    {
        /// <summary>The baked points by area resref, the instance's own dictionary.</summary>
        public static Dictionary<string, List<Vector3>> ByArea()
        {
            var walkmesh = CompositionRoot.ServerHost.Provider.GetService(typeof(IWalkmeshService));
            var field = walkmesh.GetType().GetField("_walkmeshesByArea", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Dictionary<string, List<Vector3>>)field!.GetValue(walkmesh)!;
        }
    }
}
