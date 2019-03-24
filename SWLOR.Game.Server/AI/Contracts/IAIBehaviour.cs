using FluentBehaviourTree;

namespace SWLOR.Game.Server.AI.Contracts
{
    public interface IAIBehaviour
    {
        BehaviourTreeBuilder Behaviour { get; }
        bool IgnoreNWNEvents { get; }

        bool IgnoreOnBlocked { get; }
        bool IgnoreOnCombatRoundEnd { get; }
        bool IgnoreOnConversation { get; }
        bool IgnoreOnDamaged { get; }
        bool IgnoreOnDeath { get; }
        bool IgnoreOnDisturbed { get; }
        bool IgnoreOnHeartbeat { get; }
        bool IgnoreOnPerception { get; }
        bool IgnoreOnPhysicalAttacked { get; }
        bool IgnoreOnRested { get; }
        bool IgnoreOnSpawn { get; }
        bool IgnoreOnSpellCastAt { get; }
        bool IgnoreOnUserDefined { get; }

        void OnBlocked();
        void OnCombatRoundEnd();
        void OnConversation();
        void OnDamaged();
        void OnDeath();
        void OnDisturbed();
        void OnHeartbeat();
        void OnPerception();
        void OnPhysicalAttacked();
        void OnRested();
        void OnSpawn();
        void OnSpellCastAt();
        void OnUserDefined();

    }
}
