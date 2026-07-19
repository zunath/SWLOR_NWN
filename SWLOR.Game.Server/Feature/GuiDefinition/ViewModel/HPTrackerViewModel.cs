using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HPTrackerViewModel : GuiViewModelBase<HPTrackerViewModel, GuiPayloadBase>,
        IGuiRefreshable<HPTrackerRefreshEvent>
    {
        // Parallel to the bound lists: row index -> tracked creature.
        private readonly List<uint> _creatures = new();

        // The creature this viewer has "located" by clicking its name: a looping glow applied only to this
        // player (others don't see it), or OBJECT_INVALID. Cleared when it leaves range or the window closes.
        private uint _highlightedCreature = OBJECT_INVALID;

        // The aura color currently applied to _highlightedCreature. Tracked so the glow can be recolored
        // (green -> yellow -> red) as the creature's HP changes, without re-applying it every refresh.
        private VisualEffect _highlightAura = VisualEffect.None;

        public string AddHPText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<float> HPProgresses
        {
            get => Get<GuiBindingList<float>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> HPColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> HPTexts
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
            AddHPText = "10";
            WatchOnClient(model => model.AddHPText);
            Rebuild();
        }

        public void Refresh(HPTrackerRefreshEvent payload)
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
                progresses.Add(GetProgress(current, max));
                colors.Add(GetBarColor(current, max));
                texts.Add($"{current}/{max}");
                canManage.Add(HPTrackerWindow.CanManage(Player, creature));
            }

            // Maintain the locate-glow: drop it if its creature left range (avoids a stuck glow); otherwise
            // recolor it to match the creature's current HP (green -> yellow -> red) when the color changes.
            if (_highlightedCreature != OBJECT_INVALID)
            {
                if (!_creatures.Contains(_highlightedCreature))
                {
                    ClearHighlight();
                }
                else
                {
                    var desired = AuraForCreature(_highlightedCreature);
                    if (desired != _highlightAura && GetIsObjectValid(_highlightedCreature))
                    {
                        PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _highlightedCreature, VisualEffect.None);
                        PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _highlightedCreature, desired);
                        _highlightAura = desired;
                    }
                }
            }

            Names = names;
            HPProgresses = progresses;
            HPColors = colors;
            HPTexts = texts;
            CanManage = canManage;
        }

        public Action OnClickAdd() => () =>
        {
            if (!int.TryParse(AddHPText, out var hp) || hp < 1)
            {
                SendMessageToPC(Player, ColorToken.Red("Enter a whole HP number of 1 or greater in the HP box first."));
                return;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Creature, "Click a creature to track its HP.", creature =>
            {
                if (!HPTrackerWindow.IsTrackableTarget(creature))
                {
                    SendMessageToPC(Player, ColorToken.Red("You can only track a non-DM creature."));
                    return;
                }

                if (!HPTrackerWindow.CanManage(Player, creature))
                {
                    SendMessageToPC(Player, ColorToken.Red("You can only track your own HP."));
                    return;
                }

                if (HPTracker.Has(creature))
                {
                    SendMessageToPC(Player, ColorToken.Red($"{GetName(creature)} is already being tracked."));
                    return;
                }

                HPTracker.Set(creature, hp, hp);
                HPTrackerWindow.RefreshOpenWindows();

                if (HPTrackerWindow.IsStaff(Player))
                    Log.Write(LogGroup.DM, $"HP tracker set (window): Actor={GetName(Player)} ({GetPCPublicCDKey(Player)}), Target={GetName(creature)}, After={hp}/{hp}");
            });
        };

        /// <summary>
        /// Clicking a row's name "locates" that creature: a glow is applied to it that only this viewer can
        /// see (a per-player looping VFX). Clicking the same name again clears it; clicking another name
        /// moves the glow. Locating is a view action, so it needs no manage permission.
        /// </summary>
        public Action OnClickName() => () =>
        {
            var creature = CreatureAtEventRow();
            if (creature == OBJECT_INVALID)
                return;

            if (creature == _highlightedCreature)
            {
                ClearHighlight();
                return;
            }

            ClearHighlight();
            ApplyHighlight(creature);
        };

        public Action OnClickIncrease() => () => AdjustAtRow(1);

        public Action OnClickDecrease() => () => AdjustAtRow(-1);

        public Action OnClickRemove() => () =>
        {
            var creature = CreatureAtEventRow();
            if (creature == OBJECT_INVALID || !HPTrackerWindow.CanManage(Player, creature))
                return;

            var before = HPTracker.Has(creature) ? HPTracker.Get(creature) : (Current: 0, Max: 0);
            HPTracker.Remove(creature);
            HPTrackerWindow.RefreshOpenWindows();

            if (HPTrackerWindow.IsStaff(Player))
                Log.Write(LogGroup.DM, $"HP tracker removed (window): Actor={GetName(Player)} ({GetPCPublicCDKey(Player)}), Target={GetName(creature)}, Before={before.Current}/{before.Max}");
        };

        private void AdjustAtRow(int delta)
        {
            var creature = CreatureAtEventRow();
            if (creature == OBJECT_INVALID || !HPTrackerWindow.CanManage(Player, creature))
                return;

            var before = HPTracker.Has(creature) ? HPTracker.Get(creature) : (Current: 0, Max: 0);
            HPTracker.Adjust(creature, delta);
            HPTrackerWindow.RefreshOpenWindows();

            if (HPTrackerWindow.IsStaff(Player))
            {
                var after = HPTracker.Has(creature) ? HPTracker.Get(creature) : (Current: 0, Max: 0);
                Log.Write(LogGroup.DM, $"HP tracker adjusted (window): Actor={GetName(Player)} ({GetPCPublicCDKey(Player)}), Target={GetName(creature)}, Delta={delta}, Before={before.Current}/{before.Max}, After={after.Current}/{after.Max}");
            }
        }

        private uint CreatureAtEventRow()
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _creatures.Count)
                return OBJECT_INVALID;

            return _creatures[index];
        }

        /// <summary>Bar fill 0..1 for the given current/max.</summary>
        private static float GetProgress(int current, int max)
        {
            if (max <= 0) return 0f;
            var ratio = (float)current / max;
            if (ratio < 0f) return 0f;
            if (ratio > 1f) return 1f;
            return ratio;
        }

        /// <summary>Bar color, green (full) -> yellow (half) -> red (empty) by ratio.</summary>
        private static GuiColor GetBarColor(int current, int max)
        {
            var ratio = GetProgress(current, max);

            byte r, g;
            if (ratio >= 0.5f)
            {
                r = (byte)(255 * (1f - ratio) * 2f); // 0 at full, 255 at half
                g = 255;
            }
            else
            {
                r = 255;
                g = (byte)(255 * ratio * 2f);        // 255 at half, 0 at empty
            }

            return new GuiColor(r, g, 0);
        }

        /// <summary>
        /// Applies this viewer's locate-glow to a creature, colored by its tracked HP (green -> yellow ->
        /// red), and records it so <see cref="Rebuild"/> can recolor or clear it later.
        /// </summary>
        private void ApplyHighlight(uint creature)
        {
            // The clicked creature could have been destroyed between the last Rebuild() and this click;
            // guard the VFX call just as ClearHighlight() does. Leaving _highlightedCreature at
            // OBJECT_INVALID (set by the preceding ClearHighlight() in OnClickName) keeps the state clean.
            if (!GetIsObjectValid(creature))
                return;

            var aura = AuraForCreature(creature);
            PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, creature, aura);
            _highlightedCreature = creature;
            _highlightAura = aura;
        }

        /// <summary>The aura matching a tracked creature's current HP: green (high) -> yellow -> red (low, incl. 0).</summary>
        private static VisualEffect AuraForCreature(uint creature)
        {
            if (!HPTracker.Has(creature))
                return VisualEffect.Vfx_Dur_Aura_Green;

            var (current, max) = HPTracker.Get(creature);
            var ratio = GetProgress(current, max);

            if (ratio >= 0.66f)
                return VisualEffect.Vfx_Dur_Aura_Green;
            if (ratio >= 0.33f)
                return VisualEffect.Vfx_Dur_Aura_Yellow;
            return VisualEffect.Vfx_Dur_Aura_Red;
        }

        /// <summary>Removes this viewer's locate-glow, if any. Passing VisualEffect.None clears it per-player.</summary>
        private void ClearHighlight()
        {
            if (_highlightedCreature == OBJECT_INVALID)
                return;

            if (GetIsObjectValid(_highlightedCreature))
                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _highlightedCreature, VisualEffect.None);

            _highlightedCreature = OBJECT_INVALID;
            _highlightAura = VisualEffect.None;
        }

        /// <summary>Clear the locate-glow so it never outlives the window.</summary>
        public override Action OnWindowClosed() => () =>
        {
            ClearHighlight();
        };
    }
}
