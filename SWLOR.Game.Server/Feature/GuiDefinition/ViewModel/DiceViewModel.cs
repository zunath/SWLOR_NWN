using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DiceViewModel : GuiViewModelBase<DiceViewModel, GuiPayloadBase>
    {
        private const int MinDiceCount = 1;
        private const int MaxDiceCount = 10;

        public int DiceCount
        {
            get => Get<int>();
            set
            {
                if (value < MinDiceCount)
                    value = MinDiceCount;
                else if (value > MaxDiceCount)
                    value = MaxDiceCount;

                Set(value);
                DiceCountText = $"Number of Dice: {value}";
            }
        }

        public string DiceCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> Skills
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public bool IsSkillSelectionVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedSkillId
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            IsSkillSelectionVisible = !GetIsDM(Player);
            LoadSkills();
            DiceCount = MinDiceCount;
            SelectedSkillId = (int)SkillType.Invalid;
            WatchOnClient(model => model.SelectedSkillId);
        }

        private void LoadSkills()
        {
            var skills = new GuiBindingList<GuiComboEntry>
            {
                new("None", (int)SkillType.Invalid)
            };

            if (IsSkillSelectionVisible)
            {
                foreach (var (skill, detail) in Skill.GetAllActiveSkills().OrderBy(x => x.Value.Name))
                {
                    skills.Add(new GuiComboEntry(detail.Name, (int)skill));
                }
            }

            Skills = skills;
        }

        public Action OnClickAddDie() => () => DiceCount++;
        public Action OnClickRemoveDie() => () => DiceCount--;

        public Action OnClickRollD2() => () => DoDiceRoll(2, DiceCount);
        public Action OnClickRollD4() => () => DoDiceRoll(4, DiceCount);
        public Action OnClickRollD6() => () => DoDiceRoll(6, DiceCount);
        public Action OnClickRollD8() => () => DoDiceRoll(8, DiceCount);
        public Action OnClickRollD10() => () => DoDiceRoll(10, DiceCount);
        public Action OnClickRollD20() => () => DoDiceRoll(20, DiceCount);
        public Action OnClickRollD100() => () => DoDiceRoll(100, DiceCount);

        private void DoDiceRoll(int sides, int number)
        {
            if (number < MinDiceCount)
                number = MinDiceCount;
            else if (number > MaxDiceCount)
                number = MaxDiceCount;

            var value = sides switch
            {
                2 => SWLOR.Game.Server.Service.Random.D2(number),
                4 => SWLOR.Game.Server.Service.Random.D4(number),
                6 => SWLOR.Game.Server.Service.Random.D6(number),
                8 => SWLOR.Game.Server.Service.Random.D8(number),
                10 => SWLOR.Game.Server.Service.Random.D10(number),
                20 => SWLOR.Game.Server.Service.Random.D20(number),
                100 => SWLOR.Game.Server.Service.Random.D100(number),
                _ => 0
            };

            var dieRoll = number + "d" + sides;
            var message = ColorToken.SkillCheck("Dice Roll: ") + dieRoll + ": " + value;

            if (!GetIsDM(Player))
            {
                var skillType = (SkillType)SelectedSkillId;
                if (skillType != SkillType.Invalid)
                {
                    var playerId = GetObjectUUID(Player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    if (!dbPlayer.Skills.ContainsKey(skillType))
                    {
                        AssignCommand(Player, () => SpeakString(message));
                        return;
                    }
                    var skillDetail = Skill.GetSkillDetails(skillType);
                    var totalSkill = dbPlayer.Skills[skillType].Rank;
                    var modifier = totalSkill / 2;

                    message = ColorToken.SkillCheck($"Dice Roll [{skillDetail.Name}]: ") +
                              dieRoll + "+" + modifier + ": " + (value + modifier);
                }
            }

            AssignCommand(Player, () => SpeakString(message));
        }
    }
}
