using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Eventing;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
namespace SWLOR.Shared.Events.Service
{
    internal class ModuleEventHandlers : IEventHandler
    {
        private readonly IEventAggregator _eventAggregator;

        public ModuleEventHandlers(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        [ScriptHandler<OnEventingModuleLoad>]
        public void RunModuleLoad()
        {
            _eventAggregator.Publish(new OnModuleLoad(), GetModule());
        }
         
        [ScriptHandler<OnEventingModuleEnter>]
        public void RunModuleEnter()
        {
            _eventAggregator.Publish(new OnPlayerCacheData(), GetModule());
            _eventAggregator.Publish(new OnModuleEnter(), GetModule());
        }

        [ScriptHandler<OnEventingModuleExit>]
        public void RunModuleExit()
        {
            _eventAggregator.Publish(new OnModuleExit(), GetModule());
        }

        [ScriptHandler<OnEventingModuleDeath>]
        public void RunModuleDeath()
        {
            _eventAggregator.Publish(new OnModuleDeath(), GetModule());
        }

        [ScriptHandler<OnEventingModuleDying>]
        public void RunModuleDying()
        {
            _eventAggregator.Publish(new OnModuleDying(), GetModule());
        }

        [ScriptHandler<OnEventingModuleRespawn>]
        public void RunModuleRespawn()
        {
            _eventAggregator.Publish(new OnModuleRespawn(), GetModule());
        }

        [ScriptHandler<OnEventingModuleAcquire>]
        public void RunModuleAcquire()
        {
            _eventAggregator.Publish(new OnModuleAcquire(), GetModule());
        }

        [ScriptHandler<OnEventingModuleUnacquire>]
        public void RunModuleUnacquire()
        {
            _eventAggregator.Publish(new OnModuleUnacquire(), GetModule());
        }

        [ScriptHandler<OnEventingModulePreload>]
        public void RunModulePreload()
        {
            _eventAggregator.Publish(new OnModulePreload(), GetModule());
        }

        [ScriptHandler<OnEventingModuleGuiEvent>]
        public void RunModuleGuiEvent()
        {
            _eventAggregator.Publish(new OnModuleGuiEvent(), GetModule());
        }

        [ScriptHandler<OnEventingModuleChat>]
        public void RunModuleChat()
        {
            _eventAggregator.Publish(new OnModuleChat(), GetModule());
        }

        [ScriptHandler<OnEventingModuleNuiEvent>]
        public void RunModuleNuiEvent()
        {
            _eventAggregator.Publish(new OnModuleNuiEvent(), GetModule());
        }

        [ScriptHandler<OnEventingModuleEquip>]
        public void RunModuleEquip()
        {
            _eventAggregator.Publish(new OnModuleEquip(), GetModule());
        }

        [ScriptHandler<OnEventingModuleUnequip>]
        public void RunModuleUnequip()
        {
            _eventAggregator.Publish(new OnModuleUnequip(), GetModule());
        }

        [ScriptHandler<OnEventingModuleRest>]
        public void RunModuleRest()
        {
            _eventAggregator.Publish(new OnModuleRest(), GetModule());
        }

        [ScriptHandler<OnEventingModulePlayerTarget>]
        public void RunModulePlayerTarget()
        {
            _eventAggregator.Publish(new OnModulePlayerTarget(), GetModule());
        }

        [ScriptHandler<OnEventingModuleActivate>]
        public void RunModuleActivate()
        {
            _eventAggregator.Publish(new OnModuleActivate(), GetModule());
        }

        [ScriptHandler<OnEventingModulePlayerCancelCutscene>]
        public void RunModulePlayerCancelCutscene()
        {
            _eventAggregator.Publish(new OnModulePlayerCancelCutscene(), GetModule());
        }

        [ScriptHandler<OnEventingModuleHeartbeat>]
        public void RunModuleHeartbeat()
        {
            _eventAggregator.Publish(new OnModuleHeartbeat(), GetModule());
        }

        [ScriptHandler<OnEventingModuleLevelUp>]
        public void RunModuleLevelUp()
        {
            _eventAggregator.Publish(new OnModuleLevelUp(), GetModule());
        }

        [ScriptHandler<OnEventingModuleUserDefined>]
        public void RunModuleUserDefined()
        {
            _eventAggregator.Publish(new OnModuleUserDefined(), GetModule());
        }

        [ScriptHandler<OnEventingModuleTileEvent>]
        public void RunModuleTileEvent()
        {
            _eventAggregator.Publish(new OnModuleTileEvent(), GetModule());
        }
    }
}


