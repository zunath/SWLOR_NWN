using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the profiles and the private target resolution of the server's AI instance. The engine tests drive
    /// them directly, as they did on the static <c>NPCAI</c> class before the AI service was converted.
    /// </summary>
    public static class AIInternals
    {
        private static AIService Instance => (AIService)CompositionRoot.ServerHost.Provider.GetService(typeof(IAIService));

        /// <summary>The AI profiles the module cache loaded.</summary>
        public static IReadOnlyDictionary<AIProfileType, AIProfile> Profiles => Instance.Profiles;

        /// <summary>Calls a private instance method of the AI service with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var instance = Instance;
            return instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(instance, arguments);
        }
    }
}
