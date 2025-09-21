using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Eventing;

namespace SWLOR.Shared.Events.Service
{
    internal class ModuleEventHandlers
    {
        [ScriptHandler<OnEventingModuleLoad>]
        public void RunModuleLoad()
        {
            ExecuteScript(ScriptName.OnModuleLoad, OBJECT_SELF);
        }
         
        [ScriptHandler<OnEventingModuleEnter>]
        public void RunModuleEnter()
        {
            ExecuteScript(ScriptName.OnPlayerCacheData, OBJECT_SELF);
            ExecuteScript(ScriptName.OnModuleEnter, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleExit>]
        public void RunModuleExit()
        {
            ExecuteScript(ScriptName.OnModuleExit, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleDeath>]
        public void RunModuleDeath()
        {
            ExecuteScript(ScriptName.OnModuleDeath, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleDying>]
        public void RunModuleDying()
        {
            ExecuteScript(ScriptName.OnModuleDying, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleRespawn>]
        public void RunModuleRespawn()
        {
            ExecuteScript(ScriptName.OnModuleRespawn, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleAcquire>]
        public void RunModuleAcquire()
        {
            ExecuteScript(ScriptName.OnModuleAcquire, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleUnacquire>]
        public void RunModuleUnacquire()
        {
            ExecuteScript(ScriptName.OnModuleUnacquire, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModulePreload>]
        public void RunModulePreload()
        {
            ExecuteScript(ScriptName.OnModulePreload, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleGuiEvent>]
        public void RunModuleGuiEvent()
        {
            ExecuteScript(ScriptName.OnModuleGuiEvent, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleChat>]
        public void RunModuleChat()
        {
            ExecuteScript(ScriptName.OnModuleChat, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleNuiEvent>]
        public void RunModuleNuiEvent()
        {
            ExecuteScript(ScriptName.OnModuleNuiEvent, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleEquip>]
        public void RunModuleEquip()
        {
            ExecuteScript(ScriptName.OnModuleEquip, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleUnequip>]
        public void RunModuleUnequip()
        {
            ExecuteScript(ScriptName.OnModuleUnequip, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleRest>]
        public void RunModuleRest()
        {
            ExecuteScript(ScriptName.OnModuleRest, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModulePlayerTarget>]
        public void RunModulePlayerTarget()
        {
            ExecuteScript(ScriptName.OnModulePlayerTarget, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleActivate>]
        public void RunModuleActivate()
        {
            ExecuteScript(ScriptName.OnModuleActivate, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModulePlayerCancelCutscene>]
        public void RunModulePlayerCancelCutscene()
        {
            ExecuteScript(ScriptName.OnModulePlayerCancelCutscene, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleHeartbeat>]
        public void RunModuleHeartbeat()
        {
            ExecuteScript(ScriptName.OnModuleHeartbeat, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleLevelUp>]
        public void RunModuleLevelUp()
        {
            ExecuteScript(ScriptName.OnModuleLevelUp, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleUserDefined>]
        public void RunModuleUserDefined()
        {
            ExecuteScript(ScriptName.OnModuleUserDefined, OBJECT_SELF);
        }

        [ScriptHandler<OnEventingModuleTileEvent>]
        public void RunModuleTileEvent()
        {
            ExecuteScript(ScriptName.OnModuleTileEvent, OBJECT_SELF);
        }
    }
}
