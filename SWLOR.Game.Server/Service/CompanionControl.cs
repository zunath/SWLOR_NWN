using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class CompanionControl
    {
        // The native associate radial combines its Toggle label with base TLK 8127 ("casting").
        public const int AssociateAbilitiesLabelStrRef = Domain.Creatures.CompanionControlService.AssociateAbilitiesLabelStrRef;

        private static ICompanionControlService Instance => FacadeAccessor.Resolve<ICompanionControlService>();

        public static bool IsRegisteredCompanion(uint creature)
        {
            return Instance.IsRegisteredCompanion(creature);
        }

        public static bool IsControlledCompanion(uint creature)
        {
            return Instance.IsControlledCompanion(creature);
        }

        public static void Initialize(uint companion, bool preserveActionQueue = false)
        {
            Instance.Initialize(companion, preserveActionQueue);
        }

        public static void Clear(uint companion)
        {
            Instance.Clear(companion);
        }

        public static CompanionMode GetMode(uint companion)
        {
            return Instance.GetMode(companion);
        }

        public static bool AreAbilitiesEnabled(uint companion)
        {
            return Instance.AreAbilitiesEnabled(companion);
        }

        public static bool HandleConversation(uint companion)
        {
            return Instance.HandleConversation(companion);
        }

        public static void RegisterDefensiveThreat(uint companion, uint threat)
        {
            Instance.RegisterDefensiveThreat(companion, threat);
        }

        public static void RegisterOwnerAttack(uint companion, uint target)
        {
            Instance.RegisterOwnerAttack(companion, target);
        }

        public static void BeginExplicitOrder(uint companion, float durationSeconds = 30f)
        {
            Instance.BeginExplicitOrder(companion, durationSeconds);
        }

        public static void CancelExplicitOrder(uint companion)
        {
            Instance.CancelExplicitOrder(companion);
        }

        public static void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false)
        {
            Instance.ProcessCombatRound(companion, bypassDecisionThrottle);
        }

        public static void ProcessHeartbeat(uint companion)
        {
            Instance.ProcessHeartbeat(companion);
        }

        public static void ResumeModePosition(uint companion)
        {
            Instance.ResumeModePosition(companion);
        }

        public static bool TryProcessPendingDefensiveReaction(uint companion)
        {
            return Instance.TryProcessPendingDefensiveReaction(companion);
        }

        public static uint PeekAuthorizedTarget(uint companion)
        {
            return Instance.PeekAuthorizedTarget(companion);
        }

        public static uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget)
        {
            return Instance.ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);
        }

        public static bool IsHostileAbilityTargetAuthorized(uint companion, AbilityDetail ability, uint selectedTarget)
        {
            return Instance.IsHostileAbilityTargetAuthorized(companion, ability, selectedTarget);
        }

        public static bool CanIssueAttackCommand(uint companion, uint target)
        {
            return Instance.CanIssueAttackCommand(companion, target);
        }

        public static void ResetOwnerCompanionToFollow(uint master)
        {
            Instance.ResetOwnerCompanionToFollow(master);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RenameToggleAbilitiesCommand()
        {
            Instance.RenameToggleAbilitiesCommand();
        }

        [NWNEventHandler(ScriptName.OnPlayerAttacked)]
        public static void OnOwnerAttacked()
        {
            Instance.OnOwnerAttacked();
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void OnOwnerDamaged()
        {
            Instance.OnOwnerDamaged();
        }

        [NWNEventHandler(ScriptName.OnPlayerSpellCastAt)]
        public static void OnHostileSpellCastAtOwner()
        {
            Instance.OnHostileSpellCastAtOwner();
        }

        [NWNEventHandler(ScriptName.OnPlayerRoundEnd)]
        public static void OnOwnerCombatRound()
        {
            Instance.OnOwnerCombatRound();
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnOwnerAreaExit()
        {
            Instance.OnOwnerAreaExit();
        }
    }
}
