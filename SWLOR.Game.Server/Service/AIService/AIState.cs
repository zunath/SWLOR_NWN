using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIState
    {
        public AIProfileType Profile { get; set; }
        public AIPhaseId ActivePhase { get; set; }
        public HashSet<AIPhaseId> EnteredPhases { get; } = new();
        public HashSet<string> CompletedOnceActions { get; } = new();
        public Dictionary<string, DateTime> Cooldowns { get; } = new();
        public DateTime LastDecisionTime { get; set; }
        public DateTime CombatStartedTime { get; set; }
        public bool BossTimerScheduled { get; set; }
        public int ActionCacheFeatCount { get; set; } = -1;
        public int ActionCacheFeatChecksum { get; set; }
        public List<AIActionDefinition> CachedActions { get; } = new();
        public Dictionary<AIPhaseId, List<AIActionDefinition>> CachedPhaseActions { get; } = new();

        public void ClearActionCache()
        {
            ActionCacheFeatCount = -1;
            ActionCacheFeatChecksum = 0;
            CachedActions.Clear();
            CachedPhaseActions.Clear();
        }
    }
}
