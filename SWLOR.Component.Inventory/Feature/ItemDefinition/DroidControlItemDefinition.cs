using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class DroidControlItemDefinition: IItemListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public DroidControlItemDefinition(
            IDatabaseService db, 
            IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IDroidService DroidService => _serviceProvider.GetRequiredService<IDroidService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IRecastService RecastService => _serviceProvider.GetRequiredService<IRecastService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            DroidControlUnit();

            return Builder.Build();
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
            Builder.Create("droid_control")
                .Delay(3f)
                .PlaysAnimation(AnimationType.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var droid = DroidService.GetDroid(user);
                    var droidDetails = DroidService.LoadDroidItemPropertyDetails(item);
                    if (SpaceService.IsPlayerInSpaceMode(user))
                    {
                        return "Droids cannot be activated or adjusted while in space.";
                    }

                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        var (onDelay, timeToWait) = RecastService.IsOnRecastDelay(user, RecastGroupType.DroidController);

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

                        var perkLevel = PerkService.GetPerkLevel(user, PerkType.DroidAssembly);

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
                        DroidService.SpawnDroid(user, item);
                        SetItemCursedFlag(item, true);

                    }
                    // Modify Appearance
                    else if (itemPropertyIndex == 1)
                    {
                        var droid = DroidService.GetDroid(user);
                        var payload = new AppearanceEditorPayload(droid);
                        GuiService.TogglePlayerWindow(user, GuiWindowType.AppearanceEditor, payload);
                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 2)
                    {
                        SetItemCursedFlag(item, true);
                        var payload = new DroidAIPayload(item);
                        GuiService.TogglePlayerWindow(user, GuiWindowType.DroidAI, payload);
                    }
                });
        }
    }
}
