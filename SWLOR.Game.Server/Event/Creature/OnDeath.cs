using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Creature
{
    public class OnDeath : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;
            SkillService.OnCreatureDeath(self);
            LootService.OnCreatureDeath(self);
            QuestService.OnCreatureDeath(self);
            CreatureCorpseService.OnCreatureDeath();

            if (AppCache.CustomObjectData.ContainsKey(self.GlobalID))
            {
                AppCache.CustomObjectData.Remove(self.GlobalID);
            }

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return false;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return false;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnDeath();
            });

            return true;
        }
    }
}
