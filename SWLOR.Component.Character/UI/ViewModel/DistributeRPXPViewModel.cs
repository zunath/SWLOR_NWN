using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class DistributeRPXPViewModel: GuiViewModelBase<DistributeRPXPViewModel, RPXPPayload>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();

        public DistributeRPXPViewModel(IGuiService guiService, IDatabaseService db, IServiceProvider serviceProvider) : base(guiService)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
        }
        
        private SkillType _skillType;
        private int _availableRPXP;
        private int _maxDistributableXP;

        public string SkillName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AvailableRPXP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MaxDistributableInfo
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Distribution
        {
            get => Get<string>();
            set
            {
                var rpXP = Regex.Replace(value, "[^0-9]", string.Empty);
                rpXP = rpXP.TrimStart('0');

                // If nothing is entered, default to zero.
                if (rpXP.Length < 1)
                    rpXP = "0";

                // Ensure we can convert the number. If we can't, reduce it to zero.
                if (!int.TryParse(rpXP, out var result))
                {
                    rpXP = "0";
                }

                // Handle negative prices.
                if (result < 0)
                    rpXP = "0";

                // Handle max - limit to the minimum of available RPXP and max distributable XP to prevent loss
                var maxAllowed = Math.Min(_availableRPXP, _maxDistributableXP);
                if (result > maxAllowed)
                    rpXP = maxAllowed.ToString();

                Set(rpXP);
            }
        }

        protected override void Initialize(RPXPPayload initialPayload)
        {
            Distribution = "0";
            _availableRPXP = initialPayload.MaxRPXP;
            _skillType = initialPayload.Skill;
            _maxDistributableXP = SkillService.GetMaxDistributableXP(Player, _skillType);
            SkillName = initialPayload.SkillName;
            AvailableRPXP = $"Available RP XP: {initialPayload.MaxRPXP}";
            
            UpdateMaxDistributableInfo();

            WatchOnClient(model => model.Distribution);
        }

        public Action OnClickConfirm() => () =>
        {
            ShowModal($"Are you sure you want to distribute {Distribution} RP XP into the {SkillName} skill?",
                () =>
                {
                    if (GetResRef(GetArea(Player)) == "char_migration")
                    {
                        FloatingTextStringOnCreature($"XP cannot be distributed in this area.", Player, false);
                        return;
                    }

                    var playerId = GetObjectUUID(Player);
                    var dbPlayer = _db.Get<Player>(playerId);

                    // Some skills are restricted by character type.
                    // Players shouldn't be able to see this pop-up but in case they get to it,
                    // prevent them from depositing XP into a skill they shouldn't have access to.
                    var skill = SkillService.GetSkillDetails(_skillType);
                    if (skill.CharacterTypeRestriction != CharacterType.Invalid &&
                        skill.CharacterTypeRestriction != dbPlayer.CharacterType)
                    {
                        return;
                    }

                    if (!int.TryParse(Distribution, out var amount))
                    {
                        amount = 0;
                    }

                    // Value must be within range.
                    if (amount <= 0 ||
                        amount > dbPlayer.UnallocatedXP)
                        return;

                    dbPlayer.UnallocatedXP -= amount;
                    _db.Set(dbPlayer);

                    SkillService.GiveSkillXP(Player, _skillType, amount, true, false);

                    _guiService.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP);
                    _guiService.PublishRefreshEvent(Player, new RPXPRefreshEvent());
                });
        };

        public Action OnClickCancel() => () =>
        {
            _guiService.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP);
        };

        private void UpdateMaxDistributableInfo()
        {
            if (_maxDistributableXP == 0)
            {
                MaxDistributableInfo = "Skill at max rank - no XP accepted";
            }
            else
            {
                var maxUsable = Math.Min(_availableRPXP, _maxDistributableXP);
                MaxDistributableInfo = $"Max distributable to this skill: {maxUsable} XP";
            }
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);

            _availableRPXP = dbPlayer.UnallocatedXP;
            _maxDistributableXP = SkillService.GetMaxDistributableXP(Player, _skillType);
            AvailableRPXP = $"Available RP XP: {dbPlayer.UnallocatedXP}";
            
            UpdateMaxDistributableInfo();
        }
    }
}
