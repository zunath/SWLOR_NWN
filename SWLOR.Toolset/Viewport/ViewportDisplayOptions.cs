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
                _showCeilings = settings?.ShowCeilings ?? false;
                _showMaterialMaps = settings?.ShowMaterialMaps ?? true;
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
        /// Draw an interior tileset's ceilings instead of looking into its rooms from above.
        /// </summary>
        /// <remarks>
        /// Off by default, matching Aurora: an interior seen from above is otherwise a field of blank
        /// ceiling slabs with the rooms sealed underneath them. Turning it on is for checking the
        /// ceiling itself. Exterior tilesets are unaffected - their overhead geometry is the treetops,
        /// which Aurora draws.
        /// </remarks>
        [ObservableProperty]
        private bool _showCeilings;

        /// <summary>
        /// Render normal/specular/roughness material maps on textured meshes.
        /// </summary>
        /// <remarks>
        /// On by default - it is what the game itself renders. One switch for all three map kinds
        /// rather than one each: NWN:EE treats them as a single material feature (the
        /// <c>NormalAndSpecMapped</c> renderhint), and the reason to turn them off - judging base
        /// diffuse artwork without relief and glint over it - applies to all of them at once.
        /// </remarks>
        [ObservableProperty]
        private bool _showMaterialMaps = true;

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

        partial void OnShowCeilingsChanged(bool value) => Persist();

        partial void OnShowMaterialMapsChanged(bool value) => Persist();

        private void Persist()
        {
            if (_loading || _settings == null)
                return;

            _settings.ShowAreaLighting = ShowAreaLighting;
            _settings.ShowFog = ShowFog;
            _settings.ShowCeilings = ShowCeilings;
            _settings.ShowMaterialMaps = ShowMaterialMaps;
        }
    }
}
