using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The aggro aura: the enter and exit handlers, proximity enmity and the aggro that stealth detection hands back. Moved
    /// here unchanged from <see cref="AIService"/>. It is the AI's listener of the stealth notices (<see cref="IStealthAggroListener"/>).
    /// </summary>
    public class AIAggroService : IAIAggroService
    {
        private const int ProximityEnmityAmount = 1;

        private readonly IAIService _ai;
        private readonly IAIDecisionService _decision;
        private readonly IEnmityService _enmity;
        private readonly IEnmityChangeService _enmityChange;
        private readonly IEnmityAttackService _enmityAttack;
        private readonly IEspionageInfiltrationService _espionageInfiltration;
        private readonly IStealthService _stealth;

        public AIAggroService(
            IAIService ai,
            IAIDecisionService decision,
            IEnmityService enmity,
            IEnmityChangeService enmityChange,
            IEnmityAttackService enmityAttack,
            IEspionageInfiltrationService espionageInfiltration,
            IStealthService stealth)
        {
            _ai = ai;
            _decision = decision;
            _enmity = enmity;
            _enmityChange = enmityChange;
            _enmityAttack = enmityAttack;
            _espionageInfiltration = espionageInfiltration;
            _stealth = stealth;
        }

        /// <summary>
        /// Gives stealth this service as the listener of its detection notices. A reload runs boot handlers that rerun before it
        /// adopts the carried state, so the list is full when the first detection runs.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void RegisterStealthAggroListener()
        {
            _stealth.RegisterAggroListener(this);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroEnter)]
        public void CreatureAggroEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
            if (!CreatureAIRules.IsCreatureAIEnabled(self))
                return;

            if (CreatureAIRules.IsLeashEvading(self))
                return;

            // Target is invisible
            if (GetHasEffect(entering, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility))
            {
                return;
            }

            if (!CreatureAIRules.IsInAggroRange(self, entering))
                return;

            if (!GetIsEnemy(entering, self))
            {
                var attackTarget = GetHighestEnmityTarget(entering);
                // Non-enemy entered aggro range. If they're the same faction and fighting someone, help them out!
                if (GetFactionEqual(entering, self) &&
                    GetIsEnemy(attackTarget, self) &&
                    CreatureAIRules.IsInAggroRange(self, attackTarget))
                {
                    TryAddProximityEnmity(attackTarget, self);
                }

                return;
            }

            TryBeginInfiltration(entering, self);
            if (!CanAcquireAggro(self, entering))
                return;

            TryAcquireAggro(self, entering);
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroExit)]
        public void CreatureAggroExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
            if (!CreatureAIRules.IsCreatureAIEnabled(self) || !GetIsObjectValid(exiting))
                return;

            CompleteInfiltration(exiting, self);
            RemoveProximityEnmity(exiting, self);

            if (!_ai.TryGetAllies(self, out var allies))
                return;

            foreach (var ally in allies)
            {
                if (!CreatureAIRules.IsCreatureAIEnabled(ally) || CreatureAIRules.IsInAggroRange(ally, exiting))
                    continue;

                RemoveProximityEnmity(exiting, ally);
            }
        }

        /// <summary>
        /// Returns whether a creature has an effect.
        /// </summary>
        /// <param name="effectType">The type of effect to look for.</param>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has the effect, false otherwise</returns>
        private bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                var type = GetEffectType(effect);

                if (type == effectType || otherEffectTypes.Contains(type))
                {
                    return true;
                }
                effect = GetNextEffect(creature);
            }

            return false;
        }

        /// <summary>
        /// Re-enters the normal proximity-aggro path when a later native Spot check reveals a
        /// stealthed player who is still inside the observer's aggro aura. Non-AI observers and
        /// targets that no longer satisfy the ordinary aggro guards are ignored.
        /// </summary>
        public void TryAcquireAggroAfterDetection(uint observer, uint target)
        {
            if (!CreatureAIRules.IsCreatureAIEnabled(observer))
                return;

            if (!TryAcquireAggro(observer, target))
                return;

            Log.WriteStructured(
                LogGroup.AI,
                "Stealth detection acquired normal proximity aggro: Observer={Observer} Target={Target}",
                observer,
                target);
        }

        public void AcquireAggroFromNearbyEnemies(uint target)
        {
            foreach (var enemy in ObjectSnapshot.InShape(Shape.Sphere, CreatureAIRules.AggroRadius, GetLocation(target), false, ObjectType.Creature))
            {
                if (!GetIsPC(enemy) && CreatureAIRules.IsCreatureAIEnabled(enemy))
                    TryAcquireAggro(enemy, target);
            }
        }

        private bool TryAcquireAggro(uint self, uint target)
        {
            if (self == target ||
                !GetIsObjectValid(target) ||
                GetIsDead(target) ||
                GetHasEffect(target, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility) ||
                !CreatureAIRules.IsInAggroRange(self, target) ||
                !GetIsEnemy(target, self) ||
                _ai.TryStartLeashEvade(self, target) ||
                !TryAddProximityEnmity(target, self))
            {
                return false;
            }

            _decision.ProcessTrigger(self, AITriggerType.Aggro, target);
            AddNearbyAllyProximityEnmity(self, target);
            return true;
        }

        private void AddNearbyAllyProximityEnmity(uint self, uint target)
        {
            if (!_ai.TryGetAllies(self, out var allies))
                return;

            foreach (var ally in allies)
            {
                if (!CreatureAIRules.IsCreatureAIEnabled(ally)) continue;
                if (!GetIsEnemy(target, ally)) continue;
                if (GetDistanceBetween(self, ally) > 5f) continue;
                if (!LineOfSightObject(target, ally)) continue;

                TryAddProximityEnmity(target, ally);
            }
        }

        private bool TryAddProximityEnmity(uint target, uint enemy)
        {
            if (HasProximityEnmity(target, enemy))
                return false;

            ModifyProximityEnmity(target, enemy, ProximityEnmityAmount);
            return true;
        }

        private void RemoveProximityEnmity(uint target, uint enemy)
        {
            if (_ai.TryStartLeashEvade(enemy, target))
                return;

            if (!RemoveTrackedProximityEnmity(target, enemy))
                return;

            var nextTarget = GetHighestEnmityTarget(enemy);
            if (GetIsObjectValid(nextTarget))
            {
                AttackHighestEnmityTarget(enemy);
                return;
            }

            _ai.StopCombatAfterProximityLoss(enemy);
        }

        // ---- Seams into services that are not converted yet ----
        // Each protected virtual method below has the original call as its body, so the server behaves as it did.

        protected virtual uint GetHighestEnmityTarget(uint enemy) => _enmity.GetHighestEnmityTarget(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => _enmityAttack.AttackHighestEnmityTarget(creature);

        protected virtual bool HasProximityEnmity(uint creature, uint enemy) => _enmity.HasProximityEnmity(creature, enemy);

        protected virtual void ModifyProximityEnmity(uint creature, uint enemy, int amount) => _enmityChange.ModifyProximityEnmity(creature, enemy, amount);

        protected virtual bool RemoveTrackedProximityEnmity(uint creature, uint enemy) => _enmity.RemoveProximityEnmity(creature, enemy);

        protected virtual bool TryBeginInfiltration(uint player, uint npc) => _espionageInfiltration.TryBegin(player, npc);

        protected virtual void CompleteInfiltration(uint player, uint npc) => _espionageInfiltration.Complete(player, npc);

        protected virtual bool CanAcquireAggro(uint observer, uint target) => _stealth.CanAcquireAggro(observer, target);
    }
}
