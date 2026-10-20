using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    public static class TwinPistolMastery
    {
        private static readonly Dictionary<uint, TwinPistolTrialProgress> Attempts = new();

        public static void RecordOrdinaryHit(uint actor, uint target, bool companion)
        {
            if (!CanAttempt(actor, TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId, true) ||
                !GetIsObjectValid(target) || GetIsPC(target) || !GetIsEnemy(target, actor) ||
                GetArea(target) != GetArea(actor))
                return;
            if (GetAttempt(actor).RecordOrdinaryHit(DateTime.UtcNow, target, companion))
                AwardProof(actor, TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId, KeyItemType.TwinSunsCanyonFiringTrace);
        }

        public static void RecordAbility(uint actor, bool anyHit, float distanceTravelled, TwinPistolAbilitySpec spec, int momentumSpent)
        {
            if (!CanAttempt(actor, TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId, false))
                return;
            if (GetAttempt(actor).RecordAbility(DateTime.UtcNow, anyHit, distanceTravelled, spec, momentumSpent))
                AwardProof(actor, TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId, KeyItemType.FinalSalvoCanyonRouteTrace);
        }

        public static void ClearTrialState(uint actor) => Attempts.Remove(actor);

        public static void RecordTargetChange(uint actor, uint target)
        {
            if (Attempts.TryGetValue(actor, out var attempt))
                attempt.RecordTargetChange(target);
        }

        private static bool CanAttempt(uint actor, string questId, bool requiresPistolPair)
        {
            if (!GetIsPC(actor) || GetIsDM(actor) || GetIsDead(actor) ||
                requiresPistolPair && !EquipmentPredicates.HasTwinPistol(actor) ||
                GetResRef(GetArea(actor)) != TwinPistolCapstoneQuestDefinition.TrialAreaResref)
                return false;
            var player = DB.Get<Player>(GetObjectUUID(actor));
            return player != null && player.Quests.TryGetValue(questId, out var quest) &&
                   quest.DateLastCompleted == null && quest.CurrentState == 1;
        }

        private static TwinPistolTrialProgress GetAttempt(uint actor)
        {
            if (!Attempts.TryGetValue(actor, out var progress))
                Attempts[actor] = progress = new TwinPistolTrialProgress();
            return progress;
        }

        private static void AwardProof(uint actor, string questId, KeyItemType proof)
        {
            KeyItem.GiveKeyItem(actor, proof);
            Quest.AdvanceQuest(actor, actor, questId);
            ClearTrialState(actor);
        }
    }
}
