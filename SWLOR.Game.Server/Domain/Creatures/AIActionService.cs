using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The action side of the profile engine: scoring the candidate actions, resolving their targets, checking that they can
    /// run and running the winner, with the cooldowns and once-per-phase marks. Moved here unchanged from
    /// <see cref="AIService"/>; <see cref="AIDecisionService"/> decides when and about whom.
    /// </summary>
    public class AIActionService : IAIActionService
    {
        private readonly IAbilityActivationService _activation;
        private readonly IAIService _ai;
        private readonly IAbilityCatalog _catalog;
        private readonly ICompanionControlStoreService _companionControlStore;
        private readonly ICompanionTargetAuthorityService _companionTargetAuthority;
        private readonly IEnmityChangeService _enmityChange;
        private readonly IEnmityAttackService _enmityAttack;
        private readonly IPerkService _perk;

        public AIActionService(
            IAbilityActivationService activation,
            IAIService ai,
            IAbilityCatalog catalog,
            ICompanionControlStoreService companionControlStore,
            ICompanionTargetAuthorityService companionTargetAuthority,
            IEnmityChangeService enmityChange,
            IEnmityAttackService enmityAttack,
            IPerkService perk)
        {
            _activation = activation;
            _ai = ai;
            _catalog = catalog;
            _companionControlStore = companionControlStore;
            _companionTargetAuthority = companionTargetAuthority;
            _enmityChange = enmityChange;
            _enmityAttack = enmityAttack;
            _perk = perk;
        }

        public AIActionEvaluation SelectAction(AIContext context)
        {
            var candidates = context.State.CachedActions.AsEnumerable();
            if (context.State.ActivePhase != AIPhaseId.Invalid &&
                context.State.CachedPhaseActions.TryGetValue(context.State.ActivePhase, out var phaseActions))
            {
                candidates = candidates.Concat(phaseActions);
            }

            AIActionEvaluation best = null;
            var index = 0;
            foreach (var action in candidates.Take(context.Profile.MaxCandidateActions))
            {
                var evaluation = EvaluateAction(context, action, index);
                index++;

                if (evaluation == null)
                    continue;

                if (best == null ||
                    evaluation.Score > best.Score ||
                    evaluation.Score == best.Score && evaluation.Priority < best.Priority ||
                    evaluation.Score == best.Score && evaluation.Priority == best.Priority && evaluation.Index < best.Index)
                {
                    best = evaluation;
                }
            }

            return best;
        }

        private AIActionEvaluation EvaluateAction(AIContext context, AIActionDefinition action, int index)
        {
            if (action.OncePerPhase && HasCompletedOncePerPhaseAction(context, action))
                return null;

            if (IsOnCooldown(context, action))
                return null;

            foreach (var guard in action.Guards)
            {
                if (!guard(context))
                    return null;
            }

            var target = ResolveTarget(context, action);
            context.SetEvaluatedTarget(target);

            if (!CanExecuteAction(context, action, target))
                return null;

            var score = action.Score?.Invoke(context) ?? 0;
            if (score <= 0)
                return null;

            return new AIActionEvaluation
            {
                Action = action,
                Target = target,
                Score = score,
                Priority = action.Priority,
                Index = index
            };
        }

        private uint ResolveTarget(AIContext context, AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return context.CurrentEnmityTarget;

            if (action.Type == AIActionType.Ability)
            {
                var ability = _catalog.GetAbilityDetail(action.Feat);
                AITargetSelector selector;
                if (action.TargetSelector != null)
                    selector = action.TargetSelector;
                else if (_ai.TryGetDefaultTargetSelector(action.Feat, out var defaultSelector))
                    selector = defaultSelector;
                else
                    selector = AITarget.InferDefault(action.Feat, ability);

                var selectedTarget = selector(context);
                if (ability.IsHostileAbility && IsControlledCompanion(context.Self))
                {
                    return ResolveHostileAbilityTarget(
                        context.Self,
                        ability,
                        context.CurrentEnmityTarget,
                        selectedTarget);
                }

                return selectedTarget;
            }

            if (action.TargetSelector != null)
                return action.TargetSelector(context);

            return context.Self;
        }

        private bool CanExecuteAction(AIContext context, AIActionDefinition action, uint target)
        {
            var isControlledHostileIntent = IsControlledCompanion(context.Self) &&
                                            IsHostileIntent(action);
            if (isControlledHostileIntent &&
                (!GetIsObjectValid(context.CurrentEnmityTarget) ||
                 action.Type == AIActionType.Ability && !GetIsObjectValid(target)))
            {
                return false;
            }

            switch (action.Type)
            {
                case AIActionType.Ability:
                    return CanUseAbility(context, action, target);
                case AIActionType.AttackHighestEnmity:
                case AIActionType.MoveToTarget:
                case AIActionType.Flee:
                case AIActionType.CallAllies:
                    return GetIsObjectValid(target);
                case AIActionType.ReturnHome:
                    return context.IsOutsideHomeRadius;
                case AIActionType.RandomWalk:
                    return !GetIsInCombat(context.Self) && GetCurrentAction(context.Self) == ActionType.Invalid;
                case AIActionType.Wait:
                case AIActionType.Speak:
                case AIActionType.Script:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsHostileIntent(AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return true;

            return action.Type == AIActionType.Ability &&
                   action.Feat != FeatType.Invalid &&
                   _catalog.IsFeatRegistered(action.Feat) &&
                   _catalog.GetAbilityDetail(action.Feat).IsHostileAbility;
        }

        private bool CanUseAbility(AIContext context, AIActionDefinition action, uint target)
        {
            if (action.Feat == FeatType.Invalid || !_catalog.IsFeatRegistered(action.Feat))
                return false;

            if (IsControlledCompanion(context.Self) &&
                !AreCompanionAbilitiesEnabled(context.Self))
            {
                return false;
            }

            if (!GetHasFeat(action.Feat, context.Self))
                return false;

            var ability = _catalog.GetAbilityDetail(action.Feat);
            if (ability.IsHostileAbility &&
                ability.IsSingleTargetAbility &&
                (!GetIsObjectValid(target) ||
                 target == context.Self ||
                 !GetIsReactionTypeHostile(target, context.Self)))
            {
                return false;
            }

            // Every hostile ability's selector returns no target when there is no enemy to use it on;
            // a self-centered area ability returns the creature itself only when it sees enough
            // enemies nearby. Falling back to the creature here would fire it at nothing it can see.
            if ((ability.RequiresTarget || ability.IsHostileAbility) && !GetIsObjectValid(target))
                return false;

            if (!GetIsObjectValid(target))
                target = context.Self;

            var targetLocation = GetLocation(target);
            var effectiveLevel = ability.EffectiveLevelPerkType == PerkType.Invalid
                ? 1
                : GetPerkLevel(context.Self, ability.EffectiveLevelPerkType);

            return CanActivateAbility(context.Self, target, action.Feat, effectiveLevel, targetLocation);
        }

        public void ExecuteAction(AIContext context, AIActionEvaluation evaluation)
        {
            var action = evaluation.Action;
            ProfilerPlugin.PushPerfScope(context.Self, "NpcAI.Execute");
            try
            {
                switch (action.Type)
                {
                    case AIActionType.Ability:
                        ExecuteAbility(
                            context.Self,
                            action,
                            evaluation.Target,
                            context.CurrentEnmityTarget);
                        break;
                    case AIActionType.AttackHighestEnmity:
                        AttackHighestEnmityTarget(context.Self);
                        break;
                    case AIActionType.MoveToTarget:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionMoveToObject(evaluation.Target, true);
                        });
                        break;
                    case AIActionType.Flee:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.ReturnHome:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.RandomWalk:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionRandomWalk();
                        });
                        break;
                    case AIActionType.Wait:
                        AssignCommand(context.Self, () => ActionWait(action.FloatValue));
                        break;
                    case AIActionType.Speak:
                        AssignCommand(context.Self, () => SpeakString(action.Text));
                        break;
                    case AIActionType.Script:
                        ExecuteScript(action.ScriptName, context.Self);
                        break;
                    case AIActionType.CallAllies:
                        CallAllies(context, evaluation.Target, action.FloatValue);
                        break;
                }

                ApplyCooldown(context, action);
                MarkOncePerPhaseAction(context, action);

                Log.Write(LogGroup.AI, $"{GetName(context.Self)} [{context.Profile.Type}] executed {action.DebugName} with score {evaluation.Score}.");
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
            }
        }

        private void ExecuteAbility(
            uint creature,
            AIActionDefinition action,
            uint target,
            uint authorizedAttackTarget)
        {
            var isControlledCompanion = IsControlledCompanion(creature);
            var isControlledHostileAbility = isControlledCompanion && IsHostileIntent(action);
            if (isControlledHostileAbility && !GetIsObjectValid(target))
                return;

            if (!GetIsObjectValid(target))
                target = creature;

            var targetLocation = GetLocation(target);
            if (TryUseAbility(creature, target, action.Feat, targetLocation))
                return;

            if (isControlledCompanion)
            {
                target = authorizedAttackTarget;
            }
            else if (!GetIsObjectValid(target) || target == creature)
            {
                target = GetHighestEnmityAttackTarget(creature);
            }

            if (!GetIsObjectValid(target))
                return;

            IssueAttackCommand(creature, target);
        }

        public bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName)
        {
            abilityName = string.Empty;
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetCurrentHitPoints(target) >= GetMaxHitPoints(target))
            {
                return false;
            }

            var candidates = _catalog.GetAllAbilityDetails()
                .Where(x => x.Value.IsHealingAbility &&
                            !x.Value.IsHostileAbility &&
                            x.Value.RequiresTarget &&
                            x.Value.IsSingleTargetAbility &&
                            GetHasFeat(x.Key, creature))
                .OrderByDescending(x => x.Value.AbilityLevel)
                .ThenBy(x => x.Value.Name)
                .ToList();

            foreach (var (feat, ability) in candidates)
            {
                var targetLocation = GetLocation(target);
                if (!TryUseAbility(creature, target, feat, targetLocation))
                    continue;

                abilityName = ability.Name;
                return true;
            }

            return false;
        }

        private bool IsOnCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId))
                return false;

            return context.State.Cooldowns.TryGetValue(action.CooldownId, out var availableAt) &&
                   availableAt > Clock.UtcNow;
        }

        private void ApplyCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId) || action.CooldownSeconds <= 0f)
                return;

            context.State.Cooldowns[action.CooldownId] = Clock.UtcNow.AddSeconds(action.CooldownSeconds);
        }

        private bool HasCompletedOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (context.State.ActivePhase == AIPhaseId.Invalid)
                return false;

            return context.State.CompletedOnceActions.Contains(GetOncePerPhaseKey(context, action));
        }

        private void MarkOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (!action.OncePerPhase || context.State.ActivePhase == AIPhaseId.Invalid)
                return;

            context.State.CompletedOnceActions.Add(GetOncePerPhaseKey(context, action));
        }

        private string GetOncePerPhaseKey(AIContext context, AIActionDefinition action)
        {
            return $"{context.State.ActivePhase}:{action.Type}:{action.DebugName}:{action.Feat}";
        }

        private void CallAllies(AIContext context, uint target, float radius)
        {
            foreach (var ally in context.Allies)
            {
                if (!GetIsObjectValid(ally) || ally == context.Self)
                    continue;

                if (radius > 0f && GetDistanceBetween(context.Self, ally) > radius)
                    continue;

                if (!GetIsEnemy(target, ally))
                    continue;

                ModifyEnmity(target, ally, 1);
            }
        }

        // ---- Seams into services that are not converted yet ----
        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // A unit test subclass answers them, because the callees read the static enmity tables, the database, the
        // native engine or the other services' statics for real creatures.

        protected virtual uint GetHighestEnmityAttackTarget(uint enemy) => _enmityAttack.GetHighestEnmityAttackTarget(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => _enmityAttack.AttackHighestEnmityTarget(creature);

        protected virtual void IssueAttackCommand(uint creature, uint target) => _enmityAttack.IssueAttackCommand(creature, target);

        protected virtual void ModifyEnmity(uint creature, uint enemy, int amount) => _enmityChange.ModifyEnmity(creature, enemy, amount);

        protected virtual bool IsControlledCompanion(uint creature) => _companionControlStore.IsControlledCompanion(creature);

        protected virtual bool AreCompanionAbilitiesEnabled(uint companion) => _companionControlStore.AreAbilitiesEnabled(companion);

        protected virtual uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget) =>
            _companionTargetAuthority.ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);

        protected virtual int GetPerkLevel(uint creature, PerkType perkType) => _perk.GetPerkLevel(creature, perkType);

        protected virtual bool CanActivateAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation) =>
            _activation.CanUseAbility(activator, target, feat, effectiveLevel, targetLocation);

        protected virtual bool TryUseAbility(uint creature, uint target, FeatType feat, Location targetLocation) =>
            UsePerkFeat.TryUseAbility(creature, target, feat, targetLocation);
    }
}
