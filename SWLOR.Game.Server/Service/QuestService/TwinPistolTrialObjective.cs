using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>The runtime trial awards proof only after the requested combat actions succeed.</summary>
    public sealed class TwinPistolTrialObjective : IQuestObjective
    {
        public KeyItemType Proof { get; }
        public string Description { get; }

        public TwinPistolTrialObjective(KeyItemType proof, string description)
        {
            Proof = proof;
            Description = description;
        }

        public void Initialize(uint player, string questId) { }
        public void Advance(uint player, string questId) { }
        public bool IsComplete(uint player, string questId) => KeyItem.HasKeyItem(player, Proof);
        public string GetCurrentStateText(uint player, string questId) =>
            IsComplete(player, questId) ? $"{Description} {{COMPLETE}}" : Description;
    }
}
