using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data;
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

            int rank = _skill.GetPCSkillRank(player, SkillType.Engineering);
            int level = _craft.GetIngotLevel(oreResref);
            string ingotResref = _craft.GetIngotResref(oreResref);
            if (level < 0 || string.IsNullOrWhiteSpace(ingotResref)) return false;

            int delta = rank - level;
            int count = 2;

            if (delta > 2) count = delta;
            if (count > 4) count = 4;

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
                int chance;

                switch (x)
                {
                    case 1:
                    case 2:
                        chance = 100;
                        break;
                    case 3:
                        chance = 70;
                        break;
                    case 4:
                        chance = 60;
                        break;
                    case 5:
                        chance = 50;
                        break;
                    default:
                        chance = 30;
                        break;
                }

                foreach (var ip in itemProperties)
                {
                    if(_random.D100(1) <= chance)
                    {
                        _biowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
                    }
                }
            }

            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int harvestingSkill = _skill.GetPCSkillRank(player, SkillType.Harvesting);
            int perkBonus = _perk.GetPCPerkLevel(player, PerkType.StronidiumRefining) + 1;
            int stronidiumAmount = 10 + effectiveStats.Harvesting + harvestingSkill + _random.Random(1, 5);
            stronidiumAmount *= perkBonus;
            _.CreateItemOnObject("stronidium", player.Object, stronidiumAmount);

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, level, rank);
            _skill.GiveSkillXP(player, SkillType.Engineering, xp);
            return true;
        }
    }
}
