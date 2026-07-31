using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>AI profile and bit flags with fixed runtime-behavior statements.</summary>
    public sealed partial class CreatureAiViewModel : ObservableObject
    {
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private bool _loading;

        public IReadOnlyList<string> Profiles { get; } = Enum.GetValues<AIProfileType>()
            .Where(profile => profile != AIProfileType.Invalid)
            .Select(profile => profile.ToString())
            .ToList();

        [ObservableProperty]
        private string? _profile;

        [ObservableProperty]
        private bool _randomWalk;

        [ObservableProperty]
        private bool _returnHome;

        public CreatureAiViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit)
        {
            _store = store;
            _runEdit = runEdit;
            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                var stored = _store.Locals.GetString(NPCAI.ProfileLocalVariable);
                var profile = Enum.TryParse(stored, true, out AIProfileType namedProfile) &&
                              namedProfile != AIProfileType.Invalid
                    ? namedProfile
                    : _store.Locals.GetInt(NPCAI.ProfileIdLocalVariable) is { } profileId &&
                      profileId > 0 && Enum.IsDefined(typeof(AIProfileType), profileId)
                        ? (AIProfileType)profileId
                        : AIProfileType.Generic;
                Profile = profile.ToString();
                var flags = _store.Locals.GetInt("AI_FLAGS") ?? 0;
                RandomWalk = (flags & (int)AIFlag.RandomWalk) != 0;
                ReturnHome = (flags & (int)AIFlag.ReturnHome) != 0;
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnProfileChanged(string? value)
        {
            if (_loading || string.IsNullOrWhiteSpace(value))
                return;
            if (!_runEdit("Change AI profile", () =>
                {
                    if (value == AIProfileType.Generic.ToString())
                    {
                        _store.Locals.Remove(NPCAI.ProfileLocalVariable);
                        _store.Locals.Remove(NPCAI.ProfileIdLocalVariable);
                    }
                    else
                    {
                        var profile = Enum.Parse<AIProfileType>(value);
                        _store.Locals.SetString(NPCAI.ProfileLocalVariable, profile.ToString());
                        _store.Locals.SetInt(NPCAI.ProfileIdLocalVariable, (int)profile);
                    }
                }))
            {
                Reload();
            }
        }

        partial void OnRandomWalkChanged(bool value) => WriteFlag();

        partial void OnReturnHomeChanged(bool value) => WriteFlag();

        private void WriteFlag()
        {
            if (_loading)
                return;
            if (!_runEdit("Change AI behavior", () =>
                {
                    var flags = (RandomWalk ? (int)AIFlag.RandomWalk : 0) |
                                (ReturnHome ? (int)AIFlag.ReturnHome : 0);
                    if (flags == 0)
                        _store.Locals.Remove("AI_FLAGS");
                    else
                        _store.Locals.SetInt("AI_FLAGS", flags);
                }))
            {
                Reload();
            }
        }
    }
}
