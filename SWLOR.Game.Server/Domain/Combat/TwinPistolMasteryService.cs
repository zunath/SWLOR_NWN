using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>
/// Tracks the Twin Pistol capstone mastery trials inside the trial area. The logic that used to be static in
/// <c>Service.TwinPistolMastery</c> is moved here unchanged; the per-actor trial progress is this instance's own. The
/// database, key item and quest services are injected; the unconverted equipment predicates sit behind a
/// <c>protected virtual</c> method whose body is the original call.
/// </summary>
public class TwinPistolMasteryService : ITwinPistolMasteryService
{
    private readonly Dictionary<uint, TwinPistolTrialProgress> _attempts = new();

    private readonly IDBService _db;
    private readonly IKeyItemService _keyItem;
    private readonly IQuestService _quest;

    public TwinPistolMasteryService(IDBService db, IKeyItemService keyItem, IQuestService quest)
    {
        _db = db;
        _keyItem = keyItem;
        _quest = quest;
    }

    public void RecordOrdinaryHit(uint actor, uint target, bool companion)
    {
        if (!CanAttempt(actor, TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId, true) ||
            !GetIsObjectValid(target) || GetIsPC(target) || !GetIsEnemy(target, actor) ||
            GetArea(target) != GetArea(actor))
            return;
        if (GetAttempt(actor).RecordOrdinaryHit(DateTime.UtcNow, target, companion))
            AwardProof(actor, TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId, KeyItemType.TwinSunsCanyonFiringTrace);
    }

    public void RecordAbility(uint actor, bool anyHit, PerkType perk, int cadenceSpent)
    {
        if (!CanAttempt(actor, TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId, false))
            return;
        if (GetAttempt(actor).RecordAbility(DateTime.UtcNow, anyHit, perk, cadenceSpent))
            AwardProof(actor, TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId, KeyItemType.FinalSalvoCanyonRouteTrace);
    }

    public void ClearTrialState(uint actor) => _attempts.Remove(actor);

    public void RecordTargetChange(uint actor, uint target)
    {
        if (_attempts.TryGetValue(actor, out var attempt))
            attempt.RecordTargetChange(target);
    }

    private bool CanAttempt(uint actor, string questId, bool requiresPistolPair)
    {
        if (!GetIsPC(actor) || GetIsDM(actor) || GetIsDead(actor) ||
            requiresPistolPair && !HasTwinPistol(actor) ||
            GetResRef(GetArea(actor)) != TwinPistolCapstoneQuestDefinition.TrialAreaResref)
            return false;
        var player = _db.Get<Player>(GetObjectUUID(actor));
        return player != null && player.Quests.TryGetValue(questId, out var quest) &&
               quest.DateLastCompleted == null && quest.CurrentState == 1;
    }

    private TwinPistolTrialProgress GetAttempt(uint actor)
    {
        if (!_attempts.TryGetValue(actor, out var progress))
            _attempts[actor] = progress = new TwinPistolTrialProgress();
        return progress;
    }

    private void AwardProof(uint actor, string questId, KeyItemType proof)
    {
        _keyItem.GiveKeyItem(actor, proof);
        _quest.AdvanceQuest(actor, actor, questId);
        ClearTrialState(actor);
    }

    /// <summary>The unconverted <c>EquipmentPredicates</c> helper.</summary>
    protected virtual bool HasTwinPistol(uint actor) => EquipmentPredicates.HasTwinPistol(actor);
}
