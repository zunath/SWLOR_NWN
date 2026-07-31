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
                var stored = _store.Locals.GetString("AI_PROFILE");
                Profile = Profiles.FirstOrDefault(candidate => candidate == stored) ?? AIProfileType.Generic.ToString();
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
                        _store.Locals.Remove("AI_PROFILE");
                    else
                        _store.Locals.SetString("AI_PROFILE", value);
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
