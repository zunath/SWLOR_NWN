using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Provoke: IPerk
    {
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;
        private readonly IEnmityService _enmity;
        private readonly INWScript _;

        public Provoke(INWNXCreature nwnxCreature,
            IPerkService perk,
            IEnmityService enmity,
            INWScript script)
        {
            _nwnxCreature = nwnxCreature;
            _perk = perk;
            _enmity = enmity;
            _ = script;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oTarget.IsNPC && 
                oPC.Chest.CustomItemType == CustomItemType.HeavyArmor &&
                _.GetDistanceBetween(oPC.Object, oTarget.Object) <= 9.0f;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            if (!oTarget.IsNPC) return "Only NPCs may be targeted with Provoke.";
            float distance = _.GetDistanceBetween(oPC.Object, oTarget.Object);
            if (distance > 9.0f) return "Target is too far away.";

            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";
            return null;
        }

        public int ManaCost(NWPlayer oPC, int baseManaCost)
        {
            return baseManaCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            int perkRank = _perk.GetPCPerkLevel(oPC, PerkType.Provoke);

            if (perkRank == 2) baseCooldownTime -= 5.0f;
            else if (perkRank == 3) baseCooldownTime -= 10.0f;
            else if (perkRank == 4) baseCooldownTime -= 15.0f;

            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            NWCreature npc = NWCreature.Wrap(oTarget.Object);
            Effect vfx = _.EffectVisualEffect(NWScript.VFX_IMP_CHARM);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, vfx, oTarget.Object);
            
            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(NWScript.ANIMATION_FIREFORGET_TAUNT, 1f, 1f);
            });

            _enmity.AdjustEnmity(npc, oPC, 120);

            // todo debugging
            EnmityTable table = _enmity.GetEnmityTable(npc);

            foreach (var x in table.Values)
            {
                Console.WriteLine(npc.Name + ", " + x.TargetObject.Name + ": Volatile = " + x.VolatileAmount + ", Cumulative: " + x.CumulativeAmount);
            }
            // todo end debugging
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            _nwnxCreature.RemoveFeat(oPC, (int)CustomFeatType.Provoke);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.Chest;
            
            if (equipped.Equals(oItem) || equipped.CustomItemType != CustomItemType.HeavyArmor)
            {
                _nwnxCreature.RemoveFeat(oPC, (int)CustomFeatType.Provoke);
                return;
            }

            _nwnxCreature.AddFeat(oPC, (int)CustomFeatType.Provoke);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
