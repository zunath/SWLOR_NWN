using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Conversations;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// What the pretend player has that is not a quest — a key item they carry, a skill rank, faction
    /// standing, or having finished the tutorial.
    /// </summary>
    /// <remarks>
    /// Built from what the conversation actually reads rather than from the whole game: an NPC that
    /// checks one key item gets one control, and one that checks none gets no row at all. A guard
    /// with no control behind it is a situation the writer cannot reach, which is why these exist at
    /// all — quests alone left key-item and skill conversations unnavigable.
    /// </remarks>
    public sealed partial class PlayerFactPillViewModel : ObservableObject
    {
        private readonly Action _onChanged;

        [ObservableProperty]
        private bool _isOn;

        [ObservableProperty]
        private int _amount;

        public PlayerFactPillViewModel(
            PlayerFactKind kind,
            string key,
            string label,
            Action onChanged,
            int amount = 0)
        {
            Kind = kind;
            Key = key;
            Label = label;
            _amount = amount;
            _onChanged = onChanged;
        }

        public PlayerFactKind Kind { get; }

        /// <summary>The stored value this fact is about: a key item name, a skill name, a faction id.</summary>
        public string Key { get; }

        public string Label { get; }

        /// <summary>True for facts that are simply had or not had — a key item, the tutorial.</summary>
        public bool IsToggle => Kind is PlayerFactKind.KeyItem or PlayerFactKind.Tutorial;

        /// <summary>True for facts that carry a number — a skill rank, faction standing or points.</summary>
        public bool IsAmount => !IsToggle;

        /// <summary>Applies this fact to the player being built.</summary>
        public void ApplyTo(PretendPlayer player)
        {
            switch (Kind)
            {
                case PlayerFactKind.KeyItem when IsOn:
                    player.WithKeyItem(Key);
                    break;

                case PlayerFactKind.Tutorial when IsOn:
                    player.WithTutorialCompleted();
                    break;

                case PlayerFactKind.Skill:
                    player.WithSkill(Key, Amount);
                    break;

                case PlayerFactKind.FactionStanding when int.TryParse(Key, out var standingFaction):
                    player.WithFactionStanding(standingFaction, Amount);
                    break;

                case PlayerFactKind.FactionPoints when int.TryParse(Key, out var pointsFaction):
                    player.WithFactionPoints(pointsFaction, Amount);
                    break;
            }
        }

        /// <summary>Reads this fact back off a player, after a walk moved it.</summary>
        public void ReadFrom(PretendPlayer player)
        {
            switch (Kind)
            {
                case PlayerFactKind.KeyItem:
                    IsOn = player.HasKeyItem(Key);
                    break;

                case PlayerFactKind.Tutorial:
                    IsOn = player.HasCompletedTutorial;
                    break;

                case PlayerFactKind.Skill:
                    Amount = player.GetSkillRank(Key);
                    break;

                case PlayerFactKind.FactionStanding when int.TryParse(Key, out var standingFaction):
                    Amount = player.GetFactionStanding(standingFaction);
                    break;

                case PlayerFactKind.FactionPoints when int.TryParse(Key, out var pointsFaction):
                    Amount = player.GetFactionPoints(pointsFaction);
                    break;
            }
        }

        partial void OnIsOnChanged(bool value) => _onChanged();

        partial void OnAmountChanged(int value) => _onChanged();
    }

    /// <summary>The kinds of non-quest fact a conversation can read about a player.</summary>
    public enum PlayerFactKind
    {
        KeyItem,
        Skill,
        FactionStanding,
        FactionPoints,
        Tutorial
    }
}
