using System.Text.RegularExpressions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DistributeRPXPViewModel: GuiViewModelBase<DistributeRPXPViewModel, RPXPPayload>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private SkillType _skillType;
        private string _beastId;
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
            _beastId = initialPayload.BeastId;
            _maxDistributableXP = GetMaxDistributableXP();
            SkillName = initialPayload.SkillName;
            AvailableRPXP = $"Available RP XP: {initialPayload.MaxRPXP}";

            UpdateMaxDistributableInfo();

            WatchOnClient(model => model.Distribution);
        }

        public Action OnClickConfirm() => () =>
        {
            if (_maxDistributableXP <= 0)
            {
                var playerId = GetObjectUUID(Player);
                FloatingTextStringOnCreature(GetRefusalReason(DB.Get<Player>(playerId)), Player, false);
                return;
            }

            var target = IsBeastTarget ? SkillName : $"the {SkillName} skill";
            ShowModal($"Are you sure you want to distribute {Distribution} RP XP into {target}?",
                () =>
                {
                    if (GetResRef(GetArea(Player)) == "char_migration")
                    {
                        FloatingTextStringOnCreature($"XP cannot be distributed in this area.", Player, false);
                        return;
                    }

                    if (GetIsDead(Player) || GetCurrentHitPoints(Player) <= 0)
                    {
                        FloatingTextStringOnCreature($"XP cannot be distributed while dead.", Player, false);
                        return;
                    }

                    var playerId = GetObjectUUID(Player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (IsBeastTarget)
                    {
                        DistributeToBeast(dbPlayer);
                        return;
                    }

                    // Some skills are restricted by character type.
                    // Players shouldn't be able to see this pop-up but in case they get to it,
                    // prevent them from depositing XP into a skill they shouldn't have access to.
                    var skill = Skill.GetSkillDetails(_skillType);
                    if (!skill.IsAvailableToCharacterType(dbPlayer.CharacterType))
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

                    // Refuse a skill at its max rank or rank limit, or one the skill cap blocks,
                    // and never send more than the skill can use.
                    var maxDistributableXP = Skill.GetMaxDistributableXP(dbPlayer, _skillType);
                    if (maxDistributableXP <= 0)
                    {
                        FloatingTextStringOnCreature(GetRefusalReason(dbPlayer), Player, false);
                        return;
                    }

                    amount = Math.Min(amount, maxDistributableXP);

                    // Debit only the XP the award used, so a blocked award keeps the player's RP XP.
                    var acceptedXP = Skill.GiveSkillXP(Player, _skillType, amount, true, false);
                    if (acceptedXP > 0)
                    {
                        dbPlayer = DB.Get<Player>(playerId);
                        dbPlayer.UnallocatedXP -= Math.Min(acceptedXP, dbPlayer.UnallocatedXP);
                        DB.Set(dbPlayer);
                    }

                    Gui.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP);
                    Gui.PublishRefreshEvent(Player, new RPXPRefreshEvent());
                });
        };

        private bool IsBeastTarget => !string.IsNullOrWhiteSpace(_beastId);

        private void DistributeToBeast(Player dbPlayer)
        {
            if (!int.TryParse(Distribution, out var amount))
            {
                amount = 0;
            }

            // Value must be within range.
            if (amount <= 0 ||
                amount > dbPlayer.UnallocatedXP)
                return;

            // The beast only takes XP up to its level cap, so the player keeps the rest.
            var acceptedXP = BeastMastery.GiveBeastRPXP(Player, _beastId, amount);
            if (acceptedXP <= 0)
            {
                FloatingTextStringOnCreature(GetRefusalReason(dbPlayer), Player, false);
                return;
            }

            dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            dbPlayer.UnallocatedXP -= Math.Min(acceptedXP, dbPlayer.UnallocatedXP);
            DB.Set(dbPlayer);

            Gui.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP);
            Gui.PublishRefreshEvent(Player, new RPXPRefreshEvent());
        }

        private int GetMaxDistributableXP()
        {
            if (!IsBeastTarget)
                return Skill.GetMaxDistributableXP(Player, _skillType);

            var dbBeast = DB.Get<Beast>(_beastId);
            if (dbBeast == null || dbBeast.OwnerPlayerId != GetObjectUUID(Player))
                return 0;

            var levelCap = BeastMastery.GetBeastLevelCap(Perk.GetPerkLevel(Player, PerkType.Tame));
            return BeastMastery.GetMaxDistributableXP(dbBeast, levelCap);
        }

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP);
        };

        private string GetRefusalReason(Player dbPlayer)
        {
            if (IsBeastTarget)
                return GetBeastRefusalReason();

            var details = Skill.GetSkillDetails(_skillType);
            var pcSkill = dbPlayer.Skills[_skillType];
            if (pcSkill.Rank >= details.MaxRank)
                return "Skill at max rank - no XP accepted";

            var rankCeiling = Skill.GetRankCeiling(pcSkill, details);
            if (pcSkill.Rank >= rankCeiling)
                return $"At your limit of {rankCeiling} - no XP accepted";

            return "Skill cap reached - no XP accepted";
        }

        private string GetBeastRefusalReason()
        {
            var dbBeast = DB.Get<Beast>(_beastId);
            if (dbBeast == null || dbBeast.OwnerPlayerId != GetObjectUUID(Player))
                return "Beast not found - no XP accepted";

            if (dbBeast.Level >= BeastMastery.MaxLevel)
                return "Beast at max level - no XP accepted";

            var levelCap = BeastMastery.GetBeastLevelCap(Perk.GetPerkLevel(Player, PerkType.Tame));
            return $"Tame limit of {levelCap} - no XP accepted";
        }

        private void UpdateMaxDistributableInfo()
        {
            if (_maxDistributableXP == 0)
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                MaxDistributableInfo = GetRefusalReason(dbPlayer);
            }
            else
            {
                var maxUsable = Math.Min(_availableRPXP, _maxDistributableXP);
                var target = IsBeastTarget ? "beast" : "skill";
                MaxDistributableInfo = $"Max distributable to this {target}: {maxUsable} XP";
            }
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _availableRPXP = dbPlayer.UnallocatedXP;
            _maxDistributableXP = GetMaxDistributableXP();
            AvailableRPXP = $"Available RP XP: {dbPlayer.UnallocatedXP}";

            UpdateMaxDistributableInfo();
        }
    }
}
