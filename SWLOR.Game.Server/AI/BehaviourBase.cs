using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.AI
{
    public abstract class BehaviourBase: IBehaviour
    {
        protected NWCreature Self { get; }

        protected BehaviourBase()
        {
            Self = NWCreature.Wrap(Object.OBJECT_SELF);
        }

        public virtual BehaviourTreeBuilder Behaviour => null;

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


        public virtual void OnBlocked()
        {
        }

        public virtual void OnCombatRoundEnd()
        {
        }

        public virtual void OnConversation()
        {
        }

        public virtual void OnDamaged()
        {
        }

        public virtual void OnDeath()
        {
        }

        public virtual void OnDisturbed()
        {
        }

        public virtual void OnHeartbeat()
        {
        }

        public virtual void OnPerception()
        {
        }

        public virtual void OnPhysicalAttacked()
        {
        }

        public virtual void OnRested()
        {
        }

        public virtual void OnSpawn()
        {
        }

        public virtual void OnSpellCastAt()
        {
        }

        public virtual void OnUserDefined()
        {
        }

    }
}
