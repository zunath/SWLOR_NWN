using SWLOR.Game.Server.Domain.Quests;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The capstone proofs (the facade <c>Service.CapstoneProof</c> until P2-ZN).</summary>
        public static ICapstoneProofService CapstoneProof => FacadeAccessor.Resolve<ICapstoneProofService>();

        /// <summary>The factions and player standing (the facade <c>Service.Faction</c> until P2-ZN).</summary>
        public static IFactionService Faction => FacadeAccessor.Resolve<IFactionService>();

        /// <summary>The quest registry (the facade <c>Service.Quest</c> until P2-ZN).</summary>
        public static IQuestService Quest => FacadeAccessor.Resolve<IQuestService>();

        /// <summary>The player-authored quest contracts (the facade <c>Service.QuestContractBoard</c> until P2-ZN).</summary>
        public static IQuestContractBoardService QuestContractBoard => FacadeAccessor.Resolve<IQuestContractBoardService>();

        /// <summary>The quest encounters (the facade <c>Service.QuestService.QuestEncounter</c> until P2-ZN).</summary>
        public static IQuestEncounterService QuestEncounter => FacadeAccessor.Resolve<IQuestEncounterService>();
    }
}
