using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// The base class for creating new behaviours.
    /// </summary>
    public abstract class BehaviourBase: IAIBehaviour
    {
        public virtual bool IgnoreNWNEvents => false;
        public virtual bool IgnoreOnBlocked => false;
        public virtual bool IgnoreOnCombatRoundEnd => false;
        public virtual bool IgnoreOnConversation => false;
        public virtual bool IgnoreOnDamaged => false;
        public virtual bool IgnoreOnDeath => false;
        public virtual bool IgnoreOnDisturbed => false;
        public virtual bool IgnoreOnHeartbeat => false;
        public virtual bool IgnoreOnPerception => false;
        public virtual bool IgnoreOnPhysicalAttacked => false;
        public virtual bool IgnoreOnRested => false;
        public virtual bool IgnoreOnSpawn => false;
        public virtual bool IgnoreOnSpellCastAt => false;
        public virtual bool IgnoreOnUserDefined => false;

        public virtual BehaviourTreeBuilder BuildBehaviour(NWCreature self)
        {
            return new BehaviourTreeBuilder();
        }
        
        public virtual void OnBlocked(NWCreature self)
        {
        }

        public virtual void OnCombatRoundEnd(NWCreature self)
        {
        }

        public virtual void OnConversation(NWCreature self)
        {
        }

        public virtual void OnDamaged(NWCreature self)
        {
        }

        public virtual void OnDeath(NWCreature self)
        {
        }

        public virtual void OnDisturbed(NWCreature self)
        {
        }

        public virtual void OnHeartbeat(NWCreature self)
        {
        }

        public virtual void OnPerception(NWCreature self)
        {
        }

        public virtual void OnPhysicalAttacked(NWCreature self)
        {
        }

        public virtual void OnRested(NWCreature self)
        {
        }

        public virtual void OnSpawn(NWCreature self)
        {
        }

        public virtual void OnSpellCastAt(NWCreature self)
        {
        }

        public virtual void OnUserDefined(NWCreature self)
        {
        }

    }
}
