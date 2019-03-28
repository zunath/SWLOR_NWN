using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.AI.Contracts
{
    public interface IAIBehaviour
    {
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
        

        void OnBlocked(NWCreature self);
        void OnCombatRoundEnd(NWCreature self);
        void OnConversation(NWCreature self);
        void OnDamaged(NWCreature self);
        void OnDeath(NWCreature self);
        void OnDisturbed(NWCreature self);
        void OnHeartbeat(NWCreature self);
        void OnPerception(NWCreature self);
        void OnPhysicalAttacked(NWCreature self);
        void OnRested(NWCreature self);
        void OnSpawn(NWCreature self);
        void OnSpellCastAt(NWCreature self);
        void OnUserDefined(NWCreature self);
        void OnProcessObject(NWCreature self);
    }
}
