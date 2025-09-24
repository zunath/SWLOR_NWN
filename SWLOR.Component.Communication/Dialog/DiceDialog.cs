using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.Dialog
{
    public class DiceDialog : DialogBase
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly ISkillService _skillService;

        public DiceDialog(IDatabaseService db, IRandomService random, ISkillService skillService, IDialogService dialogService) 
            : base(dialogService)
        {
            _db = db;
            _random = random;
            _skillService = skillService;
        }
        
        private enum DiceGroup
        {
            None = 0,
        }

        private class Model
        {
            private int _diceCount;
            public int DiceCount
            {
                get => _diceCount;
                set
                {
                    _diceCount = value;

                    if (_diceCount > 10)
                        _diceCount = 10;
                    else if (_diceCount < 1)
                        _diceCount = 1;
                }
            }

            public DiceGroup Group { get; set; }
            public SkillType Skill { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string GroupPageId = "GROUP_PAGE";
        private const string SkillPageId = "SKILL_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(GroupPageId, GroupPageInit)
                .AddPage(SkillPageId, SkillPageInit);

            return builder.Build();
        }

        private string BuildHeader()
        {
            var model = GetDataModel<Model>();
            var header = ColorToken.Green("Number of Dice: ") + model.DiceCount + "\n";
            header += ColorToken.Green("Skill: ");

            if (model.Skill == SkillType.Invalid)
                header += "None\n";
            else
                header += _skillService.GetSkillDetails(model.Skill).Name + "\n";

            header += ColorToken.Green("Group: ");

            switch (model.Group)
            {
                case DiceGroup.None:
                    header += "None\n";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return header;
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            page.Header = BuildHeader();

            page.AddResponse(ColorToken.Green("Add Die"), () =>
            {
                model.DiceCount++;
            });

            page.AddResponse(ColorToken.Green("Remove Die"), () =>
            {
                model.DiceCount--;
            });

            if (!GetIsDM(player))
            {
                page.AddResponse("Skills", () =>
                {
                    ChangePage(SkillPageId);
                });
            }

            page.AddResponse("d2", () =>
            {
                DoDiceRoll(2, model.DiceCount);
            });

            page.AddResponse("d4", () =>
            {
                DoDiceRoll(4, model.DiceCount);
            });

            page.AddResponse("d6", () =>
            {
                DoDiceRoll(6, model.DiceCount);
            });

            page.AddResponse("d8", () =>
            {
                DoDiceRoll(8, model.DiceCount);
            });

            page.AddResponse("d10", () =>
            {
                DoDiceRoll(10, model.DiceCount);
            });

            page.AddResponse("d20", () =>
            {
                DoDiceRoll(20, model.DiceCount);
            });

            page.AddResponse("d100", () =>
            {
                DoDiceRoll(100, model.DiceCount);
            });

        }

        private void GroupPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = BuildHeader();
        }

        private void SkillPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = BuildHeader();

            foreach (var (skill, detail) in _skillService.GetAllActiveSkills())
            {
                page.AddResponse(detail.Name, () =>
                {
                    model.Skill = skill;
                    model.Group = DiceGroup.None;
                });
            }
        }


        private void DoDiceRoll(int sides, int number)
        {
            var user = GetPC();
            var model = GetDataModel<Model>();
            int value;

            if (number < 1)
                number = 1;
            else if (number > 10)
                number = 10;

            switch (sides)
            {
                case 2:
                    value = _random.D2(number);
                    break;
                case 4:
                    value = _random.D4(number);
                    break;
                case 6:
                    value = _random.D6(number);
                    break;
                case 8:
                    value = _random.D8(number);
                    break;
                case 10:
                    value = _random.D10(number);
                    break;
                case 20:
                    value = _random.D20(number);
                    break;
                case 100:
                    value = _random.D100(number);
                    break;
                default:
                    value = 0;
                    break;
            }

            var dieRoll = number + "d" + sides;
            var message = ColorToken.SkillCheck("Dice Roll: ") + dieRoll + ": " + value;

            if (!GetIsDM(user))
            {
                var playerId = GetObjectUUID(user);
                var dbPlayer = _db.Get<Player>(playerId);

                // Skills
                 if (model.Skill != SkillType.Invalid)
                {
                    var skillDetail = _skillService.GetSkillDetails(model.Skill);
                    var totalSkill = dbPlayer.Skills[model.Skill].Rank;
                    var modifier = totalSkill / 2;
                    message = ColorToken.SkillCheck($"Dice Roll [{skillDetail.Name}]: ") + dieRoll + "+" + modifier + ": " + (value + modifier);
                }
            }

            AssignCommand(user, () => SpeakString(message));
        }

    }
}
