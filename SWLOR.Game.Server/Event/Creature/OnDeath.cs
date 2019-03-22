﻿using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnDeath : IRegisteredEvent
    {
        private readonly ISkillService _skill;
        private readonly ILootService _loot;
        private readonly IQuestService _quest;
        private readonly ICreatureCorpseService _creatureCorpse;
        
        public OnDeath(
            ISkillService skill,
            ILootService loot,
            IQuestService quest,
            ICreatureCorpseService creatureCorpse)
        {
            _skill = skill;
            _loot = loot;
            _quest = quest;
            _creatureCorpse = creatureCorpse;
        }

        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;
            _skill.OnCreatureDeath(self);
            _loot.OnCreatureDeath(self);
            _quest.OnCreatureDeath(self);
            _creatureCorpse.OnCreatureDeath();

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
