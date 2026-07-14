using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HpTrackerViewModel : GuiViewModelBase<HpTrackerViewModel, GuiPayloadBase>,
        IGuiRefreshable<HpTrackerRefreshEvent>
    {
        // Parallel to the bound lists: row index -> tracked creature.
        private readonly List<uint> _creatures = new();

        public string AddHpText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<float> HpProgresses
        {
            get => Get<GuiBindingList<float>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> HpColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> HpTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CanManage
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            AddHpText = "10";
            WatchOnClient(model => model.AddHpText);
            Rebuild();
        }

        public void Refresh(HpTrackerRefreshEvent payload)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            var creatures = HPTracker.GetTrackedInArea(Player);

            _creatures.Clear();
            var names = new GuiBindingList<string>();
            var progresses = new GuiBindingList<float>();
            var colors = new GuiBindingList<GuiColor>();
            var texts = new GuiBindingList<string>();
            var canManage = new GuiBindingList<bool>();

            foreach (var creature in creatures)
            {
                var (current, max) = HPTracker.Get(creature);

                _creatures.Add(creature);
                names.Add(GetName(creature));
                progresses.Add(HPTracker.GetProgress(current, max));
                colors.Add(HPTracker.GetBarColor(current, max));
                texts.Add($"{current}/{max}");
                canManage.Add(HpTrackerWindow.CanManage(Player, creature));
            }

            Names = names;
            HpProgresses = progresses;
            HpColors = colors;
            HpTexts = texts;
            CanManage = canManage;
        }

        public Action OnClickAdd() => () =>
        {
            if (!int.TryParse(AddHpText, out var hp) || hp < 1)
            {
                SendMessageToPC(Player, ColorToken.Red("Enter a whole HP number of 1 or greater in the HP box first."));
                return;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Creature, "Click a creature to track its HP.", creature =>
            {
                if (!HpTrackerWindow.IsTrackableTarget(creature))
                {
                    SendMessageToPC(Player, ColorToken.Red("You can only track a non-DM creature."));
                    return;
                }

                if (!HpTrackerWindow.CanManage(Player, creature))
                {
                    SendMessageToPC(Player, ColorToken.Red("You can only track your own HP."));
                    return;
                }

                HPTracker.Set(creature, hp, hp);
                HpTrackerWindow.RefreshOpenWindows();
            });
        };

        public Action OnClickIncrease() => () => AdjustAtRow(1);

        public Action OnClickDecrease() => () => AdjustAtRow(-1);

        public Action OnClickRemove() => () =>
        {
            var creature = CreatureAtEventRow();
            if (creature == OBJECT_INVALID || !HpTrackerWindow.CanManage(Player, creature))
                return;

            HPTracker.Remove(creature);
            HpTrackerWindow.RefreshOpenWindows();
        };

        private void AdjustAtRow(int delta)
        {
            var creature = CreatureAtEventRow();
            if (creature == OBJECT_INVALID || !HpTrackerWindow.CanManage(Player, creature))
                return;

            HPTracker.Adjust(creature, delta);
            HpTrackerWindow.RefreshOpenWindows();
        }

        private uint CreatureAtEventRow()
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _creatures.Count)
                return OBJECT_INVALID;

            return _creatures[index];
        }
    }
}
