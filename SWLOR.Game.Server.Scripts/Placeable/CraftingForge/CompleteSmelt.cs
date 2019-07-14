using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.CraftingForge
{
    public class CompleteSmelt: IScript
    {
        public void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnCompleteSmelt>(OnCompleteSmelt);
        }

        private void OnCompleteSmelt(OnCompleteSmelt data)
        {
            NWPlayer player = data.Player;
            string oreResref = data.OreResref;
            List<ItemProperty> itemProperties = data.ItemProperties;

            player.IsBusy = false;

            int rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            int level = CraftService.GetIngotLevel(oreResref);
            string ingotResref = CraftService.GetIngotResref(oreResref);
            if (level < 0 || string.IsNullOrWhiteSpace(ingotResref)) return;

            int delta = rank - level;
            int count = 2;

            if (delta > 2) count = delta;
            if (count > 4) count = 4;

            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel(player, PerkType.Lucky))
            {
                count++;
            }

            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel(player, PerkType.ProcessingEfficiency) * 10)
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
                    if (RandomService.D100(1) <= chance)
                    {
                        BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
                    }
                }
            }

            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int harvestingSkill = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            int perkBonus = PerkService.GetCreaturePerkLevel(player, PerkType.StronidiumRefining) + 1;
            int stronidiumAmount = 10 + effectiveStats.Harvesting + harvestingSkill + RandomService.Random(1, 5);
            stronidiumAmount *= perkBonus;
            _.CreateItemOnObject("stronidium", player.Object, stronidiumAmount);

            int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, level, rank);
            SkillService.GiveSkillXP(player, SkillType.Harvesting, xp);
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
        }
    }
}
