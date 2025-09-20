using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DroidControlItemDefinition: IItemListDefinition
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly ItemBuilder _builder = new();

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
                    var droid = Droid.GetDroid(user);
                    var droidDetails = Droid.LoadDroidItemPropertyDetails(item);
                    if (Space.IsPlayerInSpaceMode(user))
                    {
                        return "Droids cannot be activated or adjusted while in space.";
                    }

                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        var (onDelay, timeToWait) = Recast.IsOnRecastDelay(user, RecastGroup.DroidController);

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

                        var perkLevel = Perk.GetPerkLevel(user, PerkType.DroidAssembly);

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
                        Droid.SpawnDroid(user, item);
                        SetItemCursedFlag(item, true);

                    }
                    // Modify Appearance
                    else if (itemPropertyIndex == 1)
                    {
                        var droid = Droid.GetDroid(user);
                        var payload = new AppearanceEditorPayload(droid);
                        Gui.TogglePlayerWindow(user, GuiWindowType.AppearanceEditor, payload);
                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 2)
                    {
                        SetItemCursedFlag(item, true);
                        var payload = new DroidAIPayload(item);
                        Gui.TogglePlayerWindow(user, GuiWindowType.DroidAI, payload);
                    }
                });
        }
    }
}
