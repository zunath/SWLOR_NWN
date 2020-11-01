using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Bioware;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.CraftingForge
{
    public class CompleteSmelt: IScript
    {
        public void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnCompleteSmelt>(OnCompleteSmelt);
        }

        private void OnCompleteSmelt(OnCompleteSmelt data)
        {
            var player = data.Player;
            var oreResref = data.OreResref;
            var itemProperties = data.ItemProperties;

            player.IsBusy = false;

            var rank = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            var level = CraftService.GetIngotLevel(oreResref);
            var ingotResref = CraftService.GetIngotResref(oreResref);
            if (level < 0 || string.IsNullOrWhiteSpace(ingotResref)) return;

            var delta = rank - level;
            var count = 2;

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

            for (var x = 1; x <= count; x++)
            {
                var item = (NWScript.CreateItemOnObject(ingotResref, player.Object));
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
            var harvestingSkill = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            var perkBonus = PerkService.GetCreaturePerkLevel(player, PerkType.StronidiumRefining) + 1;
            var stronidiumAmount = 10 + effectiveStats.Harvesting + harvestingSkill + RandomService.Random(1, 5);
            stronidiumAmount *= perkBonus;
            NWScript.CreateItemOnObject("stronidium", player.Object, stronidiumAmount);

            var xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, level, rank);
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
