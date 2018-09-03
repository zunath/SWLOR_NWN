using SWLOR.Game.Server.Enumeration;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLoad: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly INWNXChat _nwnxChat;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IDeathService _death;
        private readonly IObjectProcessingService _objectProcessing;
        private readonly IFarmingService _farming;
        private readonly IAppStateService _appStateService;
        private readonly INWNXDamage _nwnxDamage;
        private readonly IAreaService _area;

        public OnModuleLoad(INWScript script,
            INWNXChat nwnxChat,
            INWNXEvents nwnxEvents,
            IDeathService death,
            IObjectProcessingService objectProcessing,
            IFarmingService farming,
            IAppStateService appStateService,
            INWNXDamage nwnxDamage,
            IAreaService area)
        {
            _ = script;
            _nwnxChat = nwnxChat;
            _nwnxEvents = nwnxEvents;
            _death = death;
            _objectProcessing = objectProcessing;
            _farming = farming;
            _appStateService = appStateService;
            _nwnxDamage = nwnxDamage;
            _area = area;
        }

        public bool Run(params object[] args)
        {
            _nwnxChat.RegisterChatScript("mod_on_nwnxchat");
            SetModuleEventScripts();
            SetAreaEventScripts();

            // Bioware default
            _.ExecuteScript("x2_mod_def_load", Object.OBJECT_SELF);
            
            _death.OnModuleLoad();
            _appStateService.OnModuleLoad();
            _objectProcessing.OnModuleLoad();
            _farming.OnModuleLoad();
            _area.OnModuleLoad();
            
            return true;
        }

        private void SetAreaEventScripts()
        {
            Object area = _.GetFirstArea();
            while (_.GetIsObjectValid(area) == NWScript.TRUE)
            {
                _.SetEventScript(area, NWScript.EVENT_SCRIPT_AREA_ON_ENTER, "area_on_enter");
                _.SetEventScript(area, NWScript.EVENT_SCRIPT_AREA_ON_EXIT, "area_on_exit");
                _.SetEventScript(area, NWScript.EVENT_SCRIPT_AREA_ON_HEARTBEAT, "area_on_hb");
                _.SetEventScript(area, NWScript.EVENT_SCRIPT_AREA_ON_USER_DEFINED_EVENT, "area_on_user");

                area = _.GetNextArea();
            }
        }

        private void SetModuleEventScripts()
        {
            // Vanilla NWN Event Hooks
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_ACQUIRE_ITEM, "mod_on_acquire");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_ACTIVATE_ITEM, "mod_on_activate");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_CLIENT_ENTER, "mod_on_enter");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_CLIENT_EXIT, "mod_on_leave");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_CANCEL_CUTSCENE, "mod_on_csabort");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_HEARTBEAT, "mod_on_heartbeat");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_CHAT, "mod_on_chat");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_DEATH, "mod_on_death");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_DYING, "mod_on_dying");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_EQUIP_ITEM, "mod_on_equip");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_LEVEL_UP, "mod_on_levelup");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_RESPAWN_BUTTON_PRESSED, "mod_on_respawn");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_PLAYER_REST, "mod_on_rest");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_UNEQUIP_ITEM, "mod_on_unequip");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_LOSE_ITEM, "mod_on_unacquire");
            _.SetEventScript(_.GetModule(), NWScript.EVENT_SCRIPT_MODULE_ON_USER_DEFINED_EVENT, "mod_on_user");

            // NWNX Hooks
            _nwnxEvents.SubscribeEvent(EventType.StartCombatRoundBefore, "mod_on_attack");
            _nwnxEvents.SubscribeEvent(EventType.ExamineObjectBefore, "mod_on_examine");
            _nwnxEvents.SubscribeEvent(EventType.UseFeatBefore, "mod_on_usefeat");
            _nwnxDamage.SetDamageEventScript("mod_on_applydmg");
        }
    }
}
