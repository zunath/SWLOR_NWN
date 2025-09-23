using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.UI.Payload;
using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Model;
using SWLOR.Component.Inventory.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model.Payload;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class DroidControlItemDefinition: IItemListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IDroid _droidService;
        private readonly ISpaceService _spaceService;
        private readonly IRecastService _recastService;
        private readonly IPerkService _perkService;
        private readonly IGuiService _guiService;
        private readonly ItemBuilder _builder = new();

        public DroidControlItemDefinition(
            IDatabaseService db, 
            IDroid droidService, 
            ISpaceService spaceService, 
            IRecastService recastService, 
            IPerkService perkService, 
            IGuiService guiService)
        {
            _db = db;
            _droidService = droidService;
            _spaceService = spaceService;
            _recastService = recastService;
            _perkService = perkService;
            _guiService = guiService;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            DroidControlUnit();

            return _builder.Build();
        }

        private static readonly SkillType[] _skillsUsedForAverages =
        {
            SkillType.OneHanded,
            SkillType.TwoHanded,
            SkillType.MartialArts,
            SkillType.Ranged,
            SkillType.Force,
            SkillType.Devices,
            SkillType.FirstAid
        };

        private int GetAverageSkillLevel(uint player)
        {
            if (!GetIsPC(player))
                return 50;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var skillLevel = 0;
            foreach (var skill in _skillsUsedForAverages)
            {
                var rank = dbPlayer.Skills[skill].Rank;
                if (rank > skillLevel)
                    skillLevel = rank;
            }

            skillLevel += dbPlayer.Skills[SkillType.Armor].Rank;
            
            return (int)(skillLevel / 2f);
        }

        private void DroidControlUnit()
        {
            _builder.Create("droid_control")
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var droid = _droidService.GetDroid(user);
                    var droidDetails = _droidService.LoadDroidItemPropertyDetails(item);
                    if (_spaceService.IsPlayerInSpaceMode(user))
                    {
                        return "Droids cannot be activated or adjusted while in space.";
                    }

                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        var (onDelay, timeToWait) = _recastService.IsOnRecastDelay(user, RecastGroup.DroidController);

                        if (onDelay)
                        {
                            return $"This item can be used in {timeToWait}.";
                        }

                        if (GetIsObjectValid(droid))
                        {
                            return "Only one droid may be activated at a time.";
                        }

                        var averageLevel = GetAverageSkillLevel(user);
                        var requiredLevel = droidDetails.Tier * 10 - 10;

                        if (averageLevel < requiredLevel)
                        {
                            return $"Average combat level requirement not met. (Required: {requiredLevel}, Your Average: {averageLevel})";
                        }
                    }
                    // Modify Appearance
                    else if (itemPropertyIndex == 1)
                    {
                        if (!GetIsObjectValid(droid))
                        {
                            return "Your droid must be active in order to change its appearance.";
                        }
                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 2)
                    {
                        if (GetIsObjectValid(droid))
                        {
                            return "Droid AI cannot be adjusted while active. Please dismiss your droid and try again.";
                        }

                        var perkLevel = _perkService.GetPerkLevel(user, PerkType.DroidAssembly);

                        if (perkLevel < droidDetails.Tier)
                        {
                            return $"Your Droid Assembly perk is too low to configure this droid's AI. (Required: {droidDetails.Tier})";
                        }
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        _droidService.SpawnDroid(user, item);
                        SetItemCursedFlag(item, true);

                    }
                    // Modify Appearance
                    else if (itemPropertyIndex == 1)
                    {
                        var droid = _droidService.GetDroid(user);
                        var payload = new AppearanceEditorPayload(droid);
                        _guiService.TogglePlayerWindow(user, GuiWindowType.AppearanceEditor, payload);
                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 2)
                    {
                        SetItemCursedFlag(item, true);
                        var payload = new DroidAIPayload(item);
                        _guiService.TogglePlayerWindow(user, GuiWindowType.DroidAI, payload);
                    }
                });
        }
    }
}
