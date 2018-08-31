using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Placeable.CraftingForge
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly ICraftService _craft;
        private readonly IBiowarePosition _biowarePosition;
        private readonly INWNXPlayer _nwnxPlayer;

        public OnDisturbed(INWScript script,
            IPerkService perk,
            ISkillService skill,
            ICraftService craft,
            IBiowarePosition biowarePosition,
            INWNXPlayer nwnxPlayer)
        {
            _ = script;
            _perk = perk;
            _skill = skill;
            _craft = craft;
            _biowarePosition = biowarePosition;
            _nwnxPlayer = nwnxPlayer;
        }

        public bool Run(params object[] args)
        {
            if (_.GetInventoryDisturbType() != NWScript.INVENTORY_DISTURB_TYPE_ADDED) return false;

            NWPlayer pc = NWPlayer.Wrap(_.GetLastDisturbed());
            NWItem item = NWItem.Wrap(_.GetInventoryDisturbItem());
            NWPlaceable forge = NWPlaceable.Wrap(Object.OBJECT_SELF);

            if (!checkValidity(forge, pc, item)) return false;
            startSmelt(forge, pc, item);
            return true;
        }


        private bool checkValidity(NWPlaceable forge, NWPlayer pc, NWItem item)
        {
            if (pc.IsBusy)
            {
                ReturnItemToPC(pc, item, "You are too busy.");
                return false;
            }

            if (_.GetIsObjectValid(forge.GetLocalObject("FORGE_USER")) == NWScript.TRUE)
            {
                ReturnItemToPC(pc, item, "This forge is currently in use. Please wait...");
                return false;
            }

            string[] allowed = {
                "power_unit",
                "raw_veldite",
                "raw_scordspar",
                "raw_plagionite",
                "raw_keromber",
                "raw_jasioclase",
                "raw_hemorgite",
                "raw_ochne",
                "raw_croknor",
                "raw_arkoxit",
                "raw_bisteiss"
        };

            if (!allowed.Contains(item.Resref))
            {
                ReturnItemToPC(pc, item, "Only power units and raw materials may be placed inside.");
                return false;
            }

            int level = _craft.GetIngotLevel(item.Resref);
            PCSkill pcSkill = _skill.GetPCSkill(pc, SkillType.Engineering);
            if (pcSkill == null) return false;

            int delta = pcSkill.Rank - level;
            if (delta <= -4)
            {
                ReturnItemToPC(pc, item, "You do not have enough skill to refine this material.");
                return false;
            }

            int pcPerklevel = _perk.GetPCPerkLevel(pc, PerkType.Refining);
            int orePerkLevel = _craft.GetIngotPerkLevel(item.Resref);

            if (pcPerklevel < orePerkLevel)
            {
                ReturnItemToPC(pc, item, "You do not have the perk necessary to refine this material.");
                return false;
            }

            return true;
        }

        private void startSmelt(NWPlaceable forge, NWPlayer pc, NWItem item)
        {
            int charges = forge.GetLocalInt("FORGE_CHARGES");
            if (item.Resref == "coal")
            {
                item.Destroy();
                charges += 10 + CalculatePerkCoalBonusCharges(pc);
                forge.SetLocalInt("FORGE_CHARGES", charges);

                NWPlaceable flames = NWPlaceable.Wrap(forge.GetLocalObject("FORGE_FLAMES"));
                if (!flames.IsValid)
                {
                    Vector flamePosition = _biowarePosition.GetChangedPosition(forge.Position, 0.36f, forge.Facing);
                    Location flameLocation = _.Location(forge.Area.Object, flamePosition, 0.0f);
                    flames = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "forge_flame", flameLocation));
                    forge.SetLocalObject("FORGE_FLAMES", flames.Object);
                }

                return;
            }
            else if (charges <= 0)
            {
                ReturnItemToPC(pc, item, "You must power the refinery with a power unit before refining.");
                return;
            }
            item.Destroy();

            // Ready to smelt
            float baseCraftDelay = 18.0f - (18.0f * _perk.GetPCPerkLevel(pc, PerkType.SpeedyRefining) * 0.1f);

            pc.IsBusy = true;
            forge.SetLocalObject("FORGE_USER", pc.Object);
            pc.SetLocalObject("FORGE", forge.Object);
            forge.SetLocalString("FORGE_ORE", item.Resref);

            _nwnxPlayer.StartGuiTimingBar(pc, baseCraftDelay, "cft_finish_smelt");

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), pc.Object, baseCraftDelay);
            pc.AssignCommand(() => _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_GET_MID, 1.0f, baseCraftDelay));
        }

        private void ReturnItemToPC(NWPlayer pc, NWItem item, string message)
        {
            _.CopyItem(item.Object, pc.Object, NWScript.TRUE);
            item.Destroy();
            pc.SendMessage(message);
        }

        private int CalculatePerkCoalBonusCharges(NWPlayer pc)
        {
            int perkLevel = _perk.GetPCPerkLevel(pc, PerkType.RefineryManagement);

            switch (perkLevel)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                case 4: return 5;
                case 5: return 8;
                case 6: return 10;
                default: return 0;
            }
        }
    }
}
