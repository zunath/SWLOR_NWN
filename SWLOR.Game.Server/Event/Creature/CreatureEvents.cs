using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;

namespace SWLOR.Game.Server.Event.Creature
{
    public static class CreatureEvents
    {
        [NWNEventHandler("crea_on_attacked")]
        public static void OnCreatureAttacked()
        {
            MessageHub.Instance.Publish(new OnCreaturePhysicalAttacked());
        }

        [NWNEventHandler("crea_on_blocked")]
        public static void OnCreatureBlocked()
        {
            MessageHub.Instance.Publish(new OnCreatureBlocked());
        }

        [NWNEventHandler("crea_on_convo")]
        public static void OnCreatureConversation()
        {
            MessageHub.Instance.Publish(new OnCreatureConversation());
        }

        [NWNEventHandler("crea_on_damaged")]
        public static void OnCreatureDamaged()
        {
            MessageHub.Instance.Publish(new OnCreatureDamaged());
        }

        [NWNEventHandler("crea_on_death")]
        public static void OnCreatureDeath()
        {
            MessageHub.Instance.Publish(new OnCreatureDeath());
        }

        [NWNEventHandler("crea_on_disturb")]
        public static void OnCreatureDisturbed()
        {
            MessageHub.Instance.Publish(new OnCreatureDisturbed());
        }

        [NWNEventHandler("crea_on_hb")]
        public static void OnCreatureHeartbeat()
        {
            MessageHub.Instance.Publish(new OnCreatureHeartbeat());
        }

        [NWNEventHandler("crea_on_percept")]
        public static void OnCreaturePerception()
        {
            MessageHub.Instance.Publish(new OnCreaturePerception());
        }

        [NWNEventHandler("crea_on_rested")]
        public static void OnCreatureRested()
        {
            MessageHub.Instance.Publish(new OnCreatureRested());
        }

        [NWNEventHandler("crea_on_roundend")]
        public static void OnCreatureRoundEnd()
        {
            MessageHub.Instance.Publish(new OnCreatureCombatRoundEnd());
        }

        [NWNEventHandler("crea_on_spawn")]
        public static void OnCreatureSpawn()
        {
            MessageHub.Instance.Publish(new OnCreatureSpawn());
        }

        [NWNEventHandler("crea_on_splcast")]
        public static void OnCreatureSpellCastAt()
        {
            MessageHub.Instance.Publish(new OnCreatureSpellCastAt());
        }

        [NWNEventHandler("crea_on_userdef")]
        public static void OnCreatureUserDefined()
        {
            MessageHub.Instance.Publish(new OnCreatureUserDefined());
        }
    }
}
