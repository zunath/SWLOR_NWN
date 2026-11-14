using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Quests;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Runs the private methods of the server's quest contract board instance (<c>ClaimDeliveries</c> with a character
    /// checkpoint, <c>RecoverSettlements</c>, <c>PruneHistory</c>). A headless test player has no servervault file to
    /// save, so the engine tests call the private claim directly with a no-op checkpoint, as they did on the static
    /// class before the board was converted.
    /// </summary>
    public static class QuestContractBoardInternals
    {
        /// <summary>Calls a private instance method of the container's board service with the given arguments.</summary>
        public static void Invoke(string methodName, params object[] arguments)
        {
            var board = CompositionRoot.ServerHost.Provider.GetService(typeof(IQuestContractBoardService));
            board.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(board, arguments);
        }
    }
}
