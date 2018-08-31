using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleExamine: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IFarmingService _farming;
        private readonly IDurabilityService _durability;
        private readonly IPerkService _perk;
        private readonly IItemService _item;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IExaminationService _examination;
        private readonly IRuneService _rune;

        public OnModuleExamine(
            INWScript script,
            IFarmingService farming,
            IDurabilityService durability,
            IPerkService perk,
            IItemService item,
            INWNXEvents nwnxEvents,
            IExaminationService examination,
            IRuneService rune)
        {
            _ = script;
            _farming = farming;
            _durability = durability;
            _perk = perk;
            _item = item;
            _nwnxEvents = nwnxEvents;
            _examination = examination;
            _rune = rune;
        }

        public bool Run(params object[] args)
        {
            NWPlayer examiner = NWPlayer.Wrap(Object.OBJECT_SELF);
            NWObject examinedObject = _nwnxEvents.OnExamineObject_GetTarget();
            if (_examination.OnModuleExamine(examiner, examinedObject)) return true;

            string description = _.GetDescription(examinedObject.Object, NWScript.TRUE) + "\n\n";
            description = _rune.OnModuleExamine(description, examiner, examinedObject);
            description = _item.OnModuleExamine(description, examiner, examinedObject);
            description = _perk.OnModuleExamine(description, examiner, examinedObject);
            description = _durability.OnModuleExamine(description, examinedObject);
            description = _farming.OnModuleExamine(description, examinedObject);

            if (string.IsNullOrWhiteSpace(description)) return false;
            _.SetDescription(examinedObject.Object, description, NWScript.FALSE);
            _.SetDescription(examinedObject.Object, description);
            

            return true;
        }
    }
}
