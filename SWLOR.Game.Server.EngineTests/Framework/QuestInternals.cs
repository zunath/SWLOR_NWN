using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's quest registry instance (the quest cache and the collector cleanup).
    /// The engine tests read and fill the registry directly, as they did on the static class before the registry was
    /// converted; the instance comes from the container the server boots.
    /// </summary>
    public static class QuestInternals
    {
        private static IQuestService Registry => CompositionRoot.ServerHost.Provider.GetService(typeof(IQuestService)) as IQuestService;

        /// <summary>The registry's quest cache, by quest Id.</summary>
        public static Dictionary<string, QuestDetail> Quests =>
            (Dictionary<string, QuestDetail>)typeof(QuestService)
                .GetField("_quests", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Registry);

        /// <summary>Releases the player and destroys a collector, as the idle sweep does.</summary>
        public static void DestroyItemCollector(uint collector)
        {
            typeof(QuestService).GetMethod("DestroyItemCollector", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Registry, new object[] { collector });
        }
    }
}
