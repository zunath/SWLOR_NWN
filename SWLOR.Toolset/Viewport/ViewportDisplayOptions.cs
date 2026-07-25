using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// The view-only display switches the quick-access bar owns, shared by every open area viewport.
    /// </summary>
    /// <remarks>
    /// Global rather than per-area, which is how Aurora treats them: they say how the builder wants to
    /// look at the module, not anything about a particular area, and having two open areas disagree
    /// about whether fog is on would only be confusing. Persisted, so the choice survives a restart.
    /// </remarks>
    public sealed partial class ViewportDisplayOptions : ObservableObject
    {
        private readonly ToolsetSettings? _settings;
        private readonly bool _loading;

        public ViewportDisplayOptions(ToolsetSettings? settings = null)
        {
            _settings = settings;

            _loading = true;
            try
            {
                _showAreaLighting = settings?.ShowAreaLighting ?? false;
                _showFog = settings?.ShowFog ?? false;
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// Light the scene with the area's own sun/moon colours rather than a neutral editor light.
        /// </summary>
        /// <remarks>
        /// Off by default, matching Aurora. A night area's authored light is close to black - one of
        /// this module's is ambient RGB(45,45,45) - and rendering through it buries the textures under
        /// a colour cast that makes them impossible to judge.
        /// </remarks>
        [ObservableProperty]
        private bool _showAreaLighting;

        /// <summary>Apply the area's distance fog. Off by default: fog hides the far geometry a builder is placing.</summary>
        [ObservableProperty]
        private bool _showFog;

        /// <summary>
        /// Reserved for the shadow pass, which the viewport renderer does not have yet. The bar shows
        /// the control disabled rather than omitting it, so it is clear the switch is missing rather
        /// than hidden.
        /// </summary>
        public bool ShowShadows => false;

        /// <summary>True once the renderer grows a shadow pass; the bar's button enables from this.</summary>
        public bool CanShowShadows => false;

        partial void OnShowAreaLightingChanged(bool value) => Persist();

        partial void OnShowFogChanged(bool value) => Persist();

        private void Persist()
        {
            if (_loading || _settings == null)
                return;

            _settings.ShowAreaLighting = ShowAreaLighting;
            _settings.ShowFog = ShowFog;
        }
    }
}
