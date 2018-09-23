using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.CraftingForge
{
    public class CompleteSmelt: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly ICraftService _craft;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IPlayerStatService _playerStat;

        public CompleteSmelt(
            INWScript script,
            ISkillService skill,
            ICraftService craft,
            IRandomService random,
            IPerkService perk,
            IBiowareXP2 biowareXP2,
            IPlayerStatService playerStat)
        {
            _ = script;
            _skill = skill;
            _craft = craft;
            _random = random;
            _perk = perk;
            _biowareXP2 = biowareXP2;
            _playerStat = playerStat;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (NWPlayer)args[0];
            string oreResref = (string) args[1];
            List<ItemProperty> itemProperties = (List<ItemProperty>) args[2];
            
            player.IsBusy = false;

            PCSkill pcSkill = _skill.GetPCSkill(player, SkillType.Engineering);
            int level = _craft.GetIngotLevel(oreResref);
            string ingotResref = _craft.GetIngotResref(oreResref);
            if (pcSkill == null || level < 0 || string.IsNullOrWhiteSpace(ingotResref)) return false;

            int delta = pcSkill.Rank - level;
            int count = 2;

            if (delta > 2) count = delta;
            if (count > 6) count = 6;

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(player, PerkType.Lucky))
            {
                count++;
            }

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(player, PerkType.ProcessingEfficiency) * 10)
            {
                count++;
            }

            for (int x = 1; x <= count; x++)
            {
                var item = (_.CreateItemOnObject(ingotResref, player.Object));
                foreach (var ip in itemProperties)
                {
                    _biowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
                }
            }

            int stronidiumAmount = 2 + _playerStat.EffectiveHarvestingBonus(player);
            _.CreateItemOnObject("stronidium", player.Object, stronidiumAmount);

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, level, pcSkill.Rank);
            _skill.GiveSkillXP(player, SkillType.Engineering, xp);
            return true;
        }
    }
}
