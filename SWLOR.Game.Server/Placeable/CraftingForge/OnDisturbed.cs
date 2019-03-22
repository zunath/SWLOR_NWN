using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.CraftingForge
{
    public class OnDisturbed: IRegisteredEvent
    {
        
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly ICraftService _craft;
        private readonly IBiowarePosition _biowarePosition;
        private readonly INWNXPlayer _nwnxPlayer;

        public OnDisturbed(
            IPerkService perk,
            ISkillService skill,
            ICraftService craft,
            IBiowarePosition biowarePosition,
            INWNXPlayer nwnxPlayer)
        {
            
            _perk = perk;
            _skill = skill;
            _craft = craft;
            _biowarePosition = biowarePosition;
            _nwnxPlayer = nwnxPlayer;
        }

        public bool Run(params object[] args)
        {
            if (_.GetInventoryDisturbType() != _.INVENTORY_DISTURB_TYPE_ADDED) return false;

            NWPlayer pc = (_.GetLastDisturbed());
            NWItem item = (_.GetInventoryDisturbItem());
            NWPlaceable forge = (Object.OBJECT_SELF);

            if (!CheckValidity(forge, pc, item)) return false;
            StartSmelt(forge, pc, item);
            return true;
        }


        private bool CheckValidity(NWPlaceable forge, NWPlayer pc, NWItem item)
        {
            if (pc.IsBusy)
            {
                ReturnItemToPC(pc, item, "You are too busy.");
                return false;
            }

            if (_.GetIsObjectValid(forge.GetLocalObject("FORGE_USER")) == _.TRUE)
            {
                ReturnItemToPC(pc, item, "This forge is currently in use. Please wait...");
                return false;
            }

            string[] allowed = {
                "power_core",
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
                ReturnItemToPC(pc, item, "Only power cores and raw materials may be placed inside.");
                return false;
            }

            int level = _craft.GetIngotLevel(item.Resref);
            int rank = _skill.GetPCSkillRank(pc, SkillType.Harvesting);
            
            int delta = rank - level;
            if (delta <= -4)
            {
                ReturnItemToPC(pc, item, "You do not have enough skill to refine this material.");
                return false;
            }

            int pcPerkLevel = _perk.GetPCPerkLevel(pc, PerkType.Refining);
            int orePerkLevel = _craft.GetIngotPerkLevel(item.Resref);

            if (pcPerkLevel < orePerkLevel)
            {
                ReturnItemToPC(pc, item, "You do not have the perk necessary to refine this material.");
                return false;
            }

            return true;
        }

        private void StartSmelt(NWPlaceable forge, NWPlayer pc, NWItem item)
        {
            int charges = forge.GetLocalInt("FORGE_CHARGES");
            if (item.Resref == "power_core")
            {
                item.Destroy();
                charges += 10 + CalculatePerkCoalBonusCharges(pc) + GetPowerCoreDurability(item) * 2;
                forge.SetLocalInt("FORGE_CHARGES", charges);

                NWPlaceable flames = (forge.GetLocalObject("FORGE_FLAMES"));
                if (!flames.IsValid)
                {
                    Vector flamePosition = _biowarePosition.GetChangedPosition(forge.Position, 0.36f, forge.Facing);
                    Location flameLocation = _.Location(forge.Area.Object, flamePosition, 0.0f);
                    flames = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, "forge_flame", flameLocation));
                    forge.SetLocalObject("FORGE_FLAMES", flames.Object);
                }

                return;
            }
            else if (charges <= 0)
            {
                ReturnItemToPC(pc, item, "You must power the refinery with a power unit before refining.");
                return;
            }

            // Ready to smelt
            float baseCraftDelay = 18.0f - (18.0f * _perk.GetPCPerkLevel(pc, PerkType.SpeedyRefining) * 0.1f);

            pc.IsBusy = true;
            _nwnxPlayer.StartGuiTimingBar(pc, baseCraftDelay, string.Empty);

            // Any component bonuses on the ore get applied to the end product.
            var itemProperties = item.ItemProperties.Where(x =>
                _.GetItemPropertyType(x) == (int)CustomItemPropertyType.ComponentBonus ||
                _.GetItemPropertyType(x) == (int) CustomItemPropertyType.RecommendedLevel).ToList();

            string itemResref = item.Resref;

            pc.DelayEvent<CompleteSmelt>(baseCraftDelay, pc, itemResref, itemProperties);
            
            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), pc.Object, baseCraftDelay);
            pc.AssignCommand(() => _.ActionPlayAnimation(_.ANIMATION_LOOPING_GET_MID, 1.0f, baseCraftDelay));
            item.Destroy();
        }

        private int GetPowerCoreDurability(NWItem item)
        {
            int durability = 0;
            foreach (var ip in item.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == (int)CustomItemPropertyType.ComponentBonus)
                {
                    int bonusTypeID = _.GetItemPropertySubType(ip);
                    if (bonusTypeID == (int) ComponentBonusType.DurabilityUp)
                    {
                        int amount = _.GetItemPropertyCostTableValue(ip);
                        durability += amount;
                    }
                }
            }

            return durability;
        }

        private void ReturnItemToPC(NWPlayer pc, NWItem item, string message)
        {
            _.CopyItem(item.Object, pc.Object, _.TRUE);
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
