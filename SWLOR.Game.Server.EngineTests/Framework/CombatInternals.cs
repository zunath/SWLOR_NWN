using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reads the private state of the server's combat point and targeting instances, which the engine tests used to reach
    /// as static fields of <c>CombatPoint</c> and <c>Targeting</c> before those services were converted.
    /// </summary>
    public static class CombatInternals
    {
        /// <summary>The combat points by creature, then player, then skill.</summary>
        public static Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>> CombatPointTracker()
        {
            var service = CompositionRoot.ServerHost.Provider.GetService(typeof(ICombatPointService));
            return (Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>>)service.GetType()
                .GetField("_creatureCombatPointTracker", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(service);
        }

        /// <summary>Removes every combat point of a player, as the player leaving would.</summary>
        public static void ClearPlayerCombatPoints(uint player)
        {
            var service = CompositionRoot.ServerHost.Provider.GetService(typeof(ICombatPointService));
            service.GetType().GetMethod("ClearPlayerCombatPoints", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(service, new object[] { player });
        }

        /// <summary>The targeting actions waiting for a player's next selection.</summary>
        public static IDictionary PendingTargetingActions()
        {
            var service = CompositionRoot.ServerHost.Provider.GetService(typeof(ITargetingService));
            return (IDictionary)service.GetType()
                .GetField("_playerTargetingActions", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(service);
        }
    }
}
