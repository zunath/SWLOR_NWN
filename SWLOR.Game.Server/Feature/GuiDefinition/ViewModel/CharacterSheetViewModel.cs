using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterSheetViewModel: GuiViewModelBase<CharacterSheetViewModel>
    {
        public string PortraitResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HP
        {
            get => Get<string>();
            set => Set(value);
        }
        public string FP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string STM
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Might
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Perception
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Vitality
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Willpower
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Social
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Defense
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Evasion
        {
            get => Get<int>();
            set => Set(value);
        }

        public string CharacterType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Race
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AP
        {
            get => Get<string>();
            set => Set(value);
        }

        public Action OnClickSkills() => () =>
        {

        };

        public Action OnClickPerks() => () =>
        {

        };

        public Action OnClickChangePortrait() => () =>
        {

        };

        public Action OnClickQuests() => () =>
        {

        };

        public Action OnClickRecipes() => () =>
        {

        };

        public Action OnClickKeyItems() => () =>
        {

        };

        public Action OnClickAchievements() => () =>
        {

        };

        public Action OnLoadWindow() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            PortraitResref = GetPortraitResRef(Player) + "l";

            HP = GetCurrentHitPoints(Player) + " / " + GetMaxHitPoints(Player);
            FP = Stat.GetCurrentFP(Player, dbPlayer) + " / " + Stat.GetMaxFP(Player, dbPlayer);
            STM = Stat.GetCurrentStamina(Player, dbPlayer) + " / " + Stat.GetMaxStamina(Player, dbPlayer);
            Name = GetName(Player);
            Might = GetAbilityScore(Player, AbilityType.Might);
            Perception = GetAbilityScore(Player, AbilityType.Perception);
            Vitality = GetAbilityScore(Player, AbilityType.Vitality);
            Willpower = GetAbilityScore(Player, AbilityType.Willpower);
            Social = GetAbilityScore(Player, AbilityType.Social);
            Defense = Stat.GetDefense(Player, CombatDamageType.Physical);
            Evasion = GetAC(Player);
            CharacterType = GetClassByPosition(1, Player) == ClassType.Standard ? "Standard" : "Force Sensitive";
            Race = GetStringByStrRef(Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(Player))), GetGender(Player));
            SP = $"{dbPlayer.TotalSPAcquired} / {Skill.SkillCap} ({dbPlayer.UnallocatedSP})";
            AP = $"{dbPlayer.TotalAPAcquired} / 30 ({dbPlayer.UnallocatedAP})";
            

        };

        

    }
}
